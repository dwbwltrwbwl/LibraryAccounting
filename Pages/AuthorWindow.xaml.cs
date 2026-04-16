using LibraryAccounting.AppData;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibraryAccounting.Pages
{
    public partial class AuthorWindow : Window
    {
        public string FullName { get; private set; }
        public DateTime? BirthDate { get; private set; }
        public DateTime? DeathDate { get; private set; }
        public string Country { get; private set; }
        public string City { get; private set; }

        public bool IsDuplicateError { get; set; } = false;

        public AuthorWindow(Authors author = null)
        {
            InitializeComponent();

            if (author != null)
            {
                NameBox.Text = author.FullName;
                BirthDatePicker.SelectedDate = author.BirthDate;
                DeathDatePicker.SelectedDate = author.DeathDate;
                CountryBox.Text = author.Country;
                CityBox.Text = author.City;
            }
        }

        private bool ValidateFields()
        {
            bool isValid = true;
            string errorMessage = "";

            // Валидация ФИО
            string fullName = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                SetErrorStyle(NameBox, "ФИО не может быть пустым");
                errorMessage += "• Заполните ФИО автора\n";
                isValid = false;
            }
            else if (!Regex.IsMatch(fullName, @"^[a-zA-Zа-яА-Я\s\-']+$"))
            {
                SetErrorStyle(NameBox, "ФИО может содержать только буквы, пробелы, дефисы и апострофы");
                errorMessage += "• ФИО содержит недопустимые символы\n";
                isValid = false;
            }
            else if (fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length < 2)
            {
                SetErrorStyle(NameBox, "Введите полное ФИО (минимум имя и фамилию)");
                errorMessage += "• Введите полное ФИО (имя и фамилию)\n";
                isValid = false;
            }
            else
            {
                ClearErrorStyle(NameBox);
            }

            // Валидация даты рождения
            if (BirthDatePicker.SelectedDate == null)
            {
                SetErrorStyle(BirthDatePicker, "Дата рождения обязательна");
                errorMessage += "• Укажите дату рождения\n";
                isValid = false;
            }
            else if (BirthDatePicker.SelectedDate > DateTime.Today)
            {
                SetErrorStyle(BirthDatePicker, "Дата рождения не может быть в будущем");
                errorMessage += "• Дата рождения не может быть в будущем\n";
                isValid = false;
            }
            else if (BirthDatePicker.SelectedDate < new DateTime(1500, 1, 1))
            {
                SetErrorStyle(BirthDatePicker, "Укажите корректную дату рождения (после 1500 года)");
                errorMessage += "• Укажите корректную дату рождения\n";
                isValid = false;
            }
            else
            {
                ClearErrorStyle(BirthDatePicker);
            }

            // Валидация даты смерти (если указана)
            if (DeathDatePicker.SelectedDate.HasValue)
            {
                if (DeathDatePicker.SelectedDate > DateTime.Today)
                {
                    SetErrorStyle(DeathDatePicker, "Дата смерти не может быть в будущем");
                    errorMessage += "• Дата смерти не может быть в будущем\n";
                    isValid = false;
                }
                else if (BirthDatePicker.SelectedDate.HasValue &&
                         DeathDatePicker.SelectedDate <= BirthDatePicker.SelectedDate)
                {
                    SetErrorStyle(DeathDatePicker, "Дата смерти должна быть позже даты рождения");
                    errorMessage += "• Дата смерти должна быть позже даты рождения\n";
                    isValid = false;
                }
                else
                {
                    ClearErrorStyle(DeathDatePicker);
                }
            }
            else
            {
                ClearErrorStyle(DeathDatePicker);
            }

            // Валидация страны (необязательно, но если заполнена - проверяем)
            string country = CountryBox.Text.Trim();
            if (!string.IsNullOrEmpty(country) && !Regex.IsMatch(country, @"^[a-zA-Zа-яА-Я\s\-']+$"))
            {
                SetErrorStyle(CountryBox, "Страна может содержать только буквы, пробелы и дефисы");
                errorMessage += "• Страна содержит недопустимые символы\n";
                isValid = false;
            }
            else
            {
                ClearErrorStyle(CountryBox);
            }

            // Валидация города (необязательно, но если заполнен - проверяем)
            string city = CityBox.Text.Trim();
            if (!string.IsNullOrEmpty(city) && !Regex.IsMatch(city, @"^[a-zA-Zа-яА-Я\s\-']+$"))
            {
                SetErrorStyle(CityBox, "Город может содержать только буквы, пробелы и дефисы");
                errorMessage += "• Город содержит недопустимые символы\n";
                isValid = false;
            }
            else
            {
                ClearErrorStyle(CityBox);
            }

            if (!isValid)
            {
                ErrorTextBlock.Text = $"Пожалуйста, исправьте следующие ошибки:\n\n{errorMessage}";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorTextBlock.Visibility = Visibility.Collapsed;
            }

            return isValid;
        }

        private void SetErrorStyle(Control control, string message)
        {
            control.ToolTip = message;
            control.BorderBrush = Brushes.Red;
            control.BorderThickness = new Thickness(1);
        }

        private void ClearErrorStyle(Control control)
        {
            control.ToolTip = null;
            control.ClearValue(BorderBrushProperty);
            control.ClearValue(BorderThicknessProperty);
        }

        public void ShowDuplicateError(string authorName)
        {
            ErrorTextBlock.Text = $"Автор «{authorName}» уже существует!";
            ErrorTextBlock.Visibility = Visibility.Visible;
            IsDuplicateError = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем предыдущую ошибку
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Text = "";
            IsDuplicateError = false;

            if (!ValidateFields())
                return;

            FullName = NameBox.Text.Trim();
            BirthDate = BirthDatePicker.SelectedDate;
            DeathDate = DeathDatePicker.SelectedDate;
            Country = string.IsNullOrWhiteSpace(CountryBox.Text) ? null : CountryBox.Text.Trim();
            City = string.IsNullOrWhiteSpace(CityBox.Text) ? null : CityBox.Text.Trim();

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // Обработчики для сброса ошибок при изменении полей
        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearErrorStyle(NameBox);
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            IsDuplicateError = false;
        }

        private void BirthDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearErrorStyle(BirthDatePicker);
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            // Автоматическая валидация даты смерти
            if (DeathDatePicker.SelectedDate.HasValue && BirthDatePicker.SelectedDate.HasValue)
            {
                if (DeathDatePicker.SelectedDate <= BirthDatePicker.SelectedDate)
                {
                    SetErrorStyle(DeathDatePicker, "Дата смерти должна быть позже даты рождения");
                }
                else
                {
                    ClearErrorStyle(DeathDatePicker);
                }
            }
        }

        private void DeathDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearErrorStyle(DeathDatePicker);
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            // Автоматическая валидация даты смерти
            if (DeathDatePicker.SelectedDate.HasValue && BirthDatePicker.SelectedDate.HasValue)
            {
                if (DeathDatePicker.SelectedDate <= BirthDatePicker.SelectedDate)
                {
                    SetErrorStyle(DeathDatePicker, "Дата смерти должна быть позже даты рождения");
                }
                else
                {
                    ClearErrorStyle(DeathDatePicker);
                }
            }
        }

        private void CountryBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearErrorStyle(CountryBox);
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void CityBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearErrorStyle(CityBox);
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}