using LibraryAccounting.AppData;
using System;
using System.Linq;
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
        public int? SelectedAgeRatingId { get; private set; }

        // Конструктор для добавления нового жанра
        public GenreWindow()
        {
            InitializeComponent();
            LoadAgeRatings();
            TitleText.Text = "Добавление жанра";
            ClearFields();
        }

        // Конструктор для редактирования существующего жанра
        public GenreWindow(Genres genre)
        {
            InitializeComponent();
            LoadAgeRatings();
            TitleText.Text = "Редактирование жанра";

            NameBox.Text = genre.Name;
            DescriptionBox.Text = genre.Description;

            if (genre.AgeRatingId.HasValue)
            {
                AgeRatingBox.SelectedValue = genre.AgeRatingId.Value;
            }
        }

        // Конструктор для восстановления данных при ошибке
        public GenreWindow(string name, string description, int? ageRatingId)
        {
            InitializeComponent();
            LoadAgeRatings();
            TitleText.Text = "Добавление жанра";

            NameBox.Text = name;
            DescriptionBox.Text = description;

            if (ageRatingId.HasValue)
            {
                AgeRatingBox.SelectedValue = ageRatingId.Value;
            }
        }

        private void LoadAgeRatings()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                var ageRatings = AppConnect.model01.AgeRatings
                    .OrderBy(ar => ar.SortOrder)
                    .ToList();

                AgeRatingBox.ItemsSource = ageRatings;
                AgeRatingBox.DisplayMemberPath = "AgeRatingName";
                AgeRatingBox.SelectedValuePath = "AgeRatingId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки возрастных рейтингов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFields()
        {
            NameBox.Text = "";
            DescriptionBox.Text = "";
            AgeRatingBox.SelectedIndex = -1;
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }

        private bool ValidateFields()
        {
            bool isValid = true;
            string errorMessage = "";

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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            NameValue = NameBox.Text.Trim();
            DescriptionValue = DescriptionBox.Text.Trim();
            SelectedAgeRatingId = (int)AgeRatingBox.SelectedValue;

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
        }

        private void DescriptionBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearErrorStyle(DescriptionBox);
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void AgeRatingBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearErrorStyle(AgeRatingBox);
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}