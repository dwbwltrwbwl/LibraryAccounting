using LibraryAccounting.AppData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibraryAccounting.Pages
{
    public partial class PublisherWindow : Window
    {
        public string NameValue { get; private set; }
        public int? SelectedCityId { get; private set; }

        public bool IsDuplicateError { get; set; } = false;

        // Конструктор для добавления
        public PublisherWindow()
        {
            InitializeComponent();
            TitleText.Text = "Добавление издательства";  // ← Добавить эту строку
            LoadCities();
        }

        // Конструктор для редактирования
        public PublisherWindow(Publishers publisher)
        {
            InitializeComponent();
            TitleText.Text = "Редактирование издательства";  // ← Добавить эту строку
            LoadCities();

            if (publisher != null)
            {
                NameBox.Text = publisher.PublisherName;
                if (publisher.CityId.HasValue)
                {
                    CityComboBox.SelectedValue = publisher.CityId.Value;
                }
            }
        }

        private void LoadCities(string filter = null)
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var query = AppConnect.model01.Cities.AsQueryable();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    query = query.Where(c => c.CityName.ToLower().Contains(filter.ToLower()));
                }

                CityComboBox.ItemsSource = query.OrderBy(c => c.CityName).ToList();
                CityComboBox.DisplayMemberPath = "CityName";
                CityComboBox.SelectedValuePath = "CityId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки городов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CityComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchText = CityComboBox.Text?.Trim() ?? "";
            LoadCities(searchText);
            if (CityComboBox.ItemsSource != null && ((System.Collections.IList)CityComboBox.ItemsSource).Count > 0)
                CityComboBox.IsDropDownOpen = true;
        }

        private bool ValidateFields()
        {
            bool isValid = true;
            string errorMessage = "";

            // Валидация названия
            string name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                SetErrorStyle(NameBox, "Название издательства не может быть пустым");
                errorMessage += "• Заполните название издательства\n";
                isValid = false;
            }
            else if (!Regex.IsMatch(name, @"^[a-zA-Zа-яА-Я0-9\s\.,!?\-'\(\)&]+$"))
            {
                SetErrorStyle(NameBox, "Название может содержать буквы, цифры, пробелы и знаки: .,!?- '()&");
                errorMessage += "• Название содержит недопустимые символы\n";
                isValid = false;
            }
            else if (name.Length < 2)
            {
                SetErrorStyle(NameBox, "Название должно содержать минимум 2 символа");
                errorMessage += "• Название слишком короткое (минимум 2 символа)\n";
                isValid = false;
            }
            else
            {
                ClearErrorStyle(NameBox);
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

        public void ShowDuplicateError(string publisherName)
        {
            ErrorTextBlock.Text = $"Издательство «{publisherName}» уже существует!";
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

            NameValue = NameBox.Text.Trim();
            SelectedCityId = CityComboBox.SelectedValue as int?;

            // Показываем окно с сообщением об успехе
            var messageDialog = new LibraryAccounting.Windows.MessageDialog(
                "Успех",
                "Издательство успешно сохранено!"
            );
            messageDialog.Owner = this;
            messageDialog.ShowDialog(); // Ждем, пока пользователь нажмет ОК

            // После закрытия окна сообщения, закрываем окно издательства
            DialogResult = true;
            this.Close();
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

        private void CityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearErrorStyle(CityComboBox);
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}