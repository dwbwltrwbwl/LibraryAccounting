using LibraryAccounting.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace LibraryAccounting.Windows
{
    public partial class BookMapWindow : Window
    {
        private Dictionary<string, Dictionary<string, Models.BookLocationInfo>> _fullMapData;
        private string _currentZoneFilter = "Все зоны";

        public BookMapWindow()
        {
            InitializeComponent();
            LoadShelvesMap();
        }

        private string GetZoneByShelfCode(string shelfCode)
        {
            if (string.IsNullOrEmpty(shelfCode)) return "Не определена";

            char zoneLetter = char.ToUpper(shelfCode[0]);
            switch (zoneLetter)
            {
                case 'A': return "Художественная литература";
                case 'B': return "Научная литература";
                case 'C': return "Техническая литература";
                case 'D': return "Детская литература";
                case 'E': return "Периодика и справочники";
                case 'F': return "Иностранная литература";
                case 'G': return "Искусство";
                case 'H': return "История";
                case 'I': return "Философия и психология";
                case 'J': return "Право и экономика";
                default: return "Прочая литература";
            }
        }

        private void LoadShelvesMap()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Получаем все выданные книги (на руках у читателей) - сначала данные
                var issuedBooksRaw = AppConnect.model01.Loans
                    .Where(l => l.ReturnDate == null)
                    .Include(l => l.BookCopies)
                    .Include(l => l.BookCopies.Shelves)
                    .Include(l => l.BookCopies.Rows)
                    .Include(l => l.BookCopies.Books)
                    .Include(l => l.BookCopies.Books.Authors)
                    .Include(l => l.Readers)
                    .Select(l => new
                    {
                        ShelfId = l.BookCopies.ShelfId,
                        RowId = l.BookCopies.RowId,
                        ShelfCode = l.BookCopies.Shelves != null ? l.BookCopies.Shelves.ShelfCode : "",
                        RowNumber = l.BookCopies.Rows != null ? l.BookCopies.Rows.RowNumber : 0,
                        BookTitle = l.BookCopies.Books.Title,
                        Author = l.BookCopies.Books.Authors.FullName,
                        IsOverdue = l.DueDate < DateTime.Now,
                        Reader = l.Readers.last_name + " " + l.Readers.first_name + " " + (l.Readers.middle_name ?? ""),
                        DueDate = l.DueDate,
                        Genre = l.BookCopies.Books.Genres.Name
                    })
                    .ToList();

                // Применяем метод GetZoneByShelfCode после получения данных
                var issuedBooks = issuedBooksRaw.Select(b => new
                {
                    b.ShelfId,
                    b.RowId,
                    b.ShelfCode,
                    b.RowNumber,
                    b.BookTitle,
                    b.Author,
                    b.IsOverdue,
                    b.Reader,
                    b.DueDate,
                    b.Genre,
                    ZoneName = GetZoneByShelfCode(b.ShelfCode)
                }).ToList();

                // Получаем все книги в библиотеке (доступные)
                var availableBooksRaw = AppConnect.model01.BookCopies
                    .Where(c => c.Status == "Available")
                    .Include(c => c.Shelves)
                    .Include(c => c.Rows)
                    .Include(c => c.Books)
                    .Select(c => new
                    {
                        ShelfId = c.ShelfId,
                        RowId = c.RowId,
                        ShelfCode = c.Shelves != null ? c.Shelves.ShelfCode : "",
                        RowNumber = c.Rows != null ? c.Rows.RowNumber : 0,
                        BookTitle = c.Books.Title,
                        Author = c.Books.Authors.FullName,
                        Genre = c.Books.Genres.Name
                    })
                    .ToList();

                var availableBooks = availableBooksRaw.Select(b => new
                {
                    b.ShelfId,
                    b.RowId,
                    b.ShelfCode,
                    b.RowNumber,
                    b.BookTitle,
                    b.Author,
                    b.Genre,
                    ZoneName = GetZoneByShelfCode(b.ShelfCode)
                }).ToList();

                // Получаем все стеллажи из справочника
                var allShelves = AppConnect.model01.Shelves
                    .OrderBy(s => s.SortOrder)
                    .ToList();

                if (!allShelves.Any())
                {
                    MessageBox.Show("В базе данных нет стеллажей", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем структуру стеллажей
                var shelvesData = new Dictionary<string, Dictionary<string, Models.BookLocationInfo>>();

                foreach (var shelf in allShelves)
                {
                    // Получаем все ряды для этого стеллажа
                    var rowsForShelf = AppConnect.model01.Rows
                        .Where(r => r.ShelfId == shelf.ShelfId)
                        .OrderBy(r => r.RowNumber)
                        .ToList();

                    foreach (var row in rowsForShelf)
                    {
                        string rowName = $"Ряд {row.RowNumber}";

                        if (!shelvesData.ContainsKey(shelf.ShelfCode))
                            shelvesData[shelf.ShelfCode] = new Dictionary<string, Models.BookLocationInfo>();

                        if (!shelvesData[shelf.ShelfCode].ContainsKey(rowName))
                        {
                            shelvesData[shelf.ShelfCode][rowName] = new Models.BookLocationInfo
                            {
                                ZoneName = GetZoneByShelfCode(shelf.ShelfCode),
                                ShelfNumber = shelf.ShelfCode,
                                RowNumber = rowName
                            };
                        }
                    }
                }

                // Заполняем выданные книги
                foreach (var book in issuedBooks)
                {
                    string shelfCode = book.ShelfCode;
                    string rowName = $"Ряд {book.RowNumber}";

                    if (!string.IsNullOrEmpty(shelfCode) && shelvesData.ContainsKey(shelfCode) && shelvesData[shelfCode].ContainsKey(rowName))
                    {
                        shelvesData[shelfCode][rowName].Books.Add(new Models.BookInfo
                        {
                            Title = book.BookTitle,
                            Author = book.Author,
                            IsOverdue = book.IsOverdue,
                            Reader = book.Reader,
                            DueDate = book.DueDate.ToString("dd.MM.yyyy"),
                            Genre = book.Genre
                        });
                    }
                }

                // Заполняем доступные книги
                foreach (var book in availableBooks)
                {
                    string shelfCode = book.ShelfCode;
                    string rowName = $"Ряд {book.RowNumber}";

                    if (!string.IsNullOrEmpty(shelfCode) && shelvesData.ContainsKey(shelfCode) && shelvesData[shelfCode].ContainsKey(rowName))
                    {
                        shelvesData[shelfCode][rowName].AvailableBooksCount++;
                    }
                }

                _fullMapData = shelvesData;
                ApplyZoneFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки карты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyZoneFilter()
        {
            if (_fullMapData == null) return;

            var filteredData = new Dictionary<string, Dictionary<string, Models.BookLocationInfo>>();

            foreach (var shelf in _fullMapData)
            {
                string zoneName = GetZoneByShelfCode(shelf.Key);

                bool includeShelf = _currentZoneFilter == "Все зоны";

                if (!includeShelf)
                {
                    string filterZone = _currentZoneFilter;
                    if (filterZone.Contains(": "))
                    {
                        filterZone = filterZone.Substring(filterZone.IndexOf(": ") + 2);
                    }
                    includeShelf = zoneName == filterZone;
                }

                if (includeShelf)
                {
                    filteredData[shelf.Key] = shelf.Value;
                }
            }

            BookMap.UpdateShelvesMap(filteredData);
        }

        private void ZoneFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ZoneFilterComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                _currentZoneFilter = selectedItem.Content.ToString();
                ApplyZoneFilter();
            }
        }

        private void SearchBookTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchBookTextBox.Text) && SearchBookTextBox.Text.Length >= 3)
            {
                FindBookInRealTime(SearchBookTextBox.Text.Trim());
            }
            else if (SearchBookTextBox.Text.Length < 3)
            {
                BookMap.ClearHighlight();
                ApplyZoneFilter();
            }
        }

        private void FindBookInRealTime(string searchText)
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Сначала получаем данные, потом фильтруем в памяти
                var loans = AppConnect.model01.Loans
                    .Where(l => l.ReturnDate == null)
                    .Include(l => l.BookCopies)
                    .Include(l => l.BookCopies.Shelves)
                    .Include(l => l.BookCopies.Rows)
                    .Include(l => l.BookCopies.Books)
                    .Include(l => l.BookCopies.Books.Authors)
                    .ToList();

                var loan = loans.FirstOrDefault(l =>
                    l.BookCopies.Books.Title.ToLower().Contains(searchText.ToLower()) ||
                    l.BookCopies.Books.Authors.FullName.ToLower().Contains(searchText.ToLower()));

                if (loan != null)
                {
                    string shelfCode = loan.BookCopies.Shelves?.ShelfCode ?? "A2";
                    int rowNumber = loan.BookCopies.Rows?.RowNumber ?? 1;

                    var tempData = new Dictionary<string, Dictionary<string, Models.BookLocationInfo>>();
                    if (_fullMapData.ContainsKey(shelfCode))
                    {
                        tempData[shelfCode] = _fullMapData[shelfCode];
                        BookMap.UpdateShelvesMap(tempData);
                        BookMap.HighlightLocation(shelfCode, $"Ряд {rowNumber}");
                        BookMap.ShowBookLocation(loan.BookCopies.Books.Title, shelfCode, $"Ряд {rowNumber}");
                    }
                    else
                    {
                        ApplyZoneFilter();
                    }
                }
                else
                {
                    // Ищем книгу в библиотеке
                    var bookCopies = AppConnect.model01.BookCopies
                        .Where(c => c.Status == "Available")
                        .Include(c => c.Shelves)
                        .Include(c => c.Rows)
                        .Include(c => c.Books)
                        .Include(c => c.Books.Authors)
                        .ToList();

                    var bookCopy = bookCopies.FirstOrDefault(c =>
                        c.Books.Title.ToLower().Contains(searchText.ToLower()) ||
                        c.Books.Authors.FullName.ToLower().Contains(searchText.ToLower()));

                    if (bookCopy != null)
                    {
                        string shelfCode = bookCopy.Shelves?.ShelfCode ?? "A2";
                        int rowNumber = bookCopy.Rows?.RowNumber ?? 1;

                        var tempData = new Dictionary<string, Dictionary<string, Models.BookLocationInfo>>();
                        if (_fullMapData.ContainsKey(shelfCode))
                        {
                            tempData[shelfCode] = _fullMapData[shelfCode];
                            BookMap.UpdateShelvesMap(tempData);
                            BookMap.HighlightLocation(shelfCode, $"Ряд {rowNumber}");
                            BookMap.ShowBookLocation(bookCopy.Books.Title, shelfCode, $"Ряд {rowNumber}");
                        }
                        else
                        {
                            ApplyZoneFilter();
                        }
                    }
                    else
                    {
                        ApplyZoneFilter();
                        BookMap.ClearHighlight();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка поиска: {ex.Message}");
            }
        }

        private void FindBook_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBookTextBox.Text))
            {
                MessageBox.Show("Введите название книги или автора", "Поиск",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            FindBookInRealTime(SearchBookTextBox.Text.Trim());
        }

        private void ResetSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBookTextBox.Text = "";
            ApplyZoneFilter();
            BookMap.ClearHighlight();
        }

        private void RefreshMap_Click(object sender, RoutedEventArgs e)
        {
            LoadShelvesMap();
            BookMap.ClearHighlight();
        }
    }
}