using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class UsersView : Page
    {
        public UsersView()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var users = AppConnect.model01.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Login,
                    Role = u.Roles.RoleName,
                    u.IsBlocked,
                    Status = u.IsBlocked ? "Заблокирован" : "Активен"
                })
                .ToList();

            UsersDataGrid.ItemsSource = users;
        }
        private void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
            {
                ShowError("Выберите пользователя");
                return;
            }

            dynamic selected = UsersDataGrid.SelectedItem;
            int userId = selected.UserId;

            var user = AppConnect.model01.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
                return;

            // 🔥 ЗАПРЕТ НА БЛОКИРОВКУ АДМИНИСТРАТОРА
            if (user.Roles.RoleName == "Admin")
            {
                ShowError("Невозможно заблокировать администратора");
                return;
            }

            if (user.IsBlocked)
            {
                ShowError("Пользователь уже заблокирован");
                return;
            }

            user.IsBlocked = true;
            AppConnect.model01.SaveChanges();

            LoadUsers();
            ShowInfo("Пользователь заблокирован");
        }

        private void UnblockButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
            {
                ShowError("Выберите пользователя");
                return;
            }

            dynamic selected = UsersDataGrid.SelectedItem;
            int userId = selected.UserId;

            var user = AppConnect.model01.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return;

            if (!user.IsBlocked)
            {
                ShowError("Пользователь не заблокирован");
                return;
            }

            user.IsBlocked = false;
            AppConnect.model01.SaveChanges();

            LoadUsers();
            ShowInfo("Пользователь разблокирован");
        }


        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Окно добавления пользователя будет реализовано позже");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
            {
                ShowError("Выберите пользователя для редактирования");
                return;
            }

            MessageBox.Show("Окно редактирования пользователя будет реализовано позже");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
            {
                ShowError("Выберите пользователя для удаления");
                return;
            }

            dynamic selected = UsersDataGrid.SelectedItem;
            int userId = selected.UserId;

            var user = AppConnect.model01.Users.FirstOrDefault(u => u.UserId == userId);

            if (user != null)
            {
                if (user.Login == "admin")
                {
                    ShowError("Нельзя удалить администратора");
                    return;
                }

                AppConnect.model01.Users.Remove(user);
                AppConnect.model01.SaveChanges();

                LoadUsers();
                ShowInfo("Пользователь удален");
            }
        }

        private void ShowError(string message)
        {
            var dialog = new MessageDialog("Ошибка", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void ShowInfo(string message)
        {
            var dialog = new MessageDialog("Информация", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }
    }
}
