using LibraryAccounting.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class ReaderEditWindow : Window
    {
        private Readers _reader;

        // ➕ Добавление
        public ReaderEditWindow()
        {
            InitializeComponent();
            TitleText.Text = "Добавление читателя";

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
            LoadCategories();

            // Устанавливаем максимальную дату рождения (сегодня)
            BirthDatePicker.DisplayDateEnd = DateTime.Today;
        }

        private void LoadCategories()
        {
            var categories = AppConnect.model01.ReaderCategories.ToList();

            CategoryBox.ItemsSource = categories;
            CategoryBox.DisplayMemberPath = "CategoryName";
            CategoryBox.SelectedValuePath = "CategoryId";
        }

        public ReaderEditWindow(Readers reader)
        {
            InitializeComponent();
            TitleText.Text = "Редактирование читателя";

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
            LoadCategories();

            _reader = reader;

            LastNameBox.Text = reader.last_name;
            FirstNameBox.Text = reader.first_name;
            MiddleNameBox.Text = reader.middle_name;
            PhoneBox.Text = reader.Phone;
            EmailBox.Text = reader.Email;
            PassportBox.Text = reader.PassportData;

            BirthDatePicker.SelectedDate = reader.BirthDate;
            BirthDatePicker.DisplayDateEnd = DateTime.Today;
            CategoryBox.SelectedValue = reader.CategoryId;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_reader == null)
            {
                _reader = new Readers
                {
                    RegistrationDate = DateTime.Now
                };
                AppConnect.model01.Readers.Add(_reader);
            }

            string error = "";

            // 1. Фамилия (обязательно)
            if (string.IsNullOrWhiteSpace(LastNameBox.Text))
                error += "• Фамилия\n";

            // 2. Имя (обязательно)
            if (string.IsNullOrWhiteSpace(FirstNameBox.Text))
                error += "• Имя\n";

            // 3. Отчество (НЕ обязательно - убрали проверку)

            // 4. Телефон (обязательно + полный номер)
            string phoneDigits = new string(PhoneBox.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(PhoneBox.Text))
                error += "• Телефон\n";
            else if (phoneDigits.Length != 11)
            {
                MessageBox.Show("Введите полный номер телефона (11 цифр)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneBox.Focus();
                return;
            }

            // 5. Email (обязательно)
            if (string.IsNullOrWhiteSpace(EmailBox.Text))
                error += "• Email\n";

            // 6. Паспорт (обязательно + 10 цифр)
            if (string.IsNullOrWhiteSpace(PassportBox.Text))
                error += "• Паспорт\n";
            else if (PassportBox.Text.Trim().Length != 10)
            {
                MessageBox.Show("Паспорт должен содержать 10 цифр (серия и номер)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PassportBox.Focus();
                return;
            }

            // 7. Дата рождения (обязательно + не в будущем)
            if (BirthDatePicker.SelectedDate == null)
                error += "• Дата рождения\n";
            else if (BirthDatePicker.SelectedDate.Value > DateTime.Today)
            {
                MessageBox.Show("Дата рождения не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BirthDatePicker.Focus();
                return;
            }

            // 8. Категория (обязательно)
            if (CategoryBox.SelectedValue == null)
                error += "• Категория\n";

            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show("Заполните обязательные поля:\n" + error, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка email
            if (!IsValidEmail(EmailBox.Text.Trim()))
            {
                MessageBox.Show("Некорректный email", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailBox.Focus();
                return;
            }

            // Проверка уникальности email
            int currentReaderId = _reader.ReaderId;
            bool emailExists = AppConnect.model01.Readers
                .Any(r => r.Email == EmailBox.Text.Trim() && r.ReaderId != currentReaderId);

            if (emailExists)
            {
                MessageBox.Show("Пользователь с таким Email уже существует", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailBox.Focus();
                return;
            }

            // Проверка уникальности паспорта
            bool passportExists = AppConnect.model01.Readers
                .Any(r => r.PassportData == PassportBox.Text.Trim() && r.ReaderId != currentReaderId);

            if (passportExists)
            {
                MessageBox.Show("Пользователь с таким паспортом уже существует", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PassportBox.Focus();
                return;
            }

            // Сохранение данных
            _reader.last_name = LastNameBox.Text.Trim();
            _reader.first_name = FirstNameBox.Text.Trim();
            _reader.middle_name = string.IsNullOrWhiteSpace(MiddleNameBox.Text)
                ? null
                : MiddleNameBox.Text.Trim();

            _reader.Phone = PhoneBox.Text?.Trim();
            _reader.Email = EmailBox.Text?.Trim();
            _reader.PassportData = PassportBox.Text?.Trim();
            _reader.BirthDate = BirthDatePicker.SelectedDate.Value;
            _reader.CategoryId = (int)CategoryBox.SelectedValue;
            _reader.Status = _reader.Status ?? "Активный";

            AppConnect.model01.SaveChanges();

            DialogResult = true;
            Close();
        }

        private void OnlyLetters_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[а-яА-Яa-zA-Z]+$");
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void Phone_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            string digits = new string(textBox.Text.Where(char.IsDigit).ToArray());

            if (digits.Length > 11)
                digits = digits.Substring(0, 11);

            string formatted = "+7 ";

            if (digits.Length > 1)
                formatted += "(" + digits.Substring(1, Math.Min(3, digits.Length - 1));

            if (digits.Length >= 4)
                formatted += ") " + digits.Substring(4, Math.Min(3, digits.Length - 4));

            if (digits.Length >= 7)
                formatted += "-" + digits.Substring(7, Math.Min(2, digits.Length - 7));

            if (digits.Length >= 9)
                formatted += "-" + digits.Substring(9, Math.Min(2, digits.Length - 9));

            textBox.Text = formatted;
            textBox.CaretIndex = textBox.Text.Length;
        }

        private void OnlyDigits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}