//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Windows;
namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Microsoft.Win32;
    using System.Globalization;
    using System.Windows.Threading;

    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        /// Width of output drawing
        private const float RenderWidth = 640.0f;

        /// Height of our output drawing
        private const float RenderHeight = 480.0f;

        /// Thickness of drawn joint lines
        private const double JointThickness = 3;


        /// Brush used for drawing joints that are currently tracked
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));


        /// Active Kinect sensor
        private KinectSensor sensor;


        /// Drawing group for skeleton rendering output
        private DrawingGroup drawingGroup;


        /// Drawing image that we will display
        private DrawingImage imageSource;

        private Point[,] skelPoints;


        private JointType[] types = { JointType.Head,
                                      JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.ShoulderRight, 
                                      JointType.ElbowLeft, JointType.ElbowRight,
                                      JointType.WristLeft, JointType.WristRight, 
                                      JointType.Spine, 
                                      JointType.HipCenter, JointType.HipLeft, JointType.HipRight,
                                      JointType.KneeLeft, JointType.KneeRight,
                                      JointType.AnkleLeft, JointType.AnkleRight };

        private int currentFrame = -1;
        private DispatcherTimer dispatcherTimer;
        private bool forward = true;

        /// Initializes a new instance of the MainWindow class.
        public MainWindow()
        {
            InitializeComponent();
        }



        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            combobox.Items.Add("Seilspringen");
            combobox.Items.Add("Hampelmann");
            combobox.SelectedItem = "Seilspringen";
            feedback.Text = "PJSSkeleton Application\n -----------------------";
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
                    feedback.Text = feedback.Text + "\nKinect erkannt";
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                feedback.Text = feedback.Text + "\nKeine Kinect erkannt";
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

        public void FrameReadReady(object sender, EventArgs e)
        {

            if (forward)
                ++currentFrame;
            else
                --currentFrame;

                timeline.Value = currentFrame;

            if (currentFrame < skelPoints.GetLength(0) - 2 && currentFrame >= 0) // skip last frames
            {
                    // Note: draw frame
                    renderSkeleton(currentFrame, skelPoints);
            }
            else
            {
                playButton.Content = "Play";
                dispatcherTimer.Stop();
                dispatcherTimer = null;
                if (forward)
                {
                    currentFrame = -1;
                }
                else
                {
                    currentFrame = skelPoints.GetLength(0) - 1;
                }
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
                            // output to file
                            if (aufnahme)
                            {
                                file.WriteLine(frame);
                            }

                        }
                        // Note: draw frame here
                        renderSkeleton(currentFrame, this.SkeletonToScreen(copySkel));
                    }
                }
            }
        }

        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        private Point[,] SkeletonToScreen(SkeletonPoint[] skel)
        {
            // Convert point to depth space.  
            Point[,] retArr = new Point[1,skel.Length];
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            for (int i =0; i < skel.Length; i++) {
                DepthImagePoint depth = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skel[i], DepthImageFormat.Resolution640x480Fps30);
                retArr[0, i] = new Point(depth.X, depth.Y);
            }
            return retArr;
        }

        private Skeleton[] skeletonData;
        StreamWriter file;
        Boolean aufnahme = false;
        private void AufnahmeStarten_Click(object sender, RoutedEventArgs e)
        {
            if (AufnahmeStarten.Content.Equals("\nAufnahme starten"))
            {
                String uebergabe = dateiname.Text;
                if (uebergabe.Equals(""))
                {
                    feedback.Text = feedback.Text + "\nBitte geben Sie einen Dateinamen an";
                }
                else
                {
                    file = new StreamWriter(uebergabe + combobox.SelectedValue + ".txt");
                    AufnahmeStarten.Content = "\nAufnahme beenden";
                    aufnahme = true;
                }
            }
            else
            {
                AufnahmeStarten.Content = "Aufnahme anhalten";
            }
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

                StreamReader stream = new StreamReader(filename);

                string framesString = string.Empty;

                while (!stream.EndOfStream)
                {
                    framesString += stream.ReadLine() + ";";
                }

                string[] frames = framesString.Split(';');

                skelPoints = new Point[frames.Length - 1, types.Length];
                for (int i = 0; i < frames.Length - 1; i++)
                {
                    string[] points = frames[i].Split(',');
                    for (int j = 0, k = 0; j + 2 < points.Length; k++, j += 3)
                    {
                        skelPoints[i, k] = new Point(double.Parse(points[j].Replace('.', ',')), -double.Parse(points[j + 1].Replace('.', ',')));
                    }
                }

                timeline.Maximum = skelPoints.GetLength(1);

            }
        }


        // plays the animation using drawing context.. or not
        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (playButton.Content.Equals("Play") && skelPoints != null)
            {
                if (dispatcherTimer == null)
                {
                    dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                    dispatcherTimer.Tick += new EventHandler(FrameReadReady);
                    dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
                }

                dispatcherTimer.Start();
                playButton.Content = "Pause";
            }
            else
            {
                if (dispatcherTimer != null)
                {
                    dispatcherTimer.Stop();
                }
                playButton.Content = "Play";
            }
        }

        private void directionButton_Click(object sender, RoutedEventArgs e)
        {
            if (forward)
            {
                directionButton.Content = "<";
                forward = false;
            }
            else
            {
                directionButton.Content = ">";
                forward = true;
            }
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            if (dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
                dispatcherTimer = null;
            }

            playButton.Content = "Play";

            if (forward)
            {
                currentFrame = -1;
            }
            else
            {
                currentFrame = skelPoints.GetLength(0) - 1;
            }
        }

        /**
         * Method for painting the canvas. Leave all the paint stuff to me!
         * */
        private void renderSkeleton(int frame, Point[,] animData)
        {
            // Define parameters for painting:
            int scaling = 200;  // Scale skeleton points
            int widthTop = (int)RenderWidth / 2;
            int heightTop = (int)RenderHeight / 2;
            // Do the actual painting:
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(-widthTop, -heightTop, widthTop*2, heightTop*2));
                for (int i = 0; i < animData.GetLength(1); i++)
                {
                    Point p = new Point(animData[frame, i].X * scaling, animData[frame, i].Y * scaling);
                    // Check for occlusion
                    if (p.X < -widthTop || p.X > widthTop * 2 || p.Y < -heightTop || p.Y > heightTop * 2)
                        continue;
                    dc.DrawEllipse(Brushes.Green, null, p, JointThickness, JointThickness);
                }
            }
        }
    }
}