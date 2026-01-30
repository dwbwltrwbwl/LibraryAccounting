using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryAccounting.AppData
{
    class AppConnect
    {
        public static Users CurrentUser { get; set; }

        public static bool IsAdmin =>
            CurrentUser != null && CurrentUser.RoleId == 1;
        public static bool IsLibrarian =>
            CurrentUser != null && CurrentUser.RoleId == 2;

        public static LibraryAccountingEntities model01;
    }
}
