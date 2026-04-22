using LibraryAccounting.AppData;
using LibraryAccounting.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibraryAccounting.Pages
{
    public partial class ChangePasswordWindow : Window
    {
        private bool isNewPasswordVisible = false;
        private bool isConfirmPasswordVisible = false;

        private TextBox tempNewPasswordBox = null;
        private TextBox tempConfirmPasswordBox = null;

        private bool isNewPasswordValid = false;
        private bool isPasswordMatch = false;

        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Показать/скрыть новый пароль
        /// </summary>
        private void ToggleNewPassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var border = NewPasswordBox.Parent as Grid;
            if (border == null) return;

            int index = border.Children.IndexOf(NewPasswordBox);

            if (!isNewPasswordVisible)
            {
                string currentPassword = NewPasswordBox.Password;

                tempNewPasswordBox = new TextBox
                {
                    Text = currentPassword,
                    Height = 36,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    FontSize = 14,
                    FontFamily = NewPasswordBox.FontFamily,
                    MaxLength = 50
                };

                tempNewPasswordBox.TextChanged += (s, ev) =>
                {
                    NewPasswordBox.Password = tempNewPasswordBox.Text;
                    NewPasswordBox_PasswordChanged(null, null);
                };

                border.Children.Remove(NewPasswordBox);
                border.Children.Insert(index, tempNewPasswordBox);

                button.Content = "🙈";
                isNewPasswordVisible = true;
            }
            else
            {
                string password = tempNewPasswordBox?.Text ?? "";

                if (tempNewPasswordBox != null)
                {
                    border.Children.Remove(tempNewPasswordBox);
                    tempNewPasswordBox = null;
                }

                border.Children.Insert(index, NewPasswordBox);
                NewPasswordBox.Password = password;

                button.Content = "👁️";
                isNewPasswordVisible = false;

                NewPasswordBox_PasswordChanged(null, null);
            }
        }

        /// <summary>
        /// Показать/скрыть подтверждение пароля
        /// </summary>
        private void ToggleConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var border = ConfirmPasswordBox.Parent as Grid;
            if (border == null) return;

            int index = border.Children.IndexOf(ConfirmPasswordBox);

            if (!isConfirmPasswordVisible)
            {
                string currentPassword = ConfirmPasswordBox.Password;

                tempConfirmPasswordBox = new TextBox
                {
                    Text = currentPassword,
                    Height = 36,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    FontSize = 14,
                    FontFamily = ConfirmPasswordBox.FontFamily,
                    MaxLength = 50
                };

                tempConfirmPasswordBox.TextChanged += (s, ev) =>
                {
                    ConfirmPasswordBox.Password = tempConfirmPasswordBox.Text;
                    ConfirmPasswordBox_PasswordChanged(null, null);
                };

                border.Children.Remove(ConfirmPasswordBox);
                border.Children.Insert(index, tempConfirmPasswordBox);

                button.Content = "🙈";
                isConfirmPasswordVisible = true;
            }
            else
            {
                string password = tempConfirmPasswordBox?.Text ?? "";

                if (tempConfirmPasswordBox != null)
                {
                    border.Children.Remove(tempConfirmPasswordBox);
                    tempConfirmPasswordBox = null;
                }

                border.Children.Insert(index, ConfirmPasswordBox);
                ConfirmPasswordBox.Password = password;

                button.Content = "👁️";
                isConfirmPasswordVisible = false;

                ConfirmPasswordBox_PasswordChanged(null, null);
            }
        }

        /// <summary>
        /// Получить пароль из поля (с учетом видимости)
        /// </summary>
        private string GetPassword(PasswordBox passwordBox, bool isVisible, TextBox tempTextBox)
        {
            if (isVisible && tempTextBox != null)
            {
                return tempTextBox.Text;
            }
            else
            {
                return passwordBox.Password;
            }
        }

        /// <summary>
        /// Проверка сложности нового пароля
        /// </summary>
        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string newPassword = GetPassword(NewPasswordBox, isNewPasswordVisible, tempNewPasswordBox);

            if (string.IsNullOrEmpty(newPassword))
            {
                PasswordStrengthText.Text = "";
                PasswordStrengthBorder.Background = new SolidColorBrush(Color.FromRgb(244, 241, 236));
                isNewPasswordValid = false;
                CheckFormValidity();
                return;
            }

            int strength = 0;
            string strengthText = "";
            Brush color;

            if (newPassword.Length >= 6) strength++;
            if (newPassword.Length >= 10) strength++;
            if (Regex.IsMatch(newPassword, @"[a-zа-я]")) strength++;
            if (Regex.IsMatch(newPassword, @"[A-ZА-Я]")) strength++;
            if (Regex.IsMatch(newPassword, @"[0-9]")) strength++;
            if (Regex.IsMatch(newPassword, @"[!@#$%^&*(),.?""{}|<>]")) strength++;

            if (strength <= 2)
            {
                strengthText = "Слабый";
                color = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                isNewPasswordValid = false;
            }
            else if (strength <= 4)
            {
                strengthText = "Средний";
                color = new SolidColorBrush(Color.FromRgb(255, 230, 200));
                isNewPasswordValid = true;
            }
            else
            {
                strengthText = "Сильный";
                color = new SolidColorBrush(Color.FromRgb(200, 255, 200));
                isNewPasswordValid = true;
            }

            PasswordStrengthText.Text = strengthText;
            PasswordStrengthBorder.Background = color;

            ConfirmPasswordBox_PasswordChanged(null, null);
        }

        /// <summary>
        /// Проверка совпадения паролей
        /// </summary>
        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string newPassword = GetPassword(NewPasswordBox, isNewPasswordVisible, tempNewPasswordBox);
            string confirmPassword = GetPassword(ConfirmPasswordBox, isConfirmPasswordVisible, tempConfirmPasswordBox);

            if (string.IsNullOrEmpty(confirmPassword))
            {
                isPasswordMatch = false;
                CheckFormValidity();
                return;
            }

            isPasswordMatch = (newPassword == confirmPassword);
            CheckFormValidity();
        }

        /// <summary>
        /// Проверка всей формы
        /// </summary>
        private void CheckFormValidity()
        {
            bool isFormValid = isNewPasswordValid && isPasswordMatch;

            SaveButton.IsEnabled = isFormValid;
            SaveButton.Opacity = isFormValid ? 1 : 0.6;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = GetPassword(NewPasswordBox, isNewPasswordVisible, tempNewPasswordBox);
            string confirmPassword = GetPassword(ConfirmPasswordBox, isConfirmPasswordVisible, tempConfirmPasswordBox);

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Введите новый пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AppConnect.CurrentUser == null)
                return;

            var db = AppConnect.model01 ?? new LibraryAccountingEntities();

            var user = db.Users
                .FirstOrDefault(u => u.UserId == AppConnect.CurrentUser.UserId);

            if (user == null)
            {
                MessageBox.Show("Пользователь не найден", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка сложности пароля
            if (!isNewPasswordValid)
            {
                MessageBox.Show("Пароль слишком слабый. Используйте минимум 6 символов, буквы разного регистра, цифры и спецсимволы",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Смена пароля с хэшированием
            user.PasswordHash = PasswordHasher.Hash(newPassword);
            db.SaveChanges();

            MessageBox.Show("Пароль успешно изменён",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}