using System.Linq;
using LibraryAccounting.AppData;
using System.Text.RegularExpressions;

namespace LibraryAccounting.Services
{
    public static class RegistrationService
    {
        public static string RegisterUser(
            string login,
            string password,
            string confirmPassword,
            string roleName = "Librarian")
        {
            // Проверка заполнения
            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                return "Заполните все поля";
            }

            // Проверка совпадения паролей
            if (password != confirmPassword)
            {
                return "Пароли не совпадают";
            }

            // Минимальная длина пароля
            string passwordError = ValidatePassword(password);
            if (passwordError != null)
            {
                return passwordError;
            }

            // Инициализация EF
            var context = AppConnect.model01 ?? new LibraryAccountingEntities();

            // Проверка уникальности логина
            if (context.Users.Any(u => u.Login == login))
            {
                return "Пользователь с таким логином уже существует";
            }

            // Получение роли
            var role = context.Roles.FirstOrDefault(r => r.RoleName == roleName);
            if (role == null)
            {
                return "Роль не найдена в системе";
            }

            // Создание пользователя
            var newUser = new Users
            {
                Login = login,
                PasswordHash = password, // позже заменим на хэш
                RoleId = role.RoleId
            };

            context.Users.Add(newUser);
            context.SaveChanges();

            return null; // null = успех
        }
        private static string ValidatePassword(string password)
        {
            if (password.Length < 6)
                return "Пароль должен содержать минимум 6 символов";

            if (!Regex.IsMatch(password, "[A-Z]"))
                return "Пароль должен содержать хотя бы одну заглавную букву";

            if (!Regex.IsMatch(password, "[a-z]"))
                return "Пароль должен содержать хотя бы одну строчную букву";

            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
                return "Пароль должен содержать хотя бы один специальный символ";

            return null; // пароль корректный
        }

    }
}
