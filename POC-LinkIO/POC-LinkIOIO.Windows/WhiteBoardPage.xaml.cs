using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Media.Capture;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Media.MediaProperties;
using System.ComponentModel;
using Windows.UI.Xaml.Controls.Primitives;
using link.io.csharp.model;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace POC_LinkIO
{
    public sealed partial class WhiteBoardPage : Page, INotifyPropertyChanged
    {
        private link.io.csharp.LinkIO lio;

        private CanvasInteraction canvasInteraction;

        private Color drawingColor;
        private int drawingThickness;
        private Point lastPoint;
        private Boolean isDrawing;

        private User currentUser;
        private String previousAuthor;
        private List<String> users;


        public WhiteBoardPage()
        {
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

            previousAuthor = "";
            users = new List<String>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            lio = e.Parameter as link.io.csharp.LinkIO;
        }

        private void PageLoaded(object sender, RoutedEventArgs r)
        {
            // Config the connect.io instance
            currentUser = lio.getCurrentUser();

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
                    Dictionary<string, dynamic> data = e.get<Dictionary<string, dynamic>>();
                    String str = data["img"];
                    Byte[] imgBytes = Convert.FromBase64String(str.Split(',')[1]);
                    Size size = new Size(data["w"], data["h"]);
                    Point position = new Point(data["x"], data["y"]);

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
                    Dictionary<string, dynamic> data = e.get<Dictionary<string, dynamic>>();
                    Point fromPoint = new Point(data["fromX"] * Canvas.Width, data["fromY"] * Canvas.Height);
                    Point toPoint = new Point(data["toX"] * Canvas.Width, data["toY"] * Canvas.Height);
                    int thickness = data.ContainsKey("thinckness") ? data["thickness"] : 5;
                    canvasInteraction.DrawLine(fromPoint, toPoint, data["color"], thickness);
                });

            });

            lio.on("message", async (o) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Event e = (Event)o;
                    Dictionary<string, dynamic> data = e.get<Dictionary<string, dynamic>>();
                    WriteMessage(data["author"], data["text"], false);
                });

            });

            lio.onUserInRoomChanged(async (o) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    List<String> usersConnected = new List<String>();
                    foreach (User user in o)
                    {
                        usersConnected.Add(user.Mail);
                    }
                    usersConnected.RemoveAll(item => users.Contains(item));
                    foreach (String user in usersConnected)
                    {
                        WriteMessage(null, "\n" + user + " is now connected", false);
                    }

                    List<String> usersDisconnected = users;
                    foreach(User user in o)
                    {
                        if (usersDisconnected.Contains(user.Mail))
                        {
                            usersDisconnected.Remove(user.Mail);
                        }
                    }
                    foreach (String user in usersDisconnected)
                    {
                        WriteMessage(null, "\n" + user + " is now disconnected", false);
                    }

                    foreach (User user in o)
                    {
                        if (!users.Contains(user.Mail))
                        {
                            users.Add(user.Mail);
                        }
                    }
                });
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
                    author = currentUser.Mail,
                    text = TchatText.Text
                };

                lio.send("message", message, false);
                WriteMessage(currentUser.Mail, TchatText.Text, true);
                TchatText.Text = "";
            }
        }

        private void WriteMessage(string author, string text, bool me)
        {
            if (author == null)
            {
                Grid grid = new Grid()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(3)
                };

                TextBlock block = new TextBlock()
                {
                    FontSize = 15,
                    Foreground = new SolidColorBrush(Colors.Black),
                    Text = text
                };

                grid.Children.Add(block);
                Tchat.Children.Add(grid);

                previousAuthor = "";
            }
            else
            {
                if (me)
                {
                    Grid grid = new Grid()
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(3)
                    };

                    TextBlock textblock = new TextBlock()
                    {
                        FontSize = 15,
                        Foreground = new SolidColorBrush(Colors.Black),
                        TextWrapping = TextWrapping.Wrap,
                        Text = text
                    };

                    Border rectangle = new Border()
                    {
                        BorderBrush = new SolidColorBrush(Color.FromArgb(255, 188, 199, 214)),
                        Background = new SolidColorBrush(Color.FromArgb(255, 224, 237, 255)),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(3),
                        Child = textblock
                    };

                    grid.Children.Add(rectangle);
                    Tchat.Children.Add(grid);
                }
                else
                {
                    if (!previousAuthor.Equals(author))
                    {
                        Grid grid2 = new Grid()
                        {
                            HorizontalAlignment = HorizontalAlignment.Left
                        };

                        TextBlock authorblock = new TextBlock()
                        {
                            FontSize = 15,
                            Foreground = new SolidColorBrush(Colors.Black),
                            Text = author
                        };

                        grid2.Children.Add(authorblock);
                        Tchat.Children.Add(grid2);
                    }


                    Grid grid = new Grid()
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(3, 0, 3, 3)
                    };

                    TextBlock textblock = new TextBlock()
                    {
                        FontSize = 15,
                        Foreground = new SolidColorBrush(Colors.Black),
                        TextWrapping = TextWrapping.Wrap,
                        Text = text
                    };

                    Border rectangle = new Border()
                    {
                        BorderBrush = new SolidColorBrush(Color.FromArgb(255, 188, 199, 214)),
                        Background = new SolidColorBrush(Color.FromArgb(255, 254, 254, 254)),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(3),
                        Child = textblock
                    };

                    grid.Children.Add(rectangle);
                    Tchat.Children.Add(grid);
                }

                previousAuthor = author;
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
