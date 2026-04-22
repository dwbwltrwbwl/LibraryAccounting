using LibraryAccounting.AppData;
using LibraryAccounting.Services;
using LibraryAccounting.Windows;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LibraryAccounting.Pages
{
    public partial class RegistrationView : Page
    {
        private bool isLoginAvailable = false;
        private bool isPasswordValid = false;
        private bool isPasswordMatch = false;
        private bool isPasswordVisible = false;
        private bool isEmailValid = true;
        private bool isPhoneValid = true;
        private bool isExperienceValid = true;
        private TextBox tempPasswordTextBox = null;
        private byte[] uploadedPhotoBytes = null;
        private bool isUpdatingPhoneMask = false;

        public RegistrationView()
        {
            InitializeComponent();

            // Устанавливаем максимальную дату рождения (18 лет назад)
            BirthDatePicker.DisplayDateEnd = DateTime.Today.AddYears(-18);
            BirthDatePicker.SelectedDate = null;
        }

        /// <summary>
        /// Проверка ФИО - только буквы
        /// </summary>
        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                string currentText = textBox.Text;
                string filtered = Regex.Replace(currentText, @"[^a-zA-Zа-яА-ЯёЁ\s-]", "");
                if (currentText != filtered)
                {
                    textBox.Text = filtered;
                    textBox.CaretIndex = filtered.Length;
                }
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string email = textBox.Text;

            // Фильтрация: удаляем русские буквы и недопустимые символы
            string filtered = Regex.Replace(email, @"[^a-zA-Z0-9@._\-]", "");
            if (email != filtered)
            {
                textBox.Text = filtered;
                textBox.CaretIndex = filtered.Length;
                email = filtered;
            }

            // Управление видимостью подсказки
            EmailPlaceholder.Visibility = string.IsNullOrEmpty(email) ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrEmpty(email))
            {
                isEmailValid = true;
                textBox.Background = Brushes.Transparent;
                CheckFormValidity();
                return;
            }

            // Проверка формата email
            isEmailValid = Regex.IsMatch(email, @"^[a-zA-Z0-9._\-]+@[a-zA-Z0-9._\-]+\.[a-zA-Z]{2,}$");

            textBox.Background = isEmailValid ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(255, 220, 220));

            CheckFormValidity();
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingPhoneMask) return;

            var textBox = sender as TextBox;
            if (textBox == null) return;

            string input = textBox.Text;

            // Управление видимостью подсказки
            PhonePlaceholder.Visibility = string.IsNullOrEmpty(input) ? Visibility.Visible : Visibility.Collapsed;

            // Удаляем все нецифровые символы
            string digits = Regex.Replace(input, @"[^\d]", "");

            // Форматируем по маске +7(999)999-99-99
            string formatted = "";

            if (digits.Length > 0)
            {
                formatted = "+7";

                if (digits.Length >= 4)
                {
                    formatted += "(" + digits.Substring(1, Math.Min(3, digits.Length - 1));
                    if (digits.Length >= 4) formatted += ")";

                    if (digits.Length >= 7)
                    {
                        formatted += digits.Substring(4, Math.Min(3, digits.Length - 4));
                        if (digits.Length >= 7) formatted += "-";

                        if (digits.Length >= 9)
                        {
                            formatted += digits.Substring(7, Math.Min(2, digits.Length - 7));
                            if (digits.Length >= 9) formatted += "-";

                            if (digits.Length >= 11)
                            {
                                formatted += digits.Substring(9, Math.Min(2, digits.Length - 9));
                            }
                            else if (digits.Length > 9)
                            {
                                formatted += digits.Substring(9);
                            }
                        }
                        else if (digits.Length > 7)
                        {
                            formatted += digits.Substring(7);
                        }
                    }
                    else if (digits.Length > 4)
                    {
                        formatted += digits.Substring(4);
                    }
                }
                else if (digits.Length > 0)
                {
                    formatted += digits.Substring(1);
                }
            }

            isUpdatingPhoneMask = true;
            textBox.Text = formatted;
            textBox.CaretIndex = formatted.Length;
            isUpdatingPhoneMask = false;

            // Валидация телефона (должно быть 11 цифр)
            int digitCount = Regex.Matches(formatted, @"[0-9]").Count;
            isPhoneValid = digitCount == 11;

            textBox.Background = isPhoneValid ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(255, 220, 220));

            CheckFormValidity();
        }

        /// <summary>
        /// Проверка стажа
        /// </summary>
        /// <summary>
        /// Проверка стажа
        /// </summary>
        private void ExperienceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string exp = ExperienceTextBox.Text.Trim();

            if (string.IsNullOrEmpty(exp))
            {
                isExperienceValid = true;
                ExperienceTextBox.Background = Brushes.Transparent;
                ExperienceTextBox.ToolTip = null;
                CheckFormValidity();
                return;
            }

            // Только цифры
            string filtered = Regex.Replace(exp, @"[^0-9]", "");
            if (exp != filtered)
            {
                ExperienceTextBox.Text = filtered;
                ExperienceTextBox.CaretIndex = filtered.Length;
            }

            if (!string.IsNullOrEmpty(filtered))
            {
                int years = int.Parse(filtered);

                // Базовая проверка
                bool isValidRange = years >= 0 && years <= 60;

                // Проверка относительно возраста
                bool isValidVsAge = true;
                string errorMessage = "";

                if (BirthDatePicker.SelectedDate.HasValue)
                {
                    int age = CalculateAge(BirthDatePicker.SelectedDate.Value);

                    if (years > age)
                    {
                        isValidVsAge = false;
                        errorMessage = $"Стаж ({years} лет) не может быть больше возраста ({age} лет)";
                    }
                    else if (years > age - 16) // Предполагаем, что работать начинают с 16 лет
                    {
                        // Предупреждение, но не ошибка
                        ExperienceTextBox.ToolTip = $"Внимание: стаж {years} лет при возрасте {age} лет";
                        ExperienceTextBox.Background = new SolidColorBrush(Color.FromRgb(255, 255, 200)); // Желтый фон
                    }
                }

                isExperienceValid = isValidRange && isValidVsAge;

                if (!isExperienceValid)
                {
                    ExperienceTextBox.Background = new SolidColorBrush(Color.FromRgb(255, 220, 220));
                    ExperienceTextBox.ToolTip = string.IsNullOrEmpty(errorMessage) ? "Стаж должен быть от 0 до 60 лет" : errorMessage;
                }
                else if (ExperienceTextBox.ToolTip == null || ExperienceTextBox.ToolTip.ToString() == errorMessage)
                {
                    ExperienceTextBox.Background = Brushes.Transparent;
                    if (!ExperienceTextBox.ToolTip?.ToString().StartsWith("Внимание") ?? false)
                    {
                        ExperienceTextBox.ToolTip = null;
                    }
                }
            }
            else
            {
                isExperienceValid = true;
                ExperienceTextBox.Background = Brushes.Transparent;
                ExperienceTextBox.ToolTip = null;
            }

            CheckFormValidity();
        }

        /// <summary>
        /// Проверка даты рождения
        /// </summary>
        /// <summary>
        /// Проверка даты рождения
        /// </summary>
        private void BirthDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // При изменении даты рождения перепроверяем стаж
            if (!string.IsNullOrEmpty(ExperienceTextBox.Text))
            {
                ExperienceTextBox_TextChanged(ExperienceTextBox, null);
            }

            CheckFormValidity();
        }

        /// <summary>
        /// Загрузка фото профиля
        /// </summary>
        private void UploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите фото профиля",
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ProfileImage.Source = bitmap;

                    byte[] imageData = File.ReadAllBytes(openFileDialog.FileName);

                    if (imageData.Length > 2 * 1024 * 1024)
                    {
                        var result = MessageBox.Show("Изображение слишком большое (более 2MB). Оно будет сжато. Продолжить?",
                            "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.Yes)
                        {
                            uploadedPhotoBytes = CompressImage(imageData);
                        }
                        else
                        {
                            uploadedPhotoBytes = null;
                            ProfileImage.Source = null;
                        }
                    }
                    else
                    {
                        uploadedPhotoBytes = imageData;
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка загрузки фото: {ex.Message}");
                }
            }
        }

        private byte[] CompressImage(byte[] imageData)
        {
            return imageData;
        }

        /// <summary>
        /// Проверка доступности логина
        /// </summary>
        private void LoginTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(login))
            {
                LoginStatusIndicator.Text = "";
                isLoginAvailable = false;
                CheckFormValidity();
                return;
            }

            if (!Regex.IsMatch(login, @"^[a-zA-Z0-9_.]+$"))
            {
                LoginStatusIndicator.Text = "❌";
                LoginStatusIndicator.Foreground = Brushes.Red;
                isLoginAvailable = false;
                CheckFormValidity();
                return;
            }

            if (login.Length < 3)
            {
                LoginStatusIndicator.Text = "⚠️";
                LoginStatusIndicator.Foreground = Brushes.Orange;
                isLoginAvailable = false;
                CheckFormValidity();
                return;
            }

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                bool exists = AppConnect.model01.Users.Any(u => u.Login == login);

                if (!exists)
                {
                    LoginStatusIndicator.Text = "✅";
                    LoginStatusIndicator.Foreground = Brushes.Green;
                    isLoginAvailable = true;
                }
                else
                {
                    LoginStatusIndicator.Text = "❌";
                    LoginStatusIndicator.Foreground = Brushes.Red;
                    isLoginAvailable = false;
                }
            }
            catch
            {
                LoginStatusIndicator.Text = "⚠️";
                isLoginAvailable = false;
            }

            CheckFormValidity();
        }

        /// <summary>
        /// Проверка сложности пароля
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = GetPassword();

            if (string.IsNullOrEmpty(password))
            {
                PasswordStrengthText.Text = "";
                PasswordStrengthBorder.Background = new SolidColorBrush(Color.FromRgb(244, 241, 236));
                isPasswordValid = false;
                ConfirmPasswordBox_PasswordChanged(null, null);
                return;
            }

            int strength = 0;
            string strengthText = "";
            Brush color;

            if (password.Length >= 6) strength++;
            if (password.Length >= 10) strength++;
            if (Regex.IsMatch(password, @"[a-zа-я]")) strength++;
            if (Regex.IsMatch(password, @"[A-ZА-Я]")) strength++;
            if (Regex.IsMatch(password, @"[0-9]")) strength++;
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?\"":{}|<>]")) strength++;

            if (strength <= 2)
            {
                strengthText = "Слабый";
                color = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                isPasswordValid = false;
            }
            else if (strength <= 4)
            {
                strengthText = "Средний";
                color = new SolidColorBrush(Color.FromRgb(255, 230, 200));
                isPasswordValid = true;
            }
            else
            {
                strengthText = "Сильный";
                color = new SolidColorBrush(Color.FromRgb(200, 255, 200));
                isPasswordValid = true;
            }

            PasswordStrengthText.Text = strengthText;
            PasswordStrengthBorder.Background = color;

            ConfirmPasswordBox_PasswordChanged(null, null);
        }

        private string GetPassword()
        {
            if (isPasswordVisible && tempPasswordTextBox != null)
            {
                return tempPasswordTextBox.Text;
            }
            else
            {
                return PasswordBox.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = GetPassword();
            string confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrEmpty(confirm))
            {
                isPasswordMatch = false;
                CheckFormValidity();
                return;
            }

            isPasswordMatch = (password == confirm);
            CheckFormValidity();
        }

        /// <summary>
        /// Проверка всей формы
        /// </summary>
        private void CheckFormValidity()
        {
            bool isFormValid = isLoginAvailable && isPasswordValid && isPasswordMatch &&
                               isEmailValid && isPhoneValid && isExperienceValid;

            RegisterBtn.IsEnabled = isFormValid;
            RegisterBtn.Opacity = isFormValid ? 1 : 0.6;
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var grid = PasswordBox.Parent as Grid;
            if (grid == null) return;

            int index = grid.Children.IndexOf(PasswordBox);

            if (!isPasswordVisible)
            {
                string currentPassword = PasswordBox.Password;

                tempPasswordTextBox = new TextBox
                {
                    Text = currentPassword,
                    Height = 36,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    FontSize = 14,
                    FontFamily = PasswordBox.FontFamily,
                    MaxLength = 256
                };

                tempPasswordTextBox.TextChanged += (s, ev) =>
                {
                    PasswordBox.Password = tempPasswordTextBox.Text;
                    PasswordBox_PasswordChanged(null, null);
                };

                grid.Children.Remove(PasswordBox);
                grid.Children.Insert(index, tempPasswordTextBox);

                button.Content = "🙈";
                isPasswordVisible = true;
            }
            else
            {
                string password = tempPasswordTextBox?.Text ?? "";

                if (tempPasswordTextBox != null)
                {
                    grid.Children.Remove(tempPasswordTextBox);
                    tempPasswordTextBox = null;
                }

                grid.Children.Insert(index, PasswordBox);
                PasswordBox.Password = password;

                button.Content = "👁️";
                isPasswordVisible = false;

                PasswordBox_PasswordChanged(null, null);
            }
        }

        /// <summary>
        /// Регистрация пользователя с новыми полями
        /// </summary>
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string lastName = LastNameTextBox.Text.Trim();
            string firstName = FirstNameTextBox.Text.Trim();
            string middleName = MiddleNameTextBox.Text.Trim();
            string login = LoginTextBox.Text.Trim();
            string password = GetPassword();
            string email = EmailTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();
            int? experience = null;
            DateTime? birthDate = BirthDatePicker.SelectedDate;

            // Валидация
            if (string.IsNullOrEmpty(lastName))
            {
                ShowError("Введите фамилию");
                return;
            }

            if (string.IsNullOrEmpty(firstName))
            {
                ShowError("Введите имя");
                return;
            }

            if (string.IsNullOrEmpty(login))
            {
                ShowError("Введите логин");
                return;
            }

            if (login.Length < 3)
            {
                ShowError("Логин должен содержать минимум 3 символа");
                return;
            }

            if (!Regex.IsMatch(login, @"^[a-zA-Z0-9_.]+$"))
            {
                ShowError("Логин может содержать только латиницу, цифры, _ и .");
                return;
            }

            // Валидация email
            if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ShowError("Введите корректный email");
                return;
            }

            // Валидация стажа
            // Валидация стажа
            if (!string.IsNullOrEmpty(ExperienceTextBox.Text))
            {
                experience = int.Parse(ExperienceTextBox.Text);
                if (experience < 0 || experience > 60)
                {
                    ShowError("Стаж должен быть от 0 до 60 лет");
                    return;
                }

                // Проверка стажа относительно возраста
                if (birthDate.HasValue)
                {
                    int age = CalculateAge(birthDate.Value);
                    if (experience > age)
                    {
                        ShowError($"Стаж ({experience} лет) не может быть больше возраста ({age} лет)");
                        return;
                    }

                    // Предупреждение о подозрительном стаже
                    if (experience > age - 16)
                    {
                        var result = MessageBox.Show(
                            $"Внимание: указан стаж {experience} лет при возрасте {age} лет. Продолжить?",
                            "Предупреждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Введите пароль");
                return;
            }

            if (!isPasswordValid)
            {
                ShowError("Пароль слишком слабый. Используйте минимум 6 символов, буквы разного регистра и цифры");
                return;
            }

            if (password != ConfirmPasswordBox.Password)
            {
                ShowError("Пароли не совпадают");
                return;
            }

            if (!isLoginAvailable)
            {
                ShowError("Логин уже занят или недопустим");
                return;
            }

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                string hashedPassword = PasswordHasher.Hash(password);

                var newUser = new Users
                {
                    last_name = lastName,
                    first_name = firstName,
                    middle_name = string.IsNullOrEmpty(middleName) ? null : middleName,
                    Login = login,
                    PasswordHash = hashedPassword,
                    RoleId = 2, // Librarian
                    IsBlocked = false,
                    IsApproved = false,              // ✅ ДОБАВИТЬ - ждёт одобрения
                    RegistrationRequestDate = DateTime.Now,  // ✅ ДОБАВИТЬ - дата заявки
                    Photo = uploadedPhotoBytes,
                    Email = string.IsNullOrEmpty(email) ? null : email,
                    Phone = string.IsNullOrEmpty(phone) ? null : phone,
                    ExperienceYears = experience,
                    BirthDate = birthDate,
                    RegistrationDate = DateTime.Now,
                    LastLoginDate = null
                };

                AppConnect.model01.Users.Add(newUser);
                AppConnect.model01.SaveChanges();

                var successDialog = new MessageDialog(
                    "Регистрация",
                    "Заявка на регистрацию отправлена!\nПосле одобрения администратором вы сможете войти в систему."
                );
                successDialog.Owner = Window.GetWindow(this);
                successDialog.ShowDialog();

                NavigationService.Navigate(new LoginView());
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка регистрации: {ex.Message}");
            }
        }
        /// <summary>
        /// Вычисляет возраст по дате рождения
        /// </summary>
        /// <summary>
        /// Вычисляет точный возраст по дате рождения
        /// </summary>
        private int CalculateAge(DateTime birthDate)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthDate.Year;

            // Проверяем, был ли день рождения в этом году
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
        private void ShowError(string message)
        {
            var dialog = new MessageDialog("Ошибка регистрации", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginView());
        }
    }
}