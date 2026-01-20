using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class LoginView : Page
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            ErrorTextBlock.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorTextBlock.Text = "Введите логин и пароль";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            AppConnect.model01 = new LibraryAccountingEntities();

            var user = AppConnect.model01.Users
                .FirstOrDefault(u => u.Login == login && u.PasswordHash == password);

            if (user != null)
            {
                string roleName = user.Roles.RoleName;

                var dialog = new MessageDialog(
                    "Авторизация",
                    $"Вход выполнен успешно.\nРоль пользователя: {roleName}"
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                AppConnect.CurrentUser = user;
                NavigationService.Navigate(new MainView());
            }
            else
            {
                var dialog = new MessageDialog(
                    "Ошибка авторизации",
                    "Неверный логин или пароль. Повторите попытку."
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();

                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegistrationView());
        }
    }
}
