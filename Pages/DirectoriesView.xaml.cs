using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System.Linq;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class DirectoriesView : Page
    {
        public DirectoriesView()
        {
            InitializeComponent();
            if (AppConnect.CurrentUser?.RoleId == 2) // Librarian
            {
                AddGenre.Visibility = Visibility.Collapsed;
                DeleteGenre.Visibility = Visibility.Collapsed;
                AddAuthor.Visibility = Visibility.Collapsed;
                DeleteAuthor.Visibility = Visibility.Collapsed;
            }
            LoadAll();
        }
        private bool IsAdmin()
        {
            return AppConnect.CurrentUser != null &&
                   AppConnect.CurrentUser.RoleId == 1;
        }

        private void LoadAll()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            GenresGrid.ItemsSource = AppConnect.model01.Genres.ToList();
            AuthorsGrid.ItemsSource = AppConnect.model01.Authors.ToList();
        }


        private void AddGenre_Click(object sender, RoutedEventArgs e)
        {
            string name = Prompt("Введите название жанра");
            if (string.IsNullOrWhiteSpace(name)) return;

            AppConnect.model01.Genres.Add(new Genres { Name = name });
            AppConnect.model01.SaveChanges();
            LoadAll();
        }
        private void GenresGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsAdmin())
                return;

            Genres genre = GenresGrid.SelectedItem as Genres;
            if (genre == null)
                return;

            string newName = Prompt("Редактирование жанра");
            if (string.IsNullOrWhiteSpace(newName))
                return;

            genre.Name = newName;
            AppConnect.model01.SaveChanges();
            LoadAll();
        }
        private void DeleteGenre_Click(object sender, RoutedEventArgs e)
        {
            Genres genre = GenresGrid.SelectedItem as Genres;
            if (genre == null)
            {
                ShowError("Выберите жанр для удаления");
                return;
            }

            bool used = AppConnect.model01.Books.Any(b => b.GenreId == genre.GenreId);
            if (used)
            {
                ShowError("Нельзя удалить жанр — он используется в книгах");
                return;
            }

            var dialog = new DeleteMessageDialog(
                "Удаление жанра",
                $"Удалить жанр «{genre.Name}»?"
            );
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() != true)
                return;

            AppConnect.model01.Genres.Remove(genre);
            AppConnect.model01.SaveChanges();
            LoadAll();
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            string name = Prompt("Введите ФИО автора");
            if (string.IsNullOrWhiteSpace(name)) return;

            AppConnect.model01.Authors.Add(new Authors { FullName = name });
            AppConnect.model01.SaveChanges();
            LoadAll();
        }
        private void AuthorsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsAdmin())
                return;

            Authors author = AuthorsGrid.SelectedItem as Authors;
            if (author == null)
                return;

            string newName = Prompt("Редактирование автора");
            if (string.IsNullOrWhiteSpace(newName))
                return;

            author.FullName = newName;
            AppConnect.model01.SaveChanges();
            LoadAll();
        }
        private void DeleteAuthor_Click(object sender, RoutedEventArgs e)
        {
            // Проверка выбора
            Authors author = AuthorsGrid.SelectedItem as Authors;
            if (author == null)
            {
                ShowError("Выберите автора для удаления");
                return;
            }

            // 🔥 Проверка использования автора
            bool used = AppConnect.model01.Books
                .Any(b => b.AuthorId == author.AuthorId);

            if (used)
            {
                ShowError("Нельзя удалить автора — он используется в книгах");
                return;
            }

            // Подтверждение
            var dialog = new DeleteMessageDialog(
                "Удаление автора",
                $"Удалить автора «{author.FullName}»?"
            );
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() != true)
                return;

            // Удаление
            AppConnect.model01.Authors.Remove(author);
            AppConnect.model01.SaveChanges();

            LoadAll();
        }
        private void ShowError(string message)
        {
            new MessageDialog("Ошибка", message)
            {
                Owner = Window.GetWindow(this)
            }.ShowDialog();
        }
        private string Prompt(string title)
        {
            var dialog = new InputDialog(title);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
                return dialog.Result;

            return null;
        }

    }
}
