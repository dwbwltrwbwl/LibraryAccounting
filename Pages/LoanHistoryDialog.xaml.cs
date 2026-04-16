using System.Collections.Generic;
using System.Windows;

namespace LibraryAccounting.Pages
{
    public partial class LoanHistoryDialog : Window
    {
        public LoanHistoryDialog(string readerName, IEnumerable<dynamic> loans)
        {
            InitializeComponent();
            ReaderNameText.Text = $"Читатель: {readerName}";
            HistoryDataGrid.ItemsSource = loans;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}