using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class ReadersView : Page
    {
        private List<dynamic> _allReaders;
        public ReadersView()
        {
            InitializeComponent();
            if (AppConnect.CurrentUser != null && AppConnect.CurrentUser.RoleId == 2)
            {
                DeleteButton.IsEnabled = false;
                DeleteButton.Visibility = Visibility.Collapsed; // можно оставить только IsEnabled=false
                AddButton.IsEnabled = false;
                AddButton.Visibility = Visibility.Collapsed;
            }
            LoadReaders();
        }

        /// <summary>
        /// Загрузка всех читателей
        /// </summary>
        private void LoadReaders()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            _allReaders = AppConnect.model01.Readers
    .Select(r => new
    {
        r.ReaderId,
        FullName =
            r.last_name + " " +
            r.first_name +
            (r.middle_name != null ? " " + r.middle_name : ""),
        r.last_name,
        r.first_name,
        r.middle_name,
        r.Phone,
        r.Email,
        r.PassportData,
        r.RegistrationDate
    })
    .ToList<dynamic>();

            ReadersDataGrid.ItemsSource = _allReaders;
        }
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = SearchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(search))
            {
                ReadersDataGrid.ItemsSource = _allReaders;
                return;
            }

            var filtered = _allReaders.Where(r =>
    r.FullName.ToLower().Contains(search) ||
    (!string.IsNullOrEmpty(r.Phone) && r.Phone.Contains(search)) ||
    (!string.IsNullOrEmpty(r.Email) && r.Email.ToLower().Contains(search))
).ToList();

            ReadersDataGrid.ItemsSource = filtered;
        }
        /// <summary>
        /// Добавление читателя (заглушка)
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new ReaderEditWindow();
            win.Owner = Window.GetWindow(this);

            if (win.ShowDialog() == true)
                LoadReaders();
        }

        /// <summary>
        /// Редактирование читателя (заглушка)
        /// </summary>
        private void ReadersDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ReadersDataGrid.SelectedItem == null)
                return;

            dynamic selected = ReadersDataGrid.SelectedItem;
            int readerId = selected.ReaderId;

            var reader = AppConnect.model01.Readers
                .FirstOrDefault(r => r.ReaderId == readerId);

            if (reader == null)
                return;

            var win = new ReaderEditWindow(reader);
            win.Owner = Window.GetWindow(this);

            if (win.ShowDialog() == true)
                LoadReaders();
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
