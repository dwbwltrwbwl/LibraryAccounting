using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LibraryAccounting.Pages
{
    public partial class UsersView : Page
    {
        private List<UserDisplayModel> allUsers = new List<UserDisplayModel>();
        private string currentSortColumn = "Login";
        private ListSortDirection currentSortDirection = ListSortDirection.Ascending;
        private bool isLoaded = false;

        public class UserDisplayModel
        {
            public int UserId { get; set; }
            public string Login { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public string Role { get; set; }
            public bool IsBlocked { get; set; }
            public string Status { get; set; }
            public int? Experience { get; set; }
            public string BirthDate { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string RegistrationDate { get; set; }
            public string LastLoginDate { get; set; }
        }

        public UsersView()
        {
            InitializeComponent();
            Loaded += UsersView_Loaded;
        }

        private void UsersView_Loaded(object sender, RoutedEventArgs e)
        {
            isLoaded = true;
            LoadRoleFilter();
            LoadUsers();
        }

        private void LoadRoleFilter()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                var roles = AppConnect.model01.Roles.Select(r => r.RoleName).ToList();
                roles.Insert(0, "Все");
                if (RoleFilterComboBox != null)
                {
                    RoleFilterComboBox.ItemsSource = roles;
                    RoleFilterComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRoleFilter error: {ex.Message}");
            }
        }

        private void LoadUsers()
        {
            if (!isLoaded) return;

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Сначала загружаем данные из БД без форматирования дат
                var usersFromDb = AppConnect.model01.Users
                    .Select(u => new
                    {
                        u.UserId,
                        u.Login,
                        u.last_name,
                        u.first_name,
                        u.middle_name,
                        u.Roles.RoleName,
                        u.IsBlocked,
                        u.ExperienceYears,
                        BirthDate = u.BirthDate,
                        u.Email,
                        u.Phone,
                        RegistrationDate = u.RegistrationDate,
                        LastLoginDate = u.LastLoginDate
                    })
                    .ToList();

                // Затем форматируем даты в памяти (здесь ToString() уже работает)
                allUsers = usersFromDb.Select(u => new UserDisplayModel
                {
                    UserId = u.UserId,
                    Login = u.Login ?? "",
                    LastName = u.last_name ?? "",
                    FirstName = u.first_name ?? "",
                    MiddleName = u.middle_name ?? "",
                    Role = u.RoleName ?? "",
                    IsBlocked = u.IsBlocked,
                    Status = u.IsBlocked ? "Заблокирован" : "Активен",
                    Experience = u.ExperienceYears,
                    BirthDate = u.BirthDate.HasValue ? u.BirthDate.Value.ToString("dd.MM.yyyy") : "",
                    Email = u.Email ?? "",
                    Phone = u.Phone ?? "",
                    RegistrationDate = u.RegistrationDate.HasValue ? u.RegistrationDate.Value.ToString("dd.MM.yyyy HH:mm") : "",
                    LastLoginDate = u.LastLoginDate.HasValue ? u.LastLoginDate.Value.ToString("dd.MM.yyyy HH:mm") : ""
                }).ToList();

                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadUsers error: {ex.Message}");
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void ApplyFiltersAndSort()
        {
            if (UsersDataGrid == null) return;

            try
            {
                var filtered = allUsers.AsEnumerable();

                // Поиск по логину, ФИО, email, телефону
                string searchText = SearchTextBox?.Text?.Trim().ToLower() ?? "";
                if (!string.IsNullOrEmpty(searchText))
                {
                    filtered = filtered.Where(u =>
                        u.Login.ToLower().Contains(searchText) ||
                        u.LastName.ToLower().Contains(searchText) ||
                        u.FirstName.ToLower().Contains(searchText) ||
                        (u.MiddleName?.ToLower().Contains(searchText) ?? false) ||
                        u.Email.ToLower().Contains(searchText) ||
                        u.Phone.Contains(searchText)
                    );
                }

                // Фильтр по роли
                if (RoleFilterComboBox?.SelectedItem != null)
                {
                    string selectedRole = RoleFilterComboBox.SelectedItem as string;
                    if (selectedRole != null && selectedRole != "Все")
                    {
                        filtered = filtered.Where(u => u.Role == selectedRole);
                    }
                }

                // Фильтр по статусу
                if (StatusFilterComboBox?.SelectedItem != null)
                {
                    var selectedStatusItem = StatusFilterComboBox.SelectedItem as ComboBoxItem;
                    string selectedStatus = selectedStatusItem?.Content?.ToString();
                    if (selectedStatus != null && selectedStatus != "Все")
                    {
                        bool isBlocked = (selectedStatus == "Заблокирован");
                        filtered = filtered.Where(u => u.IsBlocked == isBlocked);
                    }
                }

                // Сортировка
                var sorted = SortUsers(filtered);
                UsersDataGrid.ItemsSource = sorted.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyFiltersAndSort error: {ex.Message}");
            }
        }

        private IEnumerable<UserDisplayModel> SortUsers(IEnumerable<UserDisplayModel> users)
        {
            switch (currentSortColumn)
            {
                case "Login":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.Login)
                        : users.OrderByDescending(u => u.Login);
                case "LastName":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.LastName)
                        : users.OrderByDescending(u => u.LastName);
                case "FirstName":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.FirstName)
                        : users.OrderByDescending(u => u.FirstName);
                case "Role":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.Role)
                        : users.OrderByDescending(u => u.Role);
                case "Status":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.Status)
                        : users.OrderByDescending(u => u.Status);
                case "Experience":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.Experience)
                        : users.OrderByDescending(u => u.Experience);
                case "BirthDate":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.BirthDate)
                        : users.OrderByDescending(u => u.BirthDate);
                case "Email":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.Email)
                        : users.OrderByDescending(u => u.Email);
                case "Phone":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.Phone)
                        : users.OrderByDescending(u => u.Phone);
                case "RegistrationDate":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.RegistrationDate)
                        : users.OrderByDescending(u => u.RegistrationDate);
                case "LastLoginDate":
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.LastLoginDate)
                        : users.OrderByDescending(u => u.LastLoginDate);
                default:
                    return currentSortDirection == ListSortDirection.Ascending
                        ? users.OrderBy(u => u.Login)
                        : users.OrderByDescending(u => u.Login);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void RoleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox != null) SearchTextBox.Text = "";
            if (RoleFilterComboBox != null) RoleFilterComboBox.SelectedIndex = 0;
            if (StatusFilterComboBox != null) StatusFilterComboBox.SelectedIndex = 0;
            ApplyFiltersAndSort();
        }

        private void UsersDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column;
            var sortMemberPath = (column as DataGridBoundColumn)?.SortMemberPath ?? column.Header.ToString();

            if (currentSortColumn == sortMemberPath)
            {
                currentSortDirection = currentSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                currentSortColumn = sortMemberPath;
                currentSortDirection = ListSortDirection.Ascending;
            }

            e.Handled = true;
            ApplyFiltersAndSort();

            // Обновляем индикатор сортировки в заголовке
            if (UsersDataGrid != null)
            {
                foreach (var col in UsersDataGrid.Columns)
                {
                    col.SortDirection = null;
                }
                column.SortDirection = currentSortDirection;
            }
        }

        private void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid == null) return;

            var items = UsersDataGrid.ItemsSource as List<UserDisplayModel>;
            if (items == null || items.Count == 0)
            {
                ShowError("Нет данных для экспорта");
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Сохранить CSV файл",
                Filter = "CSV файлы (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = $"Пользователи_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();

                    // Заголовки
                    sb.AppendLine("\"Логин\";\"Фамилия\";\"Имя\";\"Отчество\";\"Роль\";\"Статус\";\"Стаж (лет)\";\"Дата рождения\";\"Email\";\"Телефон\";\"Дата регистрации\";\"Последний вход\"");

                    // Данные
                    foreach (var user in items)
                    {
                        sb.AppendLine($"\"{EscapeCsv(user.Login)}\";\"{EscapeCsv(user.LastName)}\";\"{EscapeCsv(user.FirstName)}\";\"{EscapeCsv(user.MiddleName)}\";\"{user.Role}\";\"{user.Status}\";\"{user.Experience}\";\"{user.BirthDate}\";\"{EscapeCsv(user.Email)}\";\"{EscapeCsv(user.Phone)}\";\"{user.RegistrationDate}\";\"{user.LastLoginDate}\"");
                    }

                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    ShowInfo($"Экспорт выполнен успешно!\nФайл сохранен: {saveDialog.FileName}");
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при экспорте: {ex.Message}");
                }
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }

        private void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid?.SelectedItem == null)
            {
                ShowError("Выберите пользователя");
                return;
            }

            var selected = UsersDataGrid.SelectedItem as UserDisplayModel;
            if (selected == null) return;

            if (selected.Role == "Admin")
            {
                ShowError("Невозможно заблокировать администратора");
                return;
            }

            if (selected.IsBlocked)
            {
                ShowError("Пользователь уже заблокирован");
                return;
            }

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                var user = AppConnect.model01.Users.FirstOrDefault(u => u.UserId == selected.UserId);
                if (user == null) return;

                user.IsBlocked = true;
                AppConnect.model01.SaveChanges();

                LoadUsers();
                ShowInfo("Пользователь заблокирован");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void UnblockButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid?.SelectedItem == null)
            {
                ShowError("Выберите пользователя");
                return;
            }

            var selected = UsersDataGrid.SelectedItem as UserDisplayModel;
            if (selected == null) return;

            if (!selected.IsBlocked)
            {
                ShowError("Пользователь не заблокирован");
                return;
            }

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                var user = AppConnect.model01.Users.FirstOrDefault(u => u.UserId == selected.UserId);
                if (user == null) return;

                user.IsBlocked = false;
                AppConnect.model01.SaveChanges();

                LoadUsers();
                ShowInfo("Пользователь разблокирован");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid?.SelectedItem == null)
            {
                ShowError("Выберите пользователя для удаления");
                return;
            }

            var selected = UsersDataGrid.SelectedItem as UserDisplayModel;
            if (selected == null) return;

            if (selected.Login == "admin" || selected.Role == "Admin")
            {
                ShowError("Нельзя удалить администратора");
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя {selected.Login}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                var user = AppConnect.model01.Users.FirstOrDefault(u => u.UserId == selected.UserId);
                if (user != null)
                {
                    AppConnect.model01.Users.Remove(user);
                    AppConnect.model01.SaveChanges();

                    LoadUsers();
                    ShowInfo("Пользователь удален");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при удалении: {ex.Message}");
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