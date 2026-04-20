using LibraryAccounting.AppData;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        #region Предварительная валидация ввода

        private void NameOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Разрешаем только буквы (русские и английские), дефис и пробел
            e.Handled = !Regex.IsMatch(e.Text, @"[a-zA-Zа-яА-Я\-]");
        }

        private void PhoneOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        #endregion

        #region Валидация

        private bool ValidateFields()
        {
            string newLogin = LoginBox.Text.Trim();
            string lastName = LastNameBox.Text.Trim();
            string firstName = FirstNameBox.Text.Trim();
            string middleName = MiddleNameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();

            // 1. Логин
            if (string.IsNullOrWhiteSpace(newLogin))
            {
                ShowError("Логин не может быть пустым");
                LoginBox.Focus();
                return false;
            }
            if (newLogin.Length < 3)
            {
                ShowError("Логин должен содержать минимум 3 символа");
                LoginBox.Focus();
                return false;
            }
            if (newLogin.Length > 50)
            {
                ShowError("Логин не может превышать 50 символов");
                LoginBox.Focus();
                return false;
            }
            if (!Regex.IsMatch(newLogin, @"^[a-zA-Z0-9_]+$"))
            {
                ShowError("Логин может содержать только латинские буквы, цифры и знак подчеркивания");
                LoginBox.Focus();
                return false;
            }

            // 2. Фамилия
            if (string.IsNullOrWhiteSpace(lastName))
            {
                ShowError("Фамилия не может быть пустой");
                LastNameBox.Focus();
                return false;
            }
            if (lastName.Length > 50)
            {
                ShowError("Фамилия не может превышать 50 символов");
                LastNameBox.Focus();
                return false;
            }
            if (Regex.IsMatch(lastName, @"\d"))
            {
                ShowError("Фамилия не может содержать цифры");
                LastNameBox.Focus();
                return false;
            }
            if (!Regex.IsMatch(lastName, @"^[a-zA-Zа-яА-Я\-]+$"))
            {
                ShowError("Фамилия может содержать только буквы и дефис");
                LastNameBox.Focus();
                return false;
            }

            // 3. Имя
            if (string.IsNullOrWhiteSpace(firstName))
            {
                ShowError("Имя не может быть пустым");
                FirstNameBox.Focus();
                return false;
            }
            if (firstName.Length > 50)
            {
                ShowError("Имя не может превышать 50 символов");
                FirstNameBox.Focus();
                return false;
            }
            if (Regex.IsMatch(firstName, @"\d"))
            {
                ShowError("Имя не может содержать цифры");
                FirstNameBox.Focus();
                return false;
            }
            if (!Regex.IsMatch(firstName, @"^[a-zA-Zа-яА-Я\-]+$"))
            {
                ShowError("Имя может содержать только буквы и дефис");
                FirstNameBox.Focus();
                return false;
            }

            // 4. Отчество (необязательно)
            if (!string.IsNullOrEmpty(middleName))
            {
                if (middleName.Length > 50)
                {
                    ShowError("Отчество не может превышать 50 символов");
                    MiddleNameBox.Focus();
                    return false;
                }
                if (Regex.IsMatch(middleName, @"\d"))
                {
                    ShowError("Отчество не может содержать цифры");
                    MiddleNameBox.Focus();
                    return false;
                }
                if (!Regex.IsMatch(middleName, @"^[a-zA-Zа-яА-Я\-]+$"))
                {
                    ShowError("Отчество может содержать только буквы и дефис");
                    MiddleNameBox.Focus();
                    return false;
                }
            }

            // 5. Дата рождения
            if (BirthDatePicker.SelectedDate == null)
            {
                ShowError("Укажите дату рождения");
                BirthDatePicker.Focus();
                return false;
            }
            if (BirthDatePicker.SelectedDate > DateTime.Today)
            {
                ShowError("Дата рождения не может быть в будущем");
                BirthDatePicker.Focus();
                return false;
            }
            if (BirthDatePicker.SelectedDate < new DateTime(1900, 1, 1))
            {
                ShowError("Укажите корректную дату рождения (после 1900 года)");
                BirthDatePicker.Focus();
                return false;
            }

            // 6. Email
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Email не может быть пустым");
                EmailBox.Focus();
                return false;
            }
            if (email.Length > 100)
            {
                ShowError("Email не может превышать 100 символов");
                EmailBox.Focus();
                return false;
            }
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ShowError("Введите корректный email адрес (например: user@mail.ru)");
                EmailBox.Focus();
                return false;
            }

            // 7. Телефон
            if (string.IsNullOrWhiteSpace(phone))
            {
                ShowError("Телефон не может быть пустым");
                PhoneBox.Focus();
                return false;
            }
            if (phone.Length < 10 || phone.Length > 20)
            {
                ShowError("Телефон должен содержать от 10 до 20 символов");
                PhoneBox.Focus();
                return false;
            }
            if (!Regex.IsMatch(phone, @"^[\+\d][\d\s\-\(\)]{9,19}$"))
            {
                ShowError("Введите корректный номер телефона (например: +7-999-123-45-67 или 89991234567)");
                PhoneBox.Focus();
                return false;
            }

            // 8. Стаж работы (необязательно)
            if (!string.IsNullOrWhiteSpace(ExperienceBox.Text))
            {
                if (!int.TryParse(ExperienceBox.Text, out int exp) || exp < 0 || exp > 60)
                {
                    ShowError("Стаж работы должен быть числом от 0 до 60 лет");
                    ExperienceBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private bool CheckUniqueLogin()
        {
            string newLogin = LoginBox.Text.Trim();
            var db = AppConnect.model01;

            bool loginExists = db.Users.Any(u =>
                u.Login == newLogin &&
                u.UserId != AppConnect.CurrentUser.UserId);

            if (loginExists)
            {
                ShowError("Пользователь с таким логином уже существует");
                LoginBox.Focus();
                return false;
            }
            return true;
        }

        private bool CheckUniqueEmail()
        {
            string email = EmailBox.Text.Trim();
            var db = AppConnect.model01;

            bool emailExists = db.Users.Any(u =>
                u.Email == email &&
                u.UserId != AppConnect.CurrentUser.UserId);

            if (emailExists)
            {
                ShowError("Пользователь с таким email уже существует");
                EmailBox.Focus();
                return false;
            }
            return true;
        }

        #endregion

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация полей
                if (!ValidateFields())
                    return;

                // Проверка уникальности логина
                if (!CheckUniqueLogin())
                    return;

                // Проверка уникальности email
                if (!CheckUniqueEmail())
                    return;

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

                var db = AppConnect.model01;

                // Сохраняем данные
                AppConnect.CurrentUser.Login = newLogin;
                AppConnect.CurrentUser.last_name = lastName;
                AppConnect.CurrentUser.first_name = firstName;
                AppConnect.CurrentUser.middle_name = string.IsNullOrWhiteSpace(middleName) ? null : middleName;
                AppConnect.CurrentUser.Email = email;
                AppConnect.CurrentUser.Phone = phone;
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
        private void PhoneBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string text = textBox.Text;

            // Удаляем все нецифровые символы
            string digits = Regex.Replace(text, @"\D", "");

            if (string.IsNullOrEmpty(digits))
            {
                textBox.Text = "";
                return;
            }

            string formatted = "";

            // Форматирование для российских номеров
            if (digits.Length <= 11)
            {
                if (digits.Length >= 1)
                {
                    if (digits[0] == '7' || digits[0] == '8')
                    {
                        formatted = "+7";
                        digits = digits.Substring(1);
                    }
                    else
                    {
                        formatted = "+7";
                    }
                }

                if (digits.Length >= 1)
                {
                    formatted += " (" + digits.Substring(0, Math.Min(3, digits.Length));
                    if (digits.Length >= 4)
                    {
                        formatted += ") " + digits.Substring(3, Math.Min(3, digits.Length - 3));
                        if (digits.Length >= 7)
                        {
                            formatted += "-" + digits.Substring(6, Math.Min(2, digits.Length - 6));
                            if (digits.Length >= 9)
                            {
                                formatted += "-" + digits.Substring(8, Math.Min(2, digits.Length - 8));
                            }
                        }
                    }
                }
            }

            textBox.Text = formatted;
            textBox.CaretIndex = textBox.Text.Length;
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