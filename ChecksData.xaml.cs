using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace t
{
        public partial class ChecksData : Window
    {
        private int currentUserId;
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";

        // Додаємо шлях до іконок
        private string iconsPath = @"C:\t\t\Properties\References\Categories\";

        private enum DataMode { Expenses, Income, Savings }
        private DataMode currentMode = DataMode.Expenses;

        private CheckRecord selectedRecord = null;
        private DateTime filterStartDate;
        private DateTime filterEndDate;
        private bool hasUnsavedChanges = false;
        private Dictionary<int, CheckRecord> changedRecords = new Dictionary<int, CheckRecord>();

        public class CheckRecord : INotifyPropertyChanged
        {
            private int _id;
            private string _categoryImage;
            private string _categoryName;
            private string _comment;
            private decimal _amount;
            private DateTime _date;

            public int Id
            {
                get => _id;
                set
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }

            public string CategoryImage
            {
                get => _categoryImage;
                set
                {
                    _categoryImage = value;
                    OnPropertyChanged(nameof(CategoryImage));
                    OnPropertyChanged(nameof(CategoryImageFullPath));
                }
            }

            public string CategoryImageFullPath
            {
                get
                {
                    if (string.IsNullOrEmpty(_categoryImage))
                        return string.Empty;

                    try
                    {
                        // Спробуємо кілька можливих шляхів
                        string[] possiblePaths =
                        {
                            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                "Properties", "References", "Categories", _categoryImage),
                            System.IO.Path.Combine(Environment.CurrentDirectory,
                                "Properties", "References", "Categories", _categoryImage),
                            @"C:\t\t\Properties\References\Categories\" + _categoryImage,
                            @"C:/t/t/Properties/References/Categories/" + _categoryImage
                        };

                        foreach (string path in possiblePaths)
                        {
                            if (File.Exists(path))
                            {
                                return path;
                            }
                        }

                        // Якщо файл не знайдено, повертаємо порожній рядок
                        return string.Empty;
                    }
                    catch
                    {
                        return string.Empty;
                    }
                }
            }

            public string CategoryName
            {
                get => _categoryName;
                set
                {
                    _categoryName = value;
                    OnPropertyChanged(nameof(CategoryName));
                }
            }

            public string Comment
            {
                get => _comment;
                set
                {
                    _comment = value;
                    OnPropertyChanged(nameof(Comment));
                }
            }

            public decimal Amount
            {
                get => _amount;
                set
                {
                    _amount = value;
                    OnPropertyChanged(nameof(Amount));
                    OnPropertyChanged(nameof(FormattedAmount));
                    OnPropertyChanged(nameof(AmountColor));
                }
            }

            public string FormattedAmount
            {
                get
                {
                    return Amount >= 0 ? $"+{Amount:F2} ₴" : $"{Amount:F2} ₴";
                }
            }

            public Brush AmountColor
            {
                get
                {
                    return Amount >= 0 ? Brushes.Green : Brushes.Red;
                }
            }

            public DateTime Date
            {
                get => _date;
                set
                {
                    _date = value;
                    OnPropertyChanged(nameof(Date));
                    OnPropertyChanged(nameof(FormattedDate));
                }
            }

            public string FormattedDate
            {
                get
                {
                    return Date.ToString("dd.MM.yyyy");
                }
            }

            public string Type { get; set; } // "expenses", "income", "savings"

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ChecksData(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                InitializeComponent();
                currentUserId = userId;
                filterStartDate = startDate;
                filterEndDate = endDate;

                // Ініціалізуємо заголовок таблиці
                if (tblTableTitle != null)
                    tblTableTitle.Text = "ВИТРАТИ";

                // Додаємо обробники для RadioButton
                if (rbExpenses != null)
                    rbExpenses.Checked += RbExpenses_Checked;
                if (rbIncome != null)
                    rbIncome.Checked += RbIncome_Checked;
                if (rbSavings != null)
                    rbSavings.Checked += RbSavings_Checked;

                if (dgChecks != null)
                {
                    dgChecks.MouseDoubleClick += DgChecks_MouseDoubleClick;
                    dgChecks.CellEditEnding += DgChecks_CellEditEnding;
                    dgChecks.SelectionChanged += DgChecks_SelectionChanged;
                }

                if (btnDelete != null)
                {
                    btnDelete.Visibility = Visibility.Collapsed;
                }

                if (btnBack != null)
                    btnBack.Click += BtnClose_Click;

               
                LoadExpensesChecks();
                UpdateColumnsVisibility(); // Оновлюємо видимість колонок
                this.Closing += ChecksData_Closing;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка ініціалізації вікна: {ex.Message}",
                    "Критична помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void ChecksData_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (hasUnsavedChanges)
            {
                MessageBoxResult result = MessageBox.Show(
                    "У вас є незбереженні зміни. Зберегти перед закриттям?",
                    "Незбережені зміни",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveAllChanges();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void RbExpenses_Checked(object sender, RoutedEventArgs e)
        {
            currentMode = DataMode.Expenses;
            if (tblTableTitle != null)
                tblTableTitle.Text = "ВИТРАТИ";
            LoadExpensesChecks();
            ResetSelection();
            UpdateColumnsVisibility();
        }

        private void RbIncome_Checked(object sender, RoutedEventArgs e)
        {
            currentMode = DataMode.Income;
            if (tblTableTitle != null)
                tblTableTitle.Text = "ДОХОДИ";
            LoadIncomeChecks();
            ResetSelection();
            UpdateColumnsVisibility();
        }

        private void RbSavings_Checked(object sender, RoutedEventArgs e)
        {
            currentMode = DataMode.Savings;
            if (tblTableTitle != null)
                tblTableTitle.Text = "ЗАОЩАДЖЕННЯ";
            LoadSavingsChecks();
            ResetSelection();
            UpdateColumnsVisibility();
        }

        private void UpdateColumnsVisibility()
        {
            if (dgChecks == null || dgChecks.Columns.Count < 6) return;

            bool isSavingsMode = currentMode == DataMode.Savings;

            // Оновлюємо видимість колонок
            // Колонки: 0-Id, 1-Зображення, 2-Категорія, 3-Коментар, 4-Сума, 5-Дата
            dgChecks.Columns[1].Visibility = isSavingsMode ? Visibility.Collapsed : Visibility.Visible; // Зображення
            dgChecks.Columns[2].Visibility = isSavingsMode ? Visibility.Collapsed : Visibility.Visible; // Категорія
            dgChecks.Columns[3].Visibility = isSavingsMode ? Visibility.Collapsed : Visibility.Visible; // Коментар

            // Оновлюємо ширину колонок
            if (isSavingsMode)
            {
                dgChecks.Columns[4].Width = new DataGridLength(1.5, DataGridLengthUnitType.Star); // Сума
                dgChecks.Columns[5].Width = new DataGridLength(1, DataGridLengthUnitType.Star); // Дата
            }
            else
            {
                dgChecks.Columns[4].Width = new DataGridLength(1, DataGridLengthUnitType.Star); // Сума
                dgChecks.Columns[5].Width = new DataGridLength(0.8, DataGridLengthUnitType.Star); // Дата
            }
        }

        private void ResetSelection()
        {
            try
            {
                selectedRecord = null;

                if (btnDelete != null)
                {
                    btnDelete.Visibility = Visibility.Collapsed;
                }

                if (dgChecks != null)
                {
                    dgChecks.SelectedItem = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка в ResetSelection: {ex.Message}");
            }
        }

        private void LoadExpensesChecks()
        {
            try
            {
                List<CheckRecord> records = new List<CheckRecord>();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT e.IDexpenses, e.CategoryImageExpenses, e.CategoryNameExpenses, 
                       e.AmoutExpenses, e.ExpenseDate, e.CommentExpenses
                FROM expenses e
                WHERE e.IDuser = @userid
                AND e.ExpenseDate BETWEEN @startDate AND @endDate
                ORDER BY e.ExpenseDate DESC, e.IDexpenses DESC";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);
                    cmd.Parameters.AddWithValue("@startDate", filterStartDate);
                    cmd.Parameters.AddWithValue("@endDate", filterEndDate);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CheckRecord record = new CheckRecord
                            {
                                Id = reader.GetInt32("IDexpenses"),
                                CategoryImage = reader["CategoryImageExpenses"].ToString(),
                                CategoryName = reader["CategoryNameExpenses"].ToString(),
                                Amount = -Math.Abs(reader.GetDecimal("AmoutExpenses")),
                                Date = reader.GetDateTime("ExpenseDate"),
                                Comment = reader["CommentExpenses"].ToString(),
                                Type = "expenses"
                            };

                            records.Add(record);
                        }
                    }
                }

                    if (dgChecks != null)
                    {
                        dgChecks.ItemsSource = records;
                    }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження витрат: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadIncomeChecks()
        {
            try
            {
                List<CheckRecord> records = new List<CheckRecord>();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                    SELECT i.IDincome, i.CategoryImageIncome, i.CategoryNameIncome, 
                           i.AmountIncome, i.IncomeDate, COALESCE(i.CommentIncome, '') as CommentIncome
                    FROM income i
                    WHERE i.IDuser = @userid
                    AND i.IncomeDate BETWEEN @startDate AND @endDate
                    ORDER BY i.IncomeDate DESC, i.IDincome DESC";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);
                    cmd.Parameters.AddWithValue("@startDate", filterStartDate);
                    cmd.Parameters.AddWithValue("@endDate", filterEndDate);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CheckRecord record = new CheckRecord
                            {
                                Id = reader.GetInt32("IDincome"),
                                CategoryImage = reader["CategoryImageIncome"].ToString(),
                                CategoryName = reader["CategoryNameIncome"].ToString(),
                                Amount = Math.Abs(reader.GetDecimal("AmountIncome")),
                                Date = reader.GetDateTime("IncomeDate"),
                                Comment = reader["CommentIncome"].ToString(),
                                Type = "income"
                            };

                            records.Add(record);
                        }
                    }
                }

               
                    if (dgChecks != null)
                    {
                        dgChecks.ItemsSource = records;
                    }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження доходів: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSavingsChecks()
        {
            try
            {
                List<CheckRecord> records = new List<CheckRecord>();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                    SELECT s.IDsaving, s.AmoutSaving, s.SavingDate
                    FROM saving s
                    WHERE s.IDuser = @userid
                    AND s.SavingDate BETWEEN @startDate AND @endDate
                    ORDER BY s.SavingDate DESC, s.IDsaving DESC";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);
                    cmd.Parameters.AddWithValue("@startDate", filterStartDate);
                    cmd.Parameters.AddWithValue("@endDate", filterEndDate);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CheckRecord record = new CheckRecord
                            {
                                Id = reader.GetInt32("IDsaving"),
                                CategoryImage = "",
                                CategoryName = "Заощадження",
                                Amount = reader.GetDecimal("AmoutSaving"),
                                Date = reader.GetDateTime("SavingDate"),
                                Comment = "",
                                Type = "savings"
                            };

                            records.Add(record);
                        }
                    }
                }

                
                    if (dgChecks != null)
                    {
                        dgChecks.ItemsSource = records;
                    }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження заощаджень: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowNoRecordsMessage(string message)
        {
            if (dgChecks != null)
            {
                dgChecks.ItemsSource = null;

                // Очищаємо колонки
                dgChecks.Columns.Clear();

                // Створюємо шаблонну колонку
                DataGridTemplateColumn messageColumn = new DataGridTemplateColumn();
                messageColumn.Header = "Повідомлення";
                messageColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

                // Створюємо DataTemplate для комірки
                DataTemplate cellTemplate = new DataTemplate();

                // Створюємо фабрику для TextBlock
                FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetValue(TextBlock.TextProperty, message);
                textBlockFactory.SetValue(TextBlock.FontSizeProperty, 18.0);
                textBlockFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                textBlockFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Gray);
                textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                textBlockFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);

                // Встановлюємо фабрику як візуальне дерево шаблону
                cellTemplate.VisualTree = textBlockFactory;
                messageColumn.CellTemplate = cellTemplate;

                // Додаємо колонку
                dgChecks.Columns.Add(messageColumn);

                // Створюємо список з одним елементом для відображення рядка
                dgChecks.ItemsSource = new List<object> { new { } };
            }
        }

        private void DgChecks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (btnDelete == null) return;

                if (dgChecks != null && dgChecks.SelectedItem is CheckRecord record)
                {
                    selectedRecord = record;
                    btnDelete.Visibility = Visibility.Visible;
                }
                else
                {
                    btnDelete.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка в DgChecks_SelectionChanged: {ex.Message}");
            }
        }

        private void DgChecks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgChecks == null || dgChecks.ItemsSource == null || dgChecks.Items.Count == 0)
                return;

            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridCell cell)
            {
                // У нашому XAML: 0-Id (прихована), 1-Зображення, 2-Категорія, 3-Коментар, 4-Сума, 5-Дата
                int columnIndex = cell.Column.DisplayIndex;

                // Дозволяємо редагувати коментар (колонка 3) та суму (колонка 4)
                if (columnIndex == 3 || columnIndex == 4)
                {
                    dgChecks.BeginEdit();
                }
            }
        }

        private void DgChecks_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row.Item is CheckRecord record)
                {
                    // У нашому XAML: 0-Id (прихована), 1-Зображення, 2-Категорія, 3-Коментар, 4-Сума, 5-Дата
                    int columnIndex = e.Column.DisplayIndex;

                    if (columnIndex == 3) // Колонка коментаря
                    {
                        if (e.EditingElement is TextBox textBox)
                        {
                            // Для витрат та заощаджень коментар не зберігається
                            if (currentMode == DataMode.Expenses || currentMode == DataMode.Savings)
                            {
                                MessageBox.Show("Для витрат та заощаджень коментар не можна змінювати", "Інформація",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }

                            record.Comment = textBox.Text;
                            if (!changedRecords.ContainsKey(record.Id))
                            {
                                changedRecords[record.Id] = record;
                                hasUnsavedChanges = true;
                            }
                        }
                    }
                    else if (columnIndex == 4) // Колонка суми
                    {
                        if (e.EditingElement is TextBox textBox)
                        {
                            if (decimal.TryParse(textBox.Text, out decimal newAmount))
                            {
                                // Для витрат сума має бути від'ємною
                                if (currentMode == DataMode.Expenses)
                                    record.Amount = -Math.Abs(newAmount);
                                else
                                    record.Amount = Math.Abs(newAmount);

                                if (!changedRecords.ContainsKey(record.Id))
                                {
                                    changedRecords[record.Id] = record;
                                    hasUnsavedChanges = true;
                                }
                            }
                            else
                            {
                                MessageBox.Show("Будь ласка, введіть коректну суму", "Помилка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                e.Cancel = true;
                            }
                        }
                    }
                }
            }
        }

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.' && c != ',')
                {
                    e.Handled = true;
                    return;
                }
            }

            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                string currentText = textBox.Text + e.Text;
                int dotCount = currentText.Count(c => c == '.' || c == ',');

                if (dotCount > 1)
                {
                    e.Handled = true;
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRecord == null)
            {
                MessageBox.Show("Будь ласка, виберіть запис для видалення", "Увага",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string message = "Ви впевнені, що хочете видалити цей запис?";
            MessageBoxResult result = MessageBox.Show(message, "Підтвердження видалення",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DeleteRecordFromDatabase();
            }
        }

        private void DeleteRecordFromDatabase()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName, idColumn;

                    switch (currentMode)
                    {
                        case DataMode.Expenses:
                            tableName = "expenses";
                            idColumn = "IDexpenses";
                            break;
                        case DataMode.Income:
                            tableName = "income";
                            idColumn = "IDincome";
                            break;
                        case DataMode.Savings:
                            tableName = "saving";
                            idColumn = "IDsaving";
                            break;
                        default:
                            return;
                    }

                    string query = $"DELETE FROM {tableName} WHERE {idColumn} = @id AND IDuser = @userid";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", selectedRecord.Id);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Видаляємо зі списку змінених записів, якщо він там є
                        if (changedRecords.ContainsKey(selectedRecord.Id))
                        {
                            changedRecords.Remove(selectedRecord.Id);
                        }

                        MessageBox.Show("Запис успішно видалено", "Успіх",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Перезавантажуємо дані
                        switch (currentMode)
                        {
                            case DataMode.Expenses:
                                LoadExpensesChecks();
                                break;
                            case DataMode.Income:
                                LoadIncomeChecks();
                                break;
                            case DataMode.Savings:
                                LoadSavingsChecks();
                                break;
                        }

                        ResetSelection();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка видалення запису: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveAllChanges();
        }

        private void SaveAllChanges()
        {
            try
            {
                if (changedRecords.Count > 0)
                {
                    int updatedCount = 0;

                    foreach (var record in changedRecords.Values)
                    {
                        UpdateRecordInDatabase(record);
                        updatedCount++;
                    }

                    MessageBox.Show($"{updatedCount} записів успішно збережено!", "Збереження",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    changedRecords.Clear();
                    hasUnsavedChanges = false;

                    // Оновлюємо відображення
                    switch (currentMode)
                    {
                        case DataMode.Expenses:
                            LoadExpensesChecks();
                            break;
                        case DataMode.Income:
                            LoadIncomeChecks();
                            break;
                        case DataMode.Savings:
                            LoadSavingsChecks();
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("Немає змін для збереження", "Інформація",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження змін: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRecordInDatabase(CheckRecord record)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    switch (record.Type)
                    {
                        case "expenses":
                            string expensesQuery = @"
                            UPDATE expenses 
                            SET AmoutExpenses = @amount 
                            WHERE IDexpenses = @id AND IDuser = @userid";

                            MySqlCommand expensesCmd = new MySqlCommand(expensesQuery, connection);
                            expensesCmd.Parameters.AddWithValue("@amount", Math.Abs(record.Amount));
                            expensesCmd.Parameters.AddWithValue("@id", record.Id);
                            expensesCmd.Parameters.AddWithValue("@userid", currentUserId);
                            expensesCmd.ExecuteNonQuery();
                            break;

                        case "income":
                            string incomeQuery = @"
                            UPDATE income 
                            SET AmountIncome = @amount, 
                                CommentIncome = @comment 
                            WHERE IDincome = @id AND IDuser = @userid";

                            MySqlCommand incomeCmd = new MySqlCommand(incomeQuery, connection);
                            incomeCmd.Parameters.AddWithValue("@amount", Math.Abs(record.Amount));
                            incomeCmd.Parameters.AddWithValue("@comment", record.Comment ?? "");
                            incomeCmd.Parameters.AddWithValue("@id", record.Id);
                            incomeCmd.Parameters.AddWithValue("@userid", currentUserId);
                            incomeCmd.ExecuteNonQuery();
                            break;

                        case "savings":
                            string savingsQuery = @"
                            UPDATE saving 
                            SET AmoutSaving = @amount 
                            WHERE IDsaving = @id AND IDuser = @userid";

                            MySqlCommand savingsCmd = new MySqlCommand(savingsQuery, connection);
                            savingsCmd.Parameters.AddWithValue("@amount", Math.Abs(record.Amount));
                            savingsCmd.Parameters.AddWithValue("@id", record.Id);
                            savingsCmd.Parameters.AddWithValue("@userid", currentUserId);
                            savingsCmd.ExecuteNonQuery();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення запису: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (dgChecks == null)
            {
                MessageBox.Show("Помилка: DataGrid не завантажено. Перевірте XAML файл.",
                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(currentUserId);
            mainWindow.Show();
            this.Close();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            switch (currentMode)
            {
                case DataMode.Expenses:
                    LoadExpensesChecks();
                    break;
                case DataMode.Income:
                    LoadIncomeChecks();
                    break;
                case DataMode.Savings:
                    LoadSavingsChecks();
                    break;
            }

            MessageBox.Show("Дані оновлено!", "Оновлення",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}