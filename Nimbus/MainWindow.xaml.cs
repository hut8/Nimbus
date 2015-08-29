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
using WinForms = System.Windows.Forms;
using System.Diagnostics;

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
            _viewModel.DestinationDirectory = SoundCloudMedia.DefaultDownloadDirectory;
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (SoundCloudMedia.ValidateUri(_viewModel.Uri))
            {
                var media = new SoundCloudMedia(_viewModel.Uri, _viewModel.DestinationDirectory);
                media.ProcessStateChange += media_ProcessStateChange;
                media.TitleChange += media_TitleChange;
                await media.DiscoverData();
                await media.Download();
            }
            else
            {
                MessageBox.Show("That doesn't look like a valid SoundCloud track URL");
            }
        }

        void media_TitleChange(string obj)
        {
            _viewModel.Title = obj;
        }

        void media_ProcessStateChange(bool obj)
        {
            _viewModel.IsProcessing = obj;
        }

        private void DestinationPath_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            WinForms.DialogResult result = dialog.ShowDialog();
            if (result == WinForms.DialogResult.OK)
            {
                _viewModel.DestinationDirectory = dialog.SelectedPath;
            }
        }

        private void OpenDestination_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_viewModel.DestinationDirectory);
        }

    }
}
