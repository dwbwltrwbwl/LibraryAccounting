using System;
using System.Collections.Generic;
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

namespace LibraryAccounting.Models
{
    /// <summary>
    /// Логика взаимодействия для BookLocationMap.xaml
    /// </summary>
    public partial class BookLocationMap : UserControl
    {
        public BookLocationMap()
        {
            InitializeComponent();
        }

        public void ShowBookLocation(string bookTitle, string shelf, string row)
        {
            BookInfoText.Text = $"Книга: {bookTitle} | Место: стеллаж {shelf}, полка {row}";
            HighlightLocation(shelf, row);
        }

        public void LoadShelvesMap(Dictionary<string, List<string>> shelvesData)
        {
            var shelves = new List<ShelfViewModel>();

            foreach (var shelf in shelvesData)
            {
                var rows = new List<RowViewModel>();
                foreach (var row in shelf.Value)
                {
                    rows.Add(new RowViewModel
                    {
                        RowNumber = row,
                        Style = (Style)FindResource("ShelfStyle"),
                        ToolTip = $"Стеллаж {shelf.Key}, полка {row} - свободно",
                        BookCount = 0
                    });
                }
                shelves.Add(new ShelfViewModel
                {
                    ShelfName = shelf.Key,
                    Rows = rows
                });
            }

            ShelvesMapItemsControl.ItemsSource = shelves;
        }

        public void UpdateShelvesMap(Dictionary<string, Dictionary<string, BookLocationInfo>> booksLocation)
        {
            var shelves = new List<ShelfViewModel>();

            foreach (var shelf in booksLocation)
            {
                var rows = new List<RowViewModel>();
                foreach (var row in shelf.Value)
                {
                    var style = (Style)FindResource("ShelfStyle");
                    var toolTip = $"Стеллаж {shelf.Key}, полка {row.Key} - свободно";
                    var bookCount = 0;
                    var hasOverdue = false;

                    if (row.Value.Books.Count > 0)
                    {
                        style = (Style)FindResource("ShelfWithBookStyle");
                        bookCount = row.Value.Books.Count;
                        toolTip = $"Стеллаж {shelf.Key}, полка {row.Key}\nКниги:\n" +
                                  string.Join("\n", row.Value.Books.Select(b => $"• {b.Title} ({(b.IsOverdue ? "ПРОСРОЧЕНА" : "выдана")})"));

                        if (row.Value.HasOverdue)
                        {
                            if (FindResource("OverdueShelfStyle") is Style overdueStyle)
                                style = overdueStyle;
                        }
                    }

                    rows.Add(new RowViewModel
                    {
                        RowNumber = row.Key,
                        Style = style,
                        ToolTip = toolTip,
                        BookCount = bookCount,
                        HasOverdue = row.Value.HasOverdue
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

        private void HighlightLocation(string shelf, string row)
        {
            // Поиск и подсветка нужной полки
            var itemsControl = ShelvesMapItemsControl;
            var shelfContainer = FindVisualChild<ContentPresenter>(itemsControl);
            // Реализация подсветки найденного места
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        private void Shelf_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.ToolTip != null)
            {
                MessageBox.Show(border.ToolTip.ToString(), "Информация о полке",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
    }

    public class BookLocationInfo
    {
        public List<BookInfo> Books { get; set; } = new List<BookInfo>();
        public bool HasOverdue => Books.Any(b => b.IsOverdue);
    }

    public class BookInfo
    {
        public string Title { get; set; }
        public bool IsOverdue { get; set; }
        public string Reader { get; set; }
        public string DueDate { get; set; }
    }
}
