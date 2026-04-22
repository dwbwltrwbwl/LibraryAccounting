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
using System.Data.Entity;

namespace LibraryAccounting.Pages
{
    public partial class DirectoriesView : Page
    {
        private List<Genres> allGenres = new List<Genres>();
        private List<Authors> allAuthors = new List<Authors>();
        private List<Publishers> allPublishers = new List<Publishers>();

        private string currentGenreSortColumn = "Name";
        private ListSortDirection currentGenreSortDirection = ListSortDirection.Ascending;
        private string currentAuthorSortColumn = "FullName";
        private ListSortDirection currentAuthorSortDirection = ListSortDirection.Ascending;
        private string currentPublisherSortColumn = "PublisherName";
        private ListSortDirection currentPublisherSortDirection = ListSortDirection.Ascending;

        public DirectoriesView()
        {
            InitializeComponent();

            if (AppConnect.CurrentUser?.RoleId == 2) // Librarian
            {
                AddGenre.Visibility = Visibility.Collapsed;
                DeleteGenre.Visibility = Visibility.Collapsed;
                AddAuthor.Visibility = Visibility.Collapsed;
                DeleteAuthor.Visibility = Visibility.Collapsed;
                AddPublisher.Visibility = Visibility.Collapsed;
                DeletePublisher.Visibility = Visibility.Collapsed;
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

            allGenres = AppConnect.model01.Genres
                .Include(g => g.AgeRatings)
                .ToList();
            allAuthors = AppConnect.model01.Authors
                .Include("Cities")
                .Include("Cities.Countries")
                .ToList();
            allPublishers = AppConnect.model01.Publishers
                .Include("Cities")  // ← Убедитесь, что эта строка есть
                .ToList();

            ApplyGenreFiltersAndSort();
            ApplyAuthorFiltersAndSort();
            ApplyPublisherFiltersAndSort();
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
                filtered = filtered.Where(g =>
                    g.Name.ToLower().Contains(searchText) ||
                    (g.Description != null && g.Description.ToLower().Contains(searchText))
                );
            }

            // Сортировка
            var sorted = SortGenres(filtered).ToList();

            GenresGrid.ItemsSource = sorted;
            GenresEmptyPanel.Visibility = sorted.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
                case "Description":
                    return currentGenreSortDirection == ListSortDirection.Ascending
                        ? genres.OrderBy(g => g.Description)
                        : genres.OrderByDescending(g => g.Description);
                case "AgeRating":
                    return currentGenreSortDirection == ListSortDirection.Ascending
                        ? genres.OrderBy(g => g.AgeRatings.SortOrder)  // ← изменено
                        : genres.OrderByDescending(g => g.AgeRatings.SortOrder);
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
            var dialog = new GenreWindow();
            dialog.Owner = Window.GetWindow(this);

            while (true)
            {
                if (dialog.ShowDialog() != true)
                    return;

                if (IsGenreExists(dialog.NameValue))
                {
                    ShowError($"Жанр «{dialog.NameValue}» уже существует");
                    dialog = new GenreWindow(dialog.NameValue, dialog.DescriptionValue, dialog.SelectedAgeRatingId);
                    dialog.Owner = Window.GetWindow(this);
                    continue;
                }

                try
                {
                    AppConnect.model01.Genres.Add(new Genres
                    {
                        Name = dialog.NameValue,
                        Description = dialog.DescriptionValue,
                        AgeRatingId = dialog.SelectedAgeRatingId  // ← изменено
                    });

                    AppConnect.model01.SaveChanges();
                    LoadAll();
                    ShowInfo($"Жанр «{dialog.NameValue}» успешно добавлен");
                    return;
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при добавлении жанра: {ex.Message}");
                    dialog = new GenreWindow(dialog.NameValue, dialog.DescriptionValue, dialog.SelectedAgeRatingId);
                    dialog.Owner = Window.GetWindow(this);
                    continue;
                }
            }
        }

        private void GenresGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsAdmin()) return;

            var genre = GenresGrid.SelectedItem as Genres;
            if (genre == null) return;

            var dialog = new GenreWindow(genre);
            dialog.Owner = Window.GetWindow(this);

