﻿using System;
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
using System.Net;

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
            if (!SoundCloudMedia.ValidateUri(_viewModel.Uri))
            {
                MessageBox.Show("That doesn't look like a valid SoundCloud track URL");
                return;
            }

            var media = new SoundCloudMedia(_viewModel.Uri, _viewModel.DestinationDirectory);
            media.StateChange += media_ProcessStateChange;
            media.TitleChange += media_TitleChange;
            try
            {
                await media.Download(_webClient_DownloadProgressChanged);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Download error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void media_TitleChange(string obj)
        {
            _viewModel.Title = obj;
        }

        void media_ProcessStateChange(TrackState obj)
        {
            _viewModel.TrackState = obj;
        }

        void _webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _viewModel.TotalSize = e.TotalBytesToReceive;
            _viewModel.DownloadedSize = e.BytesReceived;
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
