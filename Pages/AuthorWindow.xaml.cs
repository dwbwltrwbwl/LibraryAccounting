using LibraryAccounting.AppData;
using System;
using System.Linq;
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
        public int? SelectedCityId { get; private set; }

        public bool IsDuplicateError { get; set; } = false;

        public AuthorWindow(Authors author = null)
        {
            InitializeComponent();
            LoadCountries();

            if (author != null)
            {
                // Режим редактирования
                TitleText.Text = "Редактирование автора";

                NameBox.Text = author.FullName;
                BirthDatePicker.SelectedDate = author.BirthDate;
                DeathDatePicker.SelectedDate = author.DeathDate;

                if (author.CityId.HasValue)
                {
                    var city = AppConnect.model01.Cities.FirstOrDefault(c => c.CityId == author.CityId);
                    if (city != null)
                    {
                        CountryComboBox.SelectedValue = city.CountryId;
                        LoadCities(city.CountryId);
                        CityComboBox.SelectedValue = author.CityId;
                    }
                }
            }
            else
            {
                // Режим добавления
                TitleText.Text = "Добавление автора";
                ClearFields();  // ← ДОБАВЬТЕ ЭТУ СТРОКУ
            }
        }
        private void ClearFields()
        {
            NameBox.Text = "";
            BirthDatePicker.SelectedDate = null;
            DeathDatePicker.SelectedDate = null;
            CountryComboBox.SelectedIndex = -1;
            CityComboBox.ItemsSource = null;
            CityComboBox.IsEnabled = false;
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }
        private void LoadCountries()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                CountryComboBox.ItemsSource = AppConnect.model01.Countries
                    .OrderBy(c => c.CountryName)
                    .ToList();
                CountryComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки стран: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadCities(int? countryId)
        {
            try
            {
                if (countryId.HasValue)
                {
                    var cities = AppConnect.model01.Cities
                        .Where(c => c.CountryId == countryId.Value)
                        .OrderBy(c => c.CityName)
                        .ToList();

                    CityComboBox.ItemsSource = cities;
                    CityComboBox.IsEnabled = cities.Any();
                    CityComboBox.SelectedIndex = -1;
                }
                else
                {
                    CityComboBox.ItemsSource = null;
                    CityComboBox.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки городов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CountryComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            string searchText = comboBox.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Если поиск пустой - показываем все страны
                comboBox.ItemsSource = AppConnect.model01.Countries
                    .OrderBy(c => c.CountryName)
                    .ToList();
            }
            else
            {
                // Фильтруем страны по введенному тексту
                var filteredCountries = AppConnect.model01.Countries
                    .Where(c => c.CountryName.ToLower().Contains(searchText))
                    .OrderBy(c => c.CountryName)
                    .ToList();

                comboBox.ItemsSource = filteredCountries;

                // Если есть результат, открываем выпадающий список
                if (filteredCountries.Any())
                {
                    comboBox.IsDropDownOpen = true;
                }
            }
        }
        private void CountryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CountryComboBox.SelectedItem != null)
            {
                var selectedCountry = (Countries)CountryComboBox.SelectedItem;
                LoadCities(selectedCountry.CountryId);
            }
            else
            {
                LoadCities(null);
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
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Text = "";
            IsDuplicateError = false;

            if (!ValidateFields())
                return;

            FullName = NameBox.Text.Trim();
            BirthDate = BirthDatePicker.SelectedDate;
            DeathDate = DeathDatePicker.SelectedDate;
            SelectedCityId = CityComboBox.SelectedValue as int?;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

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
    }
}