using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.UI;

using LinkIOcsharp;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Media.Capture;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Media.MediaProperties;
using LinkIOcsharp.model;
using System.ComponentModel;

namespace POC_LinkIO
{
    public sealed partial class WhiteBoardPage : Page, INotifyPropertyChanged
    {
        private string login;
        private Color drawingColor;
        private int drawingThickness;

        private CanvasInteraction canvasInteraction;
        private Point lastPoint;
        private Boolean isDrawing;
        private LinkIOcsharp.LinkIO lio;

        

        /*public void PhotoTaken()
        {

        }*/

        public WhiteBoardPage()
        {
            Application.Current.DebugSettings.EnableFrameRateCounter = false;

            this.InitializeComponent();
            DataContext = this;

            canvasInteraction = new CanvasInteraction(Canvas);

            // Set color picker
            ColorPicker.colorChanged += (object sender, EventArgs args) =>
            {
                DrawingColor = ColorPicker.SelectedColor;
            };
            DrawingColor = ColorPicker.SelectedColor;
            

            // Set thickness picker
            List<ThicknessClass> thicknessList = new List<ThicknessClass>();
            thicknessList.Add(new ThicknessClass(1));
            thicknessList.Add(new ThicknessClass(2));
            thicknessList.Add(new ThicknessClass(5));
            thicknessList.Add(new ThicknessClass(10));
            ThicknessPicker.ItemsSource = thicknessList;
            DrawingThickness = 5;

            isDrawing = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            login = e.Parameter as string;
        }

        private void PageLoaded(object sender, RoutedEventArgs r)
        {
            // Config the connect.io instance
            lio = LinkIOImp.create()
                .connectTo("bastienbaret.com:8080")
                .withUser(login);

            lio.on("clear", async (o) =>
            {

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {

                    canvasInteraction.Clear();

                });

            });

            lio.on("image", async (o) =>
            {

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Event e = (Event)o;
                    String str = e.get<String>("img");
                    Byte[] imgBytes = Convert.FromBase64String(str.Split(',')[1]);
                    Size size = new Size(e.get<double>("w"), e.get<double>("h"));
                    Point position = new Point(e.get<double>("x"), e.get<double>("y"));

                    if (size.Width > 0 && size.Height > 0)
                    {
                        canvasInteraction.DrawImage(position, size, imgBytes);
                    }

                });

            });

            lio.on("line", async (o) =>
            {

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Event e = (Event)o;
                    Point fromPoint = new Point(e.get<double>("fromX") * Canvas.Width, e.get<double>("fromY") * Canvas.Height);
                    Point toPoint = new Point(e.get<double>("toX") * Canvas.Width, e.get<double>("toY") * Canvas.Height);
                    canvasInteraction.DrawLine(fromPoint, toPoint, e.get<String>("color"), DrawingThickness);
                });

            });

            lio.onUserInRoomChanged(async (o) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {

                    List<User> e = (List<User>)o;
                    String users = "";
                    foreach (User user in e)
                    {
                        users += user.Login + "\n";
                    }
                    Users.Text = users.Substring(0, users.Length - 1);

                });
            });

            // Connect to the server and join the "abcd" room
            lio.connect(() =>
            {
                //debug.Text = "e";
                lio.joinRoom("abcd", (string a, List<User> b) => { });
            });
        }

        public void pointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDrawing = true;
            lastPoint = e.GetCurrentPoint(Canvas).Position;

        }

        public void pointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // If we are drawing and we had the focus
            if (e.Pointer.IsInContact && isDrawing)
            {
                // Get the current point
                Point currentPoint = e.GetCurrentPoint(Canvas).Position;
                // Draw line
                canvasInteraction.DrawLine(lastPoint, currentPoint, DrawingColor, DrawingThickness);
                // Send line object
                Object lineObj = new
                {
                    fromX = lastPoint.X / Canvas.Width,
                    fromY = lastPoint.Y / Canvas.Height,
                    toX = currentPoint.X / Canvas.Width,
                    toY = currentPoint.Y / Canvas.Height,
                    color = DrawingColor.ToString()
                };

                /*<User> l = new List<User>();
                User u = new User();
                u.Login = "user1";
                u.ID = "/#JNjcUIcWHp-EhIJtAAAN";
                l.Add(u);
                cio.send("line", lineObj, l, false);*/
                lio.send("line", lineObj, false);

                // Update last point
                lastPoint = currentPoint;
            }
        }

        public void pointerReleased(object sender, PointerRoutedEventArgs p)
        {
            isDrawing = false;
        }

        public void pointerExited(object sender, PointerRoutedEventArgs p)
        {
            isDrawing = false;
        }

        private void appSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetTop(Canvas, 0);

            Canvas.Height = e.NewSize.Height;
            Canvas.Width = e.NewSize.Width;
        }

        private void ButtonClearClicked(object sender, RoutedEventArgs e)
        {
            lio.send("clear", null, true);
        }

        private async void ButtonPhotoClicked(object sender, RoutedEventArgs e)
        {
            var _MediaCapture = new MediaCapture();
            await _MediaCapture.InitializeAsync();

            var _Name = Guid.NewGuid().ToString();
            var _Opt = CreationCollisionOption.ReplaceExisting;
            var _File = await ApplicationData.Current.LocalFolder.CreateFileAsync(_Name, _Opt);

            var _ImageFormat = ImageEncodingProperties.CreatePng();
            await _MediaCapture.CapturePhotoToStorageFileAsync(_ImageFormat, _File);
            var _BitmapImage = new BitmapImage(new Uri(_File.Path));

            canvasInteraction.DrawImage(new Point(0.1, 0.1), new Size(0.1, 0.1), _BitmapImage);
        }

        public Color DrawingColor
        {
            get
            {
                return drawingColor;
            }
            set
            {
                drawingColor = value;
                OnPropertyChanged("DrawingColor");
            }
        }

        public int DrawingThickness
        {
            get
            {
                return drawingThickness;
            }
            set
            {
                drawingThickness = value;
                OnPropertyChanged("DrawingThickness");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class ThicknessClass
    {
        public ThicknessClass(int value)
        {
            Value = value;
        }

        public int Value
        {
            get;
            set;
        }
    }
}
