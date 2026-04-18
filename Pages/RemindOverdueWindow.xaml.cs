using LibraryAccounting.AppData;
using LibraryAccounting.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class RemindOverdueWindow : Window
    {
        private List<ReaderWithOverdue> _allReaders;
        private EmailService _emailService;

        public class ReaderWithOverdue
        {
            public int ReaderId { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public int OverdueDays { get; set; }
            public List<OverdueBookInfo> OverdueBooks { get; set; }
            public string OverdueInfo => OverdueBooks != null && OverdueBooks.Any()
                ? $"Просрочено книг: {OverdueBooks.Count}" : "";
        }

        public class OverdueBookInfo
        {
            public string BookTitle { get; set; }
            public string InventoryNumber { get; set; }
            public DateTime DueDate { get; set; }
            public int OverdueDays { get; set; }
        }

        public RemindOverdueWindow()
        {
            InitializeComponent();
            LoadReadersWithOverdue();
            ConfigureEmailService();
        }

        private void ConfigureEmailService()
        {
            try
            {
                var settings = new MailSettings
                {
                    SmtpServer = "smtp.yandex.ru",
                    Port = 587,  // Поменял с 465 на 587 (TLS)
                    Email = "libraryaccounting@yandex.com",
                    Password = "lhbmdfsmsliyjhbk",
                    UseSSL = true,
                    DisplayName = "Библиотека"
                };
                _emailService = new EmailService(settings);
                System.Diagnostics.Debug.WriteLine("EmailService настроен успешно");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка настройки EmailService: {ex.Message}");
                MessageBox.Show($"Ошибка настройки почты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadReadersWithOverdue()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                var today = DateTime.Today;

                var overdueLoans = AppConnect.model01.Loans
                    .Where(l => l.ReturnDate == null && l.DueDate < today)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Найдено просроченных выдач: {overdueLoans.Count}");

                var readersDict = new Dictionary<int, ReaderWithOverdue>();

                foreach (var loan in overdueLoans)
                {
                    if (!readersDict.ContainsKey(loan.ReaderId))
                    {
                        var reader = AppConnect.model01.Readers.FirstOrDefault(r => r.ReaderId == loan.ReaderId);
                        if (reader != null)
                        {
                            readersDict[loan.ReaderId] = new ReaderWithOverdue
                            {
                                ReaderId = loan.ReaderId,
                                FullName = $"{reader.last_name} {reader.first_name} {reader.middle_name}".Trim(),
                                Email = reader.Email,
                                Phone = reader.Phone,
                                OverdueBooks = new List<OverdueBookInfo>()
                            };
                        }
                    }

                    if (readersDict.ContainsKey(loan.ReaderId))
                    {
                        readersDict[loan.ReaderId].OverdueBooks.Add(new OverdueBookInfo
                        {
                            BookTitle = loan.BookCopies?.Books?.Title ?? "Неизвестная книга",
                            InventoryNumber = loan.BookCopies?.InventoryNumber ?? "",
                            DueDate = loan.DueDate,
                            OverdueDays = (today - loan.DueDate).Days
                        });
                    }
                }

                foreach (var reader in readersDict.Values)
                {
                    if (reader.OverdueBooks.Any())
                    {
                        reader.OverdueDays = reader.OverdueBooks.Max(b => b.OverdueDays);
                    }
                }

                _allReaders = readersDict.Values.OrderByDescending(r => r.OverdueDays).ToList();
                ReadersListBox.ItemsSource = _allReaders;

                System.Diagnostics.Debug.WriteLine($"Загружено читателей с просрочкой: {_allReaders.Count}");

                if (_allReaders.Count == 0)
                {
                    MessageBox.Show("Нет читателей с просроченными книгами", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text?.Trim().ToLower() ?? "";

            var filtered = _allReaders;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = _allReaders.Where(r =>
                    r.FullName.ToLower().Contains(searchText) ||
                    (r.Email != null && r.Email.ToLower().Contains(searchText)) ||
                    (r.Phone != null && r.Phone.Contains(searchText))
                ).ToList();
            }

            ReadersListBox.ItemsSource = filtered;
        }

        private void ReadersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = ReadersListBox.SelectedItem as ReaderWithOverdue;
            SendButton.IsEnabled = selected != null && !string.IsNullOrEmpty(selected?.Email);

            if (selected != null)
            {
                var booksList = string.Join("\n", selected.OverdueBooks.Select(b =>
                    $"• {b.BookTitle} (просрочка: {b.OverdueDays} дн., должна быть возвращена до {b.DueDate:dd.MM.yyyy})"));

                SelectedReaderInfo.Text = $"📚 {selected.FullName}\n📧 {selected.Email ?? "Не указан"}\n📞 {selected.Phone ?? "Не указан"}\n\nПросроченные книги:\n{booksList}";
            }
            else
            {
                SelectedReaderInfo.Text = "Выберите читателя из списка";
            }
        }

        private async void SendReminder_Click(object sender, RoutedEventArgs e)
        {
            var selected = ReadersListBox.SelectedItem as ReaderWithOverdue;
            if (selected == null)
            {
                MessageBox.Show("Выберите читателя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(selected.Email))
            {
                MessageBox.Show("У выбранного читателя не указан email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Блокируем кнопку и показываем индикатор загрузки
            SendButton.IsEnabled = false;
            SendButton.Content = "Отправка...";

            try
            {
                var subject = $"Напоминание о просроченных книгах в библиотеке";

                var booksHtml = string.Join("", selected.OverdueBooks.Select(b => $@"
                    <tr style='border-bottom:1px solid #ddd'>
                        <td style='padding:8px'>{System.Security.SecurityElement.Escape(b.BookTitle)}</td>
                        <td style='padding:8px'>{System.Security.SecurityElement.Escape(b.InventoryNumber)}</td>
                        <td style='padding:8px;color:#D32F2F;font-weight:bold'>{b.OverdueDays} дн.</td>
                        <td style='padding:8px'>{b.DueDate:dd.MM.yyyy}</td>
                    </tr>
                "));

                var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .header {{ background-color: #2F4F4F; color: white; padding: 15px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        table {{ width: 100%; border-collapse: collapse; }}
                        th {{ background-color: #F4F1EC; padding: 8px; text-align: left; }}
                        .footer {{ background-color: #F4F1EC; padding: 10px; text-align: center; font-size: 12px; color: #6F6F6F; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>Уважаемый(ая) {System.Security.SecurityElement.Escape(selected.FullName)}!</h2>
                    </div>
                    <div class='content'>
                        <p>Напоминаем, что у вас есть просроченные книги:</p>
                        <table>
                            <tr>
                                <th>Название книги</th>
                                <th>Инвентарный номер</th>
                                <th>Просрочка</th>
                                <th>Срок возврата</th>
                            </tr>
                            {booksHtml}
                        </table>
                        <p style='margin-top:20px'><strong>Пожалуйста, верните книги в библиотеку как можно скорее!</strong></p>
                        <p>Если у вас есть вопросы, свяжитесь с нами.</p>
                        <p>С уважением,<br/>Библиотека</p>
                    </div>
                    <div class='footer'>
                        Это письмо сформировано автоматически. Пожалуйста, не отвечайте на него.
                    </div>
                </body>
                </html>";

                System.Diagnostics.Debug.WriteLine($"Отправка письма на {selected.Email}...");

                var result = await _emailService.SendEmailAsync(selected.Email, selected.FullName, subject, body);

                if (result)
                {
                    MessageBox.Show($"Напоминание успешно отправлено на {selected.Email}",
                        "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при отправке письма. Проверьте настройки почты.\n\n" +
                        "Убедитесь, что:\n" +
                        "1. Включена двухфакторная аутентификация\n" +
                        "2. Создан пароль приложения\n" +
                        "3. В коде используется пароль приложения, а не пароль от почты",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    SendButton.IsEnabled = true;
                    SendButton.Content = "Отправить напоминание";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                MessageBox.Show($"Ошибка при отправке: {ex.Message}\n\nПодробности смотрите в окне Output Visual Studio",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                SendButton.IsEnabled = true;
                SendButton.Content = "Отправить напоминание";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}