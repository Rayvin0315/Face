using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FaceApiSample
{
    public partial class PicturesWindow : Window
    {
        private List<string> _pictureUrls;

        public PicturesWindow(List<string> pictureUrls)
        {
            InitializeComponent();
            _pictureUrls = pictureUrls;
            LoadPictures();
        }

        private void LoadPictures()
        {
            foreach (var url in _pictureUrls)
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(url)),
                    Width = 200,
                    Height = 150,
                    Margin = new Thickness(5)
                };
                PicturesWrapPanel.Children.Add(image);
            }
        }
    }
}

