namespace PJS.Skeleton
{
    using System;
    using System.Windows;
    using System.IO;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Microsoft.Win32;
    using System.Globalization;
    using System.Windows.Threading;


    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        private const float RenderWidth = 640.0f; // Width of output drawing
        private const float RenderHeight = 480.0f;// Height of our output drawing
        private const double JointThickness = 3; /// Thickness of drawn joint lines

        /// Brush used for drawing joints that are currently tracked
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// Active Kinect sensor
        private KinectSensor sensor;

        /// Drawing group for skeleton rendering output
        private DrawingGroup drawingGroup;

        /// Drawing image that we will display
        private DrawingImage imageSource;

        private JointType[] types = { JointType.Head,
                                      JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.ShoulderRight, 
                                      JointType.ElbowLeft, JointType.ElbowRight,
                                      JointType.WristLeft, JointType.WristRight, 
                                      JointType.Spine, 
                                      JointType.HipCenter, JointType.HipLeft, JointType.HipRight,
                                      JointType.KneeLeft, JointType.KneeRight,
                                      JointType.AnkleLeft, JointType.AnkleRight };

        private bool cycleStart = false;
        private int cycle = 0;
        private bool cycleEnd = false;
        private DispatcherTimer cyleTimer;
        private Recorder rec;
        private Player player;

        /// Initializes a new instance of the MainWindow class.
        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            rec = new Recorder(this);
            player = new Player(this, types.Length);


            println("PJSSkeleton Application\n -----------------------");
            // Starteintrag der Combobox 

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                    println("Kinect erkannt");

                    Console.WriteLine(this.sensor.ElevationAngle);
                    this.sensor.ElevationAngle = 0; // set kinect angle relative to gravity force
                    Console.WriteLine(this.sensor.ElevationAngle);

                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                println("Keine Kinect erkannt");
                //this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }


