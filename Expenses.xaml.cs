using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace t
{
    public partial class Expenses : Window
    {
        private int currentUserId;
        private DateTime currentDate;
        private ViewType viewType;

        private string connectionString =
            "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";

        private int selectedCategoryId = 0;
        private string selectedCategoryName = "";
        private string selectedCategoryImage = "";
        private Button selectedCategoryButton = null;

        // Шлях до папки з іконками
        private string iconsPath = @"C:\t\t\Properties\References\Categories\";

        public Expenses(int userId, DateTime date, ViewType type)
        {
            InitializeComponent();
            currentUserId = userId;
            currentDate = date;
            viewType = type;

            DatePicker.SelectedDate = date;

            // Встановлюємо вибраний тип залежно від viewType - ВИПРАВЛЕНО
            switch (viewType)
            {
                case ViewType.Expenses:
                    ChoiceType.SelectedIndex = 0;
                    break;
                case ViewType.Income:
                    ChoiceType.SelectedIndex = 1;
                    break;
                case ViewType.Savings:
                    ChoiceType.SelectedIndex = 2;
                    break;
            }

            UpdateInterface();
            LoadBalance();
            
        }

        private void AmountInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Дозволяємо тільки цифри, кому та крапку
            Regex regex = new Regex(@"^[0-9]+([.,][0-9]{0,2})?$");
            TextBox tb = sender as TextBox;
            string futureText = tb.Text.Insert(tb.SelectionStart, e.Text);
            e.Handled = !regex.IsMatch(futureText);
        }

        private void ChoiceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInterface();
        }

        private void UpdateInterface()
        {
            string mode = (ChoiceType.SelectedItem as ComboBoxItem).Content.ToString();
            CategoriesPanel.Children.Clear();
            selectedCategoryId = 0;
            selectedCategoryName = "";
            selectedCategoryImage = "";
            selectedCategoryButton = null;

            // Оновлюємо заголовок
            if (mode == "Витрати")
            {
                BalanceTitle.Text = "Загальний баланс:";
                LoadBalance();
                CategoryTitle.Text = "Категорії витрат";
                CategoryCount.Text = "";
                LoadCategories("expensescategory", "CNameExpenses", "ImageExpenses");
                CommentPanel.Visibility = Visibility.Visible;
                CommentInput.Text = "";
                AddCategoryButton.Visibility = Visibility.Visible;
            }
            else if (mode == "Доходи")
            {
                BalanceTitle.Text = "Загальний баланс:";
                LoadBalance();
                CategoryTitle.Text = "Категорії доходів";
                CategoryCount.Text = "";
                LoadCategories("incomecategory", "CNameIncome", "ImageIncome");
                CommentPanel.Visibility = Visibility.Visible;
                CommentInput.Text = "";
                AddCategoryButton.Visibility = Visibility.Visible;
            }
            else // Заощадження - ВИПРАВЛЕНО
            {
                BalanceTitle.Text = "Заощаджено всього:";
                LoadSavingsBalance();
                CategoryTitle.Text = "Заощадження";
                CategoryCount.Text = "(категорії не потрібні)";
                CommentPanel.Visibility = Visibility.Collapsed;
                selectedCategoryName = "Заощадження";
                selectedCategoryId = 0;
                AddCategoryButton.Visibility = Visibility.Collapsed;

                // Очищаємо панель категорій
                CategoriesPanel.Children.Clear();

            }
        }

        private void LoadCategories(string table, string nameCol, string imgCol)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connectionString))
                {
                    con.Open();

                    string query = $"SELECT IDcategory, {nameCol}, {imgCol} FROM {table} WHERE IDuser = @uid";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@uid", currentUserId);

                    int categoryCount = 0;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categoryCount++;
                            int id = reader.GetInt32("IDcategory");
                            string name = reader[nameCol].ToString();
                            string img = reader[imgCol].ToString();

                            CreateCategoryButton(id, name, img);
                        }
                    }

                    // Оновлюємо кількість категорій
                    CategoryCount.Text = $"({categoryCount} категорій)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження категорій: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateCategoryButton(int id, string name, string img)
        {
            Button button = new Button
            {
                Width = 110,
                Height = 120,
                Margin = new Thickness(5),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Tag = new Tuple<int, string, string>(id, name, img),
                Style = (Style)FindResource(typeof(Button))
            };

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Додаємо іконку
            try
            {
                string iconPath = Path.Combine(iconsPath, img);
                if (File.Exists(iconPath))
                {
                    Image iconImage = new Image
                    {
                        Width = 50,
                        Height = 50,
                        Source = new BitmapImage(new Uri(iconPath)),
                        Stretch = Stretch.Uniform
                    };
                    panel.Children.Add(iconImage);
                }
                else
                {
                    // Спробувати альтернативний шлях
                    string altPath = $"C:/t/t/Properties/References/Categories/{img}";
                    if (File.Exists(altPath))
                    {
                        Image iconImage = new Image
                        {
                            Width = 50,
                            Height = 50,
                            Source = new BitmapImage(new Uri(altPath)),
                            Stretch = Stretch.Uniform
                        };
                        panel.Children.Add(iconImage);
                    }
                    else
                    {
                        // Створити placeholder
                        Border placeholder = new Border
                        {
                            Width = 50,
                            Height = 50,
                            Background = Brushes.LightGray,
                            CornerRadius = new CornerRadius(25),
                            Child = new TextBlock
                            {
                                Text = name.Length > 0 ? name[0].ToString().ToUpper() : "?",
                                FontSize = 20,
                                FontWeight = FontWeights.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Foreground = Brushes.White
                            }
                        };
                        panel.Children.Add(placeholder);
                    }
                }
            }
            catch
            {
                // Створити placeholder при помилці
                Border placeholder = new Border
                {
                    Width = 50,
                    Height = 50,
                    Background = Brushes.LightGray,
                    CornerRadius = new CornerRadius(25),
                    Child = new TextBlock
                    {
                        Text = name.Length > 0 ? name[0].ToString().ToUpper() : "?",
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.White
                    }
                };
                panel.Children.Add(placeholder);
            }

            // Додаємо назву категорії
            TextBlock nameText = new TextBlock
            {
                Text = name,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 14,
                Margin = new Thickness(0, 5, 0, 0)
            };
            panel.Children.Add(nameText);

            button.Content = panel;
            button.Click += Category_Click;
            CategoriesPanel.Children.Add(button);
        }

        private void Category_Click(object sender, RoutedEventArgs e)
        {
            // Скидаємо попередню вибрану кнопку
            if (selectedCategoryButton != null)
            {
                selectedCategoryButton.Background = Brushes.White;
                selectedCategoryButton.BorderBrush = Brushes.LightGray;
                selectedCategoryButton.Foreground = Brushes.Black;
            }

            // Виділяємо нову вибрану кнопку
            Button clickedButton = sender as Button;
            clickedButton.Background = new SolidColorBrush(Color.FromArgb(255, 74, 144, 226));
            clickedButton.Foreground = Brushes.White;
            clickedButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 74, 144, 226));

            var data = clickedButton.Tag as Tuple<int, string, string>;
            selectedCategoryId = data.Item1;
            selectedCategoryName = data.Item2;
            selectedCategoryImage = data.Item3;
            selectedCategoryButton = clickedButton;

        }

        private void AmountInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AmountInput.Text.Contains(","))
            {
                int pos = AmountInput.SelectionStart;
                AmountInput.Text = AmountInput.Text.Replace(",", ".");
                AmountInput.SelectionStart = Math.Min(pos, AmountInput.Text.Length);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Перевірка суми
            if (!decimal.TryParse(AmountInput.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Будь ласка, введіть коректну суму більше нуля", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountInput.Focus();
                return;
            }

            // Перевірка дати
            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show("Будь ласка, оберіть дату", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DatePicker.Focus();
                return;
            }

            string mode = (ChoiceType.SelectedItem as ComboBoxItem).Content.ToString();

            // Перевірка категорії для витрат та доходів
            if ((mode == "Витрати" || mode == "Доходи") && selectedCategoryId == 0)
            {
                MessageBox.Show("Будь ласка, оберіть категорію", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (mode == "Витрати") SaveExpense(amount);
            else if (mode == "Доходи") SaveIncome(amount);
            else SaveSavings(amount);
        }

        private void SaveExpense(decimal amount)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connectionString))
                {
                    con.Open();

                    string query = @"INSERT INTO expenses 
                    (IDuser, IDcategory, CategoryImageExpenses, CategoryNameExpenses, AmoutExpenses, ExpenseDate,CommentExpenses)
                    VALUES (@u, @c, @i, @n, @a, @d, @e)";

                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", currentUserId);
                    cmd.Parameters.AddWithValue("@c", selectedCategoryId);
                    cmd.Parameters.AddWithValue("@i", selectedCategoryImage);
                    cmd.Parameters.AddWithValue("@n", selectedCategoryName);
                    cmd.Parameters.AddWithValue("@a", amount);
                    cmd.Parameters.AddWithValue("@d", DatePicker.SelectedDate);
                    cmd.Parameters.AddWithValue("@e", CommentInput.Text);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Витрату успішно записано!", "Успіх",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    MainWindow mainWindow = new MainWindow(currentUserId);
                    mainWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження витрати: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveIncome(decimal amount)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connectionString))
                {
                    con.Open();

                    string query = @"INSERT INTO income 
                    (IDuser, IDcategory, CategoryImageIncome, CategoryNameIncome, AmountIncome, IncomeDate, CommentIncome)
                    VALUES (@u, @c, @i, @n, @a, @d, @com)";

                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", currentUserId);
                    cmd.Parameters.AddWithValue("@c", selectedCategoryId);
                    cmd.Parameters.AddWithValue("@i", selectedCategoryImage);
                    cmd.Parameters.AddWithValue("@n", selectedCategoryName);
                    cmd.Parameters.AddWithValue("@a", amount);
                    cmd.Parameters.AddWithValue("@d", DatePicker.SelectedDate);
                    cmd.Parameters.AddWithValue("@com", CommentInput.Text);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Дохід успішно записано!", "Успіх",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    MainWindow mainWindow = new MainWindow(currentUserId);
                    mainWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження доходу: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSavings(decimal amount)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connectionString))
                {
                    con.Open();
                    string insertQuery = @"INSERT INTO saving (IDuser, AmoutSaving, SavingDate)
                                  VALUES (@u, @a, @d)";
                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, con);
                    insertCmd.Parameters.AddWithValue("@u", currentUserId);
                    insertCmd.Parameters.AddWithValue("@a", amount);
                    insertCmd.Parameters.AddWithValue("@d", DatePicker.SelectedDate.Value);
                    insertCmd.ExecuteNonQuery();

                    MessageBox.Show("Заощадження успішно додано!", "Успіх",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    MainWindow mainWindow = new MainWindow(currentUserId);
                    mainWindow.Show();
                    this.Close();
                }
            }
            catch (MySqlException mysqlEx)
            {
                MessageBox.Show($"Помилка бази даних при збереженні заощаджень: {mysqlEx.Message}\nКод помилки: {mysqlEx.Number}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження заощаджень: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBalance()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connectionString))
                {
                    con.Open();

                    // Баланс = доходи - витрати (без заощаджень)
                    string query = @"
                SELECT 
                    (SELECT COALESCE(SUM(AmountIncome), 0) FROM income WHERE IDuser = @UserId) -
                    (SELECT COALESCE(SUM(AmoutExpenses), 0) FROM expenses WHERE IDuser = @UserId) AS Balance";

                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@UserId", currentUserId);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        decimal balance = Convert.ToDecimal(result);
                        GeneralBalance.Text = balance.ToString("F2");

                        // Зміна кольору в залежності від балансу
                        if (balance < 0)
                            GeneralBalance.Foreground = Brushes.Red;
                        else if (balance > 0)
                            GeneralBalance.Foreground = Brushes.Green;
                        else
                            GeneralBalance.Foreground = Brushes.Gray;
                    }
                    else
                    {
                        GeneralBalance.Text = "0.00";
                        GeneralBalance.Foreground = Brushes.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження балансу: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                GeneralBalance.Text = "0.00";
            }
        }

        private void LoadSavingsBalance()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connectionString))
                {
                    con.Open();

                    string query = "SELECT COALESCE(SUM(AmoutSaving), 0) FROM saving WHERE IDuser = @UserId";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@UserId", currentUserId);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        decimal savings = Convert.ToDecimal(result);
                        GeneralBalance.Text = savings.ToString("F2");
                        GeneralBalance.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 100, 200)); 
                    }
                    else
                    {
                        GeneralBalance.Text = "0.00";
                        GeneralBalance.Foreground = Brushes.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження заощаджень: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                GeneralBalance.Text = "0.00";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(currentUserId);
            mainWindow.Show();
            this.Close();
        }

        private void ReturnExpenses_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(currentUserId);
            mainWindow.Show();
            this.Close();
        }


        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            string mode = (ChoiceType.SelectedItem as ComboBoxItem).Content.ToString();

            if (mode == "Витрати")
            {
                AddCategory addCategoryWindow = new AddCategory(currentUserId, "Expenses");
                addCategoryWindow.ShowDialog();
                this.Close();
            }
            else if (mode == "Доходи")
            {
                AddCategory addCategoryWindow = new AddCategory(currentUserId, "Income");
           
                addCategoryWindow.ShowDialog();
                this.Close();
            }
        }

       
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AmountInput.Focus();
        }

        // Новий метод для оновлення категорій після додавання
        public void LoadCategories()
        {
            UpdateInterface();
        }
    }
}