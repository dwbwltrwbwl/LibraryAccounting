using LibraryAccounting.AppData;
using LibraryAccounting.Services;
using LibraryAccounting.Windows;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class LoginView : Page
    {
        private bool isPasswordVisible = false;
        private TextBox tempPasswordTextBox = null;

        public LoginView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Показать/скрыть пароль
        /// </summary>
        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var grid = PasswordBox.Parent as Grid;
            if (grid == null) return;

            int index = grid.Children.IndexOf(PasswordBox);

            if (!isPasswordVisible)
            {
                string currentPassword = PasswordBox.Password;

                tempPasswordTextBox = new TextBox
                {
                    Text = currentPassword,
                    Height = 36,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    FontSize = 14,
                    FontFamily = PasswordBox.FontFamily,
                    MaxLength = 50
                };

                grid.Children.Remove(PasswordBox);
                grid.Children.Insert(index, tempPasswordTextBox);

                button.Content = "🙈";
                isPasswordVisible = true;
            }
            else
            {
                string password = tempPasswordTextBox?.Text ?? "";

                if (tempPasswordTextBox != null)
                {
                    grid.Children.Remove(tempPasswordTextBox);
                    tempPasswordTextBox = null;
                }

                grid.Children.Insert(index, PasswordBox);
                PasswordBox.Password = password;

                button.Content = "👁️";
                isPasswordVisible = false;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = GetPassword();

            ErrorTextBlock.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorTextBlock.Text = "Введите логин и пароль";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            AppConnect.model01 = new LibraryAccountingEntities();

            var user = AppConnect.model01.Users
                .FirstOrDefault(u => u.Login == login);

            // Проверка: существует ли пользователь
            if (user == null)
            {
                var dialog = new MessageDialog(
                    "Ошибка авторизации",
                    "Неверный логин или пароль. Повторите попытку."
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();

                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            // Проверка: правильный ли пароль (с использованием хэширования)
            if (!PasswordHasher.Verify(password, user.PasswordHash))
            {
                var dialog = new MessageDialog(
                    "Ошибка авторизации",
                    "Неверный логин или пароль. Повторите попытку."
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();

                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            // Проверка: не заблокирован ли пользователь
            if (user.IsBlocked)
            {
                var dialog = new MessageDialog(
                    "Доступ запрещён",
                    "Ваша учетная запись заблокирована.\nОбратитесь к администратору."
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                return;
            }

            // ✅ НОВАЯ ПРОВЕРКА: одобрен ли пользователь
            if (!user.IsApproved)
            {
                var dialog = new MessageDialog(
                    "Доступ запрещён",
                    "Ваша учетная запись ещё не одобрена администратором.\nПожалуйста, ожидайте подтверждения."
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                return;
            }

            // Обновляем дату последнего входа
            user.LastLoginDate = DateTime.Now;
            AppConnect.model01.SaveChanges();

            // Успешный вход
            string roleName = user.Roles.RoleName;

            var successDialog = new MessageDialog(
                "Авторизация",
                $"Вход выполнен успешно.\nРоль пользователя: {roleName}"
            );
            successDialog.Owner = Window.GetWindow(this);
            successDialog.ShowDialog();

            AppConnect.CurrentUser = user;
            NavigationService.Navigate(new MainView());
        }

        /// <summary>
        /// Получить пароль (из PasswordBox или TextBox в зависимости от режима)
        /// </summary>
        private string GetPassword()
        {
            if (isPasswordVisible && tempPasswordTextBox != null)
            {
                return tempPasswordTextBox.Text;
            }
            else
            {
                return PasswordBox.Password;
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegistrationView());
        }
    }
}