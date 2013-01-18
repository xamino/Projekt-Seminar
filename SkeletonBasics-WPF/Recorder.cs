using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;

namespace PJS.Skeleton
{
    class Recorder
    {
        StreamWriter file;
        bool aufnahme = false;

        private MainWindow mainWindow;

        const string START_RECORDING = "Aufnahme starten";
        const string STOP_RECORDING = "Aufnahme beenden";
        const string TXT_FORMAT = ".txt";
        const string FILE_NAME_EMPTY = "Bitte geben Sie einen Dateinamen an";

        public Recorder(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }


        internal void onStartClick()
        {
            if (mainWindow.AufnahmeStarten.Content.Equals(START_RECORDING))
            {
                string uebergabe = mainWindow.dateiname.Text;
                if (uebergabe.Equals(string.Empty) || uebergabe.Equals("Dateiname"))
                {
                    MessageBox.Show(FILE_NAME_EMPTY,"Hinweis", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {

                    if (file != null) // fix for file overwrite
                    {
                        file.Close();
                    }

                    file = new StreamWriter(uebergabe + mainWindow.combobox.SelectedValue + mainWindow.weight.Text + TXT_FORMAT);
                    mainWindow.AufnahmeStarten.Content = STOP_RECORDING;
                    aufnahme = true;
                }
            }
            else
            {
                stopRecording();
            }
        }


        internal bool isRecording()
        {
            return aufnahme;
        }

        internal void record(string frame)
        {
            file.WriteLine(frame);
        }

        internal void stopRecording()
        {
            mainWindow.AufnahmeStarten.Content = START_RECORDING;
            aufnahme = false;
            if (file != null)
            {
                file.Close();
            }
        }
    }
}
