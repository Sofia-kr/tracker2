using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace t
{
    public partial class AddCategory : Window
    {
        private int currentUserId;
        private string categoryType;
        private string selectedIcon = "";
        private bool isWindowLoaded = false;
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";
        private List<string> availableIcons = new List<string>
        {
            "airplane.png", "baby-carriage.png", "basket-outline.png", "border-color.png",
            "car-cog.png", "car-electric.png", "car-pickup.png", "cart-outline.png",
            "car-wrench.png", "cash.png", "cash-multiple.png", "coffee-outline.png",
            "cogs.png", "content-cut.png", "controller.png", "cookie-outline.png",
            "credit-card-multiple-outline.png", "diamond-stone.png", "family.png",
            "flower-tulip-outline.png","food.png", "food-apple.png", "gas-station.png",
            "gift-outline.png","hand-coin-outline.png", "hanger.png", "home.png",
            "medical-bag.png","sack.png", "store-outline.png", "teddy-bear.png",
            "theater.png","toothbrush-paste.png","treasure-chest.png","usd.png",
            "tshirt-crew.png","pill-multiple.png"
        };

        // Шлях до папки з іконками
        private string iconsPath = @"C:\t\t\Properties\References\Categories\";

        public AddCategory(int userId, string type)
        {
            InitializeComponent();
            currentUserId = userId;
            categoryType = type;

            if (categoryType == "Income")
            {
                rbIncome.IsChecked = true;
                this.Title = "Додати категорію доходів";
                windowTitle.Text = "ДОДАТИ КАТЕГОРІЮ ДОХОДІВ";
            }
            else
            {
                rbExpenses.IsChecked = true;
                this.Title = "Додати категорію витрат";
                windowTitle.Text = "ДОДАТИ КАТЕГОРІЮ ВИТРАТ";
            }

            this.Loaded += AddCategory_Loaded;
        }

        private void AddCategory_Loaded(object sender, RoutedEventArgs e)
        {
            isWindowLoaded = true;
            LoadAvailableIcons();
            UpdateUI();
            this.Loaded -= AddCategory_Loaded;
        }

        private void LoadAvailableIcons()
        {
            iconGrid.Children.Clear();

            foreach (string iconName in availableIcons)
            {
                Button iconButton = new Button
                {
                    Width = 80,
                    Height = 80,
                    Margin = new Thickness(5),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.LightGray,
                    Tag = iconName
                };

                StackPanel panel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Завантаження зображення
                string iconPath = Path.Combine(iconsPath, iconName);
                Image iconImage = new Image
                {
                    Width = 40,
                    Height = 40,
                    Stretch = Stretch.Uniform
                };

                try
                {
                    if (File.Exists(iconPath))
                    {
                        iconImage.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                    }
                    else
                    {
                        // Спробувати збільшені шляхи
                        string altPath = $"C:/t/t/Properties/References/Categories/{iconName}";
                        if (File.Exists(altPath))
                        {
                            iconImage.Source = new BitmapImage(new Uri(altPath, UriKind.Absolute));
                        }
                        else
                        {
                            CreateIconPlaceholder(panel, iconName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка завантаження іконки {iconName}: {ex.Message}");
                    CreateIconPlaceholder(panel, iconName);
                }

                if (iconImage.Source != null)
                {
                    panel.Children.Add(iconImage);
                }

                // Додаємо назву файлу
                string displayName = Path.GetFileNameWithoutExtension(iconName);
                TextBlock iconText = new TextBlock
                {
                    Text = displayName.Length > 8 ? displayName.Substring(0, 8) + "..." : displayName,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                panel.Children.Add(iconText);

                iconButton.Content = panel;
                iconButton.Click += IconButton_Click;

                iconGrid.Children.Add(iconButton);
            }
        }

        private void CreateIconPlaceholder(StackPanel panel, string iconName)
        {
            Border placeholder = new Border
            {
                Width = 40,
                Height = 40,
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(20),
                Child = new TextBlock
                {
                    Text = iconName.Length > 0 ? iconName[0].ToString().ToUpper() : "?",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                }
            };
            panel.Children.Add(placeholder);
        }

        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                selectedIcon = button.Tag.ToString();
                UpdateSelectedIconDisplay();

                // Підсвічуємо обрану кнопку
                foreach (var child in iconGrid.Children)
                {
                    if (child is Button btn)
                    {
                        btn.Background = Brushes.Transparent;
                        btn.BorderBrush = Brushes.LightGray;
                        btn.BorderThickness = new Thickness(1);
                    }
                }

                button.Background = new SolidColorBrush(Color.FromArgb(30, 74, 144, 226));
                button.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 74, 144, 226));
                button.BorderThickness = new Thickness(2);
            }
        }

        private void UpdateSelectedIconDisplay()
        {
            if (string.IsNullOrEmpty(selectedIcon))
            {
                selectedIconBorder.Visibility = Visibility.Collapsed;
                selectedIconName.Text = string.Empty;
                selectedIconImage.Source = null;
                return;
            }

            selectedIconBorder.Visibility = Visibility.Visible;
            selectedIconName.Text = Path.GetFileNameWithoutExtension(selectedIcon);

            try
            {
                string iconPath = Path.Combine(iconsPath, selectedIcon);
                if (File.Exists(iconPath))
                {
                    selectedIconImage.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                }
                else
                {
                    // Спробувати альтернативний шлях
                    string altPath = $"C:/t/t/Properties/References/Categories/{selectedIcon}";
                    if (File.Exists(altPath))
                    {
                        selectedIconImage.Source = new BitmapImage(new Uri(altPath, UriKind.Absolute));
                    }
                    else
                    {
                        CreateSelectedIconPlaceholder();
                    }
                }
            }
            catch
            {
                CreateSelectedIconPlaceholder();
            }
        }

        private void CreateSelectedIconPlaceholder()
        {
            Border placeholder = new Border
            {
                Width = 50,
                Height = 50,
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(25),
                Child = new TextBlock
                {
                    Text = selectedIcon.Length > 0 ? selectedIcon[0].ToString().ToUpper() : "?",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                }
            };

            // Створюємо новий контейнер для placeholder
            Grid container = new Grid();
            container.Children.Add(placeholder);
            selectedIconImage.Source = null;
        }

        private void LoadCategoryCount()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName = categoryType == "Income" ? "incomecategory" : "expensescategory";
                    string query = $"SELECT COUNT(*) FROM {tableName} WHERE IDuser = @userid";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userid", currentUserId);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        lblCategoryCount.Text = $"Поточні категорії: {count}/12";

                        if (count >= 12)
                        {
                            btnSave.IsEnabled = false;
                            btnSave.ToolTip = "Досягнуто максимальну кількість категорій (12)";
                        }
                        else
                        {
                            btnSave.IsEnabled = true;
                            btnSave.ToolTip = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження кількості категорій: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            LoadCategoryCount();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                MessageBox.Show("❗ Введіть назву категорії!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCategoryName.Focus();
                return;
            }

            if (txtCategoryName.Text.Length < 2)
            {
                MessageBox.Show("❗ Назва категорії повинна містити щонайменше 2 символи!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCategoryName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(selectedIcon))
            {
                MessageBox.Show("❗ Оберіть іконку для категорії!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SaveCategoryToDatabase())
            {
                MessageBox.Show("Категорію успішно додано!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

                // Повертаємося на форму Expenses
                Expenses expensesWindow = new Expenses(currentUserId, DateTime.Now,
                    categoryType == "Expenses" ? ViewType.Expenses : ViewType.Income);
                expensesWindow.ShowDialog();
                this.Close();
            }
        }

        private bool SaveCategoryToDatabase()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName, columnImage, columnName, idColumn;

                    if (categoryType == "Income")
                    {
                        tableName = "incomecategory";
                        columnImage = "ImageIncome";
                        columnName = "CNameIncome";
                        idColumn = "IDcategory";
                    }
                    else
                    {
                        tableName = "expensescategory";
                        columnImage = "ImageExpenses";
                        columnName = "CNameExpenses";
                        idColumn = "IDcategory";
                    }

                    // Перевірка на унікальність назви
                    string checkQuery = $"SELECT COUNT(*) FROM {tableName} WHERE {columnName} = @name AND IDuser = @userid";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@name", txtCategoryName.Text.Trim());
                        checkCmd.Parameters.AddWithValue("@userid", currentUserId);

                        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                        {
                            MessageBox.Show("Категорія з такою назвою вже існує!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                    }

                    // Додавання нової категорії
                    if (categoryType == "Income")
                    {
                        // Для incomecategory потрібно отримати наступний ID
                        string maxIdQuery = $"SELECT MAX({idColumn}) FROM {tableName} WHERE IDuser = @userid";
                        int nextId = 1;
                        using (MySqlCommand maxIdCmd = new MySqlCommand(maxIdQuery, connection))
                        {
                            maxIdCmd.Parameters.AddWithValue("@userid", currentUserId);
                            object result = maxIdCmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                nextId = Convert.ToInt32(result) + 1;
                            }
                        }

                        // Перевіряємо, чи існує вже такий ID в усій таблиці
                        string checkIdQuery = $"SELECT COUNT(*) FROM {tableName} WHERE {idColumn} = @id";
                        using (MySqlCommand checkIdCmd = new MySqlCommand(checkIdQuery, connection))
                        {
                            checkIdCmd.Parameters.AddWithValue("@id", nextId);
                            int existingIdCount = Convert.ToInt32(checkIdCmd.ExecuteScalar());

                            // Якщо ID вже існує, шукаємо вільний ID
                            while (existingIdCount > 0)
                            {
                                nextId++;
                                checkIdCmd.Parameters["@id"].Value = nextId;
                                existingIdCount = Convert.ToInt32(checkIdCmd.ExecuteScalar());
                            }
                        }

                        string insertQuery = $"INSERT INTO {tableName} (IDcategory, IDuser, {columnImage}, {columnName}) VALUES (@id, @userid, @image, @name)";
                        using (MySqlCommand cmd = new MySqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@id", nextId);
                            cmd.Parameters.AddWithValue("@userid", currentUserId);
                            cmd.Parameters.AddWithValue("@image", selectedIcon);
                            cmd.Parameters.AddWithValue("@name", txtCategoryName.Text.Trim());
                            return cmd.ExecuteNonQuery() > 0;
                        }
                    }
                    else
                    {
                        // Для expensescategory IDcategory є AUTO_INCREMENT
                        string insertQuery = $"INSERT INTO {tableName} (IDuser, {columnImage}, {columnName}) VALUES (@userid, @image, @name)";
                        using (MySqlCommand cmd = new MySqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@userid", currentUserId);
                            cmd.Parameters.AddWithValue("@image", selectedIcon);
                            cmd.Parameters.AddWithValue("@name", txtCategoryName.Text.Trim());
                            return cmd.ExecuteNonQuery() > 0;
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                MessageBox.Show($"Помилка бази даних: {mysqlEx.Message}\nКод помилки: {mysqlEx.Number}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnEditCategory_Click(object sender, RoutedEventArgs e)
        {
            EditCategory editWindow = new EditCategory(currentUserId);
            editWindow.ShowDialog();
            LoadCategoryCount();
            this.Close();
        }

        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            DeleteCategory deleteWindow = new DeleteCategory(currentUserId);
            deleteWindow.Owner = this;
            deleteWindow.ShowDialog();
            LoadCategoryCount();
        }

        private void CategoryType_Checked(object sender, RoutedEventArgs e)
        {
            if (!isWindowLoaded) return;

            if (rbExpenses.IsChecked == true)
            {
                categoryType = "Expenses";
                windowTitle.Text = "ДОДАТИ КАТЕГОРІЮ ВИТРАТ";
            }
            else if (rbIncome.IsChecked == true)
            {
                categoryType = "Income";
                windowTitle.Text = "ДОДАТИ КАТЕГОРІЮ ДОХОДІВ";
            }

            UpdateUI();
        }

        private void txtCategoryName_TextChanged(object sender, TextChangedEventArgs e)
        {
            int remaining = 13 - txtCategoryName.Text.Length;
            lblCharCount.Text = $"Залишилось символів: {remaining}";

            if (remaining < 0)
            {
                lblCharCount.Foreground = Brushes.Red;
            }
            else if (remaining < 5)
            {
                lblCharCount.Foreground = Brushes.Orange;
            }
            else
            {
                lblCharCount.Foreground = Brushes.Gray;
            }
        }

        private void txtCategoryName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && c != '-')
                {
                    e.Handled = true;
                    MessageBox.Show("Назва може містити тільки літери, цифри та пробіли!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (txtCategoryName.Text.Length + e.Text.Length > 13)
            {
                e.Handled = true;
                MessageBox.Show("Максимальна довжина назви - 13 символів!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

       
    }
}