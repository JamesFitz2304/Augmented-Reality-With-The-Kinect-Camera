using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Media;
using System.Net.Mail;


namespace KinectImage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {


        private KinectSensor sensor;
        private WriteableBitmap colorBitmap;
        private byte[] colorPixels;


        public Gesture gestureP1 = new Gesture(); //gesture object controls player gestures
        public SelectionMenu menuP1; //SelectionMenu object controls on screen prop selection buttons
        int currentHatP1 = 0; //determines current hat selected by player - hats represented by number
        int currentPropP1 = 0;

        public Gesture gestureP2 = new Gesture();
        public SelectionMenu menuP2;
        int currentHatP2 = 0;
        int currentPropP2 = 0;

        float skeleton1X = 0, skeleton2X = 0;
        bool wrongPlaces = false;

        private int numberOfPlayers = 1;
        private int trackedPlayers = 0;

        //prop image location URIs
        Uri uriTopHat = new Uri("Items/topHat.gif", UriKind.Relative);
        Uri uriFedora = new Uri("Items/fedora.gif", UriKind.Relative);
        Uri uriFedora2 = new Uri("Items/fedoraFlipped.gif", UriKind.Relative);
        Uri uriSombrero = new Uri("Items/sombrero.gif", UriKind.Relative);

        Uri uriUmbrellaClosed = new Uri("Items/umbrellaClosed.gif", UriKind.Relative);
        Uri uriUmbrellaOpen = new Uri("Items/umbrellaOpen.gif", UriKind.Relative);
        Uri uriCane = new Uri("Items/cane.png", UriKind.Relative);
        Uri uriSword = new Uri("Items/sword.png", UriKind.Relative);

        Uri uriUmbrellaClosed2 = new Uri("Items/umbrellaClosedFlipped.gif", UriKind.Relative);
        Uri uriUmbrellaOpen2 = new Uri("Items/umbrellaOpenFlipped.gif", UriKind.Relative);
        Uri uriCane2 = new Uri("Items/caneFlipped.png", UriKind.Relative);
        Uri uriSword2 = new Uri("Items/swordFlipped.png", UriKind.Relative);

        int p1HatHeight = 0, p1HatWidth = 0, p1PropHeight = 0, p1PropWidth = 0;
        int p2HatHeight = 0, p2HatWidth = 0, p2PropHeight = 0, p2PropWidth = 0;

        bool player1 = true; //to determine whether current player is player 1 or 2

        public Debug debugWindow = new Debug(); //new window which shows variable value info etc
        
        public MainWindow()
        {
            
            InitializeComponent();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (this.sensor != null)
            {
                // Turn on the color stream to receive color frames
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);

                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
                // this.depthPixels = new short[this.sensor.DepthStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                colorBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                Image.Source = colorBitmap;

                // Add an event handler to be called whenever there is new color frame data
                sensor.ColorFrameReady += SensorColorFrameReady;



                // Turn on the skeleton stream to receive skeleton frames
                sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                sensor.SkeletonFrameReady += SensorSkeletonFrameReady;

                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                // Starts the sensor
                try
                {
                    sensor.Start();
                }
                catch (IOException)
                {
                    sensor = null;
                }

                //initialises the SelectionMenu objects now that the kinect sensor is started and the button controls are active
                menuP1 = new SelectionMenu(sensor, btnHatP1, btnPropP1);

                menuP2 = new SelectionMenu(sensor, btnHatP2, btnPropP2);

                txtNoKinect.Visibility = Visibility.Hidden;
                btnRetry.Visibility = Visibility.Hidden;
               


            }

