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
            LoadAll();
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

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            string name = Prompt("Введите ФИО автора");
            if (string.IsNullOrWhiteSpace(name)) return;

            AppConnect.model01.Authors.Add(new Authors { FullName = name });
            AppConnect.model01.SaveChanges();
            LoadAll();
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
