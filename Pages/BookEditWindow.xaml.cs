using LibraryAccounting.AppData;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Логика взаимодействия для BookEditWindow.xaml
    /// </summary>
    public partial class BookEditWindow : Window
    {
        private Books _book;
        private byte[] _imageBytes;

        // ➕ Добавление
        public BookEditWindow()
        {
            InitializeComponent();
            TitleText.Text = "Добавление книги";
            LoadLists();
        }

        // ✏️ Редактирование
        public BookEditWindow(Books book) : this()
        {
            _book = book;
            TitleText.Text = "Редактирование книги";
            FillData();
        }

        private void LoadLists()
        {
            AuthorBox.ItemsSource = AppConnect.model01.Authors.ToList();
            AuthorBox.DisplayMemberPath = "FullName";

            GenreBox.ItemsSource = AppConnect.model01.Genres.ToList();
            GenreBox.DisplayMemberPath = "Name";
        }

        private void FillData()
        {
            TitleBox.Text = _book.Title;
            PublisherBox.Text = _book.Publisher;
            YearBox.Text = _book.PublishYear.ToString();
            IsbnBox.Text = _book.ISBN;

            AuthorBox.SelectedItem = _book.Authors;
            GenreBox.SelectedItem = _book.Genres;

            _imageBytes = _book.CoverImage;

            if (_imageBytes != null)
            {
                CoverImage.Source = LoadImage(_imageBytes);
            }
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.png"
            };

            if (dlg.ShowDialog() == true)
            {
                _imageBytes = File.ReadAllBytes(dlg.FileName);
                CoverImage.Source = LoadImage(_imageBytes);
            }
        }

        private BitmapImage LoadImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            BitmapImage image = new BitmapImage();

            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
            }

            return image;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Введите название книги");
                return;
            }

            if (!int.TryParse(YearBox.Text, out int year))
            {
                MessageBox.Show("Неверный год издания");
                return;
            }

            if (_book == null)
            {
                _book = new Books();
                AppConnect.model01.Books.Add(_book);
            }

            _book.Title = TitleBox.Text;
            _book.Publisher = PublisherBox.Text;
            _book.PublishYear = year;
            _book.ISBN = IsbnBox.Text;
            _book.AuthorId = ((Authors)AuthorBox.SelectedItem).AuthorId;
            _book.GenreId = ((Genres)GenreBox.SelectedItem).GenreId;
            _book.CoverImage = _imageBytes;

            AppConnect.model01.SaveChanges();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
