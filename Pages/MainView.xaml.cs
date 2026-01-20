using LibraryAccounting.AppData;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class MainView : Page
    {
        public MainView()
        {
            InitializeComponent();
            ConfigureByRole();
        }

        /// <summary>
        /// Настройка интерфейса в зависимости от роли
        /// </summary>
        private void ConfigureByRole()
        {
            if (AppConnect.CurrentUser == null)
                return;

            string role = AppConnect.CurrentUser.Roles.RoleName;

            // Пример: библиотекарь не видит отчеты
            if (role == "Librarian")
            {
                ReportsButton.Visibility = Visibility.Collapsed;
            }

            // Администратор видит всё (ничего не скрываем)
        }

        /// <summary>
        /// Получение Frame из MainWindow
        /// </summary>
        private Frame GetMainFrame()
        {
            return ((MainWindow)Application.Current.MainWindow).frameMain;
        }

        private void BooksButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainFrame().Navigate(new BooksView());
        }

        private void CopiesButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainFrame().Navigate(new BookCopiesView());
        }

        private void ReadersButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainFrame().Navigate(new ReadersView());
        }

        private void LoansButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainFrame().Navigate(new LoansView());
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainFrame().Navigate(new ReportsView());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AppConnect.CurrentUser = null;
            GetMainFrame().Navigate(new LoginView());
        }

        private void DirectoriesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Pages.DirectoriesView());
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Pages.UsersView());
        }
    }
}
