using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LibraryAccounting.Pages
{
    public partial class DirectoriesView : Page
    {
        private List<Genres> allGenres = new List<Genres>();
        private List<Authors> allAuthors = new List<Authors>();

        private string currentGenreSortColumn = "Name";
        private ListSortDirection currentGenreSortDirection = ListSortDirection.Ascending;

        private string currentAuthorSortColumn = "FullName";
        private ListSortDirection currentAuthorSortDirection = ListSortDirection.Ascending;

        private bool isGenreGrouping = false;
        private bool isAuthorGrouping = false;

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

            allGenres = AppConnect.model01.Genres.ToList();
            allAuthors = AppConnect.model01.Authors.ToList();

            ApplyGenreFiltersAndSort();
            ApplyAuthorFiltersAndSort();
        }

        #region Жанры

        private void ApplyGenreFiltersAndSort()
        {
            if (GenresGrid == null) return;

            var filtered = allGenres.AsEnumerable();

            // Поиск (фильтрация)
            string searchText = GenreSearchTextBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(g => g.Name.ToLower().Contains(searchText));
            }

            // Сортировка
            var sorted = SortGenres(filtered).ToList();

            if (isGenreGrouping)
            {
                // Группировка через CollectionViewSource
                var viewSource = new CollectionViewSource();
                viewSource.Source = sorted;
                viewSource.GroupDescriptions.Clear();
                viewSource.GroupDescriptions.Add(new PropertyGroupDescription("Name", new FirstLetterConverter()));

                GenresGrid.ItemsSource = viewSource.View;
            }
            else
            {
                GenresGrid.ItemsSource = sorted;
            }
        }

        private IEnumerable<Genres> SortGenres(IEnumerable<Genres> genres)
        {
            switch (currentGenreSortColumn)
            {
                case "GenreId":
                    return currentGenreSortDirection == ListSortDirection.Ascending
                        ? genres.OrderBy(g => g.GenreId)
                        : genres.OrderByDescending(g => g.GenreId);
                case "Name":
                    return currentGenreSortDirection == ListSortDirection.Ascending
                        ? genres.OrderBy(g => g.Name)
                        : genres.OrderByDescending(g => g.Name);
                default:
                    return currentGenreSortDirection == ListSortDirection.Ascending
                        ? genres.OrderBy(g => g.Name)
                        : genres.OrderByDescending(g => g.Name);
            }
        }

        private bool IsGenreExists(string name, int? excludeId = null)
        {
            name = name?.Trim();
            return allGenres.Any(g => g.Name.ToLower() == name.ToLower() && g.GenreId != excludeId);
        }

        private void AddGenre_Click(object sender, RoutedEventArgs e)
        {
            string name = Prompt("Введите название жанра");

            // Обрезка пробелов
            name = name?.Trim();

            // Проверка на пустоту (после обрезки пробелов)
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("Название жанра не может быть пустым");
                return;
            }

            // Проверка на существование
            if (IsGenreExists(name))
            {
                ShowError($"Жанр «{name}» уже существует");
                return;
            }

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

            string newName = Prompt("Редактирование жанра", genre.Name);

            // Обрезка пробелов
            newName = newName?.Trim();

            // Проверка на пустоту (после обрезки пробелов)
            if (string.IsNullOrWhiteSpace(newName))
            {
                ShowError("Название жанра не может быть пустым");
                return;
            }

            // Проверка на существование (исключая текущий)
            if (IsGenreExists(newName, genre.GenreId))
            {
                ShowError($"Жанр «{newName}» уже существует");
                return;
            }

            if (newName == genre.Name) return;

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

        private void GenresGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column;
            var sortMemberPath = (column as DataGridBoundColumn)?.SortMemberPath ?? column.Header.ToString();

            if (currentGenreSortColumn == sortMemberPath)
            {
                currentGenreSortDirection = currentGenreSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                currentGenreSortColumn = sortMemberPath;
                currentGenreSortDirection = ListSortDirection.Ascending;
            }

            e.Handled = true;
            ApplyGenreFiltersAndSort();

            // Обновляем индикатор сортировки
            foreach (var col in GenresGrid.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = currentGenreSortDirection;
        }

        private void GenreSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyGenreFiltersAndSort();
        }

        private void ResetGenreSearch_Click(object sender, RoutedEventArgs e)
        {
            if (GenreSearchTextBox != null) GenreSearchTextBox.Text = "";
        }

        private void GenreGroupingCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            isGenreGrouping = true;
            ApplyGenreFiltersAndSort();
        }

        private void GenreGroupingCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            isGenreGrouping = false;
            ApplyGenreFiltersAndSort();
        }

        #endregion

        #region Авторы

        private void ApplyAuthorFiltersAndSort()
        {
            if (AuthorsGrid == null) return;

            var filtered = allAuthors.AsEnumerable();

            // Поиск (фильтрация)
            string searchText = AuthorSearchTextBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(a => a.FullName.ToLower().Contains(searchText));
            }

            // Сортировка
            var sorted = SortAuthors(filtered).ToList();

            if (isAuthorGrouping)
            {
                // Группировка через CollectionViewSource
                var viewSource = new CollectionViewSource();
                viewSource.Source = sorted;
                viewSource.GroupDescriptions.Clear();
                viewSource.GroupDescriptions.Add(new PropertyGroupDescription("FullName", new FirstLetterConverter()));

                AuthorsGrid.ItemsSource = viewSource.View;
            }
            else
            {
                AuthorsGrid.ItemsSource = sorted;
            }
        }

        private IEnumerable<Authors> SortAuthors(IEnumerable<Authors> authors)
        {
            switch (currentAuthorSortColumn)
            {
                case "AuthorId":
                    return currentAuthorSortDirection == ListSortDirection.Ascending
                        ? authors.OrderBy(a => a.AuthorId)
                        : authors.OrderByDescending(a => a.AuthorId);
                case "FullName":
                    return currentAuthorSortDirection == ListSortDirection.Ascending
                        ? authors.OrderBy(a => a.FullName)
                        : authors.OrderByDescending(a => a.FullName);
                default:
                    return currentAuthorSortDirection == ListSortDirection.Ascending
                        ? authors.OrderBy(a => a.FullName)
                        : authors.OrderByDescending(a => a.FullName);
            }
        }

        private bool IsAuthorExists(string fullName, int? excludeId = null)
        {
            fullName = fullName?.Trim();
            return allAuthors.Any(a => a.FullName.ToLower() == fullName.ToLower() && a.AuthorId != excludeId);
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            string name = Prompt("Введите ФИО автора");

            // Обрезка пробелов
            name = name?.Trim();

            // Проверка на пустоту (после обрезки пробелов)
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("ФИО автора не может быть пустым");
                return;
            }

            // Проверка на существование
            if (IsAuthorExists(name))
            {
                ShowError($"Автор «{name}» уже существует");
                return;
            }

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

            string newName = Prompt("Редактирование автора", author.FullName);

            // Обрезка пробелов
            newName = newName?.Trim();

            // Проверка на пустоту (после обрезки пробелов)
            if (string.IsNullOrWhiteSpace(newName))
            {
                ShowError("ФИО автора не может быть пустым");
                return;
            }

            // Проверка на существование (исключая текущего)
            if (IsAuthorExists(newName, author.AuthorId))
            {
                ShowError($"Автор «{newName}» уже существует");
                return;
            }

            if (newName == author.FullName) return;

            author.FullName = newName;
            AppConnect.model01.SaveChanges();
            LoadAll();
        }

        private void DeleteAuthor_Click(object sender, RoutedEventArgs e)
        {
            Authors author = AuthorsGrid.SelectedItem as Authors;
            if (author == null)
            {
                ShowError("Выберите автора для удаления");
                return;
            }

            bool used = AppConnect.model01.Books.Any(b => b.AuthorId == author.AuthorId);
            if (used)
            {
                ShowError("Нельзя удалить автора — он используется в книгах");
                return;
            }

            var dialog = new DeleteMessageDialog(
                "Удаление автора",
                $"Удалить автора «{author.FullName}»?"
            );
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() != true)
                return;

            AppConnect.model01.Authors.Remove(author);
            AppConnect.model01.SaveChanges();
            LoadAll();
        }

        private void AuthorsGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column;
            var sortMemberPath = (column as DataGridBoundColumn)?.SortMemberPath ?? column.Header.ToString();

            if (currentAuthorSortColumn == sortMemberPath)
            {
                currentAuthorSortDirection = currentAuthorSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                currentAuthorSortColumn = sortMemberPath;
                currentAuthorSortDirection = ListSortDirection.Ascending;
            }

            e.Handled = true;
            ApplyAuthorFiltersAndSort();

            // Обновляем индикатор сортировки
            foreach (var col in AuthorsGrid.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = currentAuthorSortDirection;
        }

        private void AuthorSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyAuthorFiltersAndSort();
        }

        private void ResetAuthorSearch_Click(object sender, RoutedEventArgs e)
        {
            if (AuthorSearchTextBox != null) AuthorSearchTextBox.Text = "";
        }

        private void AuthorGroupingCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            isAuthorGrouping = true;
            ApplyAuthorFiltersAndSort();
        }

        private void AuthorGroupingCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            isAuthorGrouping = false;
            ApplyAuthorFiltersAndSort();
        }

        #endregion

        #region Общие методы

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabControl?.SelectedIndex == 0)
                ApplyGenreFiltersAndSort();
            else if (MainTabControl?.SelectedIndex == 1)
                ApplyAuthorFiltersAndSort();
        }

        private void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            int selectedTab = MainTabControl?.SelectedIndex ?? 0;

            if (selectedTab == 0)
                ExportGenresToCSV();
            else
                ExportAuthorsToCSV();
        }

        private void ExportGenresToCSV()
        {
            var items = GenresGrid.ItemsSource as IEnumerable<Genres>;
            if (items == null || !items.Any())
            {
                ShowError("Нет данных для экспорта");
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Сохранить CSV файл",
                Filter = "CSV файлы (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = $"Жанры_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("\"ID\";\"Название жанра\"");

                    foreach (var genre in items)
                    {
                        sb.AppendLine($"\"{genre.GenreId}\";\"{EscapeCsv(genre.Name)}\"");
                    }

                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    ShowInfo($"Экспорт выполнен успешно!\nФайл сохранен: {saveDialog.FileName}");
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при экспорте: {ex.Message}");
                }
            }
        }

        private void ExportAuthorsToCSV()
        {
            var items = AuthorsGrid.ItemsSource as IEnumerable<Authors>;
            if (items == null || !items.Any())
            {
                ShowError("Нет данных для экспорта");
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Сохранить CSV файл",
                Filter = "CSV файлы (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = $"Авторы_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("\"ID\";\"ФИО автора\"");

                    foreach (var author in items)
                    {
                        sb.AppendLine($"\"{author.AuthorId}\";\"{EscapeCsv(author.FullName)}\"");
                    }

                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    ShowInfo($"Экспорт выполнен успешно!\nФайл сохранен: {saveDialog.FileName}");
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при экспорте: {ex.Message}");
                }
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }

        private string Prompt(string title, string defaultValue = "")
        {
            var dialog = new InputDialog(title);
            dialog.Owner = Window.GetWindow(this);
            dialog.SetDefaultValue(defaultValue);

            if (dialog.ShowDialog() == true)
                return dialog.Result?.Trim();

            return null;
        }

        private void ShowError(string message)
        {
            new MessageDialog("Ошибка", message)
            {
                Owner = Window.GetWindow(this)
            }.ShowDialog();
        }

        private void ShowInfo(string message)
        {
            new MessageDialog("Информация", message)
            {
                Owner = Window.GetWindow(this)
            }.ShowDialog();
        }

        #endregion
    }

    // Конвертер для группировки по первой букве
    public class FirstLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = value as string;
            if (string.IsNullOrEmpty(str))
                return "Без названия";

            char firstChar = char.ToUpper(str[0]);
            if (char.IsLetter(firstChar))
                return firstChar.ToString();
            else
                return "#";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}