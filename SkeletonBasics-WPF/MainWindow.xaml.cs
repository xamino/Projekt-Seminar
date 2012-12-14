﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Microsoft.Win32;
    using System.Globalization;

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


        /// Initializes a new instance of the MainWindow class.
        public MainWindow()
        {
            InitializeComponent();
        }


        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
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
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
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

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {

                            string frame = "";
                            foreach (var joint in types)
                            {
                                SkeletonPoint j = skel.Joints[joint].Position;
                                if (frame.Length != 0)
                                {
                                    frame += ",";
                                }

                                frame += j.X.ToString(CultureInfo.InvariantCulture) + "," + j.Y.ToString(CultureInfo.InvariantCulture) + "," + j.Z.ToString(CultureInfo.InvariantCulture);
                                // output to file
                                DrawJoint(dc, this.SkeletonPointToScreen(j));
                            }
                        }
                    }
                }
            }
        }

        private void DrawJoint(DrawingContext dc, Point p)
        {
            Brush drawBrush = this.trackedJointBrush;
            dc.DrawEllipse(drawBrush, null, p, JointThickness, JointThickness);
        }


        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
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

                skelPoints = new Point[frames.Length, types.Length * 3];
                for (int i = 0; i < frames.Length; i++)
                {

                    string[] points = frames[i].Split(',');
                    for (int j = 0; j + 2 < points.Length; j += 3)
                    {
                        skelPoints[i, j] = new Point(double.Parse(points[j].Replace('.', ',')) * 500, double.Parse(points[j + 1].Replace('.', ',')) * -500);
                    }
                }
            }
        }


        // plays the animation using drawing context.. or not
        private void playButton_Click(object sender, RoutedEventArgs e)
        {

            for (int i = 0; i < skelPoints.GetLength(0)-1; i++)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {

                    for (int j = 0; j < skelPoints.GetLength(1); j++)
                    {
                        DrawJoint(dc, skelPoints[i, j]);
                    }

                }

                //System.Threading.Thread.Sleep(10);
                // sleep geht nicht weil das bild noch nicht gezeichnet wurde -> blockiert ganzes programm.
            }
        }
    }
}