using LibraryAccounting.AppData;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibraryAccounting.Pages
{
    public partial class PublisherWindow : Window
    {
        public string NameValue { get; private set; }
        public string CityValue { get; private set; }

        public bool IsDuplicateError { get; set; } = false;

        public PublisherWindow(Publishers publisher = null)
        {
            InitializeComponent();

            if (publisher != null)
            {
                NameBox.Text = publisher.PublisherName;
                CityBox.Text = publisher.City;
            }
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

            // Валидация города (необязательно, но если заполнен - проверяем)
            string city = CityBox.Text.Trim();
            if (!string.IsNullOrEmpty(city))
            {
                if (!Regex.IsMatch(city, @"^[a-zA-Zа-яА-Я\s\-']+$"))
                {
                    SetErrorStyle(CityBox, "Город может содержать только буквы, пробелы и дефисы");
                    errorMessage += "• Город содержит недопустимые символы\n";
                    isValid = false;
                }
                else if (city.Length < 2)
                {
                    SetErrorStyle(CityBox, "Название города должно содержать минимум 2 символа");
                    errorMessage += "• Название города слишком короткое\n";
                    isValid = false;
                }
                else
                {
                    ClearErrorStyle(CityBox);
                }
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

        public void ShowDuplicateError(string publisherName)
        {
            ErrorTextBlock.Text = $"Издательство «{publisherName}» уже существует!";
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

            NameValue = NameBox.Text.Trim();
            CityValue = string.IsNullOrWhiteSpace(CityBox.Text) ? null : CityBox.Text.Trim();

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

        private void CityBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearErrorStyle(CityBox);
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}