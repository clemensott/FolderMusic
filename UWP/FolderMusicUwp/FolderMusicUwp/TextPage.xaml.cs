using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace FolderMusicUwp
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class TextPage : Page
    {
        public TextPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshText();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            RefreshText();
        }

        private async void RefreshText()
        {
            List<Task<Eintrag>> tasks = new List<Task<Eintrag>>();
            List<Eintrag> einträge = new List<Eintrag>();

            for (uint i = 0; i < 1000; i++)
            {
                string filename = string.Format("Text{0}.txt", i);
                string path = ApplicationData.Current.LocalFolder.Path + "\\" + filename;

                tasks.Add(Eintrag.Get(path));
            }

            foreach (Task<Eintrag> task in tasks)
            {
                var eintrag = await task;

                if (eintrag != null) einträge.Add(eintrag);
            }

            einträge = einträge.OrderBy(x => x.Time.Ticks).ToList();
            tblText.Text = "";

            foreach (Eintrag eintrag in einträge)
            {
                tblText.Text += eintrag.Text + "\n";
            }
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

            private Eintrag() { }

            public async static Task<Eintrag> Get(string path)
            {
                try
                {
                    string text = await PathIO.ReadTextAsync(path);
                    Eintrag eintrag = new Eintrag();

                    string[] parts = text.Split(';');

                    foreach (string part in parts)
                    {
                        long value;

                        if (long.TryParse(part, out value) && value > 1000000000000)
                        {
                            eintrag.Text += eintrag.GetDateTimeString(value);
                        }
                        else if (part != "") eintrag.Text += part;

                        eintrag.Text += "\n";
                    }

                    return eintrag;
                }
                catch { }

                return null;
            }

            private string GetDateTimeString(long ticks)
            {
                var dateTime = new DateTime(ticks);

                if (Time.Ticks == 0) Time = dateTime;

                return string.Format("{0,2}.{1,2}.{2,4}", dateTime.Day, dateTime.Month, dateTime.Year).Replace(" ", "0")
                    + " " + string.Format("{0,2}:{1,2}:{2,2},{3,3}", dateTime.Hour, dateTime.Minute,
                    dateTime.Second, dateTime.Millisecond).Replace(" ", "0");
            }
        }
    }
}
