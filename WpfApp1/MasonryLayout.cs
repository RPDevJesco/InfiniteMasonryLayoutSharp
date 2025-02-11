namespace MasonryLayout
{
    /// <summary>
    /// Represents a pattern for item sizing in the masonry layout
    /// </summary>
    public class Pattern
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    /// <summary>
    /// Represents an item in the masonry layout
    /// </summary>
    public class MasonryItem
    {
        public string Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Pattern Pattern { get; set; }
        public string ImageSource { get; set; }
        public bool IsVisible { get; set; }
    }

    /// <summary>
    /// Represents the position of an item in the layout
    /// </summary>
    public class ItemPosition
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    /// <summary>
    /// Configuration options for the masonry layout
    /// </summary>
    public class MasonryOptions
    {
        public int VirtualizeBuffer { get; set; } = 2000;
        public int BaseUnit { get; set; } = 200;
        public int Gap { get; set; } = 10;
        public int PageSize { get; set; } = 24;
    }

    /// <summary>
    /// Delegate for item rendering events
    /// </summary>
    public delegate void ItemRenderEventHandler(MasonryItem item, ItemPosition position);

    /// <summary>
    /// Core masonry layout engine that can be used with various UI frameworks
    /// </summary>
    public class MasonryLayout
    {
        private readonly MasonryOptions _options;
        private readonly List<Pattern> _patterns;
        private readonly List<MasonryItem> _items;
        private int _currentPage;
        private bool _isLoading;
        private bool _hasMore;
        private double _containerWidth;
        private double _containerHeight;

        public event ItemRenderEventHandler OnItemRender;
        public event EventHandler OnLayoutUpdated;

        public MasonryLayout(MasonryOptions options = null)
        {
            _options = options ?? new MasonryOptions();
            _patterns = new List<Pattern>
            {
                new Pattern { Width = 2, Height = 1 }, // Horizontal rectangle
                new Pattern { Width = 1, Height = 2 }, // Vertical rectangle
                new Pattern { Width = 2, Height = 2 }, // Large square
                new Pattern { Width = 1, Height = 1 }  // Small square
            };
            _items = new List<MasonryItem>();
        }

        /// <summary>
        /// Updates the container dimensions and triggers a layout recalculation
        /// </summary>
        public void UpdateContainerSize(double width, double height)
        {
            _containerWidth = width;
            _containerHeight = height;
            LayoutItems();
        }

        /// <summary>
        /// Calculates the layout positions for all items
        /// </summary>
        private Dictionary<string, ItemPosition> CalculateLayout()
        {
            var positions = new Dictionary<string, ItemPosition>();
            var grid = new List<List<string>>();
            var cols = (int)Math.Floor(_containerWidth / (_options.BaseUnit + _options.Gap));

            int currentX = 0;
            int currentY = 0;

            foreach (var item in _items)
            {
                var pattern = item.Pattern;
                bool placed = false;

                while (!placed)
                {
                    if (currentX + pattern.Width <= cols)
                    {
                        bool fits = true;
                        for (int y = currentY; y < currentY + pattern.Height; y++)
                        {
                            while (grid.Count <= y)
                                grid.Add(new List<string>());

                            for (int x = currentX; x < currentX + pattern.Width; x++)
                            {
                                while (grid[y].Count <= x)
                                    grid[y].Add(null);

                                if (grid[y][x] != null)
                                {
                                    fits = false;
                                    break;
                                }
                            }
                            if (!fits) break;
                        }

                        if (fits)
                        {
                            for (int y = currentY; y < currentY + pattern.Height; y++)
                            {
                                for (int x = currentX; x < currentX + pattern.Width; x++)
                                {
                                    grid[y][x] = item.Id;
                                }
                            }

                            positions[item.Id] = new ItemPosition
                            {
                                Left = currentX * (_options.BaseUnit + _options.Gap),
                                Top = currentY * (_options.BaseUnit + _options.Gap),
                                Width = pattern.Width * _options.BaseUnit + (pattern.Width - 1) * _options.Gap,
                                Height = pattern.Height * _options.BaseUnit + (pattern.Height - 1) * _options.Gap
                            };
                            placed = true;
                        }
                    }

                    if (!placed)
                    {
                        currentX++;
                        if (currentX >= cols)
                        {
                            currentX = 0;
                            currentY++;
                        }
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// Updates the layout and triggers rendering of visible items
        /// </summary>
        public void LayoutItems(double scrollY = 0, double viewportHeight = 0)
        {
            var positions = CalculateLayout();
            var viewportTop = scrollY - _options.VirtualizeBuffer;
            var viewportBottom = scrollY + viewportHeight + _options.VirtualizeBuffer;

            foreach (var item in _items)
            {
                if (positions.TryGetValue(item.Id, out var position))
                {
                    bool shouldRender = position.Top < viewportBottom &&
                        position.Top + position.Height > viewportTop;

                    item.IsVisible = shouldRender;

                    if (shouldRender)
                    {
                        OnItemRender?.Invoke(item, position);
                    }
                }
            }

            // Update container height based on the last item's position
            if (positions.Any())
            {
                _containerHeight = positions.Values.Max(p => p.Top + p.Height) + _options.Gap;
            }

            OnLayoutUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Adds new items to the layout
        /// </summary>
        public void AddItems(IEnumerable<MasonryItem> newItems)
        {
            _items.AddRange(newItems);
            LayoutItems();
        }

        /// <summary>
        /// Clears all items from the layout
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _currentPage = 0;
            _hasMore = true;
            LayoutItems();
        }

        /// <summary>
        /// Gets the current container height
        /// </summary>
        public double ContainerHeight => _containerHeight;

        /// <summary>
        /// Gets whether more items can be loaded
        /// </summary>
        public bool HasMore => _hasMore;

        /// <summary>
        /// Gets whether items are currently being loaded
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// Sets the loading state
        /// </summary>
        public void SetLoading(bool loading)
        {
            _isLoading = loading;
        }

        /// <summary>
        /// Sets whether more items are available
        /// </summary>
        public void SetHasMore(bool hasMore)
        {
            _hasMore = hasMore;
        }
    }
}