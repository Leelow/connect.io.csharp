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
using Windows.UI.Xaml.Controls.Primitives;

namespace POC_LinkIO
{
    public sealed partial class WhiteBoardPage : Page, INotifyPropertyChanged
    {
        private string login;
        private string server = "bastienbaret.com:8080";
        private LinkIOcsharp.LinkIO lio;

        private CanvasInteraction canvasInteraction;

        private Color drawingColor;
        private int drawingThickness;
        private Point lastPoint;
        private Boolean isDrawing;

        private List<String> users;


        public WhiteBoardPage()
        {
            Application.Current.DebugSettings.EnableFrameRateCounter = false;

            InitializeComponent();
            DataContext = this;

            canvasInteraction = new CanvasInteraction(Canvas);

            // Set color picker
            ColorPicker.colorChanged += (object sender, EventArgs args) =>
            {
                DrawingColor = ColorPicker.SelectedColor;
            };
            DrawingColor = ColorPicker.SelectedColor;
            

            // Set thickness picker
            int[] colorTab = {1,2,5,10,15,20,25};
            List<ThicknessClass> thicknessList = new List<ThicknessClass>();
            for (int i = 0; i < colorTab.Length; i++)
            {
                thicknessList.Add(new ThicknessClass(colorTab[i]));
            }
            ThicknessPicker.ItemsSource = thicknessList;
            DrawingThickness = 5;

            isDrawing = false;

            users = new List<String>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            login = e.Parameter as string;
        }

        private void PageLoaded(object sender, RoutedEventArgs r)
        {
            // Config the connect.io instance
            users.Add(login);
            lio = LinkIOImp.create().connectTo(server).withUser(login);

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
                    int thickness = e.containsKey("thinckness") ? e.get<int>("thickness") : 5;
                    canvasInteraction.DrawLine(fromPoint, toPoint, e.get<String>("color"), thickness);
                });

            });

            lio.on("message", async (o) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Event e = (Event)o;
                    Tchat.Text += "\n" + e.get<string>("author") + " : " + e.get<string>("text");
                });

            });

            lio.onUserInRoomChanged(async (o) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    List<String> usersConnected = new List<String>();
                    foreach (User user in o)
                    {
                        usersConnected.Add(user.Login);
                    }
                    usersConnected.RemoveAll(item => users.Contains(item));
                    foreach (String user in usersConnected)
                    {
                        Tchat.Text += "\n" + user + " is now connected";
                    }

                    List<String> usersDisconnected = users;
                    foreach(User user in o)
                    {
                        if (usersDisconnected.Contains(user.Login))
                        {
                            usersDisconnected.Remove(user.Login);
                        }
                    }
                    foreach (String user in usersDisconnected)
                    {
                        Tchat.Text += "\n" + user + " is now disconnected";
                    }

                    foreach (User user in o)
                    {
                        if (!users.Contains(user.Login))
                        {
                            users.Add(user.Login);
                        }
                    }
                });
            });


            // Connect to the server and join the "abcd" room
            lio.connect(() =>
            {
                lio.joinRoom("abcd", (string a, List<User> b) => { });
            });
        }

        /*public void updateUsersConnected(List<User> o)
        {
            String users = "";
            foreach (User user in o)
            {
                users += user.Login + "\n";
            }
            Users.Text = users.Substring(0, users.Length - 1);
        }*/


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
                    color = "#" + DrawingColor.ToString().Substring(3,6)
                };

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

        private void SendMessage(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && TchatText.Text != "")
            {
                Object message = new
                {
                    author = login,
                    text = TchatText.Text
                };

                lio.send("message", message, true);
                TchatText.Text = "";
            }
        }

        private void OnThumbDragStarted(object sender, DragStartedEventArgs args)
        {
            Thumb thumb = (Thumb)sender;
            Grid grid = (Grid)thumb.Parent;
            ColumnDefinition leftColDef = grid.ColumnDefinitions[0];
            ColumnDefinition rightColDef = grid.ColumnDefinitions[2];

            leftColDef.Width = new GridLength(leftColDef.ActualWidth, GridUnitType.Star);
            rightColDef.Width = new GridLength(rightColDef.ActualWidth, GridUnitType.Star);
        }

        private void OnThumbDragDelta(object sender, DragDeltaEventArgs args)
        {
            Thumb thumb = (Thumb)sender;
            Grid grid = (Grid)thumb.Parent;
            ColumnDefinition leftColDef = grid.ColumnDefinitions[0];
            ColumnDefinition rightColDef = grid.ColumnDefinitions[2];

            try
            {
                leftColDef.Width = new GridLength(leftColDef.Width.Value + args.HorizontalChange, GridUnitType.Star);
                rightColDef.Width = new GridLength(rightColDef.Width.Value - args.HorizontalChange, GridUnitType.Star);
            }
            catch (System.ArgumentException)
            {
            }
        }

        private void OnThumbPointerEntered(object sender, PointerRoutedEventArgs args)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 0);
        }

        private void OnThumbPointerExited(object sender, PointerRoutedEventArgs args)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
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
