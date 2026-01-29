using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class LoansView : Page
    {
        public LoansView()
        {
            InitializeComponent();
            LoadLoans();
        }

        /// <summary>
        /// Загрузка всех выдач
        /// </summary>
        private void LoadLoans()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var loans = AppConnect.model01.Loans
                .Select(l => new
                {
                    l.LoanId,
                    Reader =
    l.Readers.last_name + " " +
    l.Readers.first_name + " " +
    (l.Readers.middle_name ?? ""),
                    Book = l.BookCopies.Books.Title,
                    l.BookCopies.InventoryNumber,
                    l.LoanDate,
                    l.DueDate,
                    l.ReturnDate
                })
                .ToList();

            LoansDataGrid.ItemsSource = loans;
        }

        /// <summary>
        /// Выдача книги
        /// </summary>
        private void IssueButton_Click(object sender, RoutedEventArgs e)
        {
            // Упрощённый пример: первая доступная книга и первый читатель
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var reader = AppConnect.model01.Readers.FirstOrDefault();
            var copy = AppConnect.model01.BookCopies
                .FirstOrDefault(c => c.Status == "Available");

            if (reader == null || copy == null)
            {
                ShowError("Нет доступных книг или читателей");
                return;
            }

            var loan = new Loans
            {
                ReaderId = reader.ReaderId,
                CopyId = copy.CopyId,
                LoanDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14)
            };

            AppConnect.model01.Loans.Add(loan);
            AppConnect.model01.SaveChanges();

            LoadLoans();
            ShowInfo("Книга успешно выдана");
        }

        /// <summary>
        /// Возврат книги
        /// </summary>
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoansDataGrid.SelectedItem == null)
            {
                ShowError("Выберите запись для возврата");
                return;
            }

            dynamic selected = LoansDataGrid.SelectedItem;
            int loanId = selected.LoanId;

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var loan = AppConnect.model01.Loans
                .FirstOrDefault(l => l.LoanId == loanId);

            if (loan != null && loan.ReturnDate == null)
            {
                loan.ReturnDate = DateTime.Now;
                AppConnect.model01.SaveChanges();

                LoadLoans();
                ShowInfo("Книга успешно возвращена");
            }
            else
            {
                ShowError("Книга уже возвращена");
            }
        }

        private void ShowError(string message)
        {
            var dialog = new MessageDialog("Ошибка", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void ShowInfo(string message)
        {
            var dialog = new MessageDialog("Информация", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }
    }
}
