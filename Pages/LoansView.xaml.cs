using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LibraryAccounting.Pages
{
    public partial class LoansView : Page
    {
        private List<dynamic> allLoans;

        public LoansView()
        {
            InitializeComponent();
            LoadLoans();
            LoadReadersForFilter();
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
                    Reader = l.Readers.last_name + " " + l.Readers.first_name + " " + (l.Readers.middle_name ?? ""),
                    ReaderId = l.ReaderId,
                    Book = l.BookCopies.Books.Title,
                    l.BookCopies.InventoryNumber,
                    l.LoanDate,
                    l.DueDate,
                    l.ReturnDate,
                    l.ExtendCount
                })
                .OrderByDescending(l => l.LoanDate)
                .ToList();

            allLoans = loans.Cast<dynamic>().ToList();
            ApplyFilter();
        }

        /// <summary>
        /// Загрузка читателей для фильтра
        /// </summary>
        private void LoadReadersForFilter()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var readers = AppConnect.model01.Readers
                .Select(r => new
                {
                    r.ReaderId,
                    FullName = r.last_name + " " + r.first_name + " " + (r.middle_name ?? "")
                })
                .OrderBy(r => r.FullName)
                .ToList();

            // Добавляем пункт "Все читатели"
            var readersList = new List<dynamic>();
            readersList.Add(new { ReaderId = 0, FullName = "-- Все читатели --" });
            foreach (var reader in readers)
            {
                readersList.Add(reader);
            }

            ReaderFilterComboBox.ItemsSource = readersList;
            ReaderFilterComboBox.SelectedValuePath = "ReaderId";
            ReaderFilterComboBox.DisplayMemberPath = "FullName";
            ReaderFilterComboBox.SelectedValue = 0;
        }

        /// <summary>
        /// Применение фильтров
        /// </summary>
        private void ApplyFilter()
        {
            if (allLoans == null) return;

            var filtered = allLoans.AsEnumerable();

            // Фильтр по читателю
            int selectedReaderId = (int)(ReaderFilterComboBox?.SelectedValue ?? 0);
            if (selectedReaderId != 0)
            {
                filtered = filtered.Where(l => l.ReaderId == selectedReaderId);
            }

            // Фильтр по активным (невозвращенным)
            if (ShowOnlyActiveCheckBox?.IsChecked == true)
            {
                filtered = filtered.Where(l => l.ReturnDate == null);
            }

            LoansDataGrid.ItemsSource = filtered.ToList();

            // Обновляем счетчик
            int totalCount = filtered.Count();
            int activeCount = filtered.Count(l => l.ReturnDate == null);
            int returnedCount = totalCount - activeCount;

            if (selectedReaderId != 0)
            {
                var reader = allLoans.FirstOrDefault(l => l.ReaderId == selectedReaderId);
                string readerName = reader != null ? reader.Reader : "читателя";
                RecordsCount.Text = $"Всего: {totalCount} (активных: {activeCount}, возвращено: {returnedCount})";
            }
            else
            {
                RecordsCount.Text = $"Всего записей: {totalCount} (активных: {activeCount}, возвращено: {returnedCount})";
            }
        }

        /// <summary>
        /// Фильтр по читателю
        /// </summary>
        private void ReaderFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// Сброс фильтра
        /// </summary>
        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            ReaderFilterComboBox.SelectedValue = 0;
            ShowOnlyActiveCheckBox.IsChecked = false;
            ApplyFilter();
        }

        /// <summary>
        /// Фильтр по активным
        /// </summary>
        private void ShowOnlyActive_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// Показать историю выбранного читателя
        /// </summary>
        private void ShowReaderHistory_Click(object sender, RoutedEventArgs e)
        {
            if (LoansDataGrid.SelectedItem == null)
            {
                // Если ничего не выбрано, показываем диалог выбора читателя
                ShowReaderSelectionDialog();
                return;
            }

            dynamic selected = LoansDataGrid.SelectedItem;
            int readerId = selected.ReaderId;
            string readerName = selected.Reader;

            ShowReaderHistoryDialog(readerId, readerName);
        }

        /// <summary>
        /// Показать диалог выбора читателя
        /// </summary>
        private void ShowReaderSelectionDialog()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var readers = AppConnect.model01.Readers
                .Select(r => new
                {
                    r.ReaderId,
                    FullName = r.last_name + " " + r.first_name + " " + (r.middle_name ?? "")
                })
                .OrderBy(r => r.FullName)
                .ToList();

            var dialog = new ReaderSelectionDialog(readers);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true && dialog.SelectedReaderId > 0)
            {
                string readerName = readers.First(r => r.ReaderId == dialog.SelectedReaderId).FullName;
                ShowReaderHistoryDialog(dialog.SelectedReaderId, readerName);
            }
        }

        /// <summary>
        /// Показать историю читателя
        /// </summary>
        private void ShowReaderHistoryDialog(int readerId, string readerName)
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var loans = AppConnect.model01.Loans
                .Where(l => l.ReaderId == readerId)
                .Select(l => new
                {
                    l.LoanId,
                    Книга = l.BookCopies.Books.Title,
                    Инвентарный_номер = l.BookCopies.InventoryNumber,
                    Дата_выдачи = l.LoanDate,
                    Срок_возврата = l.DueDate,
                    Дата_возврата = l.ReturnDate,
                    Статус = l.ReturnDate == null ? (l.DueDate < DateTime.Now ? "Просрочена" : "На руках") : "Возвращена",
                    Продлений = l.ExtendCount ?? 0
                })
                .OrderByDescending(l => l.Дата_выдачи)
                .ToList();

            var historyDialog = new LoanHistoryDialog(readerName, loans);
            historyDialog.Owner = Window.GetWindow(this);
            historyDialog.ShowDialog();
        }

        /// <summary>
        /// Выдача книги
        /// </summary>
        private void IssueButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new IssueLoanWindow();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() != true)
                return;

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var loan = new Loans
            {
                ReaderId = dialog.SelectedReaderId,
                CopyId = dialog.SelectedCopyId,
                LoanDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(dialog.Days),
                ExtendCount = 0
            };

            var copy = AppConnect.model01.BookCopies
                .FirstOrDefault(c => c.CopyId == dialog.SelectedCopyId);

            if (copy != null)
                copy.Status = "Issued";

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

                var copy = AppConnect.model01.BookCopies
                    .FirstOrDefault(c => c.CopyId == loan.CopyId);

                if (copy != null)
                    copy.Status = "Available";

                AppConnect.model01.SaveChanges();

                LoadLoans();
                ShowInfo("Книга успешно возвращена");
            }
            else
            {
                ShowError("Книга уже возвращена");
            }
        }

        /// <summary>
        /// Продление книги
        /// </summary>
        private void ExtendButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoansDataGrid.SelectedItem == null)
            {
                ShowError("Выберите запись для продления");
                return;
            }

            dynamic selected = LoansDataGrid.SelectedItem;
            int loanId = selected.LoanId;

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var loan = AppConnect.model01.Loans
                .FirstOrDefault(l => l.LoanId == loanId);

            if (loan == null)
            {
                ShowError("Запись не найдена");
                return;
            }

            if (loan.ReturnDate != null)
            {
                ShowError("Нельзя продлить — книга уже возвращена");
                return;
            }

            if (loan.DueDate < DateTime.Now)
            {
                ShowError("Нельзя продлить просроченную книгу");
                return;
            }

            int currentExtendCount = loan.ExtendCount ?? 0;
            if (currentExtendCount >= 2)
            {
                ShowError("Достигнут лимит продлений (максимум 2)");
                return;
            }

            loan.ExtendCount = currentExtendCount + 1;
            loan.DueDate = loan.DueDate.AddDays(7);

            AppConnect.model01.SaveChanges();

            LoadLoans();

            int remainingExtends = 2 - (loan.ExtendCount ?? 0);
            ShowInfo($"Срок продлен на 7 дней. Осталось продлений: {remainingExtends}");
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