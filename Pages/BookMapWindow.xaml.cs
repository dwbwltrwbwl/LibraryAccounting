using LibraryAccounting.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Windows
{
    public partial class BookMapWindow : Window
    {
        public BookMapWindow()
        {
            InitializeComponent();
            LoadShelvesMap();
        }

        private void LoadShelvesMap()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Сначала получаем все выданные книги
                var issuedBooks = AppConnect.model01.Loans
                    .Where(l => l.ReturnDate == null)
                    .Select(l => new
                    {
                        l.BookCopies.Shelf,
                        l.BookCopies.Row,
                        BookTitle = l.BookCopies.Books.Title,
                        IsOverdue = l.DueDate < DateTime.Now,
                        Reader = l.Readers.last_name + " " + l.Readers.first_name + " " + (l.Readers.middle_name ?? ""),
                        DueDate = l.DueDate
                    })
                    .ToList();

                // Группируем по стеллажам и полкам
                var shelvesData = new System.Collections.Generic.Dictionary<string,
                    System.Collections.Generic.Dictionary<string, Models.BookLocationInfo>>();

                foreach (var book in issuedBooks)
                {
                    string shelf = book.Shelf ?? "Не указан";
                    string row = book.Row ?? "Не указана";

                    if (!shelvesData.ContainsKey(shelf))
                        shelvesData[shelf] = new System.Collections.Generic.Dictionary<string, Models.BookLocationInfo>();

                    if (!shelvesData[shelf].ContainsKey(row))
                        shelvesData[shelf][row] = new Models.BookLocationInfo();

                    shelvesData[shelf][row].Books.Add(new Models.BookInfo
                    {
                        Title = book.BookTitle,
                        IsOverdue = book.IsOverdue,
                        Reader = book.Reader,
                        DueDate = book.DueDate.ToString("dd.MM.yyyy")
                    });
                }

                BookMap.UpdateShelvesMap(shelvesData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки карты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBookTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Опционально: поиск в реальном времени
        }

        private void FindBook_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBookTextBox.Text))
            {
                MessageBox.Show("Введите название книги", "Поиск", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var bookTitle = SearchBookTextBox.Text.Trim();

                // Находим книгу через Loans (выданные)
                var loan = AppConnect.model01.Loans
                    .FirstOrDefault(l => l.ReturnDate == null && l.BookCopies.Books.Title.Contains(bookTitle));

                if (loan == null)
                {
                    MessageBox.Show($"Книга \"{bookTitle}\" не найдена на руках", "Поиск",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string shelf = loan.BookCopies.Shelf ?? "Не указан";
                string row = loan.BookCopies.Row ?? "Не указана";

                BookMap.ShowBookLocation(bookTitle, shelf, row);

                MessageBox.Show($"Книга \"{bookTitle}\" находится:\nСтеллаж: {shelf}\nПолка: {row}",
                    "Местоположение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}