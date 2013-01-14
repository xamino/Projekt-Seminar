using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using Microsoft.Kinect;
using System.Windows;

namespace PJS.Skeleton
{
    class Detector
    {

        private bool cycleStart = false;
        private int cycle = 0;
        private bool cycleEnd = false;
        private DispatcherTimer cyleTimer;

        private MainWindow wnd;
        private bool timedOut;

        public Detector(MainWindow mainWindow)
        {
            this.wnd = mainWindow;
        }

        internal int getCyle()
        {
            return cycle;
        }

        internal void resetCycle()
        {
            cycleStart = false;
            cycleEnd = false;
            cycle = 0;
        }

        // uses frame informations from kinects skeleton and passes it to the detector
        internal void AnalyseFrame(Microsoft.Kinect.Skeleton skel)
        {
            SkeletonPoint head = skel.Joints[JointType.Head].Position;
            SkeletonPoint leftWrist = skel.Joints[JointType.WristLeft].Position;
            SkeletonPoint rightWrist = skel.Joints[JointType.WristRight].Position;
            SkeletonPoint leftFoot = skel.Joints[JointType.AnkleLeft].Position;
            SkeletonPoint rightFoot = skel.Joints[JointType.AnkleRight].Position;

            detect(SkeletonPointToPoint(head), SkeletonPointToPoint(leftWrist), SkeletonPointToPoint(rightWrist), SkeletonPointToPoint(leftFoot), SkeletonPointToPoint(rightFoot));

        }

        // uses frame information from a matrix and passes it to the detector
        internal void AnalyseFrame(Point[,] skel, int f)
        {
            Point head = skel[f, 0];
            Point leftWrist = skel[f, 6];
            Point rightWrist = skel[f, 7];
            Point leftFoot = skel[f, 14];
            Point rightFoot = skel[f, 15];

            detect(head, leftWrist, rightWrist, leftFoot, rightFoot);
        }

        // currently only defined for Hampelmann
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

            // Detect start of cycle
            //        / \
            //        \O/
            //         |
            //        / \
            //        \ /
            if (!cycleStart && armsUp && armsClose && feetClose) 
            {
                timedOut = false;

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

            // Detect end of cycle (following a start)
            //
            //     ___ O ___
            //         |
            //        / \
            //       /   \
            else if (cycleStart && !armsUp && !armsClose && !feetClose)
            {
                cycleEnd = true;
                cycleStart = false;
                // Console.WriteLine("End det"); 
                return;
            }

            // Start -> End -> Start sets both booleans to true
            // and one cycle has been completed
            if (cycleStart && cycleEnd)
            {
                cycleStart = false;
                cycleEnd = false;
                ++cycle;
                cyleTimer.Stop();
                wnd.println("Cycle " + cycle + " Detected.");
            }

        }

        // called when there has not been a new cycle detected for the time defined in the Timer.
        // stops the timer and resets the cycles
        private void CycleTimout(object sender, EventArgs e)
        {
            resetCycle();
            cyleTimer.Stop();
            timedOut = true;

        }

        internal bool IsTimedOut()
        {
            return timedOut;
        }


        private Point SkeletonPointToPoint(SkeletonPoint sp)
        {
            return new Point(sp.X, sp.Y);
        }

    }
}
