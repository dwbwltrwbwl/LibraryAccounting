using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LibraryAccounting.Pages
{
    public partial class LoansView : Page
    {
        private List<LoanViewModel> allLoans;
        private List<ReaderViewModel> _allReaders;
        private string _readerSearchText = "";

        public class LoanViewModel
        {
            public int LoanId { get; set; }
            public string Reader { get; set; }
            public int ReaderId { get; set; }
            public string Book { get; set; }
            public string InventoryNumber { get; set; }
            public DateTime LoanDate { get; set; }
            public DateTime DueDate { get; set; }
            public DateTime? ReturnDate { get; set; }
            public int? ExtendCount { get; set; }
            public bool IsOverdue => ReturnDate == null && DueDate < DateTime.Now;
        }

        public class ReaderViewModel
        {
            public int ReaderId { get; set; }
            public string FullName { get; set; }
        }

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
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var loans = AppConnect.model01.Loans
                    .Select(l => new LoanViewModel
                    {
                        LoanId = l.LoanId,
                        Reader = l.Readers.last_name + " " + l.Readers.first_name + " " + (l.Readers.middle_name ?? ""),
                        ReaderId = l.ReaderId,
                        Book = l.BookCopies.Books.Title,
                        InventoryNumber = l.BookCopies.InventoryNumber,
                        LoanDate = l.LoanDate,
                        DueDate = l.DueDate,
                        ReturnDate = l.ReturnDate,
                        ExtendCount = l.ExtendCount
                    })
                    .OrderByDescending(l => l.LoanDate)
                    .ToList();

                allLoans = loans;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузка читателей для фильтра
        /// </summary>
        private void LoadReadersForFilter(string filter = "")
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var query = AppConnect.model01.Readers
                    .Select(r => new ReaderViewModel
                    {
                        ReaderId = r.ReaderId,
                        FullName = (r.last_name + " " + r.first_name + " " + (r.middle_name ?? "")).Trim()
                    })
                    .OrderBy(r => r.FullName)
                    .ToList();

                // Применяем фильтр поиска
                var filteredReaders = query.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filteredReaders = query.Where(r =>
                        r.FullName.ToLower().Contains(filter.ToLower()));
                }

                // Добавляем пункт "Все читатели"
                var readersList = new List<ReaderViewModel>();
                readersList.Add(new ReaderViewModel { ReaderId = 0, FullName = "-- Все читатели --" });
                readersList.AddRange(filteredReaders);

                _allReaders = readersList;
                ReaderFilterComboBox.ItemsSource = _allReaders;
                ReaderFilterComboBox.SelectedValuePath = "ReaderId";
                ReaderFilterComboBox.DisplayMemberPath = "FullName";

                // Сохраняем введенный текст
                if (!string.IsNullOrWhiteSpace(_readerSearchText))
                {
                    ReaderFilterComboBox.Text = _readerSearchText;
                }

                // Если фильтр пустой, выбираем "Все читатели"
                if (string.IsNullOrWhiteSpace(filter) && ReaderFilterComboBox.SelectedValue == null)
                {
                    ReaderFilterComboBox.SelectedValue = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки читателей: {ex.Message}");
            }
        }

        private void ReaderFilterComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            _readerSearchText = ReaderFilterComboBox.Text ?? "";
            LoadReadersForFilter(_readerSearchText);
            ReaderFilterComboBox.IsDropDownOpen = true;

            // Сбрасываем выбранное значение при ручном вводе
            if (ReaderFilterComboBox.SelectedValue != null && (int)ReaderFilterComboBox.SelectedValue != 0)
            {
                ReaderFilterComboBox.SelectedValue = null;
            }
        }

        /// <summary>
        /// Применение фильтров
        /// </summary>
        private void ApplyFilter()
        {
            if (allLoans == null) return;

            var filtered = allLoans.AsEnumerable();

            // Фильтр по читателю
            int selectedReaderId = 0;
            if (ReaderFilterComboBox.SelectedValue != null)
            {
                int.TryParse(ReaderFilterComboBox.SelectedValue.ToString(), out selectedReaderId);
            }

            if (selectedReaderId != 0)
            {
                filtered = filtered.Where(l => l.ReaderId == selectedReaderId);
            }

            // Фильтр по активным (невозвращенным)
            if (ShowOnlyActiveCheckBox?.IsChecked == true)
            {
                filtered = filtered.Where(l => l.ReturnDate == null);
            }

            var resultList = filtered.ToList();
            LoansDataGrid.ItemsSource = resultList;

            // Обновляем счетчик
            int totalCount = resultList.Count;
            int activeCount = resultList.Count(l => l.ReturnDate == null);
            int overdueCount = resultList.Count(l => l.IsOverdue);
            int returnedCount = totalCount - activeCount;

            if (selectedReaderId != 0)
            {
                var reader = allLoans.FirstOrDefault(l => l.ReaderId == selectedReaderId);
                string readerName = reader != null ? reader.Reader : "читателя";

                if (ShowOnlyActiveCheckBox?.IsChecked == true)
                {
                    RecordsCount.Text = $"{readerName}: активных {activeCount} (просрочено {overdueCount})";
                }
                else
                {
                    RecordsCount.Text = $"{readerName}: всего {totalCount} (активных {activeCount}, возвращено {returnedCount}, просрочено {overdueCount})";
                }
            }
            else
            {
                if (ShowOnlyActiveCheckBox?.IsChecked == true)
                {
                    RecordsCount.Text = $"Всего активных выдач: {activeCount} (просрочено {overdueCount})";
                }
                else
                {
                    RecordsCount.Text = $"Всего записей: {totalCount} (активных {activeCount}, возвращено {returnedCount}, просрочено {overdueCount})";
                }
            }
        }

        /// <summary>
        /// Фильтр по читателю
        /// </summary>
        private void ReaderFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Сбрасываем поисковый текст при выборе из списка
            if (ReaderFilterComboBox.SelectedItem is ReaderViewModel selectedReader)
            {
                if (selectedReader.ReaderId != 0)
                {
                    _readerSearchText = "";
                }
            }
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
                ShowReaderSelectionDialog();
                return;
            }

            var selected = LoansDataGrid.SelectedItem as LoanViewModel;
            if (selected != null)
            {
                ShowReaderHistoryDialog(selected.ReaderId, selected.Reader);
            }
        }

        /// <summary>
        /// Показать диалог выбора читателя
        /// </summary>
        private void ShowReaderSelectionDialog()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var readers = AppConnect.model01.Readers
                    .Select(r => new ReaderViewModel
                    {
                        ReaderId = r.ReaderId,
                        FullName = (r.last_name + " " + r.first_name + " " + (r.middle_name ?? "")).Trim()
                    })
                    .OrderBy(r => r.FullName)
                    .ToList();

                var dialog = new ReaderSelectionDialog(readers.Cast<dynamic>().ToList());
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true && dialog.SelectedReaderId > 0)
                {
                    string readerName = readers.First(r => r.ReaderId == dialog.SelectedReaderId).FullName;
                    ShowReaderHistoryDialog(dialog.SelectedReaderId, readerName);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Показать историю читателя
        /// </summary>
        private void ShowReaderHistoryDialog(int readerId, string readerName)
        {
            try
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

                var historyDialog = new LoanHistoryDialog(readerName, loans.Cast<dynamic>().ToList());
                historyDialog.Owner = Window.GetWindow(this);
                historyDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки истории: {ex.Message}");
            }
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
            {
                copy.Status = "Issued";

                // ✅ ДОБАВИТЬ: Обновляем информацию об экземпляре
                copy.LastReaderId = dialog.SelectedReaderId;
                copy.LastLoanDate = DateTime.Now;
                copy.TotalLoans = copy.TotalLoans + 1;
            }

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
                {
                    copy.Status = "Available";
                    // LastReaderId и LastLoanDate не обнуляем, оставляем историю
                }

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

            var selected = LoansDataGrid.SelectedItem as LoanViewModel;
            if (selected == null)
            {
                ShowError("Ошибка выбора записи");
                return;
            }

            if (selected.ReturnDate != null)
            {
                ShowError("Нельзя продлить — книга уже возвращена");
                return;
            }

            if (selected.IsOverdue)
            {
                ShowError("Нельзя продлить просроченную книгу");
                return;
            }

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var loan = AppConnect.model01.Loans
                    .FirstOrDefault(l => l.LoanId == selected.LoanId);

                if (loan == null)
                {
                    ShowError("Запись не найдена");
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
            catch (Exception ex)
            {
                ShowError($"Ошибка продления: {ex.Message}");
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

        private void ShowMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mapWindow = new BookMapWindow();
                mapWindow.Owner = Window.GetWindow(this);
                mapWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка открытия карты: {ex.Message}");
            }
        }
    }
}