        /// Execute shutdown tasks
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// Event handler for Kinect sensor's SkeletonFrameReady event
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            if (skeletons.Length != 0)
            {
                foreach (Skeleton skel in skeletons)
                {
                    if (skel.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        string frame = "";
                        // Array to save data for rendering
                        SkeletonPoint[] copySkel = new SkeletonPoint[types.Length];
                        int index = 0;
                        foreach (var joint in types)
                        {
                            SkeletonPoint j = skel.Joints[joint].Position;
                            // Save data to copy for rendering:
                            copySkel[index] = j;
                            index++;

                            // ^^ done
                            if (frame.Length != 0)
                            {
                                frame += ",";
                            }

                            frame += j.X.ToString(CultureInfo.InvariantCulture) + "," + j.Y.ToString(CultureInfo.InvariantCulture) + "," + j.Z.ToString(CultureInfo.InvariantCulture);

                        }
                        // output to file
                        if (rec.isRecording())
                        {
                            startDetection(skel);
                            if (cycle > 5 && cycle <= 15)
                                rec.record(frame);

                            if (cycle > 15)
                            {
                                rec.stopRecording();
                                cycle = 0;
                            }
                        }
                        // Note: draw frame here
                        renderSkeleton(0, this.SkeletonToScreen(copySkel), false);
                    }
                }
            }
        }

        // based on meters, timer limit missing
        private void startDetection(Skeleton skel)
        {
            SkeletonPoint head = skel.Joints[JointType.Head].Position;
            SkeletonPoint leftWrist = skel.Joints[JointType.WristLeft].Position;
            SkeletonPoint rightWrist = skel.Joints[JointType.WristRight].Position;
            SkeletonPoint leftFoot = skel.Joints[JointType.AnkleLeft].Position;
            SkeletonPoint rightFoot = skel.Joints[JointType.AnkleRight].Position;

            detect(SkeletonPointToPoint(head), SkeletonPointToPoint(leftWrist), SkeletonPointToPoint(rightWrist), SkeletonPointToPoint(leftFoot), SkeletonPointToPoint(rightFoot));

        }
        private void startDetection(Point[,] skel, int f)
        {
            Point head = skel[f, 0];
            Point leftWrist = skel[f, 6];
            Point rightWrist = skel[f, 7];
            Point leftFoot = skel[f, 14];
            Point rightFoot = skel[f, 15];

            detect(head, leftWrist, rightWrist, leftFoot, rightFoot);
        }

        private void detect(Point head, Point leftWrist, Point rightWrist, Point leftFoot, Point rightFoot)
        {
            // wrists should be above head
            bool armsUp = leftWrist.Y > head.Y && rightWrist.Y > head.Y ? true : false; // < because coordsystem
            // arms should be close together
            bool armsClose = Math.Abs(leftWrist.X - rightWrist.X) < 0.4 ? true : false;
            // feet should be close together
            bool feetClose = Math.Abs(rightFoot.X - leftFoot.X) < 0.2 ? true : false;

            //Console.WriteLine("Arms up " + armsUp);
            //Console.WriteLine("Arms close " + armsClose);
            //Console.WriteLine("Feet close " + feetClose);

            if (!cycleStart && armsUp && armsClose && feetClose) // detect start
            {
                // start of a cycle detected
                cycleStart = true;
                if (cyleTimer == null)
                {
                    cyleTimer = new DispatcherTimer();
                    cyleTimer.Tick += new EventHandler(CycleTimout);
                    cyleTimer.Interval = new TimeSpan(0, 0, 0, 2, 500);
                }

                if (cyleTimer != null)
                    cyleTimer.Start();

                //Console.WriteLine("Start det"); 
            }
            // Detect end of cycle following a start
            else if (cycleStart && !armsUp && !armsClose && !feetClose)
            {
                cycleEnd = true;
                cycleStart = false;
                // Console.WriteLine("End det"); 
                return;
            }


            if (cycleStart && cycleEnd)
            {
                cycleStart = false;
                cycleEnd = false;
                ++cycle;
                cyleTimer.Stop();
                println("Cycle " + cycle + " Detected.");
            }

        }

        private void CycleTimout(object sender, EventArgs e)
        {
            cycleStart = false;
            cycleEnd = false;
            cycle = 0;
            cyleTimer.Stop();
            println("Time out");

            if (rec.isRecording())
            {
                rec.stopRecording();
                println("Aufname wegen timeout gestoppt.");
            }

        }

        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        private Point[,] SkeletonToScreen(SkeletonPoint[] skel)
        {
            // Convert point to depth space.  
            Point[,] retArr = new Point[1, skel.Length];
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            for (int i = 0; i < skel.Length; i++)
            {
                DepthImagePoint depth = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skel[i], DepthImageFormat.Resolution640x480Fps30);
                retArr[0, i] = new Point(depth.X, depth.Y);
            }
            return retArr;
        }



        private Point SkeletonPointToPoint(SkeletonPoint sp)
        {
            return new Point(sp.X, sp.Y);
        }

        /**
         * Method for painting the canvas. Leave all the paint stuff to me!
         * */
        private void renderSkeleton(int frame, Point[,] animData, Boolean scale)
        {
            // Define parameters for painting:
            int scaling = scale ? 200 : 1;  // Scale skeleton points
            int x = scale ? -(int)RenderWidth / 2 : 0;
            int y = scale ? -(int)RenderHeight / 2 : 0;
            int inverse = scale ? -1 : 1;
            // Do the actual painting:
            using (DrawingContext dc = this.drawingGroup.Open())
            {

                dc.DrawRectangle(Brushes.Black, null, new Rect(x, y, RenderWidth, RenderHeight));

                for (int i = 0; i < animData.GetLength(1); i++)
                {
                    Point p = new Point(animData[frame, i].X * scaling, animData[frame, i].Y * scaling * inverse);
                    // Check for occlusion
                    if (p.X < x || p.X > RenderWidth || p.Y < y || p.Y > RenderHeight)
                        continue;
                    dc.DrawEllipse(Brushes.Green, null, p, JointThickness, JointThickness);
                }
            }
        }

        internal void println(string p)
        {
            feedback.Text += p + "\n";
            feedback.ScrollToEnd();
        }
        // loads a textfile containing animation data and puts it in the skelPoints array.
        private void loadAnimation_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                openAnimationPath.Text = filename;

                player.load(filename);
            }
        }

        // plays the animation using drawing context.. or not
        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            player.onPlayClicked();
        }

        private void directionButton_Click(object sender, RoutedEventArgs e)
        {
            player.reverse();
        }


        private void AufnahmeStarten_Click(object sender, RoutedEventArgs e)
        {
            rec.onStartClick();
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            player.stop();
        }

        private void timeline_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!player.hasChangedValue())
                player.pause();
            else
                startDetection(player.getData(), (int)timeline.Value);
            if(player.getData() != null) renderSkeleton((int)timeline.Value, player.getData(), true);
        }

        private void FpsButton_Click(object sender, RoutedEventArgs e)
        {
            int fps = int.Parse(FPSText.Text);
            if (fps <= 0 || fps > 120) return;

            player.setFrameRate(fps);
        }
    }
}