            while (true)
            {
                if (dialog.ShowDialog() != true)
                    return;

                if (IsGenreExists(dialog.NameValue, genre.GenreId))
                {
                    ShowError($"Жанр «{dialog.NameValue}» уже существует");
                    dialog = new GenreWindow(dialog.NameValue, dialog.DescriptionValue, dialog.SelectedAgeRatingId);
                    dialog.Owner = Window.GetWindow(this);
                    continue;
                }

                try
                {
                    genre.Name = dialog.NameValue;
                    genre.Description = dialog.DescriptionValue;
                    genre.AgeRatingId = dialog.SelectedAgeRatingId;  // ← изменено

                    AppConnect.model01.SaveChanges();
                    LoadAll();
                    ShowInfo($"Жанр «{dialog.NameValue}» успешно обновлен");
                    return;
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при обновлении жанра: {ex.Message}");
                    dialog = new GenreWindow(dialog.NameValue, dialog.DescriptionValue, dialog.SelectedAgeRatingId);
                    dialog.Owner = Window.GetWindow(this);
                    continue;
                }
            }
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

        #endregion

        #region Авторы

        // В ApplyAuthorFiltersAndSort - обновите поиск
        private void ApplyAuthorFiltersAndSort()
        {
            if (AuthorsGrid == null) return;

            // Загружаем авторов с городами и странами
            var filtered = allAuthors.AsEnumerable();

            string searchText = AuthorSearchTextBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(a =>
                    a.FullName.ToLower().Contains(searchText) ||
                    (a.Cities != null && a.Cities.Countries != null && a.Cities.Countries.CountryName.ToLower().Contains(searchText)) ||
                    (a.Cities != null && a.Cities.CityName.ToLower().Contains(searchText))
                );
            }

