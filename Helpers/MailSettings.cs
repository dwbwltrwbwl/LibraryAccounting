using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryAccounting.Helpers
{
    public class MailSettings
    {
        public string SmtpServer { get; set; } = "smtp.yandex.ru";
        public int Port { get; set; } = 587;
        public string Email { get; set; }
        public string Password { get; set; }
        public bool UseSSL { get; set; } = true;
        public string DisplayName { get; set; } = "Библиотека";
    }
}
