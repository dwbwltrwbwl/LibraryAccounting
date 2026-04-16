using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LibraryAccounting.Pages
{
    public partial class ReaderSelectionDialog : Window
    {
        public int SelectedReaderId { get; private set; }

        public ReaderSelectionDialog(IEnumerable<dynamic> readers)
        {
            InitializeComponent();
            ReadersListBox.ItemsSource = readers;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (ReadersListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите читателя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedReaderId = (int)ReadersListBox.SelectedValue;
            DialogResult = true;
            Close();
        }

        private void ReadersListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Select_Click(null, null);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}