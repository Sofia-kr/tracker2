using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";
        // Словники для зберігання категорій
        private Dictionary<string, CategoryInfo> expenseCategories = new Dictionary<string, CategoryInfo>();
        private Dictionary<string, CategoryInfo> incomeCategories = new Dictionary<string, CategoryInfo>();

        // Поточні змінні
        private string currentOperationType = "Витрати";
        private string selectedCategoryName = "";
        private int selectedCategoryId = 0;
        private string selectedCategoryImage = "";

        // Клас для зберігання інформації про категорію
        private class CategoryInfo
        {
            public int Id { get; set; }
            public string Image { get; set; }
            public string Name { get; set; }
        }

        public Expenses(int userId, DateTime cDate)
        {
            InitializeComponent();
            currentUserId = userId;
            currentDate = cDate;
            // Ініціалізація
            InitializeWindow(currentDate);
        }

        private void InitializeWindow(DateTime currentDateTime)
        {
            // Встановлюємо поточну дату
            DatePicker.SelectedDate = currentDateTime;

            // Завантажуємо баланс
            LoadBalance();

            // Встановлюємо початковий тип операції
            ChoiceType.SelectedIndex = 0;

            // Оновлюємо інтерфейс
            UpdateInterface();
        }

        private void UpdateInterface()
        {

            // Оновлюємо категорії в залежності від типу
            if (currentOperationType == "Витрати")
            {
                LoadExpenseCategories();
                CategoryTitle.Text = "Категорії витрат";
                InfoText3.Text = "• Максимум 12 категорій витрат";
                CategoryPanel.Visibility = Visibility.Visible;
            }
            else if (currentOperationType == "Доходи")
            {
                LoadIncomeCategories();
                CategoryTitle.Text = "Категорії доходів";
                InfoText3.Text = "• Максимум 12 категорій доходів";
                CategoryPanel.Visibility = Visibility.Visible;
            }
            else if (currentOperationType == "Заощадження")
            {
                CategoryTitle.Text = "Заощадження";
                InfoText1.Text = "• Для заощаджень категорії не потрібні";
                InfoText2.Text = "• Сума відкладається на заощадження";
                InfoText3.Text = "• Кошти можна витратити на цілі";
                lblCategoryCount.Text = "";
                CategoriesPanel.Children.Clear();

                // Показуємо інформацію про заощадження
                TextBlock savingsInfo = new TextBlock
                {
                    Text = "Заощадження не потребують категорій.\nВведіть суму та натисніть 'Записати'.",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                CategoriesPanel.Children.Add(savingsInfo);
            }

            UpdateCategoryButtons();
            ClearCategorySelection();
            ClearForm();
        }

        private void LoadBalance()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Отримуємо загальний баланс (доходи - витрати)
                    decimal totalIncome = 0;
                    decimal totalExpenses = 0;


                    // Доходи
                    string incomeQuery = "SELECT SUM(AmountIncome) FROM income WHERE IDuser = @userid";
                    MySqlCommand incomeCmd = new MySqlCommand(incomeQuery, connection);
                    incomeCmd.Parameters.AddWithValue("@userid", currentUserId);
                    object incomeResult = incomeCmd.ExecuteScalar();
                    if (incomeResult != null && incomeResult != DBNull.Value)
                    {
                        totalIncome = Convert.ToDecimal(incomeResult);
                    }

                    // Витрати
                    string expenseQuery = "SELECT SUM(AmoutExpenses) FROM expenses WHERE IDuser = @userid";
                    MySqlCommand expenseCmd = new MySqlCommand(expenseQuery, connection);
                    expenseCmd.Parameters.AddWithValue("@userid", currentUserId);
                    object expenseResult = expenseCmd.ExecuteScalar();
                    if (expenseResult != null && expenseResult != DBNull.Value)
                    {
                        totalExpenses = Convert.ToDecimal(expenseResult);
                    }

                 
                    decimal balance = totalIncome - totalExpenses;
                    GeneralBalance.Text = $"{balance:N2} ₴";

                    // Змінюємо колір в залежності від значення
                    if (balance < 0)
                    {
                        GeneralBalance.Foreground = Brushes.Red;
                    }
                    else if (balance > 0)
                    {
                        GeneralBalance.Foreground = Brushes.Green;
                    }
                    else
                    {
                        GeneralBalance.Foreground = Brushes.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження балансу: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                GeneralBalance.Text = "0.00 ₴";
            }
        }

        private void LoadExpenseCategories()
        {
            try
            {
                expenseCategories.Clear();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT IDcategory, CNameExpenses, ImageExpenses FROM expensescategory WHERE IDuser = @userid ORDER BY CNameExpenses";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        int categoryId = reader.GetInt32("IDcategory");
                        string categoryName = reader["CNameExpenses"].ToString();
                        string image = reader["ImageExpenses"].ToString();

                        expenseCategories[categoryName] = new CategoryInfo
                        {
                            Id = categoryId,
                            Name = categoryName,
                            Image = image
                        };
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження категорій витрат: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadIncomeCategories()
        {
            try
            {
                incomeCategories.Clear();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT IDcategory, CNameIncome, ImageIncome FROM incomecategory WHERE IDuser = @userid ORDER BY CNameIncome";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        int categoryId = reader.GetInt32("IDcategory");
                        string categoryName = reader["CNameIncome"].ToString();
                        string image = reader["ImageIncome"].ToString();

                        incomeCategories[categoryName] = new CategoryInfo
                        {
                            Id = categoryId,
                            Name = categoryName,
                            Image = image
                        };
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження категорій доходів: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateCategoryButtons()
        {
            // Очищаємо контейнер для кнопок
            if (CategoriesPanel == null) return;

            CategoriesPanel.Children.Clear();

            // Якщо заощадження - не показуємо категорії
            if (currentOperationType == "Заощадження")
            {
                return;
            }

            // Отримуємо поточні категорії в залежності від типу операції
            Dictionary<string, CategoryInfo> currentCategories =
                currentOperationType == "Доходи" ? incomeCategories : expenseCategories;

            // Оновлюємо кількість категорій
            lblCategoryCount.Text = $"{currentCategories.Count}/12 категорій";

            // Перевіряємо, чи є категорії
            if (currentCategories.Count == 0)
            {
                TextBlock noCategoriesText = new TextBlock
                {
                    Text = "Немає категорій. Натисніть '+' щоб створити нову.",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(20)
                };

                Border noCategoriesBorder = new Border
                {
                    Background = Brushes.LightYellow,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20),
                    Child = noCategoriesText
                };

                CategoriesPanel.Children.Add(noCategoriesBorder);
            }
            else
            {
                // Створюємо кнопки для кожної категорії
                foreach (var category in currentCategories)
                {
                    CategoryInfo info = category.Value;

                    // Створюємо контейнер для кнопки
                    Border categoryBorder = new Border
                    {
                        Width = 150,
                        Height = 150,
                        Margin = new Thickness(10),
                        Background = Brushes.White,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10),
                        Cursor = Cursors.Hand,
                        Tag = info // Зберігаємо інформацію про категорію в Tag
                    };

                    // Створюємо StackPanel для вмісту кнопки
                    StackPanel contentPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    // Додаємо іконку
                    try
                    {
                        Image iconImage = new Image
                        {
                            Width = 60,
                            Height = 60,
                            Margin = new Thickness(0, 0, 0, 10),
                            Source = new BitmapImage(new Uri($"C:/t/t/Properties/References/Categories/{info.Image}")),
                            Stretch = Stretch.Uniform
                        };
                        contentPanel.Children.Add(iconImage);
                    }
                    catch
                    {
                        // Якщо іконка не знайдена, використовуємо заміщувач
                        Border placeholder = new Border
                        {
                            Width = 60,
                            Height = 60,
                            Margin = new Thickness(0, 0, 0, 10),
                            Background = Brushes.LightGray,
                            CornerRadius = new CornerRadius(30),
                            Child = new TextBlock
                            {
                                Text = "📁",
                                FontSize = 24,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        };
                        contentPanel.Children.Add(placeholder);
                    }

                    // Додаємо назву категорії
                    TextBlock categoryNameText = new TextBlock
                    {
                        Text = info.Name,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 130
                    };
                    contentPanel.Children.Add(categoryNameText);

                    // Додаємо обробник подій
                    categoryBorder.MouseDown += CategoryButton_MouseDown;
                    categoryBorder.MouseEnter += CategoryBorder_MouseEnter;
                    categoryBorder.MouseLeave += CategoryBorder_MouseLeave;

                    categoryBorder.Child = contentPanel;
                    CategoriesPanel.Children.Add(categoryBorder);
                }
            }

            // Додаємо кнопку для додавання нової категорії (тільки для доходів і витрат)
            if (currentOperationType != "Заощадження")
            {
                Border addCategoryBorder = new Border
                {
                    Width = 150,
                    Height = 150,
                    Margin = new Thickness(10),
                    Background = Brushes.LightBlue,
                    BorderBrush = Brushes.DodgerBlue,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Cursor = Cursors.Hand
                };

                StackPanel addContentPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock plusText = new TextBlock
                {
                    Text = "+",
                    FontSize = 48,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DodgerBlue,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                addContentPanel.Children.Add(plusText);

                TextBlock addText = new TextBlock
                {
                    Text = "Додати категорію",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 130,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                addContentPanel.Children.Add(addText);

                addCategoryBorder.Child = addContentPanel;
                addCategoryBorder.MouseDown += AddCategoryButton_Click;
                addCategoryBorder.MouseEnter += AddCategoryBorder_MouseEnter;
                addCategoryBorder.MouseLeave += AddCategoryBorder_MouseLeave;

                CategoriesPanel.Children.Add(addCategoryBorder);
            }
        }

        private void CategoryButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is CategoryInfo categoryInfo)
            {
                selectedCategoryName = categoryInfo.Name;
                selectedCategoryId = categoryInfo.Id;
                selectedCategoryImage = categoryInfo.Image;

                // Підсвічуємо вибрану категорію
                HighlightSelectedCategory(border);

                // Показуємо повідомлення про вибір категорії
                MessageBox.Show($"Вибрано категорію: {selectedCategoryName}", "Категорія вибрана",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Відкриваємо вікно додавання категорії
            AddCategory addCategoryWindow = new AddCategory(currentUserId, currentOperationType);
            addCategoryWindow.ShowDialog();

            // Оновлюємо категорії після додавання
            if (currentOperationType == "Витрати")
            {
                LoadExpenseCategories();
            }
            else if (currentOperationType == "Доходи")
            {
                LoadIncomeCategories();
            }

            UpdateCategoryButtons();
            ClearCategorySelection();
        }

        private void HighlightSelectedCategory(Border selectedBorder)
        {
            // Скидаємо підсвічування всіх кнопок категорій
            foreach (var child in CategoriesPanel.Children)
            {
                if (child is Border border)
                {
                    if (border.Tag is CategoryInfo)
                    {
                        border.Background = Brushes.White;
                        border.BorderBrush = Brushes.LightGray;
                        border.BorderThickness = new Thickness(1);
                    }
                }
            }

            // Підсвічуємо вибрану кнопку
            selectedBorder.Background = Brushes.LightSkyBlue;
            selectedBorder.BorderBrush = Brushes.DodgerBlue;
            selectedBorder.BorderThickness = new Thickness(2);
        }

        private void ClearCategorySelection()
        {
            selectedCategoryName = "";
            selectedCategoryId = 0;
            selectedCategoryImage = "";

            // Скидаємо підсвічування всіх кнопок
            foreach (var child in CategoriesPanel.Children)
            {
                if (child is Border border)
                {
                    if (border.Tag is CategoryInfo)
                    {
                        border.Background = Brushes.White;
                        border.BorderBrush = Brushes.LightGray;
                        border.BorderThickness = new Thickness(1);
                    }
                }
            }
        }

        private void CategoryBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.AliceBlue;
                border.BorderBrush = Brushes.DodgerBlue;
            }
        }

        private void CategoryBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                // Перевіряємо, чи це вибрана категорія
                if (border.Tag is CategoryInfo categoryInfo &&
                    categoryInfo.Id == selectedCategoryId)
                {
                    border.Background = Brushes.LightSkyBlue;
                    border.BorderBrush = Brushes.DodgerBlue;
                }
                else
                {
                    border.Background = Brushes.White;
                    border.BorderBrush = Brushes.LightGray;
                }
            }
        }

        private void AddCategoryBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.LightCyan;
            }
        }

        private void AddCategoryBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.LightBlue;
            }
        }

        private void ChoiceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChoiceType.SelectedItem is ComboBoxItem selectedItem)
            {
                currentOperationType = selectedItem.Content.ToString();

                // Оновлюємо інтерфейс
                UpdateInterface();
            }
        }

        private void ClearForm()
        {
            AmountInput.Text = "";
            CommentInput.Text = "";
            //DatePicker.SelectedDate = DateTime.Now;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Перевірки
            if (!ValidateInput())
                return;

            // Визначаємо тип операції та зберігаємо
            if (currentOperationType == "Витрати")
            {
                SaveExpense();
            }
            else if (currentOperationType == "Доходи")
            {
                SaveIncome();
            }
            else if (currentOperationType == "Заощадження")
            {
                SaveSavings();
            }
        }

        private bool ValidateInput()
        {
            // Перевірка категорії (тільки для витрат і доходів)
            if (currentOperationType != "Заощадження")
            {
                if (string.IsNullOrEmpty(selectedCategoryName) || selectedCategoryId == 0)
                {
                    MessageBox.Show("Будь ласка, виберіть категорію!", "Увага",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // Перевірка суми
            if (string.IsNullOrWhiteSpace(AmountInput.Text))
            {
                MessageBox.Show("Будь ласка, введіть суму!", "Увага",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountInput.Focus();
                return false;
            }

            if (!decimal.TryParse(AmountInput.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Будь ласка, введіть коректну суму (додатне число)!", "Увага",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountInput.Focus();
                return false;
            }

            //// Перевірка дати
            //if (!DatePicker.SelectedDate.HasValue)
            //{
            //    MessageBox.Show("Будь ласка, виберіть дату!", "Увага",
            //        MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return false;
            //}

            //if (DatePicker.SelectedDate.Value > DateTime.Now)
            //{
            //    MessageBox.Show("Дата не може бути у майбутньому!", "Увага",
            //        MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return false;
            //}

            return true;
        }

        private void SaveExpense()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"INSERT INTO expenses 
                           (IDuser, IDcategory, CategotyImageExpenses, CategoryNameExpenses, AmoutExpenses, ExpenseDate) 
                           VALUES (@userid, @categoryid, @image, @categoryname, @amount, @date)";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);
                    cmd.Parameters.AddWithValue("@categoryid", selectedCategoryId);
                    cmd.Parameters.AddWithValue("@image", selectedCategoryImage);
                    cmd.Parameters.AddWithValue("@categoryname", selectedCategoryName);
                    cmd.Parameters.AddWithValue("@amount", decimal.Parse(AmountInput.Text.Replace(',', '.')));
                    cmd.Parameters.AddWithValue("@date", DatePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Витрати успішно збережено!", "Успіх",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Оновлюємо баланс
                        LoadBalance();

                        // Повертаємося на головне вікно
                        ReturnToMainWindow();
                    }
                    else
                    {
                        MessageBox.Show("Помилка при збереженні витрат!", "Помилка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                MessageBox.Show($"Помилка бази даних: {mysqlEx.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveIncome()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"INSERT INTO income 
                           (IDuser, IDcategory, CategoryImageIncome, CategoryNameIncome, AmountIncome, IncomeDate) 
                           VALUES (@userid, @categoryid, @image, @categoryname, @amount, @date)";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);
                    cmd.Parameters.AddWithValue("@categoryid", selectedCategoryId);
                    cmd.Parameters.AddWithValue("@image", selectedCategoryImage);
                    cmd.Parameters.AddWithValue("@categoryname", selectedCategoryName);
                    cmd.Parameters.AddWithValue("@amount", decimal.Parse(AmountInput.Text.Replace(',', '.')));
                    cmd.Parameters.AddWithValue("@date", DatePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Доходи успішно збережено!", "Успіх",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Оновлюємо баланс
                        LoadBalance();

                        // Повертаємося на головне вікно
                        ReturnToMainWindow();
                    }
                    else
                    {
                        MessageBox.Show("Помилка при збереженні доходів!", "Помилка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                MessageBox.Show($"Помилка бази даних: {mysqlEx.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSavings()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"INSERT INTO saving 
                           (IDuser, AmoutSaving, SavingDate) 
                           VALUES (@userid, @amount, @date)";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);
                    cmd.Parameters.AddWithValue("@amount", decimal.Parse(AmountInput.Text.Replace(',', '.')));
                    cmd.Parameters.AddWithValue("@date", DatePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Заощадження успішно збережено!", "Успіх",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Оновлюємо баланс
                        LoadBalance();

                        // Повертаємося на головне вікно
                        ReturnToMainWindow();
                    }
                    else
                    {
                        MessageBox.Show("Помилка при збереженні заощаджень!", "Помилка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                MessageBox.Show($"Помилка бази даних: {mysqlEx.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReturnToMainWindow()
        {
            MainWindow mainWindow = new MainWindow(currentUserId);
            mainWindow.Show();
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ReturnToMainWindow();
        }

        private void ReturnExpenses_Click(object sender, RoutedEventArgs e)
        {
            ReturnToMainWindow();
        }

        private void AmountInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Дозволяємо тільки цифри та кому/крапку
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.' && c != ',')
                {
                    e.Handled = true;
                    return;
                }
            }

            // Перевіряємо, щоб була тільки одна кома/крапка
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

        private void AmountInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Замінюємо кому на крапку
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Text.Contains(","))
            {
                int cursorPosition = textBox.SelectionStart;
                textBox.Text = textBox.Text.Replace(',', '.');
                textBox.SelectionStart = cursorPosition;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}