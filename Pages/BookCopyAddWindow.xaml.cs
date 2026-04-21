using LibraryAccounting.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LibraryAccounting.Windows
{
    public partial class BookCopyAddWindow : Window
    {
        private BookCopies _copy;
        private bool _isEdit;

        // ➕ ДОБАВЛЕНИЕ
        public BookCopyAddWindow()
        {
            InitializeComponent();
            _isEdit = false;
            TitleText.Text = "Добавление экземпляра";

            // Скрываем поля только для просмотра при добавлении
            StatusLabel.Visibility = Visibility.Collapsed;
            StatusBox.Visibility = Visibility.Collapsed;
            AddedDateLabel.Visibility = Visibility.Collapsed;
            AddedDateBox.Visibility = Visibility.Collapsed;
            LastReaderLabel.Visibility = Visibility.Collapsed;
            LastReaderBox.Visibility = Visibility.Collapsed;
            LastLoanDateLabel.Visibility = Visibility.Collapsed;
            LastLoanDateBox.Visibility = Visibility.Collapsed;
            TotalLoansLabel.Visibility = Visibility.Collapsed;
            TotalLoansBox.Visibility = Visibility.Collapsed;

            LoadBooks();
            LoadShelves();
            SetInventoryMask();
        }

        // ✏️ РЕДАКТИРОВАНИЕ
        public BookCopyAddWindow(BookCopies copy)
        {
            InitializeComponent();
            _copy = copy;
            _isEdit = true;
            TitleText.Text = "Редактирование экземпляра";

            // Показываем все поля
            StatusLabel.Visibility = Visibility.Visible;
            StatusBox.Visibility = Visibility.Visible;
            AddedDateLabel.Visibility = Visibility.Visible;
            AddedDateBox.Visibility = Visibility.Visible;
            LastReaderLabel.Visibility = Visibility.Visible;
            LastReaderBox.Visibility = Visibility.Visible;
            LastLoanDateLabel.Visibility = Visibility.Visible;
            LastLoanDateBox.Visibility = Visibility.Visible;
            TotalLoansLabel.Visibility = Visibility.Visible;
            TotalLoansBox.Visibility = Visibility.Visible;

            LoadBooks();
            LoadShelves();
            FillData();
            SetInventoryMask();
        }

        private void LoadBooks(string filter = null)
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var query = AppConnect.model01.Books.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(b => b.Title.ToLower().Contains(filter.ToLower()));
            }

            BookBox.ItemsSource = query.OrderBy(b => b.Title).ToList();
            BookBox.DisplayMemberPath = "Title";
            BookBox.SelectedValuePath = "BookId";
        }

        private void LoadShelves(string filter = null)
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            // Сначала получаем данные без форматирования
            var shelvesRaw = AppConnect.model01.Shelves
                .OrderBy(s => s.SortOrder)
                .Select(s => new
                {
                    s.ShelfId,
                    s.ShelfCode,
                    s.ShelfName,
                    s.Zone,
                    s.SortOrder
                })
                .ToList();

            // Форматируем Display после получения данных
            var shelves = shelvesRaw.Select(s => new
            {
                s.ShelfId,
                Display = $"{s.ShelfCode} - {s.ShelfName}",
                s.ShelfCode,
                s.ShelfName,
                s.Zone,
                s.SortOrder
            }).ToList();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var filteredList = shelves
                    .Where(s => s.Display.ToLower().Contains(filter.ToLower()))
                    .ToList();
                ShelfBox.ItemsSource = filteredList;
            }
            else
            {
                ShelfBox.ItemsSource = shelves;
            }

            ShelfBox.DisplayMemberPath = "Display";
            ShelfBox.SelectedValuePath = "ShelfId";
        }

        private void LoadRows(int? shelfId, string filter = null)
        {
            if (!shelfId.HasValue)
            {
                RowBox.ItemsSource = null;
                RowBox.IsEnabled = false;
                ShelfInfoText.Visibility = Visibility.Collapsed;
                return;
            }

            // Получаем данные о рядах с количеством книг
            var rowsRaw = AppConnect.model01.Rows
                .Where(r => r.ShelfId == shelfId.Value)
                .Select(r => new
                {
                    r.RowId,
                    r.RowNumber,
                    r.Capacity,
                    BooksCount = AppConnect.model01.BookCopies.Count(c => c.ShelfId == shelfId.Value && c.RowId == r.RowId)
                })
                .ToList();

            var rows = rowsRaw.Select(r => new
            {
                r.RowId,
                Display = $"Ряд {r.RowNumber} (книг: {r.BooksCount}/{r.Capacity})",
                r.RowNumber,
                IsFull = r.BooksCount >= r.Capacity
            }).ToList();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var filteredList = rows
                    .Where(r => r.Display.ToLower().Contains(filter.ToLower()))
                    .ToList();
                RowBox.ItemsSource = filteredList;
            }
            else
            {
                RowBox.ItemsSource = rows;
            }

            RowBox.DisplayMemberPath = "Display";
            RowBox.SelectedValuePath = "RowId";
            RowBox.IsEnabled = true;
        }

        private void BookBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchText = BookBox.Text?.Trim() ?? "";
            LoadBooks(searchText);
            if (BookBox.ItemsSource != null && ((System.Collections.IList)BookBox.ItemsSource).Count > 0)
                BookBox.IsDropDownOpen = true;
        }

        private void ShelfBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchText = ShelfBox.Text?.Trim() ?? "";
            LoadShelves(searchText);
            if (ShelfBox.ItemsSource != null && ((System.Collections.IList)ShelfBox.ItemsSource).Count > 0)
                ShelfBox.IsDropDownOpen = true;
        }

        private void ShelfBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShelfBox.SelectedItem != null)
            {
                dynamic selected = ShelfBox.SelectedItem;
                int shelfId = selected.ShelfId;
                LoadRows(shelfId);
                ShelfInfoText.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoadRows(null);
                ShelfInfoText.Visibility = Visibility.Collapsed;
            }
        }
        private void RowBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RowBox.SelectedItem != null && ShelfBox.SelectedItem != null)
            {
                dynamic selectedShelf = ShelfBox.SelectedItem;
                dynamic selectedRow = RowBox.SelectedItem;
                int shelfId = selectedShelf.ShelfId;
                int rowId = selectedRow.RowId;

                ShowShelfInfo(shelfId, rowId);
            }
            else
            {
                ShelfInfoText.Visibility = Visibility.Collapsed;
            }
        }
        private void RowBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (ShelfBox.SelectedItem == null) return;

            dynamic selectedShelf = ShelfBox.SelectedItem;
            int shelfId = selectedShelf.ShelfId;

            string searchText = RowBox.Text?.Trim() ?? "";
            LoadRows(shelfId, searchText);
            if (RowBox.ItemsSource != null && ((System.Collections.IList)RowBox.ItemsSource).Count > 0)
                RowBox.IsDropDownOpen = true;
        }

        private void SetInventoryMask()
        {
            if (string.IsNullOrEmpty(InventoryBox.Text))
            {
                InventoryBox.Text = "INV-";
                InventoryBox.CaretIndex = InventoryBox.Text.Length;
            }
        }

        private void InventoryBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string text = textBox.Text;

            if (!text.StartsWith("INV-"))
            {
                text = "INV-" + text.Replace("INV-", "");
                textBox.Text = text;
                textBox.CaretIndex = textBox.Text.Length;
                return;
            }

            if (text.IndexOf("INV-", 1) > 0)
            {
                text = "INV-" + text.Substring(text.LastIndexOf("INV-") + 4);
                textBox.Text = text;
                textBox.CaretIndex = textBox.Text.Length;
            }

            string afterInv = text.Substring(4);
            if (afterInv.Length > 10)
            {
                afterInv = afterInv.Substring(0, 10);
                textBox.Text = "INV-" + afterInv;
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private void InventoryBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            int cursorPos = textBox.CaretIndex;

            if (cursorPos <= 4 && (e.Text == "\b" || string.IsNullOrEmpty(e.Text)))
            {
                if (cursorPos <= 4 && e.Text == "\b")
                {
                    e.Handled = true;
                    return;
                }
            }

            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        private void FillData()
        {
            InventoryBox.Text = _copy.InventoryNumber;

            // Дата добавления
            AddedDateBox.Text = _copy.AddedDate.ToString("dd.MM.yyyy HH:mm");

            // Последний читатель
            if (_copy.LastReaderId.HasValue)
            {
                var lastReader = AppConnect.model01.Readers.FirstOrDefault(r => r.ReaderId == _copy.LastReaderId);
                if (lastReader != null)
                {
                    LastReaderBox.Text = lastReader.last_name + " " + lastReader.first_name + " " + (lastReader.middle_name ?? "");
                }
            }
            else
            {
                LastReaderBox.Text = "—";
            }

            // Последняя выдача
            LastLoanDateBox.Text = _copy.LastLoanDate?.ToString("dd.MM.yyyy") ?? "—";

            // Всего выдач
            TotalLoansBox.Text = _copy.TotalLoans.ToString();

            // Выбор статуса
            switch (_copy.Status)
            {
                case "Available":
                    StatusBox.SelectedIndex = 0;
                    break;
                case "Issued":
                    StatusBox.SelectedIndex = 1;
                    break;
                case "Lost":
                    StatusBox.SelectedIndex = 2;
                    break;
                case "WrittenOff":
                    StatusBox.SelectedIndex = 3;
                    break;
                default:
                    StatusBox.SelectedIndex = 0;
                    break;
            }

            // Выбор книги
            BookBox.SelectedItem = AppConnect.model01.Books
                .FirstOrDefault(b => b.BookId == _copy.BookId);
            BookBox.IsEnabled = false;

            // Выбор стеллажа и ряда
            if (_copy.ShelfId.HasValue)
            {
                var shelf = AppConnect.model01.Shelves.FirstOrDefault(s => s.ShelfId == _copy.ShelfId);
                if (shelf != null)
                {
                    // Находим и выбираем нужный стеллаж в списке
                    var shelvesList = ShelfBox.ItemsSource as System.Collections.IEnumerable;
                    if (shelvesList != null)
                    {
                        foreach (var item in shelvesList)
                        {
                            var shelfItem = item.GetType().GetProperty("ShelfId")?.GetValue(item);
                            if (shelfItem != null && (int)shelfItem == _copy.ShelfId.Value)
                            {
                                ShelfBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                    LoadRows(_copy.ShelfId);

                    if (_copy.RowId.HasValue)
                    {
                        RowBox.SelectedValue = _copy.RowId;
                    }
                }
            }
        }
        private bool CheckShelfCapacity(int shelfId, int rowId, int? excludeCopyId = null)
        {
            try
            {
                // Получаем информацию о полке
                var row = AppConnect.model01.Rows.FirstOrDefault(r => r.RowId == rowId);
                if (row == null || row.Capacity == null) return true;

                // Считаем количество книг на этой полке (исключая текущую при редактировании)
                int booksCount = AppConnect.model01.BookCopies
                    .Count(c => c.ShelfId == shelfId && c.RowId == rowId &&
                           (!excludeCopyId.HasValue || c.CopyId != excludeCopyId.Value));

                // Проверяем, не превышен ли лимит
                if (booksCount >= row.Capacity)
                {
                    MessageBox.Show($"На данной полке уже максимальное количество книг ({row.Capacity} шт.).\n" +
                        "Выберите другое место для размещения экземпляра.",
                        "Превышение вместимости", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Показываем информацию о заполненности полки
                int freeSpace = row.Capacity - booksCount;
                if (freeSpace <= 10)
                {
                    MessageBox.Show($"Внимание: на выбранной полке осталось только {freeSpace} свободных мест из {row.Capacity}.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка проверки вместимости: {ex.Message}");
                return true;
            }
        }
        private void ShowShelfInfo(int shelfId, int rowId)
        {
            try
            {
                var row = AppConnect.model01.Rows.FirstOrDefault(r => r.RowId == rowId);
                if (row == null) return;

                int booksCount = AppConnect.model01.BookCopies
                    .Count(c => c.ShelfId == shelfId && c.RowId == rowId);

                int freeSpace = row.Capacity - booksCount;

                // Можно показать в ToolTip или отдельной метке
                ShelfInfoText.Text = $"📊 Заполненность: {booksCount}/{row.Capacity} книг (свободно: {freeSpace})";
                ShelfInfoText.Visibility = Visibility.Visible;

                if (freeSpace <= 5)
                {
                    ShelfInfoText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (freeSpace <= 10)
                {
                    ShelfInfoText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    ShelfInfoText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения информации о полке: {ex.Message}");
            }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (BookBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите книгу", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BookBox.Focus();
                return;
            }

            string inventory = InventoryBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(inventory) || inventory == "INV-")
            {
                MessageBox.Show("Введите инвентарный номер (должен начинаться с INV-)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InventoryBox.Focus();
                return;
            }

            if (ShelfBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите стеллаж", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ShelfBox.Focus();
                return;
            }

            if (RowBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите ряд", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RowBox.Focus();
                return;
            }

            // Проверка уникальности инвентарного номера
            int currentCopyId = _isEdit ? _copy.CopyId : 0;
            bool exists = AppConnect.model01.BookCopies.Any(c =>
                c.InventoryNumber == inventory &&
                (!_isEdit || c.CopyId != currentCopyId));

            if (exists)
            {
                MessageBox.Show("Экземпляр с таким инвентарным номером уже существует",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                InventoryBox.Focus();
                return;
            }

            if (_isEdit && StatusBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusBox.Focus();
                return;
            }

            // Получаем выбранные значения
            var book = (Books)BookBox.SelectedItem;
            dynamic selectedShelf = ShelfBox.SelectedItem;
            dynamic selectedRow = RowBox.SelectedItem;
            int shelfId = selectedShelf.ShelfId;
            int rowId = selectedRow.RowId;

            // Проверка вместимости полки
            if (!CheckShelfCapacity(shelfId, rowId, _isEdit ? _copy.CopyId : (int?)null))
            {
                return;
            }

            try
            {
                if (!_isEdit)
                {
                    _copy = new BookCopies();
                    AppConnect.model01.BookCopies.Add(_copy);
                    _copy.AddedDate = DateTime.Now;
                    _copy.TotalLoans = 0;
                }

                _copy.BookId = book.BookId;
                _copy.InventoryNumber = inventory;
                _copy.ShelfId = shelfId;
                _copy.RowId = rowId;

                // Установка статуса
                if (_isEdit)
                {
                    string selectedStatus = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                    // Проверка при смене статуса на "Выдана"
                    if (selectedStatus == "Выдана")
                    {
                        bool hasLoan = AppConnect.model01.Loans.Any(l => l.CopyId == _copy.CopyId && l.ReturnDate == null);
                        if (!hasLoan)
                        {
                            MessageBox.Show("Нельзя установить статус 'Выдана' без оформленной выдачи книги",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    switch (selectedStatus)
                    {
                        case "Доступна":
                            _copy.Status = "Available";
                            break;
                        case "Выдана":
                            _copy.Status = "Issued";
                            break;
                        case "Потеряна":
                            _copy.Status = "Lost";
                            break;
                        case "Списана":
                            _copy.Status = "WrittenOff";
                            break;
                    }
                }
                else
                {
                    _copy.Status = "Available";
                }

                AppConnect.model01.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}