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
        private List<Books> _allBooks;
        private List<string> _allRows;
        private List<string> _allShelves;

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
            LoadRows();
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
            LoadRows();
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

            _allBooks = query.OrderBy(b => b.Title).ToList();
            BookBox.ItemsSource = _allBooks;
            BookBox.DisplayMemberPath = "Title";
            BookBox.SelectedValuePath = "BookId";
        }

        private void LoadRows(string filter = null)
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            IQueryable<string> query = AppConnect.model01.BookCopies
                .Select(c => c.Row)
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .OrderBy(r => r);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(r => r.ToLower().Contains(filter.ToLower()));
            }

            _allRows = query.ToList();
            RowBox.ItemsSource = _allRows;

            // Добавляем возможность ввода нового ряда
            if (!_allRows.Contains(RowBox.Text) && !string.IsNullOrWhiteSpace(RowBox.Text))
            {
                var newList = _allRows.ToList();
                newList.Insert(0, RowBox.Text);
                RowBox.ItemsSource = newList;
            }
        }

        private void LoadShelves(string filter = null)
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            IQueryable<string> query = AppConnect.model01.BookCopies
                .Select(c => c.Shelf)
                .Where(s => !string.IsNullOrEmpty(s) && s != "1")  // ← добавляем s != "1"
                .Distinct()
                .OrderBy(s => s);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(s => s.ToLower().Contains(filter.ToLower()));
            }

            _allShelves = query.ToList();
            ShelfBox.ItemsSource = _allShelves;

            // Добавляем возможность ввода новой полки
            if (!_allShelves.Contains(ShelfBox.Text) && !string.IsNullOrWhiteSpace(ShelfBox.Text))
            {
                var newList = _allShelves.ToList();
                newList.Insert(0, ShelfBox.Text);
                ShelfBox.ItemsSource = newList;
            }
        }

        private void BookBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchText = BookBox.Text?.Trim() ?? "";
            LoadBooks(searchText);
            if (BookBox.ItemsSource != null && ((System.Collections.IList)BookBox.ItemsSource).Count > 0)
                BookBox.IsDropDownOpen = true;
        }

        private void RowBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchText = RowBox.Text?.Trim() ?? "";
            LoadRows(searchText);
            if (RowBox.ItemsSource != null && ((System.Collections.IList)RowBox.ItemsSource).Count > 0)
                RowBox.IsDropDownOpen = true;
        }

        private void ShelfBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchText = ShelfBox.Text?.Trim() ?? "";
            LoadShelves(searchText);
            if (ShelfBox.ItemsSource != null && ((System.Collections.IList)ShelfBox.ItemsSource).Count > 0)
                ShelfBox.IsDropDownOpen = true;
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
            RowBox.Text = _copy.Row;
            ShelfBox.Text = _copy.Shelf;

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

            BookBox.SelectedItem = AppConnect.model01.Books
                .FirstOrDefault(b => b.BookId == _copy.BookId);

            BookBox.IsEnabled = false;
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

            string row = RowBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(row))
            {
                MessageBox.Show("Введите номер ряда", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RowBox.Focus();
                return;
            }

            string shelf = ShelfBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(shelf))
            {
                MessageBox.Show("Введите номер полки", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ShelfBox.Focus();
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

            try
            {
                if (!_isEdit)
                {
                    _copy = new BookCopies();
                    AppConnect.model01.BookCopies.Add(_copy);
                    _copy.AddedDate = DateTime.Now;
                    _copy.TotalLoans = 0;
                }

                var book = (Books)BookBox.SelectedItem;

                _copy.BookId = book.BookId;
                _copy.InventoryNumber = inventory;
                _copy.Row = row;
                _copy.Shelf = shelf;

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