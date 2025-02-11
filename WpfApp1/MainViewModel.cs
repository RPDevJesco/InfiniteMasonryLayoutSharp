using System.Windows.Input;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MasonryLayout;

namespace MasonryDemo
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly string _imageDirectory;
        private int _currentPage;
        private bool _isLoading;
        private bool _hasMore = true;

        public event EventHandler<IEnumerable<MasonryItem>> ItemsLoaded;
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand LoadMoreCommand { get; }
        public ICommand ClearCommand { get; }

        public MainViewModel()
        {
            _imageDirectory = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Images");
            LoadMoreCommand = new RelayCommand(async () => await LoadMoreAsync(), () => !IsLoading && HasMore);
            ClearCommand = new RelayCommand(Clear);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    ((RelayCommand)LoadMoreCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasMore
        {
            get => _hasMore;
            private set
            {
                if (_hasMore != value)
                {
                    _hasMore = value;
                    OnPropertyChanged();
                    ((RelayCommand)LoadMoreCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private async Task LoadMoreAsync()
        {
            if (IsLoading || !HasMore) return;

            IsLoading = true;
            try
            {
                var items = await Task.Run(() => FetchItems(_currentPage, 24));
                if (items.Any())
                {
                    _currentPage++;
                    ItemsLoaded?.Invoke(this, items);
                }
                else
                {
                    HasMore = false;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private IEnumerable<MasonryItem> FetchItems(int page, int pageSize)
        {
            var items = new List<MasonryItem>();
            var patterns = new[]
            {
                new Pattern { Width = 2, Height = 1 },
                new Pattern { Width = 1, Height = 2 },
                new Pattern { Width = 2, Height = 2 },
                new Pattern { Width = 1, Height = 1 }
            };

            for (int i = 0; i < pageSize; i++)
            {
                var imageIndex = page * pageSize + i;
                // should probably update this for all image formats, but i'm lazy.
                var imagePath = System.IO.Path.Combine(_imageDirectory, $"{imageIndex + 1}.png");

                if (File.Exists(imagePath))
                {
                    var pattern = patterns[i % patterns.Length];
                    items.Add(new MasonryItem
                    {
                        Id = $"{page}-{i}",
                        Pattern = pattern,
                        ImageSource = imagePath,
                        Width = pattern.Width * 200,
                        Height = pattern.Height * 200
                    });
                }
                else
                {
                    // If we can't find the image, we might have reached the end
                    break;
                }
            }

            return items;
        }

        private void Clear()
        {
            _currentPage = 0;
            HasMore = true;
            ItemsLoaded?.Invoke(this, Enumerable.Empty<MasonryItem>());
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged() => 
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}