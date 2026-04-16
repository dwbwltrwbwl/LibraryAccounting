using LibraryAccounting.AppData;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibraryAccounting.Pages
{
    public partial class GenreWindow : Window
    {
        public string NameValue { get; private set; }
        public string DescriptionValue { get; private set; }
        public string AgeRatingValue { get; private set; }

        public GenreWindow(string name = "", string desc = "", string age = "")
        {
            InitializeComponent();
            NameBox.Text = name;
            DescriptionBox.Text = desc;

            if (!string.IsNullOrEmpty(age))
            {
                foreach (ComboBoxItem item in AgeRatingBox.Items)
                {
                    if (item.Content.ToString() == age)
                    {
                        AgeRatingBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        public GenreWindow(Genres genre)
        {
            InitializeComponent();
            NameBox.Text = genre.Name;
            DescriptionBox.Text = genre.Description;

            // Выбираем элемент в ComboBox по тексту
            foreach (ComboBoxItem item in AgeRatingBox.Items)
            {
                if (item.Content.ToString() == genre.AgeRating)
                {
                    AgeRatingBox.SelectedItem = item;
                    break;
                }
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
                SetErrorStyle(NameBox, "Название не может быть пустым.");
                errorMessage += "• Заполните название жанра\n";
                isValid = false;
            }
            else if (!Regex.IsMatch(name, @"^[a-zA-Zа-яА-Я0-9\s\.,!?\-']+$"))
            {
                SetErrorStyle(NameBox, "Название может содержать буквы, цифры, пробелы и знаки: .,!?- '");
                errorMessage += "• Название содержит недопустимые символы\n";
                isValid = false;
            }
            else
            {
                ClearErrorStyle(NameBox);
            }

            // Валидация описания
            string description = DescriptionBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                SetErrorStyle(DescriptionBox, "Описание не может быть пустым.");
                errorMessage += "• Заполните описание жанра\n";
                isValid = false;
            }
            else
            {
                ClearErrorStyle(DescriptionBox);
            }

            // Валидация возрастного рейтинга
            if (AgeRatingBox.SelectedItem == null)
            {
                SetErrorStyle(AgeRatingBox, "Выберите возрастной рейтинг.");
                errorMessage += "• Выберите возрастной рейтинг\n";
                isValid = false;
            }
            else
            {
                ClearErrorStyle(AgeRatingBox);
            }

            if (!isValid)
            {
                MessageBox.Show($"Пожалуйста, исправьте следующие ошибки:\n\n{errorMessage}",
                    "Ошибка валидации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            NameValue = NameBox.Text.Trim();
            DescriptionValue = DescriptionBox.Text.Trim();
            AgeRatingValue = (AgeRatingBox.SelectedItem as ComboBoxItem).Content.ToString();

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearErrorStyle(NameBox);
        }

        private void DescriptionBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearErrorStyle(DescriptionBox);
        }

        private void AgeRatingBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearErrorStyle(AgeRatingBox);
        }
    }
}