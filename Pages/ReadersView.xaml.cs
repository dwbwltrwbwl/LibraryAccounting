using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class ReadersView : Page
    {
        public ReadersView()
        {
            InitializeComponent();
            LoadReaders();
        }

        /// <summary>
        /// Загрузка всех читателей
        /// </summary>
        private void LoadReaders()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var readers = AppConnect.model01.Readers
                .Select(r => new
                {
                    r.ReaderId,
                    r.FullName,
                    r.Phone,
                    r.Email,
                    r.PassportData,
                    r.RegistrationDate
                })
                .ToList();

            ReadersDataGrid.ItemsSource = readers;
        }

        /// <summary>
        /// Поиск читателей
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string search = SearchTextBox.Text.Trim().ToLower();

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var result = AppConnect.model01.Readers
                .Where(r =>
                    r.FullName.ToLower().Contains(search) ||
                    r.Phone.Contains(search) ||
                    r.Email.ToLower().Contains(search))
                .Select(r => new
                {
                    r.ReaderId,
                    r.FullName,
                    r.Phone,
                    r.Email,
                    r.PassportData,
                    r.RegistrationDate
                })
                .ToList();

            ReadersDataGrid.ItemsSource = result;
        }

        /// <summary>
        /// Добавление читателя (заглушка)
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInfo("Окно добавления читателя будет реализовано позже.");
        }

        /// <summary>
        /// Редактирование читателя (заглушка)
        /// </summary>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReadersDataGrid.SelectedItem == null)
            {
                ShowError("Выберите читателя для редактирования");
                return;
            }

            ShowInfo("Окно редактирования читателя будет реализовано позже.");
        }

        /// <summary>
        /// Удаление читателя
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReadersDataGrid.SelectedItem == null)
            {
                ShowError("Выберите читателя для удаления");
                return;
            }

            dynamic selected = ReadersDataGrid.SelectedItem;
            int readerId = selected.ReaderId;

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var reader = AppConnect.model01.Readers
                .FirstOrDefault(r => r.ReaderId == readerId);

            if (reader != null)
            {
                AppConnect.model01.Readers.Remove(reader);
                AppConnect.model01.SaveChanges();

                LoadReaders();
                ShowInfo("Читатель успешно удален");
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
