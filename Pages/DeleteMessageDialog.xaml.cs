using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LibraryAccounting.Pages
{
    /// <summary>
    /// Логика взаимодействия для DeleteMessageDialog.xaml
    /// </summary>
    public partial class DeleteMessageDialog : Window
    {
        public DeleteMessageDialog(string title, string message)
        {
            InitializeComponent();
            Title = title;
            MessageText.Text = message;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // 🔥 ОБЯЗАТЕЛЬНО
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // 🔥 ОБЯЗАТЕЛЬНО
            Close();
        }
    }
}