            var sorted = SortAuthors(filtered).ToList();
            AuthorsGrid.ItemsSource = sorted;
            AuthorsEmptyPanel.Visibility = sorted.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
                case "BirthDate":
                    return currentAuthorSortDirection == ListSortDirection.Ascending
                        ? authors.OrderBy(a => a.BirthDate)
                        : authors.OrderByDescending(a => a.BirthDate);
                case "DeathDate":
                    return currentAuthorSortDirection == ListSortDirection.Ascending
                        ? authors.OrderBy(a => a.DeathDate)
                        : authors.OrderByDescending(a => a.DeathDate);
                case "Country":
                    return currentAuthorSortDirection == ListSortDirection.Ascending
                        ? authors.OrderBy(a => a.Cities.Countries.CountryName)
                        : authors.OrderByDescending(a => a.Cities.Countries.CountryName);
                case "City":
                    return currentAuthorSortDirection == ListSortDirection.Ascending
                        ? authors.OrderBy(a => a.Cities.CityName)
                        : authors.OrderByDescending(a => a.Cities.CityName);
                default:
                    return currentAuthorSortDirection == ListSortDirection.Ascending
                        ? authors.OrderBy(a => a.FullName)
                        : authors.OrderByDescending(a => a.FullName);
            }
        }

        private bool IsAuthorExists(string fullName, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return false;
            fullName = fullName.Trim();
            return allAuthors.Any(a =>
                a.FullName != null &&
                a.FullName.Trim().Equals(fullName, StringComparison.OrdinalIgnoreCase) &&
                a.AuthorId != excludeId);
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AuthorWindow();
            dialog.Owner = Window.GetWindow(this);

            while (true)
            {
                if (dialog.ShowDialog() != true)
                    return;

                if (IsAuthorExists(dialog.FullName))
                {
                    ShowError($"Автор «{dialog.FullName}» уже существует");
                    // Создаём новое окно с теми же данными
                    var newDialog = new AuthorWindow();
                    newDialog.Owner = Window.GetWindow(this);
                    newDialog.NameBox.Text = dialog.FullName;
                    newDialog.BirthDatePicker.SelectedDate = dialog.BirthDate;
                    newDialog.DeathDatePicker.SelectedDate = dialog.DeathDate;

                    // Восстанавливаем выбранные страну и город
                    if (dialog.SelectedCityId.HasValue)
                    {
                        var city = AppConnect.model01.Cities.FirstOrDefault(c => c.CityId == dialog.SelectedCityId);
                        if (city != null)
                        {
                            newDialog.CountryComboBox.SelectedValue = city.CountryId;
                            newDialog.LoadCities(city.CountryId);
                            newDialog.CityComboBox.SelectedValue = dialog.SelectedCityId;
                        }
                    }

                    dialog = newDialog;
                    continue;
                }

                try
                {
                    AppConnect.model01.Authors.Add(new Authors
                    {
                        FullName = dialog.FullName,
                        BirthDate = dialog.BirthDate,
                        DeathDate = dialog.DeathDate,
                        CityId = dialog.SelectedCityId
                    });

                    AppConnect.model01.SaveChanges();
                    LoadAll();
                    return;
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при добавлении автора: {ex.Message}");
                    return;
                }
            }
        }

        private void AuthorsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsAdmin()) return;

            var author = AuthorsGrid.SelectedItem as Authors;
            if (author == null) return;

            var dialog = new AuthorWindow(author);
            dialog.Owner = Window.GetWindow(this);

            while (true)
            {
                if (dialog.ShowDialog() != true)
                    return;

                if (IsAuthorExists(dialog.FullName, author.AuthorId))
                {
                    ShowError($"Автор «{dialog.FullName}» уже существует");
                    // Создаём новое окно с текущими данными
                    var newDialog = new AuthorWindow(author);
                    newDialog.Owner = Window.GetWindow(this);
                    newDialog.NameBox.Text = dialog.FullName;
                    newDialog.BirthDatePicker.SelectedDate = dialog.BirthDate;
                    newDialog.DeathDatePicker.SelectedDate = dialog.DeathDate;

                    // Восстанавливаем выбранные страну и город
                    if (dialog.SelectedCityId.HasValue)
                    {
                        var city = AppConnect.model01.Cities.FirstOrDefault(c => c.CityId == dialog.SelectedCityId);
                        if (city != null)
                        {
                            newDialog.CountryComboBox.SelectedValue = city.CountryId;
                            newDialog.LoadCities(city.CountryId);
                            newDialog.CityComboBox.SelectedValue = dialog.SelectedCityId;
                        }
                    }

                    dialog = newDialog;
                    continue;
                }

                try
                {
                    author.FullName = dialog.FullName;
                    author.BirthDate = dialog.BirthDate;
                    author.DeathDate = dialog.DeathDate;
                    author.CityId = dialog.SelectedCityId;

                    AppConnect.model01.SaveChanges();
                    LoadAll();
                    return;
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при обновлении автора: {ex.Message}");
                    return;
                }
            }
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

        #endregion

        #region Издательства

        private void ApplyPublisherFiltersAndSort()
        {
            if (PublishersGrid == null) return;

            var filtered = allPublishers.AsEnumerable();

            string searchText = PublisherSearchTextBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(p =>
                    p.PublisherName.ToLower().Contains(searchText) ||
                    (p.Cities != null && p.Cities.CityName.ToLower().Contains(searchText))  // ← Проверьте
                );
            }

            var sorted = SortPublishers(filtered).ToList();

            PublishersGrid.ItemsSource = sorted;
            PublishersEmptyPanel.Visibility = sorted.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private IEnumerable<Publishers> SortPublishers(IEnumerable<Publishers> publishers)
        {
            switch (currentPublisherSortColumn)
            {
                case "PublisherId":
                    return currentPublisherSortDirection == ListSortDirection.Ascending
                        ? publishers.OrderBy(p => p.PublisherId)
                        : publishers.OrderByDescending(p => p.PublisherId);
                case "PublisherName":
                    return currentPublisherSortDirection == ListSortDirection.Ascending
                        ? publishers.OrderBy(p => p.PublisherName)
                        : publishers.OrderByDescending(p => p.PublisherName);
                case "City":
                    return currentPublisherSortDirection == ListSortDirection.Ascending
                        ? publishers.OrderBy(p => p.Cities.CityName)
                        : publishers.OrderByDescending(p => p.Cities.CityName);
                default:
                    return currentPublisherSortDirection == ListSortDirection.Ascending
                        ? publishers.OrderBy(p => p.PublisherName)
                        : publishers.OrderByDescending(p => p.PublisherName);
            }
        }

        private bool IsPublisherExists(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            name = name.Trim();
            return allPublishers.Any(p =>
                p.PublisherName != null &&
                p.PublisherName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase) &&
                p.PublisherId != excludeId);
        }

        private void AddPublisher_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PublisherWindow();
            dialog.Owner = Window.GetWindow(this);

            while (true)
            {
                if (dialog.ShowDialog() != true)
                    return;

                if (IsPublisherExists(dialog.NameValue))
                {
                    ShowError($"Издательство «{dialog.NameValue}» уже существует");
                    dialog = new PublisherWindow();
                    dialog.Owner = Window.GetWindow(this);
                    dialog.NameBox.Text = dialog.NameValue;
                    // Восстанавливаем выбранный город
                    if (dialog.SelectedCityId.HasValue)
                    {
                        dialog.CityComboBox.SelectedValue = dialog.SelectedCityId.Value;
                    }
                    continue;
                }

                try
                {
                    AppConnect.model01.Publishers.Add(new Publishers
                    {
                        PublisherName = dialog.NameValue,
                        CityId = dialog.SelectedCityId
                    });

                    AppConnect.model01.SaveChanges();
                    LoadAll();
                    return;
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при добавлении издательства: {ex.Message}");
                    return;
                }
            }
        }

        private void PublishersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsAdmin()) return;

            var publisher = PublishersGrid.SelectedItem as Publishers;
            if (publisher == null) return;

            var dialog = new PublisherWindow(publisher);
            dialog.Owner = Window.GetWindow(this);

            while (true)
            {
                if (dialog.ShowDialog() != true)
                    return;

                if (IsPublisherExists(dialog.NameValue, publisher.PublisherId))
                {
                    ShowError($"Издательство «{dialog.NameValue}» уже существует");
                    dialog = new PublisherWindow(publisher);
                    dialog.Owner = Window.GetWindow(this);
                    dialog.NameBox.Text = dialog.NameValue;
                    // Восстанавливаем выбранный город
                    if (dialog.SelectedCityId.HasValue)
                    {
                        dialog.CityComboBox.SelectedValue = dialog.SelectedCityId.Value;
                    }
                    continue;
                }

                try
                {
                    publisher.PublisherName = dialog.NameValue;
                    publisher.CityId = dialog.SelectedCityId;

                    AppConnect.model01.SaveChanges();
                    LoadAll();
                    return;
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при обновлении издательства: {ex.Message}");
                    return;
                }
            }
        }

        private void DeletePublisher_Click(object sender, RoutedEventArgs e)
        {
            var publisher = PublishersGrid.SelectedItem as Publishers;
            if (publisher == null)
            {
                ShowError("Выберите издательство");
                return;
            }

            var dialog = new DeleteMessageDialog("Удаление", $"Удалить издательство «{publisher.PublisherName}»?");
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() != true)
                return;

            AppConnect.model01.Publishers.Remove(publisher);
            AppConnect.model01.SaveChanges();
            LoadAll();
        }

        private void PublisherSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyPublisherFiltersAndSort();
        }

        private void ResetPublisherSearch_Click(object sender, RoutedEventArgs e)
        {
            if (PublisherSearchTextBox != null) PublisherSearchTextBox.Text = "";
        }

        private void PublishersGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column;
            var sortMemberPath = (column as DataGridBoundColumn)?.SortMemberPath ?? column.Header.ToString();

            if (currentPublisherSortColumn == sortMemberPath)
            {
                currentPublisherSortDirection = currentPublisherSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                currentPublisherSortColumn = sortMemberPath;
                currentPublisherSortDirection = ListSortDirection.Ascending;
            }

            e.Handled = true;
            ApplyPublisherFiltersAndSort();

            foreach (var col in PublishersGrid.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = currentPublisherSortDirection;
        }

        #endregion

        #region Общие методы

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabControl?.SelectedIndex == 0)
                ApplyGenreFiltersAndSort();
            else if (MainTabControl?.SelectedIndex == 1)
                ApplyAuthorFiltersAndSort();
            else if (MainTabControl?.SelectedIndex == 2)
                ApplyPublisherFiltersAndSort();
        }

        private void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            int selectedTab = MainTabControl?.SelectedIndex ?? 0;

            if (selectedTab == 0)
                ExportGenresToCSV();
            else if (selectedTab == 1)
                ExportAuthorsToCSV();
            else if (selectedTab == 2)
                ExportPublishersToCSV();
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
                    sb.AppendLine("\"ID\";\"Название жанра\";\"Описание\";\"Возрастной рейтинг\"");

                    foreach (var genre in items)
                    {
                        string ageRatingName = genre.AgeRatings?.AgeRatingName ?? "Не указан";
                        sb.AppendLine($"\"{genre.GenreId}\";\"{EscapeCsv(genre.Name)}\";\"{EscapeCsv(genre.Description)}\";\"{EscapeCsv(ageRatingName)}\"");
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
                    sb.AppendLine("\"ID\";\"ФИО автора\";\"Дата рождения\";\"Дата смерти\";\"Страна\";\"Город\"");

                    foreach (var author in items)
                    {
                        string countryName = author.Cities?.Countries?.CountryName ?? "";
                        string cityName = author.Cities?.CityName ?? "";

                        sb.AppendLine($"\"{author.AuthorId}\";\"{EscapeCsv(author.FullName)}\";\"{author.BirthDate:dd.MM.yyyy}\";\"{author.DeathDate:dd.MM.yyyy}\";\"{EscapeCsv(countryName)}\";\"{EscapeCsv(cityName)}\"");
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

        private void ExportPublishersToCSV()
        {
            var items = PublishersGrid.ItemsSource as IEnumerable<Publishers>;
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
                FileName = $"Издательства_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("\"ID\";\"Название издательства\";\"Город\"");

                    foreach (var publisher in items)
                    {
                        string cityName = publisher.Cities?.CityName ?? "";
                        sb.AppendLine($"\"{publisher.PublisherId}\";\"{EscapeCsv(publisher.PublisherName)}\";\"{EscapeCsv(cityName)}\"");
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
}