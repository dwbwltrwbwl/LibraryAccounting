using LibraryAccounting.AppData;
using System;
using System.Windows;

namespace LibraryAccounting.Pages
{
    public partial class ReaderEditWindow : Window
    {
        private Readers _reader;

        // ➕ Добавление
        public ReaderEditWindow()
        {
            InitializeComponent();
            TitleText.Text = "Добавление читателя";
        }

        // ✏️ Редактирование
        public ReaderEditWindow(Readers reader)
        {
            InitializeComponent();
            TitleText.Text = "Редактирование читателя";
            _reader = reader;

            LastNameBox.Text = reader.last_name;
            FirstNameBox.Text = reader.first_name;
            MiddleNameBox.Text = reader.middle_name;
            PhoneBox.Text = reader.Phone;
            EmailBox.Text = reader.Email;
            PassportBox.Text = reader.PassportData;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_reader == null)
            {
                _reader = new Readers
                {
                    RegistrationDate = DateTime.Now
                };
                AppConnect.model01.Readers.Add(_reader);
            }

            if (string.IsNullOrWhiteSpace(LastNameBox.Text) ||
    string.IsNullOrWhiteSpace(FirstNameBox.Text))
            {
                MessageBox.Show("Фамилия и имя обязательны");
                return;
            }

            _reader.last_name = LastNameBox.Text.Trim();
            _reader.first_name = FirstNameBox.Text.Trim();
            _reader.middle_name = string.IsNullOrWhiteSpace(MiddleNameBox.Text)
                ? null
                : MiddleNameBox.Text.Trim();

            _reader.Phone = PhoneBox.Text?.Trim();
            _reader.Email = EmailBox.Text?.Trim();
            _reader.PassportData = PassportBox.Text?.Trim();

            AppConnect.model01.SaveChanges();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
