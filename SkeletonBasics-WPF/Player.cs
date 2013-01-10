using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.IO;

namespace PJS.Skeleton
{
    class Player
    {
        private MainWindow wnd;
        private DispatcherTimer dispatcherTimer;
        private int playDirection = 0; // -1 back, 0 still, 1 forward
        private bool changedValue = false;
        private Point[,] skelPoints;
        private TimeSpan frameTimeSpan = new TimeSpan(0, 0, 0, 0, (int)((1.0d / 30) * 1000));
        private int types;

        public Player(MainWindow mainWindow, int types)
        {
            this.types = types;
            this.wnd = mainWindow;
        }

        internal void onPlayClicked()
        {
            if (wnd.playButton.Content.Equals("Play") && skelPoints != null)
                play();
            else
                pause();
        }

        internal void setFrameRate(int fps){

            int milliseconds = (int)((1.0d / fps) * 1000);

            frameTimeSpan = new TimeSpan(0,0,0,0,milliseconds);

           if (dispatcherTimer != null)
            {
                dispatcherTimer.Interval = frameTimeSpan;
            }
        }

        internal void play()
        {
            if (skelPoints == null) return;

            if (wnd.directionButton.Content.Equals(">"))
                playDirection = 1;
            else
                playDirection = -1;

            if (dispatcherTimer == null)
            {
                dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(FrameReadReady);
                dispatcherTimer.Interval = frameTimeSpan;
            }

            if (dispatcherTimer != null)
                dispatcherTimer.Start();

            wnd.playButton.Content = "Pause";
        }

        internal void pause()
        {
            if (dispatcherTimer != null)
                dispatcherTimer.Stop();

            playDirection = 0;

            wnd.playButton.Content = "Play";
        }

        internal void stop()
        {
            if (dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
                dispatcherTimer = null;
            }

            wnd.playButton.Content = "Play";

            playDirection = 0;

            changedValue = true;
            if (wnd.directionButton.Content.Equals(">"))
                wnd.timeline.Value = 0;
            else
                wnd.timeline.Value = skelPoints.GetLength(0) - 1;
            changedValue = false;
        }

        internal void reverse()
        {
            if (playDirection != -1)
            {
                wnd.directionButton.Content = "<";
                playDirection = -1;
            }
            else
            {
                wnd.directionButton.Content = ">";
                playDirection = 1;
            }
        }

        public void FrameReadReady(object sender, EventArgs e)
        {
            changedValue = true;

            if (playDirection == 1)
                if (wnd.timeline.Value >= wnd.timeline.Maximum)
                    stop();
                else
                    ++wnd.timeline.Value;

            else if (playDirection == -1)
                if (wnd.timeline.Value <= wnd.timeline.Minimum)
                    stop();
                else
                    --wnd.timeline.Value;

            changedValue = false;
        }

        internal bool hasChangedValue()
        {
            return changedValue;
        }

        internal void load(string filename)
        {
            StreamReader stream = new StreamReader(filename);

            string framesString = string.Empty;

            while (!stream.EndOfStream)
            {
                framesString += stream.ReadLine() + ";";
            }

            string[] frames = framesString.Split(';');

            skelPoints = new Point[frames.Length - 1, types];
            for (int i = 0; i < frames.Length - 1; i++)
            {
                string[] points = frames[i].Split(',');
                for (int j = 0, k = 0; j + 2 < points.Length; k++, j += 3)
                {
                    skelPoints[i, k] = new Point(double.Parse(points[j].Replace('.', ',')), double.Parse(points[j + 1].Replace('.', ',')));
                }
            }

            wnd.timeline.Maximum = skelPoints.GetLength(0) - 2;
        }

        internal Point[,] getData()
        {
            return skelPoints;
        }
    }
}
