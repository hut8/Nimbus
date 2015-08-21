using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Win32;

namespace Nimbus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Title = "Save MP3",
                DefaultExt = ".mp3",
                OverwritePrompt = true,
                ValidateNames = true
            };
            bool? confirm = dialog.ShowDialog();
            if (!(confirm.HasValue && confirm.Value)) return;

            var media = new SoundCloudMedia(URL.Text);

            using (var destination = File.OpenWrite(dialog.FileName))
            {
                await media.Download(destination);
            }

        }
    }
}
