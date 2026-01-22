using LibraryAccounting.AppData;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class AccountView : Page
    {
        public AccountView()
        {
            InitializeComponent();
            LoadUser();
        }

        private void LoadUser()
        {
            if (AppConnect.CurrentUser == null)
                return;

            LoginText.Text = AppConnect.CurrentUser.Login;

            string lastName = AppConnect.CurrentUser.last_name ?? "";
            string firstName = AppConnect.CurrentUser.first_name ?? "";
            string middleName = AppConnect.CurrentUser.middle_name ?? "";

            FullNameText.Text = $"{lastName} {firstName} {middleName}".Trim();

            RoleText.Text = AppConnect.CurrentUser.Roles.RoleName;
        }


        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция смены пароля будет реализована позже",
                "Информация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            AppConnect.CurrentUser = null;

            var frame = ((MainWindow)Application.Current.MainWindow).frameMain;
            frame.Navigate(new LoginView());
        }
    }
}
