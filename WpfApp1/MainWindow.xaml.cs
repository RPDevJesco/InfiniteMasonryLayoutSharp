using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MasonryDemo;
using MasonryLayout;

namespace WpfApp1 
{
    public partial class MainWindow : Window
    {
        private readonly MasonryLayout.MasonryLayout _masonryLayout;
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = (MainViewModel)DataContext;

            // Don't forget to initialize the layout.
            _masonryLayout = new MasonryLayout.MasonryLayout(new MasonryOptions
            {
                BaseUnit = 200,
                Gap = 10,
                PageSize = 24,
                VirtualizeBuffer = 2000
            });
            
            _masonryLayout.OnItemRender += MasonryLayout_OnItemRender;
            _masonryLayout.OnLayoutUpdated += MasonryLayout_OnLayoutUpdated;
            _viewModel.ItemsLoaded += ViewModel_ItemsLoaded;
            
            MasonryContainer.SizeChanged += (s, e) =>
            {
                _masonryLayout.UpdateContainerSize(MasonryContainer.ActualWidth, MasonryContainer.ActualHeight);
            };
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            
            // Update layout based on scroll position
            _masonryLayout.LayoutItems(e.VerticalOffset, scrollViewer.ViewportHeight);

            // Check if we need to load more items, threshold of 1000px
            if (scrollViewer.ScrollableHeight - e.VerticalOffset < 1000)
            {
                _viewModel.LoadMoreCommand.Execute(null);
            }
        }

        private void MasonryLayout_OnItemRender(MasonryItem item, ItemPosition position)
        {
            var image = MasonryContainer.Children.Cast<UIElement>()
                .FirstOrDefault(x => x.Uid == item.Id) as Image;

            if (image == null)
            {
                image = new Image
                {
                    Uid = item.Id,
                    Stretch = System.Windows.Media.Stretch.UniformToFill,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                
                image.MouseLeftButtonUp += (s, e) => OpenModal(item.ImageSource);

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(item.ImageSource, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    image.Source = bitmap;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load image: {ex.Message}");
                    return;
                }

                MasonryContainer.Children.Add(image);
            }

            Canvas.SetLeft(image, position.Left);
            Canvas.SetTop(image, position.Top);
            image.Width = position.Width;
            image.Height = position.Height;
        }

        private void MasonryLayout_OnLayoutUpdated(object sender, EventArgs e)
        {
            MasonryContainer.Height = _masonryLayout.ContainerHeight;
        }

        private void ViewModel_ItemsLoaded(object sender, IEnumerable<MasonryItem> items)
        {
            _masonryLayout.AddItems(items);
        }

        private void OpenModal(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                
                ModalImage.Source = bitmap;
                ImageModal.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load full-size image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseModal_Click(object sender, RoutedEventArgs e)
        {
            ImageModal.Visibility = Visibility.Collapsed;
        }
    }
}