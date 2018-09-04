﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=391641 dokumentiert.

namespace MusicAppTest
{
    public sealed partial class MainPage : Page
    {
        private List<Eintrag> einträge;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Wird aufgerufen, wenn diese Seite in einem Rahmen angezeigt werden soll.
        /// </summary>
        /// <param name="e">Ereignisdaten, die beschreiben, wie diese Seite erreicht wurde.
        /// Dieser Parameter wird normalerweise zum Konfigurieren der Seite verwendet.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Seite vorbereiten, um sie hier anzuzeigen.

            // TODO: Wenn Ihre Anwendung mehrere Seiten enthält, stellen Sie sicher, dass
            // die Hardware-Zurück-Taste behandelt wird, indem Sie das
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed-Ereignis registrieren.
            // Wenn Sie den NavigationHelper verwenden, der bei einigen Vorlagen zur Verfügung steht,
            // wird dieses Ereignis für Sie behandelt.
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CreateStartedFile();

            RefreshText();
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
        }

        private async void CreateStartedFile()
        {
            string filenameWithExtention = "Started.txt";
            string path = ApplicationData.Current.LocalFolder.Path + "\\" + filenameWithExtention;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            }
            catch
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync(filenameWithExtention);
            }
        }

        private void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            //throw new NotImplementedException();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            RefreshText();
        }

        private async void RefreshText()
        {
            einträge = new List<Eintrag>();

            for (uint i = 0; i < 1000; i++)
            {
                string filename = string.Format("Text{0}.txt", i);
                //await ReadAndAdd(i, filename);
            }

            einträge = einträge.OrderBy(x => x.Time.Ticks).ToList();
            tblText.Text = "";

            foreach (Eintrag eintrag in einträge)
            {
                tblText.Text += eintrag.Text;
            }

            try
            {
                string path = ApplicationData.Current.LocalFolder.Path + "\\" + "Started.txt";
                long startTicks = long.Parse(await PathIO.ReadTextAsync(path));

                tblText.Text += string.Format("Start:\n{0}", Eintrag.GetDateTimeString(startTicks));
            }
            catch { }

            try
            {
                string path = ApplicationData.Current.LocalFolder.Path + "\\" + "StartedBackup1.txt";
                long startTicks = long.Parse(await PathIO.ReadTextAsync(path));

                tblText.Text += string.Format("\nStartBackup1:\n{0}", Eintrag.GetDateTimeString(startTicks));
            }
            catch { }

            try
            {
                string path = ApplicationData.Current.LocalFolder.Path + "\\" + "StartedBackup2.txt";
                long startTicks = long.Parse(await PathIO.ReadTextAsync(path));

                tblText.Text += string.Format("\nStartBackup2:\n{0}", Eintrag.GetDateTimeString(startTicks));
            }
            catch { }
        }

        private async Task ReadAndAdd(uint no, string filename)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);
                string fileText = await PathIO.ReadTextAsync(file.Path);

                if (fileText != "") einträge.Add(new Eintrag(no, fileText));
            }
            catch { }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            for (uint i = 0; i < 1000; i++)
            {
                Delete(i);
            }
        }

        private async void Delete(uint i)
        {
            try
            {
                string filename = string.Format("Text{0}.txt", i);
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);

                await file.DeleteAsync();
            }
            catch { }
        }

        class Eintrag
        {
            public DateTime Time { get; private set; }

            public string Text { get; private set; }

            public Eintrag(uint no, string text)
            {
                Text = no.ToString() + ":\n";
                string[] parts = text.Split(';');

                foreach (string part in parts)
                {
                    long value;

                    if (long.TryParse(part, out value) && value > 1000000000000) Text += SetTimeAndGetDateTimeString(value);
                    else if (part != "") Text += part;

                    Text += "\n";
                }
            }

            private string SetTimeAndGetDateTimeString(long ticks)
            {
                var dateTime = new DateTime(ticks);

                if (Time.Ticks == 0) Time = dateTime;

                return string.Format("{0,2}.{1,2}.{2,4}", dateTime.Day, dateTime.Month, dateTime.Year).Replace(" ", "0")
                    + " " + string.Format("{0,2}:{1,2}:{2,2},{3,3}", dateTime.Hour, dateTime.Minute,
                    dateTime.Second, dateTime.Millisecond).Replace(" ", "0");
            }

            public static string GetDateTimeString(long ticks)
            {
                var dateTime = new DateTime(ticks);

                return string.Format("{0,2}.{1,2}.{2,4}", dateTime.Day, dateTime.Month, dateTime.Year).Replace(" ", "0")
                    + " " + string.Format("{0,2}:{1,2}:{2,2},{3,3}", dateTime.Hour, dateTime.Minute,
                    dateTime.Second, dateTime.Millisecond).Replace(" ", "0");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
        }
    }
}
