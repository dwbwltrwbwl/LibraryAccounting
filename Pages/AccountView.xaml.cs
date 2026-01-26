using LibraryAccounting.AppData;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System;

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

            LoginText.Text = AppConnect.CurrentUser.Login;

            string lastName = AppConnect.CurrentUser.last_name ?? "";
            string firstName = AppConnect.CurrentUser.first_name ?? "";
            string middleName = AppConnect.CurrentUser.middle_name ?? "";

            FullNameText.Text = $"{lastName} {firstName} {middleName}".Trim();
            RoleText.Text = AppConnect.CurrentUser.Roles.RoleName;

            _photoBytes = AppConnect.CurrentUser.Photo;
            UserPhoto.Source = LoadImage(_photoBytes);
        }

        /// <summary>
        /// Загрузка изображения или заглушки
        /// </summary>
        private BitmapImage LoadImage(byte[] bytes)
        {
            try
            {
                // Если фото есть — загружаем
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

                // 🔥 Заглушка
                return new BitmapImage(
                    new Uri("pack://application:,,,/Images/nofoto.png")
                );
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Загрузка нового фото
        /// </summary>
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при загрузке фото:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Удаление фото (возврат заглушки)
        /// </summary>
        private void RemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            _photoBytes = null;
            UserPhoto.Source = LoadImage(null);

            AppConnect.CurrentUser.Photo = null;
            AppConnect.model01.SaveChanges();
        }

        /// <summary>
        /// Смена пароля
        /// </summary>
        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChangePasswordWindow
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }

        /// <summary>
        /// Выход из аккаунта
        /// </summary>
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            AppConnect.CurrentUser = null;

            ((MainWindow)Application.Current.MainWindow)
                .frameMain.Navigate(new LoginView());
        }
    }
}
