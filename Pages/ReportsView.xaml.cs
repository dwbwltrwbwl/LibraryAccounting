using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class ReportsView : Page
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Отчет: все выданные книги
        /// </summary>
        private void IssuedBooks_Click(object sender, RoutedEventArgs e)
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var report = AppConnect.model01.Loans
                .Where(l => l.ReturnDate == null)
                .Select(l => new
                {
                    Reader = l.Readers.FullName,
                    Book = l.BookCopies.Books.Title,
                    l.BookCopies.InventoryNumber,
                    l.LoanDate,
                    l.DueDate
                })
                .ToList();

            ReportsDataGrid.ItemsSource = report;
        }

        /// <summary>
        /// Отчет: просроченные книги
        /// </summary>
        private void Overdue_Click(object sender, RoutedEventArgs e)
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            DateTime today = DateTime.Now.Date;

            var report = AppConnect.model01.Loans
                .Where(l => l.ReturnDate == null && l.DueDate < today)
                .Select(l => new
                {
                    Reader = l.Readers.FullName,
                    Book = l.BookCopies.Books.Title,
                    l.BookCopies.InventoryNumber,
                    l.DueDate
                })
                .ToList();

            ReportsDataGrid.ItemsSource = report;
        }

        /// <summary>
        /// Отчет: самые популярные книги
        /// </summary>
        private void PopularBooks_Click(object sender, RoutedEventArgs e)
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var report = AppConnect.model01.Loans
                .GroupBy(l => l.BookCopies.Books.Title)
                .Select(g => new
                {
                    Book = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .ToList();

            ReportsDataGrid.ItemsSource = report;
        }

        private void ShowInfo(string message)
        {
            var dialog = new MessageDialog("Информация", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