            if (sensor == null)
            {
                txtNoKinect.Visibility = Visibility.Visible;
                btnRetry.Visibility = Visibility.Visible;

            }

        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];
            trackedPlayers = 0;

            if (numberOfPlayers == 1)
            {
                btnHatP2.Visibility = Visibility.Hidden;
                btnPropP2.Visibility = Visibility.Hidden;
                headwearPreviewP2.Visibility = Visibility.Hidden;
                propPreviewP2.Visibility = Visibility.Hidden;
                player1 = true;
            }
            else if(numberOfPlayers == 2)
            {

                btnHatP2.Visibility = Visibility.Visible;
                btnPropP2.Visibility = Visibility.Visible;
                headwearPreviewP2.Visibility = Visibility.Visible;
                propPreviewP2.Visibility = Visibility.Visible;

            }

            if (numberOfPlayers == 2)
            {
                if (wrongPlaces)
                {
                    if (player1 == true)
                    {
                        player1 = false;
                    }
                    else if (player1 == false)
                    {
                        player1 = true;
                    }
                    wrongPlaces = false;
                }
            }

            using ( SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {

                        if (trackedPlayers == 1 && numberOfPlayers == 1)
                        {
                            break;
                        }

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            trackedPlayers++;
                            debugWindow.txtTrackedPlayers.Text = Convert.ToString(trackedPlayers);
                            if (player1)
                            {
                                checkIfButtonPressed(skel, menuP1, btnHatP1, btnPropP1);
                                checkForAGesture(skel, gestureP1);

                                //debug
                                debugWindow.txtDepthValue.Text = Convert.ToString(menuP1.depthValue);
                                debugWindow.txtDepthDifference.Text = Convert.ToString(menuP1.depthValue - sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skel.Joints[JointType.HandLeft].Position, DepthImageFormat.Resolution640x480Fps30).Depth);
                                //debug

                                if (currentHatP1 != 0)
                                {
                                    DrawHat(skel, gestureP1);
                                }

                                if (currentPropP1 != 0)
                                {
                                    DrawHandProp(skel);
                                }

                                if (!(gestureP1.topHatOnHead != true && currentHatP1 == 2))
                                {
                                    checkHeadRotation(skel);
                                }

                                if(showPlayers.IsChecked == true)
                                {
                                    DrawPlayerPointer(skel);
                                }
                                else
                                {
                                    player1Pointer.Visibility = Visibility.Hidden;
                                }

                             
                                skeleton1X = skel.Position.X;
                               

                                player1 = false;


                                //DEBUG PURPOSES
                                //<------------------------------------------------------------------------------------->
                                double yUpper = skel.Joints[JointType.Head].Position.Y - (skel.Joints[JointType.Head].Position.Y * 0.2);
                                double yLower = skel.Joints[JointType.Head].Position.Y + (skel.Joints[JointType.Head].Position.Y * 0.2);

                                double xUpper = skel.Joints[JointType.Head].Position.X - (skel.Joints[JointType.Head].Position.X * 0.2);
                                double xLower = skel.Joints[JointType.Head].Position.X + (skel.Joints[JointType.Head].Position.X * 0.2);

                                debugWindow.txtSkeleX.Text = Convert.ToString(skel.Joints[JointType.Head].Position.X);
                                debugWindow.txtSkeleY.Text = Convert.ToString(skel.Joints[JointType.Head].Position.Y);
                                debugWindow.handX.Text = Convert.ToString(skel.Joints[JointType.HandRight].Position.X);
                                debugWindow.handY.Text = Convert.ToString(skel.Joints[JointType.HandRight].Position.Y);

                                debugWindow.txtCountdown.Text = Convert.ToString(gestureP1.countdown);

                                double HHXDifference = Math.Abs(skel.Joints[JointType.Head].Position.X - skel.Joints[JointType.HandRight].Position.X);
                                double HHYDifference = Math.Abs(skel.Joints[JointType.Head].Position.Y - skel.Joints[JointType.HandRight].Position.Y);

                                debugWindow.txtHHX.Text = Convert.ToString(HHXDifference);
                                debugWindow.txtHHY.Text = Convert.ToString(HHYDifference);

                                debugWindow.txtHeadHandDifferenceY.Text = Convert.ToString(skel.Joints[JointType.HandLeft].Position.Y - skel.Joints[JointType.Head].Position.Y);

                                double handYDifference = Math.Abs(gestureP1.partOneHandYPosition - skel.Joints[JointType.HandRight].Position.Y);
                                if (gestureP1.partOneComplete)
                                {
                                    debugWindow.checkOne.IsChecked = true;
                                }

                                if (gestureP1.partTwoComplete)
                                {
                                    debugWindow.checkTwo.IsChecked = true;
                                }

                                debugWindow.txtPause.Text = Convert.ToString(gestureP1.pauseFrames);
                                //END OF DEBUGGING                    

                            }
                            else
                            {

                                checkIfButtonPressed(skel, menuP2, btnHatP2, btnPropP2);
                                checkForAGesture(skel, gestureP2);

                                if (currentHatP2 != 0)
                                {
                                    DrawHat(skel, gestureP2);
                                }


                                if (currentPropP2 != 0)
                                {
                                    DrawHandProp(skel);
                                }

                                if (!(gestureP2.topHatOnHead != true && currentHatP2 == 2))
                                {
                                    checkHeadRotation(skel);
                                }

                                if (showPlayers.IsChecked == true)
                                {
                                    DrawPlayerPointer(skel);
                                }
                              
                                skeleton2X = skel.Position.X;
                                
                                player1 = true;

                            }

                        }
                    }
                }

            }

            if (numberOfPlayers == 2)
            {
                if (skeleton1X < skeleton2X)
                {
                    wrongPlaces = true;
                }
            }

            if (trackedPlayers == 0)
            {
                imgAttention.Visibility = Visibility.Visible;
                txtNoPlayers.Visibility = Visibility.Visible;

                headwearP1.Visibility = Visibility.Hidden;
                handPropP1.Visibility = Visibility.Hidden;
                headwearP2.Visibility = Visibility.Hidden;
                handPropP2.Visibility = Visibility.Hidden;

                btnHatP1.Visibility = Visibility.Hidden;
                btnHatP2.Visibility = Visibility.Hidden;
                btnPropP1.Visibility = Visibility.Hidden;
                btnPropP2.Visibility = Visibility.Hidden;

                headwearPreviewP1.Visibility = Visibility.Hidden;
                headwearPreviewP2.Visibility = Visibility.Hidden;

                propPreviewP1.Visibility = Visibility.Hidden;
                propPreviewP2.Visibility = Visibility.Hidden;


                debugWindow.txtTrackedPlayers.Text = Convert.ToString(trackedPlayers);
            }
            else
            {
                imgAttention.Visibility = Visibility.Hidden;
                txtNoPlayers.Visibility = Visibility.Hidden;

                if (numberOfPlayers == 2)
                {


                    btnHatP1.Visibility = Visibility.Visible;
                    btnHatP2.Visibility = Visibility.Visible;
                    btnPropP1.Visibility = Visibility.Visible;
                    btnPropP2.Visibility = Visibility.Visible;

                    if (currentHatP1 != 0)
                    {
                        headwearPreviewP1.Visibility = Visibility.Visible;
                    }

                    if (currentHatP2 != 0)
                    {
                        headwearPreviewP2.Visibility = Visibility.Visible;
                    }

                    if (currentPropP1 != 0)
                    {
                        propPreviewP1.Visibility = Visibility.Visible;
                    }

                    if (currentPropP2 != 0)
                    {
                        propPreviewP2.Visibility = Visibility.Visible;
                    }

                 
                }

                else if (numberOfPlayers == 1)
                {
                    btnHatP1.Visibility = Visibility.Visible;
                    btnHatP2.Visibility = Visibility.Hidden;
                    btnPropP1.Visibility = Visibility.Visible;
                    btnPropP2.Visibility = Visibility.Hidden;

                    if (currentHatP1 != 0)
                    {
                        headwearPreviewP1.Visibility = Visibility.Visible;
                    }

                    

                    if (currentPropP1 != 0)
                    {
                        propPreviewP1.Visibility = Visibility.Visible;
                    }

                    headwearPreviewP2.Visibility = Visibility.Hidden;
                    propPreviewP2.Visibility = Visibility.Hidden;

                    
                }


            }

        }

        private void DrawHat(Skeleton skeleton, Gesture player)
        {

            float hatHeight = 0;
            float hatWidth = 0;


            if(player1)
            {
                hatHeight = p1HatHeight;
                hatWidth = p1HatWidth;
            }
            else
            {
                hatHeight = p2HatHeight;
                hatWidth = p2HatWidth;
            }

            DepthImagePoint depthPoint;

            if ((player.currentGesture == 2) && (player.topHatOnHead == false))
            {
                Joint hand;

                if (player1)
                {
                    hand = skeleton.Joints[JointType.HandRight];
                }
                else
                {
                    hand = skeleton.Joints[JointType.HandLeft];
                }

                depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);

            }
            else
            {
                Joint head = skeleton.Joints[JointType.Head];
                depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(head.Position, DepthImageFormat.Resolution640x480Fps30);
            }


            if (depthPoint.Depth < 900)
            {
                if (player1)
                {
                    headwearP1.Visibility = Visibility.Hidden;
                }
                else
                {
                    headwearP2.Visibility = Visibility.Hidden;
                }
                return;
            }

            ColorImagePoint colourPoint = this.sensor.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution640x480Fps30, depthPoint, ColorImageFormat.RgbResolution1280x960Fps12);

            float ratio;

            ratio = 2000 / (float)depthPoint.Depth;


            float ratioWidth = ratio * (hatWidth);
            float ratioHeight = ratio * (hatHeight);


            if (player1)
            {
                headwearP1.Width = ratioWidth;
                headwearP1.Height = ratioHeight;
                debugWindow.txtRatio.Text = Convert.ToString(depthPoint.Depth);
            }
            else
            {
                headwearP2.Width = ratioWidth;
                headwearP2.Height = ratioHeight;
            }


            float jointX = colourPoint.X;
            float jointY = colourPoint.Y;

            float hatX = jointX - (ratioWidth / 2);
            float hatY = jointY - (ratioHeight - (ratioHeight / 4));



            if (player1)
            {
                System.Windows.Controls.Canvas.SetLeft(headwearP1, hatX);
                System.Windows.Controls.Canvas.SetTop(headwearP1, hatY);
                headwearP1.Visibility = Visibility.Visible;

                if (currentHatP1 == 2 && gestureP1.topHatOnHead == false)
                {
                    double handElbowDifference = Math.Abs(skeleton.Joints[JointType.HandRight].Position.X - skeleton.Joints[JointType.ElbowRight].Position.X);
                    double handElbowDifferenceY = Math.Abs(skeleton.Joints[JointType.HandRight].Position.Y - skeleton.Joints[JointType.ElbowRight].Position.Y);
                    double OldRange = (0.3);
                    double NewRange = (60);
                    double NewValue = (((handElbowDifference) * NewRange) / OldRange);
                    debugWindow.txtHandElbow.Text = Convert.ToString(handElbowDifference);
                    if (skeleton.Joints[JointType.HandRight].Position.X > skeleton.Joints[JointType.ElbowRight].Position.X)
                    {
                        headwearP1.RenderTransform = new RotateTransform(NewValue);

                    }
                    else
                    {
                        headwearP1.RenderTransform = new RotateTransform(-NewValue);

                    }

                }


                //debugging purposes only
                debugWindow.txtCanvasX.Text = Convert.ToString(jointX);
                debugWindow.txtCanvasY.Text = Convert.ToString(jointY);

                debugWindow.txtHatX.Text = Convert.ToString(Canvas.GetLeft(headwearP1));
                debugWindow.txtHatY.Text = Convert.ToString(Canvas.GetTop(headwearP1));

                debugWindow.txtHatHeight.Text = Convert.ToString(headwearP1.Height);
                debugWindow.txtHatWidth.Text = Convert.ToString(headwearP1.Width);

            }
            else
            {
                System.Windows.Controls.Canvas.SetLeft(headwearP2, hatX);
                System.Windows.Controls.Canvas.SetTop(headwearP2, hatY);
                headwearP2.Visibility = Visibility.Visible;

                if (currentHatP2 == 2 && gestureP2.topHatOnHead == false)
                {
                    if (skeleton.Joints[JointType.HandLeft].Position.X < skeleton.Joints[JointType.ElbowLeft].Position.X)
                    {
                        double handElbowDifference = Math.Abs(skeleton.Joints[JointType.HandLeft].Position.X - skeleton.Joints[JointType.ElbowLeft].Position.X);
                        double handElbowDifferenceY = Math.Abs(skeleton.Joints[JointType.HandLeft].Position.Y - skeleton.Joints[JointType.ElbowLeft].Position.Y);
                        double OldRange = (0.3);
                        double NewRange = (60);
                        double NewValue = (((handElbowDifference) * NewRange) / OldRange);
                        debugWindow.txtHandElbow.Text = Convert.ToString(handElbowDifference);
                        if (skeleton.Joints[JointType.HandLeft].Position.X > skeleton.Joints[JointType.ElbowLeft].Position.X)
                        {
                            headwearP2.RenderTransform = new RotateTransform(NewValue);

                        }
                        else
                        {
                            headwearP2.RenderTransform = new RotateTransform(-NewValue);

                        }
                    }
                }

            }
        }


        private void DrawHandProp(Skeleton skeleton)
        {

            float propHeight = 0;
            float propWidth = 0;
            Joint hand;
            Joint elbow;

            if(player1)
            {
                handPropP1.Visibility = Visibility.Visible;
                propHeight = p1PropHeight;
                propWidth = p1PropWidth;
                hand = skeleton.Joints[JointType.HandLeft];
                elbow = skeleton.Joints[JointType.ElbowLeft];
            }
            else
            {
                handPropP2.Visibility = Visibility.Visible;
                propHeight = p2PropHeight;
                propWidth = p2PropWidth;

                hand = skeleton.Joints[JointType.HandRight];
                elbow = skeleton.Joints[JointType.ElbowRight];
            }

            DepthImagePoint depthPoint;

            depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);

            if (depthPoint.Depth < 900)
            {
                if (player1)
                {
                    handPropP1.Visibility = Visibility.Hidden;
                }
                else
                {
                    handPropP2.Visibility = Visibility.Hidden;
                }
                return;
            }

            ColorImagePoint colourPoint = this.sensor.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution640x480Fps30, depthPoint, ColorImageFormat.RgbResolution1280x960Fps12);

            float ratio;

            ratio = 2000 / (float)depthPoint.Depth;

            

            double headHandDifference = hand.Position.Y - skeleton.Joints[JointType.Head].Position.Y;

            if (player1 && currentPropP1 == 1)
            {
                if (headHandDifference < -0.2)
                {
                    handPropP1.Source = new BitmapImage(uriUmbrellaClosed);
                    propHeight = 155;
                    propWidth = 271;
                    handPropP1.RenderTransformOrigin = new Point(0.886, 0.951);
                }
                else
                {
                    handPropP1.Source = new BitmapImage(uriUmbrellaOpen);
                    propWidth = 400;
                    propHeight = 400;
                    handPropP1.RenderTransformOrigin = new Point(0.946, 0.858);
                }
            }
            else if (!player1 && currentPropP2 == 1)
            {
                if (headHandDifference < -0.2)
                {
                    handPropP2.Source = new BitmapImage(uriUmbrellaClosed);
                    propHeight = 198;
                    propWidth = 345;
                    handPropP2.RenderTransformOrigin = new Point(0.886, 0.951);
                }
                else
                {
                    handPropP2.Source = new BitmapImage(uriUmbrellaOpen);
                    propWidth = 485;
                    propHeight = 485;
                    handPropP2.RenderTransformOrigin = new Point(0.946, 0.858);
                }
            }

            float ratioWidth = ratio * (propWidth);
            float ratioHeight = ratio * (propHeight);

            if (player1)
            {
                handPropP1.Width = ratioWidth;
                handPropP1.Height = ratioHeight;
            }
            else
            {
                handPropP2.Width = ratioWidth;
                handPropP2.Height = ratioHeight;
            }

            //float jointX = colourPoint.X;
            //float jointY = colourPoint.Y;

            float propX = colourPoint.X - (ratioWidth);
            float propY = colourPoint.Y - (ratioHeight - (ratioHeight / 4));

            if (player1)
            {
                System.Windows.Controls.Canvas.SetLeft(handPropP1, propX);
                System.Windows.Controls.Canvas.SetTop(handPropP1, propY);
            }
            else
            {
                System.Windows.Controls.Canvas.SetLeft(handPropP2, propX);
                System.Windows.Controls.Canvas.SetTop(handPropP2, propY);
            }


            
            double handElbowDifference = (hand.Position.X - elbow.Position.X);
            

            double handElbowDifferenceY = (hand.Position.Y - elbow.Position.Y);
            double oldMin = 0;
            double oldMax = 0;
            double newMin = 0;
            double newMax = 0;
            double oldRange = 0;
            double newRange = 0;
            double newValue = 0;
            

            if (hand.Position.Y > elbow.Position.Y)
            {
                if (player1)
                {//0.3 - -1 
                    oldMax = 0.2;
                    oldMin = -0.25;
                    oldRange = (oldMax - oldMin);

                    newMax = 80;
                    newMin = -10;
                    newRange = (newMax - newMin);

                    newValue = (((handElbowDifference - oldMin) * newRange) / oldRange) + newMin;
                    handPropP1.RenderTransform = new RotateTransform(newValue);
                }
                else
                {
                    oldMax = 0.3;
                    oldMin = 0;
                    oldRange = (oldMax - oldMin);

                    newMax = 90;
                    newMin = 30;
                    newRange = (newMax - newMin);

                    newValue = (((handElbowDifference - oldMin) * newRange) / oldRange) + newMin;
                    handPropP2.RenderTransform = new RotateTransform(newValue);

                    debugWindow.txtLeftHandElbow.Text = Convert.ToString(Math.Round(handElbowDifference, 3));
                }
            }
            else if (hand.Position.Y < elbow.Position.Y)
            {
                if (player1)
                {
                    oldMax = -0.45;
                    oldMin = -0.9;
                    oldRange = (oldMax - oldMin);

                    newMax = -10;
                    newMin = -100;
                    newRange = (newMax - newMin);
                    newValue = (((headHandDifference - oldMin) * newRange) / oldRange) + newMin;
                    handPropP1.RenderTransform = new RotateTransform(newValue);
                }
                else
                {
                    oldMax = -0.45;
                    oldMin = -0.9;
                    oldRange = (oldMax - oldMin);

                    newMax = 90;
                    newMin = 180;
                    newRange = (newMax - newMin);
                    newValue = (((headHandDifference - oldMin) * newRange) / oldRange) + newMin;
                    debugWindow.txtLeftHandElbow.Text = Convert.ToString(Math.Round(handElbowDifference, 3));
                    handPropP2.RenderTransform = new RotateTransform(newValue);
                }
            }


            debugWindow.txtRotation.Text = Convert.ToString(newValue);
            


        }

         private void DrawPlayerPointer(Skeleton skeleton)
        {

            DepthImagePoint depthPoint;

            Joint head = skeleton.Joints[JointType.Head];
            depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(head.Position, DepthImageFormat.Resolution640x480Fps30);

            float depthX = depthPoint.X;
            float depthY = depthPoint.Y;

            depthX = Math.Max(0, Math.Min(depthPoint.X * 640, 640));  //convert to 640, 480 space
            depthY = Math.Max(0, Math.Min(depthPoint.Y * 480, 480));  //convert to 640, 480 space 

            ColorImagePoint colourPoint = this.sensor.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution640x480Fps30, depthPoint, ColorImageFormat.RgbResolution1280x960Fps12);

            if (player1)
            {
                player1Pointer.Visibility = Visibility.Visible;
                System.Windows.Controls.Canvas.SetLeft(player1Pointer, colourPoint.X);
                System.Windows.Controls.Canvas.SetTop(player1Pointer, colourPoint.Y);
            }
            else
            {
                if (numberOfPlayers == 2)
                {
                    player2Pointer.Visibility = Visibility.Visible;
                    System.Windows.Controls.Canvas.SetLeft(player2Pointer, colourPoint.X);
                    System.Windows.Controls.Canvas.SetTop(player2Pointer, colourPoint.Y);
                }
            }

             if (numberOfPlayers == 1)
             {
                 player2Pointer.Visibility = Visibility.Hidden;
             }
        }

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);


                }
            }

        }

        private void checkIfButtonPressed(Skeleton skel, SelectionMenu selectMenu, Button hat, Button prop)
        {
            Joint hand;

            if(player1)
            {
                hand = skel.Joints[JointType.HandLeft];
            }
            else
            {
                hand = skel.Joints[JointType.HandRight];
            }
          

            //check if button has already been pressed
            //check whether hand is over a button
            //check whether the button has been pressed

            if (selectMenu.buttonIsPressed)
            {
                selectMenu.isButtonPressed(hand);
                return;
            }
            else
            {

                hat.Opacity = 0.5;
                prop.Opacity = 0.5;

                selectMenu.checkHandLocation(hand);

                if (selectMenu.currentButton == 1)
                {
                    debugWindow.checkOnHat.IsChecked = true;
                    debugWindow.checkOnProp.IsChecked = false;
                }
                else if (selectMenu.currentButton == 2)
                {
                    debugWindow.checkOnProp.IsChecked = true;
                    debugWindow.checkOnHat.IsChecked = false;

                }
                else if (selectMenu.currentButton == 0)
                {
                    debugWindow.checkOnHat.IsChecked = false;
                    debugWindow.checkOnProp.IsChecked = false;
                }


                selectMenu.isButtonPressed(hand);
                if (selectMenu.buttonIsPressed)
                {
                    switch (selectMenu.currentButton)
                    {
  
                        case 1: 
                        hat.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); //force button click event
                        hat.Opacity = 0.75;

                        if (player1)
                        {
                            debugWindow.checkPressHat.IsChecked = true;
                        }

                        break;

                        case 2:
                        prop.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        prop.Opacity = 0.75;

                        if (player1)
                        {
                            debugWindow.checkPressProp.IsChecked = true;
                        }

                        break;

                        default: break;
 
                    }
                }
                else
                {
                    debugWindow.checkPressProp.IsChecked = false;
                    debugWindow.checkPressHat.IsChecked = false;
                }
            }

            }
        

        private void checkForAGesture(Skeleton skel, Gesture theGesture)
        {

            if (theGesture.countdown == 0) //checks whether the gesture countdown is on 0 - gestures can only be performed every 20 frames
            {

                Joint head = skel.Joints[JointType.Head];
                Joint hand;

                if(player1)
                {
                    hand = skel.Joints[JointType.HandRight];
                }
                else
                {
                    hand = skel.Joints[JointType.HandLeft];
                }

                theGesture.checkGesture(head, hand, player1, sensor);

                if (theGesture.partOneComplete == false)
                {
                    debugWindow.checkOne.IsChecked = false;
                }


            }
            else if (theGesture.countdown == 1)
            {
                headwearP1.RenderTransform = new RotateTransform(0);

                debugWindow.checkTwo.IsChecked = false;
                debugWindow.checkOne.IsChecked = false;
                theGesture.countdownTimer();
            }
            else if (theGesture.countdown > 1)
            {
                theGesture.countdownTimer();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(1);
        }


        private void btnCamera_Click(object sender, RoutedEventArgs e)
        {

            if (txtBoxEmail.Text != String.Empty)
            {

                btnHatP1.Visibility = Visibility.Hidden;
                btnHatP2.Visibility = Visibility.Hidden;
                btnPropP1.Visibility = Visibility.Hidden;
                btnPropP2.Visibility = Visibility.Hidden;
                headwearPreviewP1.Visibility = Visibility.Hidden;
                headwearPreviewP2.Visibility = Visibility.Hidden;
                propPreviewP1.Visibility = Visibility.Hidden;
                propPreviewP2.Visibility = Visibility.Hidden;

                int height = (int)Kinect.Height;
                int width = (int)Kinect.Width;

                RenderTargetBitmap kinectCapture = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

                kinectCapture.Render(Kinect);

                PngBitmapEncoder kinectPNG = new PngBitmapEncoder();

                kinectPNG.Frames.Add(BitmapFrame.Create(kinectCapture));

                string fileName = ("../../Kinect Captures/" + DateTime.Now.ToString("ddMMyyHHmmss") + ".png");

                using (Stream fileStream = File.Create(fileName))
                {
                    kinectPNG.Save(fileStream);
                }

                try
                {

                    MailMessage mail = new MailMessage();
                    SmtpClient smtpServer = new SmtpClient(""); 

                    mail.From = new MailAddress(""); 
                    mail.To.Add(txtBoxEmail.Text);
                    if(txtBoxEmail2.Text!="")
                    {
                        mail.CC.Add(txtBoxEmail2.Text);
                    }
                    mail.Bcc.Add("");
                    mail.Subject = "University of Lincoln Open Day - Augmented Reality with the Kinect Camera";
                    mail.IsBodyHtml = true;
                    mail.Body = "Dear potential student,<br/><br/>Thank you for using my augmented reality Kinect system at the University of Lincoln open day. The image from the open day is attatched to this email.<br/><br/>For further information about the University of Lincoln, please visit the website by <a href=\"http://www.lincoln.ac.uk/home/\">clicking here</a>.";

                    smtpServer.UseDefaultCredentials = false;
                    smtpServer.Port = 587; //465
                    smtpServer.Credentials = new System.Net.NetworkCredential("", "");
                    smtpServer.EnableSsl = true;
                    System.Net.Mail.Attachment attachment;

                    attachment = new System.Net.Mail.Attachment(fileName);
                    attachment.Name = (DateTime.Now.ToString("ddMMyyHHmmss") + ".png");


                    mail.Attachments.Add(attachment);

                    smtpServer.Send(mail);

                    btnHatP1.Visibility = Visibility.Visible;
                    btnHatP2.Visibility = Visibility.Visible;
                    btnPropP1.Visibility = Visibility.Visible;
                    btnPropP2.Visibility = Visibility.Visible;
                    headwearPreviewP1.Visibility = Visibility.Visible;
                    headwearPreviewP2.Visibility = Visibility.Visible;
                    propPreviewP1.Visibility = Visibility.Visible;
                    propPreviewP2.Visibility = Visibility.Visible;

                    MessageBox.Show("Mail sent");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            else
            {
                MessageBox.Show("Please enter a valid email address.");
            }

        }

        private void btnHatP1_Click(object sender, RoutedEventArgs e)
        {
            currentHatP1++;
            gestureP1.topHatOnHead = true;
  

            if (currentHatP1 > 3)
            {
                currentHatP1 = 0;
            }

            switch (currentHatP1)
            {
                case 0:

                    headwearP1.Visibility = Visibility.Hidden;
                    headwearPreviewP1.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    headwearP1.Visibility = Visibility.Visible;
                    headwearPreviewP1.Visibility = Visibility.Visible;
                    headwearP1.Source = new BitmapImage(uriFedora);
                    headwearPreviewP1.Source = new BitmapImage(uriFedora);
                    gestureP1.currentGesture = 1;
                    p1HatHeight = 130;
                    p1HatWidth = 168;
                    headwearP1.RenderTransformOrigin = new Point(0.537, 0.891);
                    break;

                case 2:
                    headwearP1.Visibility = Visibility.Visible;
                    headwearPreviewP1.Visibility = Visibility.Visible;
                    headwearP1.Source = new BitmapImage(uriTopHat);
                    headwearPreviewP1.Source = new BitmapImage(uriTopHat);
                    gestureP1.currentGesture = 2;
                    p1HatHeight = 202;
                    p1HatWidth = 160;
                    headwearP1.RenderTransformOrigin = new Point(0.5, 0.784);
                    break;

                case 3:

                    headwearP1.Visibility = Visibility.Visible;
                    headwearPreviewP1.Visibility = Visibility.Visible;
                    headwearP1.Source = new BitmapImage(uriSombrero);
                    headwearPreviewP1.Source = new BitmapImage(uriSombrero);
                    gestureP1.currentGesture = 0;
                    p1HatHeight = 113;
                    p1HatWidth = 260;
                    headwearP1.RenderTransformOrigin = new Point(0.51, 0.955);
                    break;



                default:
                    break;
            }

            
        }

        private void btnPropP1_Click(object sender, RoutedEventArgs e)
        {
            currentPropP1++;

            if (currentPropP1 > 3)
            {
                currentPropP1 = 0;
            }


            switch (currentPropP1)
            {
                case 0:

                    handPropP1.Visibility = Visibility.Hidden;
                    propPreviewP1.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    handPropP1.Visibility = Visibility.Visible;
                    propPreviewP1.Visibility = Visibility.Visible;
                    handPropP1.Source = new BitmapImage(uriUmbrellaClosed);
                    propPreviewP1.Source = new BitmapImage(uriUmbrellaOpen);
                    break;

                case 2:

                    handPropP1.Visibility = Visibility.Visible;
                    propPreviewP1.Visibility = Visibility.Visible;
                    handPropP1.Source = new BitmapImage(uriCane);
                    propPreviewP1.Source = new BitmapImage(uriCane);
                    handPropP1.RenderTransformOrigin = new Point(0.896, 0.936);

                    p1PropWidth = 378;
                    p1PropHeight = 288;
                    break;

                case 3:

                    handPropP1.Visibility = Visibility.Visible;
                    propPreviewP1.Visibility = Visibility.Visible;
                    handPropP1.Source = new BitmapImage(uriSword);
                    propPreviewP1.Source = new BitmapImage(uriSword);
                    handPropP1.RenderTransformOrigin = new Point(0.949,0.917);

                    p1PropWidth = 378;
                    p1PropHeight = 214;
                    break;
              

                default:
                    break;
            }


        }

        private void btnHatP2_Click(object sender, RoutedEventArgs e)
        {
            currentHatP2++;
            gestureP2.topHatOnHead = true;


            if (currentHatP2 > 3)
            {
                currentHatP2 = 0;
            }

            switch (currentHatP2)
            {
                case 0:

                    headwearP2.Visibility = Visibility.Hidden;
                    headwearPreviewP2.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    headwearP2.Visibility = Visibility.Visible;
                    headwearPreviewP2.Visibility = Visibility.Visible;
                    headwearP2.Source = new BitmapImage(uriFedora);
                    headwearPreviewP2.Source = new BitmapImage(uriFedora);
                    gestureP2.currentGesture = 1;
                    p2HatHeight = 130;
                    p2HatWidth = 168;
                    headwearP2.RenderTransformOrigin = new Point(0.537, 0.891);
                    break;

                case 2:
                    headwearP2.Visibility = Visibility.Visible;
                    headwearPreviewP2.Visibility = Visibility.Visible;
                    headwearP2.Source = new BitmapImage(uriTopHat);
                    headwearPreviewP2.Source = new BitmapImage(uriTopHat);
                    gestureP2.currentGesture = 2;
                    p2HatHeight = 202;
                    p2HatWidth = 160;
                    headwearP2.RenderTransformOrigin = new Point(0.5, 0.784);
                    break;

                case 3:

                    headwearP2.Visibility = Visibility.Visible;
                    headwearPreviewP2.Visibility = Visibility.Visible;
                    headwearP2.Source = new BitmapImage(uriSombrero);
                    headwearPreviewP2.Source = new BitmapImage(uriSombrero);
                    gestureP2.currentGesture = 0;
                    p2HatHeight = 113;
                    p2HatWidth = 260;
                    headwearP2.RenderTransformOrigin = new Point(0.51, 0.955);
                    break;



                default:
                    break;
            }


        }

        private void btnPropP2_Click(object sender, RoutedEventArgs e)
        {
            {
                currentPropP2++;

                if (currentPropP2 > 3)
                {
                    currentPropP2 = 0;
                }


                switch (currentPropP2)
                {
                    case 0:

                        handPropP2.Visibility = Visibility.Hidden;
                        propPreviewP2.Visibility = Visibility.Hidden;
                        break;
                    case 1:
                        handPropP2.Visibility = Visibility.Visible;
                        propPreviewP2.Visibility = Visibility.Visible;
                        handPropP2.Source = new BitmapImage(uriUmbrellaClosed2);
                        propPreviewP2.Source = new BitmapImage(uriUmbrellaOpen2);
                        break;

                    case 2:

                        handPropP2.Visibility = Visibility.Visible;
                        propPreviewP2.Visibility = Visibility.Visible;
                        handPropP2.Source = new BitmapImage(uriCane2);
                        propPreviewP2.Source = new BitmapImage(uriCane2);
                        handPropP2.RenderTransformOrigin = new Point(0.896, 0.936);

                        p2PropWidth = 378;
                        p2PropHeight = 288;
                        break;

                    case 3:

                        handPropP2.Visibility = Visibility.Visible;
                        propPreviewP2.Visibility = Visibility.Visible;
                        handPropP2.Source = new BitmapImage(uriSword2);
                        propPreviewP2.Source = new BitmapImage(uriSword2);
                        handPropP2.RenderTransformOrigin = new Point(0.949, 0.917);

                        p2PropWidth = 378;
                        p2PropHeight = 214;
                        break;


                    default:
                        break;
                }
            }
        }



        private void btnRetry_Click(object sender, RoutedEventArgs e)
        {
            Window_Loaded(sender, e);
        }


        private void checkHeadRotation(Skeleton skele)
        {

            Joint leftShoulder = skele.Joints[JointType.ShoulderLeft];
            Joint rightShoulder = skele.Joints[JointType.ShoulderRight];
            Joint head = skele.Joints[JointType.Head];

            float leftShoulderToHead = Math.Abs(head.Position.X - leftShoulder.Position.X);
            float rightShoulderToHead = Math.Abs(head.Position.X - rightShoulder.Position.X);

            float headRotation = Math.Abs(rightShoulderToHead - leftShoulderToHead);

            debugWindow.txtLeftRightShoulder.Text = Convert.ToString(headRotation);

         double OldRange = 0.2; 
         double NewRange = 40;
         double NewValue = (headRotation * NewRange) / OldRange;
         
            if (leftShoulderToHead < rightShoulderToHead)
            {
                NewValue = -NewValue;
            }



            if(player1)
            {
                if (gestureP1.gestureComplete == true && gestureP1.currentGesture == 1)
                {
                    headwearP1.RenderTransform = new RotateTransform(NewValue + 30);
                }
                else
                {
                    headwearP1.RenderTransform = new RotateTransform(NewValue);
                }
            }
            else
            {
                if (gestureP2.gestureComplete == true && gestureP2.currentGesture == 1)
                {
                    headwearP2.RenderTransform = new RotateTransform(NewValue - 30);
                }
                else
                {
                    headwearP2.RenderTransform = new RotateTransform(NewValue);
                }
            }
           
            
        }

  

        private void showDebug_Click(object sender, RoutedEventArgs e)
        {

            if (!debugWindow.IsVisible)
            {
            debugWindow.Show();
            }
          
        }

     
        private void player1_Checked(object sender, RoutedEventArgs e)
        {
            numberOfPlayers = 1;
            _2Player.IsChecked = false;
            radioOne.IsChecked = true;
        }

        private void player2_Checked(object sender, RoutedEventArgs e)
        {
            numberOfPlayers = 2;
            _1Player.IsChecked = false;
            radioTwo.IsChecked = true;
        }

        private void radioOne_Checked(object sender, RoutedEventArgs e)
        {
            numberOfPlayers = 1;
            _1Player.IsChecked = true;
            _2Player.IsChecked = false;
            
        }

        private void radioTwo_Checked(object sender, RoutedEventArgs e)
        {
            numberOfPlayers = 2;
            _2Player.IsChecked = true;
            _1Player.IsChecked = false;
            
        }

        private void _1Player_Unchecked(object sender, RoutedEventArgs e)
        {
            _1Player.IsChecked = true;
        }

        private void _2Player_Unchecked(object sender, RoutedEventArgs e)
        {
            _2Player.IsChecked = true;
        }

        private void showPlayers_Unchecked(object sender, RoutedEventArgs e)
        {
            player1Pointer.Visibility = Visibility.Hidden;
            player2Pointer.Visibility = Visibility.Hidden;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

   




    }
    public class Gesture
    {

        public bool partOneComplete;
        public bool partTwoComplete;
        public bool gestureComplete;

        public bool topHatOnHead;

        double partOneHeadYPosition;
        public double partOneHandYPosition;

        public int pauseFrames;

        public int countdown;

        public int currentGesture;

        SoundPlayer mLady = new SoundPlayer(Properties.Resources.mlady);
        SoundPlayer mLadyP2 = new SoundPlayer(Properties.Resources.mladyConz);

        public Gesture()
        {
            partOneComplete = false;
            partTwoComplete = false;
            gestureComplete = false;

            partOneHeadYPosition = 0;
            partOneHandYPosition = 0;

            pauseFrames = 0;
            countdown = 0;

            currentGesture = 0;

            topHatOnHead = true;



        }

        public void checkFedoraPartOne(Joint head, Joint hand)
        {

            SkeletonPoint headPoint = head.Position;
            SkeletonPoint handPoint = hand.Position;

            double headHandXDifference = Math.Abs(headPoint.X - handPoint.X);
            double headHandYDifference = Math.Abs(headPoint.Y - handPoint.Y);

            if (headHandXDifference <= 0.18)
            {
                if (headHandYDifference <= 0.15)
                {
                    partOneHeadYPosition = headPoint.Y;
                    partOneHandYPosition = handPoint.Y;
                    partOneComplete = true;
                    return;
                }
            }

            partOneComplete = false;



        }

        public void checkFedoraPartTwo(Joint head, Joint hand)
        {

            SkeletonPoint headPoint = head.Position;
            SkeletonPoint handPoint = hand.Position;

            double handDifferenceY = Math.Abs(partOneHandYPosition - handPoint.Y);
            double headHandDifferenceX = Math.Abs(headPoint.X - handPoint.X);

            if (partOneHeadYPosition > headPoint.Y)
            {
                if (handDifferenceY >= 0.1 & partOneHandYPosition > handPoint.Y & headHandDifferenceX < 0.2)
                {
                    partTwoComplete = true;
                }
            }
        }

        public void checkTopHatPartOne(Joint head, Joint hand, KinectSensor sensor)
        {
            SkeletonPoint headPoint = head.Position;
            SkeletonPoint handPoint = hand.Position;

            DepthImagePoint handDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(handPoint, DepthImageFormat.Resolution640x480Fps30);
            DepthImagePoint headDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(headPoint, DepthImageFormat.Resolution640x480Fps30);

            double headHandXDifference = Math.Abs(headPoint.X - handPoint.X);
            double headHandYDifference = Math.Abs(headPoint.Y - handPoint.Y);

            double headHandDepthDifference = Math.Abs(handDepthPoint.Depth - headDepthPoint.Depth);

            if (headHandXDifference < 0.1 && headHandYDifference < 0.1)
            {
                if (headHandDepthDifference < 200)
                {
                    partOneComplete = true;
                    return;
                }
            }

            partOneComplete = false;
        }

        public void checkGesture(Joint head, Joint hand, bool player1, KinectSensor sensor)
        {

            if (countdown != 0)
            {
                return;
            }
            else
            {
                gestureComplete = false;
            }

            switch (currentGesture)
            {
                case 0:
                    break;

                case 1:

                    if (partOneComplete)
                    {
                        if (partTwoComplete)
                        {
                            gestureResult(player1);
                            return;
                        }
                        else
                        {
                            pauseFrames++;
                            checkFedoraPartTwo(head, hand);

                            if (!partTwoComplete && pauseFrames > 20)
                            {
                                partOneComplete = false;
                                partTwoComplete = false;
                                pauseFrames = 0;
                                return;
                            }
                        }
                    }
                    else
                    {
                        checkFedoraPartOne(head, hand);
                        return;
                    }

                    break;

                case 2:

                    if (partOneComplete)
                    {
                        gestureResult(player1);
                        return;
                    }
                    else
                    {
                        checkTopHatPartOne(head, hand, sensor);
                        return;
                    }

                default:
                    break;
            }

        }

        void gestureResult(bool player1)
        {

            switch (currentGesture)
            {
                case 1:
                    if (player1)
                    {
                        mLady.Play();
                    }
                    else
                    {
                        mLadyP2.Play();
                    }
                    countdown = 20;
                    pauseFrames = 0;
                    gestureComplete = true;
                    partOneComplete = false;
                    partTwoComplete = false;
                    break;

                case 2:
                    if (topHatOnHead == true)
                    {
                        gestureComplete = true;
                        topHatOnHead = false;
                        countdown = 20;
                        pauseFrames = 0;

                        partOneComplete = false;
                        partTwoComplete = false;
                    }
                    else if (topHatOnHead == false)
                    {
                        gestureComplete = true;
                        topHatOnHead = true;
                        countdown = 20;
                        pauseFrames = 0;

                        partOneComplete = false;
                        partTwoComplete = false;
                    }
                    break;

                default:
                    break;
            }
        }

        public void countdownTimer()
        {
            countdown--;
        }

    }

    public class SelectionMenu
    {
        KinectSensor sensor;

        double hatX, hatY, hatWidth, hatHeight;

        double propX, propY, propWidth, propHeight;

        public bool buttonIsPressed = false;

        public int currentButtonHover = 0, currentButton = 0;

        public double depthValue;


        public SelectionMenu(KinectSensor theSensor, Button hat, Button prop)
        {
            sensor = theSensor;
    


            hatX = Canvas.GetLeft(hat);
            hatY = Canvas.GetTop(hat);
            hatWidth = hat.Width;
            hatHeight = hat.Height;

            propX = Canvas.GetLeft(prop);
            propY = Canvas.GetTop(prop);
            propWidth = prop.Width;
            propHeight = prop.Height;

        }

        public void checkHandLocation(Joint hand)
        {

            DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint colourPoint = this.sensor.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution640x480Fps30, depthPoint, ColorImageFormat.RgbResolution1280x960Fps12);

            if (colourPoint.X >= hatX && colourPoint.X <= (hatX + hatWidth) && colourPoint.Y >= hatY && colourPoint.Y <= (hatY + hatHeight)) //if hand is over the next hat button
            {   
                    if (currentButton == 1) //if the hand was already on the Next button
                    {
                        return;
                    }
                    else
                    {
                        currentButton = 1;
                        depthValue = depthPoint.Depth;
                        return;
                        
                    }
                }
            
        else if(colourPoint.X >= propX && colourPoint.X <= (propX + propWidth) && colourPoint.Y >= propY && colourPoint.Y <= (propY + propHeight)) //if hand is over the next prop button
            {
   
                        if (currentButton == 2) //if the hand was already on the Next button
                        {
                            return;
                        }
                        else
                        {
  
                            currentButton = 2;
                            depthValue = depthPoint.Depth;
                            return;
                        }
                    }
            else
            {

                depthValue = 0;
                buttonIsPressed = false;
                currentButton = 0;

            }

        }


        public void isButtonPressed(Joint hand)
        {

            double depth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30).Depth;

            double depthDifference = depthValue - depth;

            if ( currentButton != 0 && depthDifference > 75)
            {
                buttonIsPressed = true;
            }
            else
            {
                buttonIsPressed = false;
            }

        }

    }

}


