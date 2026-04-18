using LibraryAccounting.AppData;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace LibraryAccounting.Pages
{
    public partial class AccountView : Page
    {
        private byte[] _photoBytes;

        public AccountView()
        {
            InitializeComponent();
            LoadUser();
        }

        private void LoadUser()
        {
            if (AppConnect.CurrentUser == null)
                return;

            // Личная информация
            LastNameBox.Text = AppConnect.CurrentUser.last_name;
            FirstNameBox.Text = AppConnect.CurrentUser.first_name;
            MiddleNameBox.Text = AppConnect.CurrentUser.middle_name;
            BirthDatePicker.SelectedDate = AppConnect.CurrentUser.BirthDate;

            // Контактная информация
            EmailBox.Text = AppConnect.CurrentUser.Email;
            PhoneBox.Text = AppConnect.CurrentUser.Phone;

            // Информация об аккаунте
            LoginBox.Text = AppConnect.CurrentUser.Login;
            RoleText.Text = AppConnect.CurrentUser.Roles?.RoleName ?? "Не указана";
            ExperienceBox.Text = AppConnect.CurrentUser.ExperienceYears?.ToString() ?? "";

            // Даты
            RegistrationDateBox.Text = AppConnect.CurrentUser.RegistrationDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана";
            LastLoginDateBox.Text = AppConnect.CurrentUser.LastLoginDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указан";

            // Статус
            StatusText.Text = AppConnect.CurrentUser.IsBlocked == true ? "🔒 Заблокирован" : "✅ Активен";
            if (AppConnect.CurrentUser.IsBlocked == true)
            {
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }

            // Фото
            _photoBytes = AppConnect.CurrentUser.Photo;
            UserPhoto.Source = LoadImage(_photoBytes);
        }

        private BitmapImage LoadImage(byte[] bytes)
        {
            try
            {
                if (bytes != null && bytes.Length > 0)
                {
                    BitmapImage image = new BitmapImage();
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                    }
                    return image;
                }
                return new BitmapImage(new Uri("pack://application:,,,/Images/nofoto.png"));
            }
            catch
            {
                return null;
            }
        }

        private void LoadPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите фотографию"
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                _photoBytes = File.ReadAllBytes(dlg.FileName);
                UserPhoto.Source = LoadImage(_photoBytes);

                AppConnect.CurrentUser.Photo = _photoBytes;
                AppConnect.model01.SaveChanges();
                ShowInfo("Фото успешно обновлено");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке фото: {ex.Message}");
            }
        }

        private void RemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            _photoBytes = null;
            UserPhoto.Source = LoadImage(null);

            AppConnect.CurrentUser.Photo = null;
            AppConnect.model01.SaveChanges();
            ShowInfo("Фото удалено");
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newLogin = LoginBox.Text.Trim();
                string lastName = LastNameBox.Text.Trim();
                string firstName = FirstNameBox.Text.Trim();
                string middleName = MiddleNameBox.Text.Trim();
                string email = EmailBox.Text.Trim();
                string phone = PhoneBox.Text.Trim();
                int? experience = null;

                if (!string.IsNullOrWhiteSpace(ExperienceBox.Text))
                {
                    if (int.TryParse(ExperienceBox.Text, out int exp))
                        experience = exp;
                }

                // Валидация
                if (string.IsNullOrWhiteSpace(newLogin))
                {
                    ShowError("Логин не может быть пустым");
                    return;
                }

                if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
                {
                    ShowError("Фамилия и имя обязательны");
                    return;
                }

                // Проверка уникальности логина
                var db = AppConnect.model01;
                bool loginExists = db.Users.Any(u =>
                    u.Login == newLogin &&
                    u.UserId != AppConnect.CurrentUser.UserId);

                if (loginExists)
                {
                    ShowError("Пользователь с таким логином уже существует");
                    return;
                }

                // Сохраняем данные
                AppConnect.CurrentUser.Login = newLogin;
                AppConnect.CurrentUser.last_name = lastName;
                AppConnect.CurrentUser.first_name = firstName;
                AppConnect.CurrentUser.middle_name = string.IsNullOrWhiteSpace(middleName) ? null : middleName;
                AppConnect.CurrentUser.Email = string.IsNullOrWhiteSpace(email) ? null : email;
                AppConnect.CurrentUser.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
                AppConnect.CurrentUser.BirthDate = BirthDatePicker.SelectedDate;
                AppConnect.CurrentUser.ExperienceYears = experience;
                AppConnect.CurrentUser.Photo = _photoBytes;

                db.SaveChanges();
                ShowInfo("Данные профиля успешно обновлены");

                // Обновляем отображение
                LoadUser();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChangePasswordWindow
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из аккаунта?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AppConnect.CurrentUser = null;
                ((MainWindow)Application.Current.MainWindow).frameMain.Navigate(new LoginView());
            }
        }

        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}