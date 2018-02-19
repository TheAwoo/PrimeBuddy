using System;
using System.Windows;
using System.Drawing;
using System.IO;
using System.Threading;
using Tesseract;

namespace PrimeBuddy
{
    public partial class MainWindow : Window
    {
        FileSystemWatcher Watcher;
        StreamWriter LogFile;

        public MainWindow()
        {
            InitializeComponent();

            // Watch for screenshots
            Watcher = new FileSystemWatcher(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Warframe"));
            Watcher.Created += Watcher_Created;
            Watcher.EnableRaisingEvents = true;

            String logpath = "./primebuddy.log";

            // Load history
            using (StreamReader filereader = new StreamReader(new FileStream(logpath, FileMode.OpenOrCreate)))
            {
                foreach (string line in filereader.ReadToEnd().Split('\n'))
                    if (line.Trim() != String.Empty) ItemList.Items.Add(line.Trim());
            }

            // Keep history
            LogFile = new StreamWriter(new FileStream(logpath, FileMode.OpenOrCreate));
        }

        void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            getPrimeFromFile(new Uri(e.FullPath), e.Name);
        }

        protected void getPrimeFromFile(Uri imageUri, string filename)
        {
            // Wait for IO to complete and stuff
            // Required to let game save the file properly
            Thread.Sleep(500);

            // Crop image, save temporarily
            using (Bitmap image = new Bitmap(imageUri.AbsolutePath))
            {
                // TODO: Add the crop rect config to the option sheet when implementing
                Rectangle croprect = new Rectangle(757, 884, 406, 32);
                Bitmap temp = new Bitmap(croprect.Width, croprect.Height);
                Graphics g = Graphics.FromImage(temp);
                g.DrawImage(image, -croprect.X, -croprect.Y);
                temp.Save("./tempcrop.jpg");
            }

            // OCR
            using (TesseractEngine engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default))
            {
                // Tweaking some things for this use case
                engine.SetVariable("load_system_dawg", false);
                engine.SetVariable("load_freq_dawg", false);
                engine.SetVariable("user_words_suffix", "user-words");
                engine.SetVariable("tessedit_char_whitelist", "QWERTYUIOPASDFGHJKLZXCVBNM &");
                engine.SetVariable("tessedit_char_blacklist", ".,/=-^*()!@#$%\\][{}");
                engine.SetVariable("language_model_penalty_non_dict_word", 0.6);

                using (Pix image = Pix.LoadFromFile("./tempcrop.jpg"))
                {
                    // Scale up for additional reliability
                    Pix filteredimage = image.Scale(3.0f, 3.0f);

                    using (var page = engine.Process(filteredimage))
                    {
                        string text = page.GetText().Trim();

                        if (String.IsNullOrWhiteSpace(text))
                            text = "UNKNOWN";

                        Dispatcher.Invoke(() => 
                        {
                            ItemList.Items.Add(string.Format("{0} - {1}", filename, text));
                            LogFile.Write(string.Format("{0} - {1}" + Environment.NewLine, filename, text));
                            LogFile.Flush(); // To ensure no data gets lost, since i'm leaving the stream open
                        });
                    }
                }
            }

            // Cleanup!
            File.Delete("./tempcrop.jpg");
        }

        void onClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LogFile.Close();
        }
    }
}
