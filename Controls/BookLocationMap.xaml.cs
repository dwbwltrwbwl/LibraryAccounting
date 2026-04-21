using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibraryAccounting.Models
{
    /// <summary>
    /// Логика взаимодействия для BookLocationMap.xaml
    /// </summary>
    public partial class BookLocationMap : UserControl
    {
        private Dictionary<string, Dictionary<string, BookLocationInfo>> _currentMapData;

        public BookLocationMap()
        {
            InitializeComponent();
        }

        public void ShowBookLocation(string bookTitle, string shelfCode, string rowName)
        {
            BookInfoText.Text = $"🔍 Книга: {bookTitle} | Место: стеллаж {shelfCode}, {rowName}";
            HighlightLocation(shelfCode, rowName);
        }

        public void ClearHighlight()
        {
            BookInfoText.Text = "";
            if (_currentMapData != null)
            {
                UpdateShelvesMap(_currentMapData);
            }
        }

        public void UpdateShelvesMap(Dictionary<string, Dictionary<string, BookLocationInfo>> booksLocation)
        {
            _currentMapData = booksLocation;
            var shelves = new List<ShelfViewModel>();

            foreach (var shelf in booksLocation)
            {
                var rows = new List<RowViewModel>();

                foreach (var row in shelf.Value)
                {
                    var style = (Style)FindResource("ShelfStyle");
                    var toolTip = $"📚 Стеллаж {shelf.Key}, {row.Key}\n";
                    var bookCount = 0;
                    var booksList = new List<BookInfo>();

                    if (!string.IsNullOrEmpty(row.Value.ZoneName))
                    {
                        toolTip += $"📍 Зона: {row.Value.ZoneName}\n";
                    }
                    toolTip += "━━━━━━━━━━━━━━━━━━━━━\n";

                    if (row.Value.Books.Count > 0)
                    {
                        style = (Style)FindResource("ShelfWithBookStyle");
                        bookCount = row.Value.Books.Count;
                        booksList.AddRange(row.Value.Books);

                        toolTip += $"📖 ВЫДАНО книг: {row.Value.Books.Count}\n";
                        toolTip += "━━━━━━━━━━━━━━━━━━━━━\n";

                        foreach (var book in row.Value.Books)
                        {
                            toolTip += $"📕 {book.Title}\n";
                            if (!string.IsNullOrEmpty(book.Author))
                                toolTip += $"   Автор: {book.Author}\n";
                            toolTip += $"   Читатель: {book.Reader}\n";
                            toolTip += $"   Должна быть: {book.DueDate}\n";
                            if (book.IsOverdue)
                                toolTip += $"   ⚠️ ПРОСРОЧЕНА!\n";
                            toolTip += "━━━━━━━━━━━━━━━━━━━━━\n";
                        }
                    }

                    if (row.Value.AvailableBooksCount > 0)
                    {
                        toolTip += $"✅ В БИБЛИОТЕКЕ: {row.Value.AvailableBooksCount} шт.\n";
                        toolTip += "━━━━━━━━━━━━━━━━━━━━━\n";
                    }

                    if (row.Value.Books.Count == 0 && row.Value.AvailableBooksCount == 0)
                    {
                        toolTip += "✅ Полка свободна";
                    }

                    if (row.Value.HasOverdue)
                    {
                        if (FindResource("OverdueShelfStyle") is Style overdueStyle)
                            style = overdueStyle;
                    }
                    else if (row.Value.Books.Count > 0)
                    {
                        style = (Style)FindResource("ShelfWithBookStyle");
                    }

                    rows.Add(new RowViewModel
                    {
                        RowNumber = row.Key,
                        Style = style,
                        ToolTip = toolTip,
                        BookCount = bookCount,
                        HasOverdue = row.Value.HasOverdue,
                        Books = booksList,
                        ShelfName = shelf.Key,
                        AvailableBooksCount = row.Value.AvailableBooksCount,
                        ZoneName = row.Value.ZoneName
                    });
                }

                shelves.Add(new ShelfViewModel
                {
                    ShelfName = shelf.Key,
                    Rows = rows.OrderBy(r => r.RowNumber).ToList()
                });
            }

            ShelvesMapItemsControl.ItemsSource = shelves.OrderBy(s => s.ShelfName).ToList();
        }

        public void HighlightLocation(string shelfCode, string rowName)
        {
            ClearHighlight();

            var itemsControl = ShelvesMapItemsControl;
            var shelfContainers = FindVisualChildren<ContentPresenter>(itemsControl);

            foreach (var container in shelfContainers)
            {
                var shelfData = container.Content as ShelfViewModel;
                if (shelfData != null && shelfData.ShelfName == shelfCode)
                {
                    var rowsPanel = FindVisualChild<ItemsControl>(container);
                    if (rowsPanel != null)
                    {
                        var rowContainers = FindVisualChildren<ContentPresenter>(rowsPanel);
                        foreach (var rowContainer in rowContainers)
                        {
                            var rowData = rowContainer.Content as RowViewModel;
                            if (rowData != null && rowData.RowNumber == rowName)
                            {
                                var border = FindVisualChild<Border>(rowContainer);
                                if (border != null)
                                {
                                    border.Background = new SolidColorBrush(Color.FromRgb(255, 235, 150));
                                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                                    border.BorderThickness = new Thickness(3);

                                    BookInfoText.Text = $"📍 НАЙДЕНО: Стеллаж {shelfCode}, {rowName}";
                                }
                                break;
                            }
                        }
                    }
                    break;
                }
            }
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    yield return result;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            return FindVisualChildren<T>(parent).FirstOrDefault();
        }

        private void Shelf_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.ToolTip != null)
            {
                var customDialog = new Window
                {
                    Title = "Информация о полке",
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = border.ToolTip.ToString(),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(10)
                        },
                        MaxHeight = 400,
                        Width = 350
                    },
                    Width = 400,
                    Height = 450,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = Brushes.White,
                    Owner = Window.GetWindow(this)
                };
                customDialog.ShowDialog();
            }
        }

        public void ShowFoundBooks(List<BookLocationMatch> foundBooks)
        {
            if (_currentMapData == null) return;

            var filteredData = new Dictionary<string, Dictionary<string, BookLocationInfo>>();

            foreach (var shelf in _currentMapData)
            {
                var matchedRows = new Dictionary<string, BookLocationInfo>();

                foreach (var row in shelf.Value)
                {
                    bool hasMatch = row.Value.Books.Any(b =>
                        foundBooks.Any(f => f.BookTitle == b.Title && f.Shelf == shelf.Key && f.Row == row.Key));

                    if (hasMatch)
                    {
                        matchedRows[row.Key] = row.Value;
                    }
                }

                if (matchedRows.Any())
                {
                    filteredData[shelf.Key] = matchedRows;
                }
            }

            UpdateShelvesMap(filteredData);

            if (foundBooks.Any())
            {
                var first = foundBooks.First();
                HighlightLocation(first.Shelf, first.Row);
                BookInfoText.Text = $"🔍 Найдено книг: {foundBooks.Count}";
            }
        }
    }

    public class ShelfViewModel
    {
        public string ShelfName { get; set; }
        public List<RowViewModel> Rows { get; set; }
    }

    public class RowViewModel
    {
        public string RowNumber { get; set; }
        public Style Style { get; set; }
        public string ToolTip { get; set; }
        public int BookCount { get; set; }
        public bool HasOverdue { get; set; }
        public List<BookInfo> Books { get; set; } = new List<BookInfo>();
        public string ShelfName { get; set; }
        public int AvailableBooksCount { get; set; }
        public string ZoneName { get; set; }
    }

    public class BookLocationInfo
    {
        public string ZoneName { get; set; }
        public string ShelfNumber { get; set; }
        public string RowNumber { get; set; }
        public List<BookInfo> Books { get; set; } = new List<BookInfo>();
        public int AvailableBooksCount { get; set; } = 0;
        public bool HasOverdue => Books.Any(b => b.IsOverdue);
    }

    public class BookInfo
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public bool IsOverdue { get; set; }
        public string Reader { get; set; }
        public string DueDate { get; set; }
        public string Genre { get; set; }
        public string ZoneName { get; set; }
    }

    public class BookLocationMatch
    {
        public string BookTitle { get; set; }
        public string Shelf { get; set; }
        public string Row { get; set; }
    }
}