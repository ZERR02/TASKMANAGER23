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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;

namespace TASKMANAGER23
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
 
        public ObservableCollection<Class1> AllTasks { get; set; } = new ObservableCollection<Class1>();


        private ObservableCollection<Class1> _filteredTasks = new ObservableCollection<Class1>();
        public ObservableCollection<Class1> FilteredTasks
        {
            get => _filteredTasks;
            set
            {
                _filteredTasks = value;
                OnPropertyChanged();
            }
        }

        private string _newTaskText;
        private string _searchText = string.Empty;
        private string _searchStatus;

        private string dbPath = "tasks.db";

        public string NewTaskText
        {
            get => _newTaskText;
            set
            {
                _newTaskText = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterTasks(); 
                UpdateSearchStatus();
            }
        }

        public string SearchStatus
        {
            get => _searchStatus;
            set
            {
                _searchStatus = value;
                OnPropertyChanged();
            }
        }


        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            InitDatabase();
            LoadTasks();
        }

        private void InitDatabase()
        {
            if (!File.Exists(dbPath))
                SQLiteConnection.CreateFile(dbPath);

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "CREATE TABLE IF NOT EXISTS Tasks (Id INTEGER PRIMARY KEY AUTOINCREMENT, Description TEXT, IsCompleted TINYINT)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();
            }
        }

        private void LoadTasks()
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT * FROM Tasks", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var task = new Class1
                        {
                            Id = reader.GetInt32(0),
                            Description = reader.GetString(1),
                            IsCompleted = reader.GetInt32(2) == 1
                        };

                        task.PropertyChanged += (s, args) => UpdateTaskInDb((Class1)s);
                        AllTasks.Add(task);
                    }
                }
            }


            UpdateFilteredCollection();
        }


        private void FilterTasks()
        {
            UpdateFilteredCollection();
        }


        private void UpdateFilteredCollection()
        {
            FilteredTasks.Clear();

            IEnumerable<Class1> filtered;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = AllTasks;
                SearchStatus = "";
            }
            else
            {
                string searchLower = SearchText.ToLower();
                filtered = AllTasks.Where(task =>
                    task.Description.ToLower().Contains(searchLower));

                int foundCount = filtered.Count();
                if (foundCount == 0)
                {
                    SearchStatus = "Ничего не найдено";
                }
                else
                {
                    SearchStatus = $"Найдено задач: {foundCount} из {AllTasks.Count}";
                }
            }

            foreach (var task in filtered)
            {
                FilteredTasks.Add(task);
            }

   
            OnPropertyChanged(nameof(HasSearchText));
        }

        private void UpdateSearchStatus()
        {
            OnPropertyChanged(nameof(HasSearchText));
        }

        private void AddTaskToDb(Class1 task)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("INSERT INTO Tasks (Description, IsCompleted) VALUES (@desc, @comp); SELECT last_insert_rowid();", conn);
                cmd.Parameters.AddWithValue("@desc", task.Description);
                cmd.Parameters.AddWithValue("@comp", task.IsCompleted ? 1 : 0);

                task.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void UpdateTaskInDb(Class1 task)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("UPDATE Tasks SET Description=@desc, IsCompleted=@comp WHERE Id=@id", conn);
                cmd.Parameters.AddWithValue("@desc", task.Description);
                cmd.Parameters.AddWithValue("@comp", task.IsCompleted ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", task.Id);
                cmd.ExecuteNonQuery();
            }
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewTaskText))
            {
                var newTask = new Class1 { Description = NewTaskText };

                AddTaskToDb(newTask);
                newTask.PropertyChanged += (s, args) => UpdateTaskInDb((Class1)s);

                AllTasks.Add(newTask);


                UpdateFilteredCollection();

                NewTaskText = string.Empty;
            }
            else
            {
                MessageBox.Show("Введите описание задачи");
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchText = string.Empty;
            var searchTextBox = FindName("SearchTextBox") as TextBox;
            searchTextBox?.Focus();
        }

        private void ShowAllTasks_Click(object sender, RoutedEventArgs e)
        {
            SearchText = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}