using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class UserApprovalView : Page
    {
        public class UserRequestViewModel
        {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string Login { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string RequestDate { get; set; }
            public int? Experience { get; set; }
            public string BirthDate { get; set; }
        }

        public UserApprovalView()
        {
            InitializeComponent();
            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var requests = AppConnect.model01.Users
                    .Where(u => u.IsApproved == false && u.RoleId == 2)
                    .Select(u => new
                    {
                        u.UserId,
                        u.last_name,
                        u.first_name,
                        u.middle_name,
                        u.Login,
                        u.Email,
                        u.Phone,
                        u.RegistrationRequestDate,
                        u.ExperienceYears,
                        u.BirthDate
                    })
                    .ToList()
                    .Select(u => new UserRequestViewModel
                    {
                        UserId = u.UserId,
                        FullName = (u.last_name ?? "") + " " + (u.first_name ?? "") + " " + (u.middle_name ?? ""),
                        Login = u.Login,
                        Email = u.Email ?? "",
                        Phone = u.Phone ?? "",
                        RequestDate = u.RegistrationRequestDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана",
                        Experience = u.ExperienceYears,
                        BirthDate = u.BirthDate?.ToString("dd.MM.yyyy") ?? ""
                    })
                    .OrderByDescending(u => u.RequestDate)
                    .ToList();

                UsersDataGrid.ItemsSource = requests;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить отображение деталей
        }

        private void ApproveUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            int userId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите одобрить регистрацию этого пользователя?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var user = AppConnect.model01.Users.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                {
                    user.IsApproved = true;
                    AppConnect.model01.SaveChanges();

                    ShowInfo($"Пользователь {user.Login} успешно одобрен");
                    LoadRequests();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при одобрении: {ex.Message}");
            }
        }

        private void RejectUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            int userId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите отклонить регистрацию?\nПользователь будет удалён из системы.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var user = AppConnect.model01.Users.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                {
                    AppConnect.model01.Users.Remove(user);
                    AppConnect.model01.SaveChanges();

                    ShowInfo("Заявка отклонена, пользователь удалён");
                    LoadRequests();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при отклонении: {ex.Message}");
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