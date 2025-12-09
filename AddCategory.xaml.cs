using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public AddCategory(int userId, string type = "Expenses")
        {
            InitializeComponent();
            currentUserId = userId;
            categoryType = type;

            if (categoryType == "Income")
            {
                rbIncome.IsChecked = true;
                this.Title = "Додати категорію доходів";
            }
            else
            {
                rbExpenses.IsChecked = true;
                this.Title = "Додати категорію витрат";
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
                // Створюємо кнопку з іконкою
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

                // Створюємо StackPanel для іконки
                StackPanel panel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Додаємо зображення
                try
                {
                    Image iconImage = new Image
                    {
                        Width = 40,
                        Height = 40,
                        Source = new BitmapImage(new Uri($"C:/t/t/Properties/References/Categories/{iconName}")),
                        Stretch = Stretch.Uniform
                    };
                    panel.Children.Add(iconImage);
                }
                catch
                {
                    // Якщо зображення не знайдено, використовуємо текст
                    TextBlock textIcon = new TextBlock
                    {
                        Text = "...",
                        FontSize = 24,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    panel.Children.Add(textIcon);
                }

                // Додаємо назву файлу
                string displayName = System.IO.Path.GetFileNameWithoutExtension(iconName);
                TextBlock iconText = new TextBlock
                {
                    Text = displayName.Length > 8 ? displayName.Substring(0, 8) + "..." : displayName,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                panel.Children.Add(iconText);

                iconButton.Content = panel;
                iconButton.Click += IconButton_Click;

                iconGrid.Children.Add(iconButton);
            }
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
                    }
                }

                button.Background = new SolidColorBrush(Color.FromArgb(30, 74, 144, 226));
                button.BorderBrush = Brushes.DodgerBlue;
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
            selectedIconName.Text = System.IO.Path.GetFileNameWithoutExtension(selectedIcon);

            try
            {
                string packUri = $"C:/t/t/Properties/References/Categories/{selectedIcon}";
                selectedIconImage.Source = new BitmapImage(new Uri(packUri, UriKind.Absolute));
                return;
            }
            catch
            {
                // Ignore and try fallback
            }

            try
            {
                string filePath = System.IO.Path.Combine("C:/t/t/Properties/References/Categories", selectedIcon);
                if (System.IO.File.Exists(filePath))
                {
                    selectedIconImage.Source = new BitmapImage(new Uri(filePath, UriKind.Absolute));
                }
                else
                {
                    selectedIconImage.Source = null;
                }
            }
            catch
            {
                selectedIconImage.Source = null;
            }
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

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    lblCategoryCount.Text = $"Поточні категорії: {count}/12";

                    if (count >= 12)
                    {
                        btnSave.IsEnabled = false;
                        btnSave.ToolTip = "Досягнуто максимальну кількість категорій (12)";
                        ShowNotification("Досягнуто максимальну кількість категорій (12).", false);
                    }
                    else
                    {
                        btnSave.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Помилка завантаження кількості категорій: {ex.Message}", false);
            }
        }

        private void UpdateUI()
        {
            if (categoryType == "Income")
            {
                rbIncome.IsChecked = true;
                LoadCategoryCount();
            }
            else
            {
                rbExpenses.IsChecked = true;
                LoadCategoryCount();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                ShowNotification("❗ Введіть назву категорії!", false);
                txtCategoryName.Focus();
                return;
            }

            if (txtCategoryName.Text.Length < 2)
            {
                ShowNotification("❗ Назва категорії повинна містити щонайменше 2 символи!", false);
                txtCategoryName.Focus();
                return;
            }

            // Перевірка іконки
            if (string.IsNullOrEmpty(selectedIcon))
            {
                ShowNotification("❗ Оберіть іконку для категорії!", false);
                return;
            }

            // Перевірка кількості категорій
            if (!CheckCategoryLimit())
            {
                return;
            }

            // Збереження в базу даних
            if (SaveCategoryToDatabase())
            {
                ShowNotification("Категорію успішно додано!", true);

                // Повертаємося на форму Expenses
                Expenses expensesWindow = Application.Current.Windows.OfType<Expenses>().FirstOrDefault();
                if (expensesWindow != null)
                {
                    expensesWindow.UpdateCategoryButtons();
                }

                this.Close();
            }
        }

        private bool CheckCategoryLimit()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName = categoryType == "Income" ? "incomecategory" : "expensescategory";
                    string query = $"SELECT COUNT(*) FROM {tableName} WHERE IDuser = @userid";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count >= 12)
                    {
                        ShowNotification("Досягнуто максимальну кількість категорій (12).", false);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Помилка перевірки ліміту: {ex.Message}", false);
                return false;
            }
        }

        private bool SaveCategoryToDatabase()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName, columnImage, columnName;

                    if (categoryType == "Income")
                    {
                        tableName = "incomecategory";
                        columnImage = "ImageIncome";
                        columnName = "CNameIncome";
                    }
                    else
                    {
                        tableName = "expensescategory";
                        columnImage = "ImageExpenses";
                        columnName = "CNameExpenses";
                    }

                    // Перевірка, чи не існує вже категорія з такою назвою
                    string checkQuery = $"SELECT COUNT(*) FROM {tableName} WHERE {columnName} = @name AND IDuser = @userid";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
                    checkCmd.Parameters.AddWithValue("@name", txtCategoryName.Text.Trim());
                    checkCmd.Parameters.AddWithValue("@userid", currentUserId);

                    int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (existingCount > 0)
                    {
                        ShowNotification("Категорія з такою назвою вже існує!", false);
                        return false;
                    }

                    // Додавання нової категорії
                    string insertQuery = $"INSERT INTO {tableName} ({columnImage}, {columnName}, IDuser) VALUES (@image, @name, @userid)";

                    MySqlCommand cmd = new MySqlCommand(insertQuery, connection);
                    cmd.Parameters.AddWithValue("@image", selectedIcon);
                    cmd.Parameters.AddWithValue("@name", txtCategoryName.Text.Trim());
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    return rowsAffected > 0;
                }
            }
            catch (MySqlException mysqlEx)
            {
                ShowNotification($"Помилка бази даних: {mysqlEx.Message}", false);
                return false;
            }
            catch (Exception ex)
            {
                ShowNotification($"Помилка збереження: {ex.Message}", false);
                return false;
            }
        }

        private void ShowNotification(string message, bool isSuccess)
        {
            var brush = isSuccess ? Brushes.Green : Brushes.Red;

            // Створюємо сповіщення
            Border notification = new Border
            {
                Background = isSuccess ? Brushes.LightGreen : Brushes.LightCoral,
                BorderBrush = brush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 10, 0, 0)
            };

            TextBlock textBlock = new TextBlock { Text = message, FontSize = 14, Foreground = brush, FontWeight = FontWeights.SemiBold };

            notification.Child = textBlock;

            // Додаємо сповіщення до вікна
            if (this.Content is Grid mainGrid)
            {
                // Видаляємо попереднє сповіщення
                var oldNotification = mainGrid.Children.OfType<Border>()
                    .FirstOrDefault(b => b.Name == "notificationBorder");
                if (oldNotification != null)
                {
                    mainGrid.Children.Remove(oldNotification);
                }

                notification.Name = "notificationBorder";
                Grid.SetRowSpan(notification, 3);
                mainGrid.Children.Add(notification);

                // Автоматичне приховування через 3 секунди
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, args) =>
                {
                    mainGrid.Children.Remove(notification);
                    timer.Stop();
                };
                timer.Start();
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
        }

        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            DeleteCategory deleteWindow = new DeleteCategory(currentUserId);
            deleteWindow.ShowDialog();
            LoadCategoryCount();
        }

        private void CategoryType_Checked(object sender, RoutedEventArgs e)
        {
            if (!isWindowLoaded) return;

            if (rbExpenses.IsChecked == true)
            {
                categoryType = "Expenses";
            }
            else if (rbIncome.IsChecked == true)
            {
                categoryType = "Income";
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
                    ShowNotification("❗ Назва може містити тільки літери, цифри та пробіли!", false);
                    return;
                }
            }

            if (txtCategoryName.Text.Length + e.Text.Length > 13)
            {
                e.Handled = true;
                ShowNotification("❗ Максимальна довжина назви - 13 символів!", false);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnSave.IsEnabled)
            {
                btnSave_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}