using System.Windows;

namespace LibraryAccounting.Pages
{
    /// <summary>
    /// Логика взаимодействия для InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public string Result { get; private set; }

        public InputDialog(string title)
        {
            InitializeComponent();
            TitleText.Text = title;
            InputBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = InputBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public void SetDefaultValue(string defaultValue)
        {
            if (!string.IsNullOrEmpty(defaultValue))
            {
                InputBox.Text = defaultValue;  // Было InputTextBox, нужно InputBox
                InputBox.SelectAll();
            }
        }
    }
}