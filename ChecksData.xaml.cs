using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7803706;password=DrUIbcmB1f;database=sql7803706;Charset=utf8mb4;";
        private bool isExpensesMode = true;
        private CheckRecord selectedRecord = null;

        // Клас для представлення запису
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
                    OnPropertyChanged(nameof(CategoryImagePathForBinding));
                }
            }

            // Повний шлях до зображення
            public string CategoryImageFullPath
            {
                get
                {
                    if (string.IsNullOrEmpty(_categoryImage))
                        return string.Empty;

                    try
                    {
                        // Абсолютний шлях до папки з зображеннями
                        return $"C:/t/t/Properties/References/Categories/{_categoryImage}";
                    }
                    catch
                    {
                        return string.Empty;
                    }
                }
            }

            // Метод для отримання шляху до зображення з урахуванням значення за замовчуванням
            public string GetCategoryImagePath()
            {
                // Якщо шлях до зображення порожній або null
                if (string.IsNullOrEmpty(CategoryImageFullPath))
                {
                    // Повертаємо шлях до зображення за замовчуванням
                    return GetDefaultCategoryImagePath();
                }

                // Перевіряємо, чи існує файл за вказаним шляхом
                if (!System.IO.File.Exists(CategoryImageFullPath))
                {
                    // Якщо файл не існує, повертаємо шлях за замовчуванням
                    return GetDefaultCategoryImagePath();
                }

                // Якщо все добре, повертаємо оригінальний шлях
                return CategoryImageFullPath;
            }

            // Метод для отримання шляху до зображення за замовчуванням
            private string GetDefaultCategoryImagePath()
            {
                // Повертаємо шлях до зображення за замовчуванням
                return "C:/t/t/Properties/References/Categories/dots-vertical.png";
            }

            // Властивість для зв'язування, яка використовує метод GetCategoryImagePath
            public string CategoryImagePathForBinding => GetCategoryImagePath();

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
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Клас конвертера для зображень (можна використовувати замість методу)
        public class CategoryImageConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is CheckRecord checkData)
                {
                    // Викликаємо метод GetCategoryImagePath() об'єкта CheckData
                    return checkData.GetCategoryImagePath();
                }
                else if (value is string imagePath)
                {
                    // Якщо передано рядок (шлях)
                    if (string.IsNullOrEmpty(imagePath))
                    {
                        return "C:/t/t/Properties/References/Categories/dots-vertical.png";
                    }

                    // Перевіряємо, чи існує файл
                    if (!System.IO.File.Exists(imagePath))
                    {
                        return "C:/t/t/Properties/References/Categories/dots-vertical.png";
                    }

                    return imagePath;
                }

                return "C:/t/t/Properties/References/Categories/dots-vertical.png";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public ChecksData(int userId)
        {
            try
            {
                InitializeComponent();
                Console.WriteLine("InitializeComponent викликано успішно");

                // Перевіряємо чи завантажились елементи
                if (btnDelete == null)
                {
                    MessageBox.Show("Помилка: кнопка btnDelete не знайдена в XAML. Будь ласка, перевірте XAML файл.",
                        "Помилка ініціалізації", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Створюємо кнопку програмно
                    CreateDeleteButtonProgrammatically();
                }

                currentUserId = userId;

                // Встановлюємо обробники подій
                if (dgChecks != null)
                {
                    dgChecks.MouseDoubleClick += DgChecks_MouseDoubleClick;
                    dgChecks.CellEditEnding += DgChecks_CellEditEnding;
                }

                // Ініціалізуємо кнопку delete
                if (btnDelete != null)
                {
                    btnDelete.Visibility = Visibility.Collapsed;
                }

                // Завантажуємо записи витрат за замовчуванням
                LoadExpensesChecks();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка ініціалізації вікна: {ex.Message}",
                    "Критична помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void CreateDeleteButtonProgrammatically()
        {
            // Створюємо кнопку програмно
            btnDelete = new Button
            {
                Name = "btnDelete",
                Content = "ВИДАЛИТИ ЗАПИС",
                Width = 150,
                Height = 40,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(255, 68, 68)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Visibility = Visibility.Collapsed
            };

            btnDelete.Click += BtnDelete_Click;

            // Знаходимо StackPanel для кнопок
            if (VisualTreeHelper.GetChildrenCount(this) > 0)
            {
                var mainGrid = VisualTreeHelper.GetChild(this, 0) as Grid;
                if (mainGrid != null && VisualTreeHelper.GetChildrenCount(mainGrid) > 3)
                {
                    var lastBorder = VisualTreeHelper.GetChild(mainGrid, 3) as Border;
                    if (lastBorder != null && lastBorder.Child is StackPanel panel)
                    {
                        panel.Children.Insert(0, btnDelete);
                    }
                }
            }
        }

        private void RbExpenses_Checked(object sender, RoutedEventArgs e)
        {
            isExpensesMode = true;
            if (txtInstruction != null)
                txtInstruction.Text = "Перегляньте та редагуйте ваші витрати";
            LoadExpensesChecks();
            ResetSelection();
        }

        private void RbIncome_Checked(object sender, RoutedEventArgs e)
        {
            isExpensesMode = false;
            if (txtInstruction != null)
                txtInstruction.Text = "Перегляньте та редагуйте ваші доходи";
            LoadIncomeChecks();
            ResetSelection();
        }

        private void ResetSelection()
        {
            try
            {
                selectedRecord = null;

                // Додайте перевірку на null
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

                    // Перевіряємо, чи є стовпець CommentExpenses
                    string commentColumn = "'' as CommentExpenses";
                    string checkColumnQuery = "SHOW COLUMNS FROM expenses LIKE 'CommentExpenses'";
                    MySqlCommand checkCmd = new MySqlCommand(checkColumnQuery, connection);
                    var result = checkCmd.ExecuteScalar();
                    if (result != null)
                    {
                        commentColumn = "COALESCE(CommentExpenses, '') as CommentExpenses";
                    }

                    string query = $@"
                    SELECT e.IDexpense, e.CategotyImageExpenses, e.CategoryNameExpenses, 
                           e.AmoutExpenses, e.ExpenseDate, {commentColumn}
                    FROM expenses e
                    WHERE e.IDuser = @userid
                    ORDER BY e.ExpenseDate DESC, e.IDexpense DESC";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CheckRecord record = new CheckRecord
                            {
                                Id = reader.GetInt32("IDexpense"),
                                CategoryImage = reader["CategotyImageExpenses"].ToString(),
                                CategoryName = reader["CategoryNameExpenses"].ToString(),
                                Amount = -Math.Abs(reader.GetDecimal("AmoutExpenses")), // Мінус для витрат
                                Date = reader.GetDateTime("ExpenseDate"),
                                Comment = reader["CommentExpenses"].ToString()
                            };

                            records.Add(record);
                        }
                    }
                }

                // Перевіряємо, чи є записи
                if (records.Count == 0)
                {
                    ShowNoRecordsMessage("Витрат не було");
                }
                else
                {
                    if (dgChecks != null)
                    {
                        dgChecks.ItemsSource = records;
                    }
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
                    ORDER BY i.IncomeDate DESC, i.IDincome DESC";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CheckRecord record = new CheckRecord
                            {
                                Id = reader.GetInt32("IDincome"),
                                CategoryImage = reader["CategoryImageIncome"].ToString(),
                                CategoryName = reader["CategoryNameIncome"].ToString(),
                                Amount = Math.Abs(reader.GetDecimal("AmountIncome")), // Плюс для доходів
                                Date = reader.GetDateTime("IncomeDate"),
                                Comment = reader["CommentIncome"].ToString()
                            };

                            records.Add(record);
                        }
                    }
                }

                // Перевіряємо, чи є записи
                if (records.Count == 0)
                {
                    ShowNoRecordsMessage("Доходів не було");
                }
                else
                {
                    if (dgChecks != null)
                    {
                        dgChecks.ItemsSource = records;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження доходів: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowNoRecordsMessage(string message)
        {
            if (dgChecks != null)
            {
                // Очищаємо DataGrid
                dgChecks.ItemsSource = null;
                dgChecks.Columns.Clear();

                // Створюємо одну колонку з повідомленням
                DataGridTextColumn messageColumn = new DataGridTextColumn();
                messageColumn.Header = "Повідомлення";
                messageColumn.Binding = new System.Windows.Data.Binding(".");

                // Додаємо стиль для центрування тексту
                Style cellStyle = new Style(typeof(TextBlock));
                cellStyle.Setters.Add(new Setter(TextBlock.TextProperty, message));
                cellStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 18.0));
                cellStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
                cellStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Gray));
                cellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                cellStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                cellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));

                messageColumn.CellStyle = cellStyle;

                dgChecks.Columns.Add(messageColumn);
                dgChecks.ItemsSource = new List<string> { message };
            }
        }

        private void DgChecks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (btnDelete == null)
                {
                    Console.WriteLine("Попередження: btnDelete є null в DgChecks_SelectionChanged");
                    return;
                }

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
            // Перевіряємо, чи DataGrid не null і чи є реальні записи
            if (dgChecks == null || dgChecks.ItemsSource == null || dgChecks.Items.Count == 0)
                return;

            // Знаходимо клітинку, по якій клікнули
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridCell cell)
            {
                // Визначаємо колонку
                int columnIndex = cell.Column.DisplayIndex;

                // Якщо це колонка коментаря (індекс 2) або суми (індекс 3)
                if (columnIndex == 2 || columnIndex == 3)
                {
                    dgChecks.BeginEdit();
                }
            }
        }

        // Новий метод для обробки завершення редагування комірки
        private void DgChecks_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row.Item is CheckRecord record)
                {
                    // Визначаємо, яку колонку редагували
                    int columnIndex = e.Column.DisplayIndex;

                    if (columnIndex == 2) // Колонка коментаря
                    {
                        if (e.EditingElement is TextBox textBox)
                        {
                            record.Comment = textBox.Text;
                            UpdateCommentInDatabase(record);
                        }
                    }
                    else if (columnIndex == 3) // Колонка суми
                    {
                        if (e.EditingElement is TextBox textBox)
                        {
                            if (decimal.TryParse(textBox.Text, out decimal newAmount))
                            {
                                // Для витрат - від'ємне значення, для доходів - додатнє
                                record.Amount = isExpensesMode ? -Math.Abs(newAmount) : Math.Abs(newAmount);
                                UpdateAmountInDatabase(record);
                            }
                            else
                            {
                                MessageBox.Show("Будь ласка, введіть коректну суму", "Помилка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                e.Cancel = true; // Скасувати редагування
                            }
                        }
                    }
                }
            }
        }

        private void CommentTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Цей метод більше не потрібен, оскільки використовуємо CellEditEnding
        }

        private void AmountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Цей метод більше не потрібен, оскільки використовуємо CellEditEnding
        }

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Дозволяємо тільки цифри та крапку/кому
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.' && c != ',')
                {
                    e.Handled = true;
                    return;
                }
            }

            // Перевіряємо, щоб була тільки одна крапка/кома
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

        private void UpdateCommentInDatabase(CheckRecord record)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName = isExpensesMode ? "expenses" : "income";
                    string idColumn = isExpensesMode ? "IDexpense" : "IDincome";
                    string commentColumn = isExpensesMode ? "CommentExpenses" : "CommentIncome";

                    // Перевіряємо, чи існує стовпець для коментаря у витратах
                    if (isExpensesMode)
                    {
                        string checkColumnQuery = "SHOW COLUMNS FROM expenses LIKE 'CommentExpenses'";
                        MySqlCommand checkCmd = new MySqlCommand(checkColumnQuery, connection);
                        var result = checkCmd.ExecuteScalar();
                        if (result == null)
                        {
                            // Створюємо стовпець, якщо його немає
                            string alterQuery = "ALTER TABLE expenses ADD COLUMN CommentExpenses VARCHAR(255) AFTER CategoryNameExpenses";
                            MySqlCommand alterCmd = new MySqlCommand(alterQuery, connection);
                            alterCmd.ExecuteNonQuery();
                        }
                    }

                    string query = $"UPDATE {tableName} SET {commentColumn} = @comment WHERE {idColumn} = @id AND IDuser = @userid";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@comment", record.Comment ?? "");
                    cmd.Parameters.AddWithValue("@id", record.Id);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Оновлюємо відображення
                        dgChecks.Items.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення коментаря: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateAmountInDatabase(CheckRecord record)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName = isExpensesMode ? "expenses" : "income";
                    string idColumn = isExpensesMode ? "IDexpense" : "IDincome";
                    string amountColumn = isExpensesMode ? "AmoutExpenses" : "AmountIncome";

                    // Для витрат зберігаємо додатне значення, для доходів - додатнє
                    decimal amountToSave = Math.Abs(record.Amount);

                    string query = $"UPDATE {tableName} SET {amountColumn} = @amount WHERE {idColumn} = @id AND IDuser = @userid";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@amount", amountToSave);
                    cmd.Parameters.AddWithValue("@id", record.Id);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Оновлюємо відображення
                        dgChecks.Items.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення суми: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

                    string tableName = isExpensesMode ? "expenses" : "income";
                    string idColumn = isExpensesMode ? "IDexpense" : "IDincome";

                    string query = $"DELETE FROM {tableName} WHERE {idColumn} = @id AND IDuser = @userid";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", selectedRecord.Id);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Запис успішно видалено", "Успіх",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Оновлюємо список записів
                        if (isExpensesMode)
                        {
                            LoadExpensesChecks();
                        }
                        else
                        {
                            LoadIncomeChecks();
                        }

                        // Скидаємо вибір
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Перевіряємо чи всі елементи завантажені
            if (dgChecks == null)
            {
                MessageBox.Show("Помилка: DataGrid не завантажено. Перевірте XAML файл.",
                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}