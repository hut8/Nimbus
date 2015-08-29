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
        protected ViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel = new ViewModel();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (SoundCloudMedia.ValidateUri(URL.Text))
            {
                var media = new SoundCloudMedia(URL.Text);
                media.ProcessStateChange += media_ProcessStateChange;
                await media.DiscoverData();
                await media.Download();
            }
            else
            {
                MessageBox.Show("That doesn't look like a valid SoundCloud track URL");
            }
        }

        void media_ProcessStateChange(bool obj)
        {
            _viewModel.IsProcessing = obj;
        }
    }
}
