using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.Foundation;
using System.IO;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace POC_ConnectIO
{
    public class CanvasInteraction
    {

        private Canvas Canvas;

        public CanvasInteraction(Canvas c)
        {
            this.Canvas = c;
        }

        public void DrawLine(Point fromPoint, Point toPoint, String color)
        {
            byte r = Convert.ToByte(color.Substring(1, 2), 16);
            byte g = Convert.ToByte(color.Substring(3, 2), 16);
            byte b = Convert.ToByte(color.Substring(5, 2), 16);
            DrawLine(fromPoint, toPoint, Color.FromArgb(255, r, g, b));
        }

        public void DrawLine(Point fromPoint, Point toPoint, Color color)
        {

            Line line = new Line();
            SolidColorBrush brush = new SolidColorBrush();
            brush.Color = color;
            line.Stroke = brush;
            line.StrokeThickness = 5;
            line.X1 = fromPoint.X;
            line.Y1 = fromPoint.Y;
            line.X2 = toPoint.X;
            line.Y2 = toPoint.Y;
            Canvas.Children.Add(line);

        }

        public void DrawImage(Point center, Size s, BitmapImage image)
        {

            Image img = new Image();
            img.Source = (ImageSource)image;

            int imageHeight = image.PixelHeight == 0 ? 320 : image.PixelWidth;
            int imageWidth = image.PixelWidth == 0 ? 180 : image.PixelWidth;

            ScaleTransform resizeTransform = new ScaleTransform();
            resizeTransform.ScaleX = (s.Width / imageHeight) * Canvas.Width;
            resizeTransform.ScaleY = (s.Height / imageWidth) * Canvas.Height;

            TranslateTransform posTransform = new TranslateTransform();
            posTransform.X = center.X * Canvas.Width;
            posTransform.Y = center.Y * Canvas.Height;

            TransformGroup transform = new TransformGroup();
            transform.Children.Add(resizeTransform);
            transform.Children.Add(posTransform);

            img.RenderTransform = transform;

            Canvas.Children.Add(img);

        }

        public void DrawImage(Point center, Size s, Byte[] img_bytes)
        {

            InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream();
            DataWriter writer = new DataWriter(ms.GetOutputStreamAt(0));
            writer.WriteBytes(img_bytes);
            writer.StoreAsync().GetResults();
            BitmapImage image = new BitmapImage();
            image.SetSource(ms);

            DrawImage(center, s, image);

        }

        public void Clear()
        {
            Canvas.Children.Clear();
        }

    }
}
