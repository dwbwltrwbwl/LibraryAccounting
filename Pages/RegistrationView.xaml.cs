using LibraryAccounting.Services;
using LibraryAccounting.Windows;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    /// <summary>
    /// Логика взаимодействия для RegistrationView.xaml
    /// </summary>
    public partial class RegistrationView : Page
    {
        public RegistrationView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Регистрация пользователя
        /// </summary>
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string lastName = LastNameTextBox.Text.Trim();
            string firstName = FirstNameTextBox.Text.Trim();
            string middleName = MiddleNameTextBox.Text.Trim();

            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            string error = RegistrationService.RegisterUser(
                login,
                password,
                confirmPassword,
                lastName,
                firstName,
                middleName
            );

            if (error == null)
            {
                var dialog = new MessageDialog(
                    "Регистрация",
                    "Пользователь успешно зарегистрирован."
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();

                NavigationService.Navigate(new LoginView());
            }
            else
            {
                var dialog = new MessageDialog(
                    "Ошибка регистрации",
                    error
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }


        /// <summary>
        /// Возврат к странице авторизации
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginView());
        }
    }
}
