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
            SetActiveButton(null);
            MainContentFrame.Navigate(new MainDashboardView());
        }

        /// <summary>
        /// Настройка интерфейса в зависимости от роли
        /// </summary>
        private void ConfigureByRole()
        {
            if (AppConnect.IsLibrarian)
            {
                UsersButton.Visibility = Visibility.Collapsed; // ❌ скрыто
                ReportsButton.Visibility = Visibility.Visible; // ✅ доступно
            }

            if (AppConnect.IsAdmin)
            {
                UsersButton.Visibility = Visibility.Visible; // ✅
                ReportsButton.Visibility = Visibility.Visible;
            }
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
            SetActiveButton(BooksButton);
            MainContentFrame.Navigate(new BooksView());
        }

        private void CopiesButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(CopiesButton);
            MainContentFrame.Navigate(new BookCopiesView());
        }

        private void ReadersButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ReadersButton);
            MainContentFrame.Navigate(new ReadersView());
        }

        private void LoansButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(LoansButton);
            MainContentFrame.Navigate(new LoansView());
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ReportsButton);
            MainContentFrame.Navigate(new ReportsView());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AppConnect.CurrentUser = null;
            GetMainFrame().Navigate(new LoginView());
        }

        private void DirectoriesButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(DirectoriesButton);
            MainContentFrame.Navigate(new DirectoriesView());
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(UsersButton);
            MainContentFrame.Navigate(new UsersView());
        }
        private void SetActiveButton(Button activeButton)
        {
            Button[] menuButtons =
            {
        BooksButton,
        CopiesButton,
        ReadersButton,
        LoansButton,
        ReportsButton,
        DirectoriesButton,
        UsersButton,
        AccountButton
    };

            // Сброс всех вкладок
            foreach (var btn in menuButtons)
                btn.Style = (Style)FindResource("MenuButtonStyle");

            // Активная вкладка
            if (activeButton != null)
                activeButton.Style = (Style)FindResource("MenuButtonActiveStyle");

            // 🔥 Главная — ВСЕГДА выделена
            HomeButton.Style = (Style)FindResource("MenuButtonHomeStyle");
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(null);
            MainContentFrame.Navigate(new MainDashboardView());
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(AccountButton);
            MainContentFrame.Navigate(new AccountView());
        }

    }
}
