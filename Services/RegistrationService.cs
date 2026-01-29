using System.Linq;
using LibraryAccounting.Services;
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
    string lastName,
    string firstName,
    string middleName
)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                return "Введите фамилию";

            if (string.IsNullOrWhiteSpace(firstName))
                return "Введите имя";

            if (string.IsNullOrWhiteSpace(login))
                return "Введите логин";

            if (string.IsNullOrWhiteSpace(password))
                return "Введите пароль";

            string passwordError = ValidatePassword(password);
            if (passwordError != null)
                return passwordError;

            if (password != confirmPassword)
                return "Пароли не совпадают";

            using (var db = new LibraryAccountingEntities())
            {
                bool exists = db.Users.Any(u => u.Login == login);
                if (exists)
                    return "Пользователь с таким логином уже существует";

                var user = new Users
                {
                    Login = login,
                    PasswordHash = PasswordHasher.Hash(password),
                    last_name = lastName,
                    first_name = firstName,
                    middle_name = string.IsNullOrWhiteSpace(middleName)
                        ? null
                        : middleName,
                    RoleId = 2
                };

                db.Users.Add(user);
                db.SaveChanges();
            }

            return null;
        }
        public static string ValidatePassword(string password)
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
