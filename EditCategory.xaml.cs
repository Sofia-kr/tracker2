using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace t
{
    public partial class EditCategory : Window
    {
        private int currentUserId;
        private string currentCategoryType = "Expenses"; // Початковий тип - витрати
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";

        private Dictionary<int, CategoryInfo> categories = new Dictionary<int, CategoryInfo>();
        private Dictionary<string, string> iconDictionary = new Dictionary<string, string>();
        private int selectedCategoryId = 0;
        private string selectedCategoryName = "";
        private string selectedCategoryImage = "";
        private bool isEditing = false;

        private class CategoryInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Image { get; set; }
            public string Type { get; set; } // "Expenses" або "Income"
        }

        public EditCategory(int userId)
        {
            InitializeComponent();
            currentUserId = userId;

            // Встановлюємо початкове значення ComboBox
            cmbCategoryType.SelectedIndex = 0;

            InitializeIconDictionary();
            LoadCategories();
        }

        private void CmbCategoryType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCategoryType.SelectedItem is ComboBoxItem selectedItem)
            {
                currentCategoryType = selectedItem.Tag.ToString();
                LoadCategories();

                // Скидаємо вибір категорії
                ResetSelection();
            }
        }

        private void ResetSelection()
        {
            selectedCategoryId = 0;
            selectedCategoryName = "";
            selectedCategoryImage = "";
            isEditing = false;

            // Ховаємо панель редагування
            pnlEdit.Visibility = Visibility.Collapsed;
            btnSave.IsEnabled = false;

            // Скидаємо підсвічування всіх кнопок
            foreach (var child in CategoriesPanel.Children)
            {
                if (child is Border border)
                {
                    border.Background = Brushes.White;
                    border.BorderBrush = Brushes.LightGray;
                    border.BorderThickness = new Thickness(1);
                }
            }
        }

        private void InitializeIconDictionary()
        {
            iconDictionary["Продукти"] = "food.png";
            iconDictionary["Транспорт"] = "airplane.png";
            iconDictionary["Діти"] = "baby-carriage.png";
            iconDictionary["Покупки"] = "basket-outline.png";
            iconDictionary["Комунікації"] = "border-color.png";
            iconDictionary["Автосервіс"] = "car-cog.png";
            iconDictionary["Електротранспорт"] = "car-electric.png";
            iconDictionary["Вантажівка"] = "car-pickup.png";
            iconDictionary["Супермаркет"] = "cart-outline.png";
            iconDictionary["Ремонт авто"] = "car-wrench.png";
            iconDictionary["Готівка"] = "cash.png";
            iconDictionary["Фінанси"] = "cash-multiple.png";
            iconDictionary["Кафе"] = "coffee-outline.png";
            iconDictionary["Сервіси"] = "cogs.png";
            iconDictionary["Краса"] = "content-cut.png";
            iconDictionary["Розваги"] = "controller.png";
            iconDictionary["Солодощі"] = "cookie-outline.png";
            iconDictionary["Кредити"] = "credit-card-multiple-outline.png";
            iconDictionary["Костюми"] = "diamond-stone.png";
            iconDictionary["Родина"] = "family.png";
            iconDictionary["Квіти"] = "flower-tulip-outline.png";
            iconDictionary["Їжа"] = "food-apple.png";
            iconDictionary["Пальне"] = "gas-station.png";
            iconDictionary["Подарунки"] = "gift-outline.png";
            iconDictionary["Донати"] = "hand-coin-outline.png";
            iconDictionary["Одяг"] = "hanger.png";
            iconDictionary["Квартплата"] = "home.png";
            iconDictionary["Медицина"] = "medical-bag.png";
            iconDictionary["Товари"] = "sack.png";
            iconDictionary["Магазин"] = "store-outline.png";
            iconDictionary["Іграшки"] = "teddy-bear.png";
            iconDictionary["Кіно"] = "theater.png";
            iconDictionary["Гігієна"] = "toothbrush-paste.png";
            iconDictionary["Накопичення"] = "treasure-chest.png";
            iconDictionary["Валюта"] = "usd.png";
            iconDictionary["Одяг"] = "tshirt-crew.png";
            iconDictionary["Ліки"] = "pill-multiple.png";
        }

        private void LoadCategories()
        {
            try
            {
                categories.Clear();
                CategoriesPanel.Children.Clear();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName = currentCategoryType == "Expenses" ? "expensescategory" : "incomecategory";
                    string nameColumn = currentCategoryType == "Expenses" ? "CNameExpenses" : "CNameIncome";
                    string imageColumn = currentCategoryType == "Expenses" ? "ImageExpenses" : "ImageIncome";

                    string query = $"SELECT IDcategory, {nameColumn}, {imageColumn} FROM {tableName} WHERE IDuser = @userid ORDER BY {nameColumn}";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        int categoryId = reader.GetInt32("IDcategory");
                        string categoryName = reader[nameColumn].ToString();
                        string image = reader[imageColumn].ToString();

                        categories[categoryId] = new CategoryInfo
                        {
                            Id = categoryId,
                            Name = categoryName,
                            Image = image,
                            Type = currentCategoryType
                        };

                        CreateCategoryButton(categoryId, categoryName, image);
                    }
                    reader.Close();
                }

                if (categories.Count == 0)
                {
                    TextBlock noCategories = new TextBlock
                    {
                        Text = currentCategoryType == "Expenses" ?
                            "У вас немає категорій витрат для редагування" :
                            "У вас немає категорій доходів для редагування",
                        FontSize = 16,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    };

                    CategoriesPanel.Children.Add(noCategories);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження категорій: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateCategoryButton(int categoryId, string categoryName, string image)
        {
            Border categoryBorder = new Border
            {
                Width = 150,
                Height = 150,
                Margin = new Thickness(15),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Cursor = Cursors.Hand,
                Tag = categoryId
            };

            StackPanel contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Іконка
            try
            {
                Image iconImage = new Image
                {
                    Width = 60,
                    Height = 60,
                    Margin = new Thickness(0, 0, 0, 10),
                    Source = new BitmapImage(new Uri($"C:/t/t/Properties/References/Categories/{image}")),
                    Stretch = Stretch.Uniform
                };
                contentPanel.Children.Add(iconImage);
            }
            catch
            {
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

            // Назва категорії (з можливістю подвійного кліку)
            TextBlock nameText = new TextBlock
            {
                Text = categoryName,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 130,
                Tag = categoryId
            };

            // Подвійний клік для редагування
            nameText.MouseDown += NameText_MouseDown;
            contentPanel.Children.Add(nameText);

            // Обробники подій
            categoryBorder.MouseDown += CategoryBorder_MouseDown;
            categoryBorder.MouseEnter += CategoryBorder_MouseEnter;
            categoryBorder.MouseLeave += CategoryBorder_MouseLeave;

            categoryBorder.Child = contentPanel;
            CategoriesPanel.Children.Add(categoryBorder);
        }

        private void CategoryBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
            {
                if (sender is Border border && border.Tag != null && int.TryParse(border.Tag.ToString(), out int categoryId))
                {
                    selectedCategoryId = categoryId;
                    selectedCategoryName = categories[categoryId].Name;
                    selectedCategoryImage = categories[categoryId].Image;

                    // Підсвічуємо вибрану категорію
                    HighlightSelectedCategory(border);

                    // Показуємо панель редагування
                    ShowEditPanel();
                }
            }
        }

        private void NameText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                if (sender is TextBlock textBlock && textBlock.Tag != null && int.TryParse(textBlock.Tag.ToString(), out int categoryId))
                {
                    selectedCategoryId = categoryId;
                    selectedCategoryName = categories[categoryId].Name;
                    selectedCategoryImage = categories[categoryId].Image;

                    // Починаємо редагування
                    isEditing = true;

                    // Показуємо панель редагування
                    ShowEditPanel();

                    // Фокусуємося на текстовому полі
                    txtCategoryName.Focus();
                    txtCategoryName.SelectAll();
                }
            }
        }

        private void ShowEditPanel()
        {
            pnlEdit.Visibility = Visibility.Visible;
            txtCategoryName.Text = selectedCategoryName;
            txtCharCount.Text = $"{selectedCategoryName.Length}/13 символів";

            try
            {
                imgSelectedIcon.Source = new BitmapImage(new Uri($"C:/t/t/Properties/References/Categories/{selectedCategoryImage}"));
            }
            catch
            {
                imgSelectedIcon.Source = null;
            }

            btnSave.IsEnabled = true;
        }

        private void HighlightSelectedCategory(Border selectedBorder)
        {
            foreach (var child in CategoriesPanel.Children)
            {
                if (child is Border border)
                {
                    if (border.Tag != null && int.TryParse(border.Tag.ToString(), out int borderId))
                    {
                        if (borderId == selectedCategoryId)
                        {
                            border.Background = Brushes.LightSkyBlue;
                            border.BorderBrush = Brushes.DodgerBlue;
                            border.BorderThickness = new Thickness(2);
                        }
                        else
                        {
                            border.Background = Brushes.White;
                            border.BorderBrush = Brushes.LightGray;
                            border.BorderThickness = new Thickness(1);
                        }
                    }
                }
            }
        }

        private void CategoryBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.Tag != null && int.TryParse(border.Tag.ToString(), out int borderId))
                {
                    if (borderId != selectedCategoryId)
                    {
                        border.Background = Brushes.AliceBlue;
                        border.BorderBrush = Brushes.DodgerBlue;
                    }
                }
            }
        }

        private void CategoryBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.Tag != null && int.TryParse(border.Tag.ToString(), out int borderId))
                {
                    if (borderId != selectedCategoryId)
                    {
                        border.Background = Brushes.White;
                        border.BorderBrush = Brushes.LightGray;
                    }
                }
            }
        }

        private void TxtCategoryName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtCategoryName.Text.Length > 13)
            {
                txtCategoryName.Text = txtCategoryName.Text.Substring(0, 13);
                txtCategoryName.CaretIndex = 13;
            }

            txtCharCount.Text = $"{txtCategoryName.Text.Length}/13 символів";

            // Автоматично оновлюємо іконку при зміні назви
            if (iconDictionary.ContainsKey(txtCategoryName.Text))
            {
                selectedCategoryImage = iconDictionary[txtCategoryName.Text];
                try
                {
                    imgSelectedIcon.Source = new BitmapImage(new Uri($"C:/t/t/Properties/References/Categories/{selectedCategoryImage}"));
                }
                catch { }
            }
        }

        private void TxtCategoryName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                txtCategoryName.Text = selectedCategoryName;
                MessageBox.Show("Назва категорії не може бути порожньою", "Увага",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnChangeIcon_Click(object sender, RoutedEventArgs e)
        {
            // Створюємо діалог вибору іконки
            Window iconDialog = new Window
            {
                Title = "Вибір іконки",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            WrapPanel iconsPanel = new WrapPanel
            {
                Margin = new Thickness(10)
            };

            // Додаємо доступні іконки
            foreach (var icon in iconDictionary)
            {
                Border iconBorder = new Border
                {
                    Width = 70,
                    Height = 70,
                    Margin = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Cursor = Cursors.Hand,
                    Tag = icon.Value
                };

                StackPanel iconContent = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                try
                {
                    Image iconImage = new Image
                    {
                        Width = 40,
                        Height = 40,
                        Source = new BitmapImage(new Uri($"C:/t/t/Properties/References/Categories/{icon.Value}")),
                        Stretch = Stretch.Uniform
                    };
                    iconContent.Children.Add(iconImage);
                }
                catch { }

                TextBlock iconName = new TextBlock
                {
                    Text = icon.Key,
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 60
                };
                iconContent.Children.Add(iconName);

                iconBorder.MouseDown += (s, args) =>
                {
                    selectedCategoryImage = icon.Value;
                    imgSelectedIcon.Source = new BitmapImage(new Uri($"C:/t/t/Properties/References/Categories/{icon.Value}"));
                    iconDialog.Close();
                };

                iconBorder.Child = iconContent;
                iconsPanel.Children.Add(iconBorder);
            }

            ScrollViewer scrollViewer = new ScrollViewer
            {
                Content = iconsPanel
            };

            iconDialog.Content = scrollViewer;
            iconDialog.ShowDialog();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCategoryId == 0 || string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                MessageBox.Show("Будь ласка, виберіть категорію та введіть назву", "Увага",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtCategoryName.Text.Length > 13)
            {
                MessageBox.Show("Назва категорії не може бути довшою за 13 символів", "Увага",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UpdateCategoryInDatabase();
        }

        private void UpdateCategoryInDatabase()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName = currentCategoryType == "Expenses" ? "expensescategory" : "incomecategory";
                    string nameColumn = currentCategoryType == "Expenses" ? "CNameExpenses" : "CNameIncome";
                    string imageColumn = currentCategoryType == "Expenses" ? "ImageExpenses" : "ImageIncome";

                    // Перевіряємо, чи існує вже така назва
                    string checkQuery = $"SELECT COUNT(*) FROM {tableName} WHERE IDuser = @userid AND {nameColumn} = @name AND IDcategory != @categoryid";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
                    checkCmd.Parameters.AddWithValue("@userid", currentUserId);
                    checkCmd.Parameters.AddWithValue("@name", txtCategoryName.Text.Trim());
                    checkCmd.Parameters.AddWithValue("@categoryid", selectedCategoryId);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        MessageBox.Show("Категорія з такою назвою вже існує", "Увага",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Оновлюємо категорію
                    string updateQuery = $"UPDATE {tableName} SET {nameColumn} = @name, {imageColumn} = @image WHERE IDuser = @userid AND IDcategory = @categoryid";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection);
                    updateCmd.Parameters.AddWithValue("@userid", currentUserId);
                    updateCmd.Parameters.AddWithValue("@categoryid", selectedCategoryId);
                    updateCmd.Parameters.AddWithValue("@name", txtCategoryName.Text.Trim());
                    updateCmd.Parameters.AddWithValue("@image", selectedCategoryImage);

                    int rowsAffected = updateCmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Оновлюємо відповідні записи в таблицях expenses або income
                        string updateRecordsTable = currentCategoryType == "Expenses" ? "expenses" : "income";

                        if (currentCategoryType == "Expenses")
                        {
                            // Для витрат
                            string updateRecordsQuery = @"UPDATE expenses 
                                                         SET CategoryNameExpenses = @name, 
                                                             CategotyImageExpenses = @image 
                                                         WHERE IDuser = @userid AND IDcategory = @categoryid";

                            MySqlCommand updateRecordsCmd = new MySqlCommand(updateRecordsQuery, connection);
                            updateRecordsCmd.Parameters.AddWithValue("@userid", currentUserId);
                            updateRecordsCmd.Parameters.AddWithValue("@categoryid", selectedCategoryId);
                            updateRecordsCmd.Parameters.AddWithValue("@name", txtCategoryName.Text.Trim());
                            updateRecordsCmd.Parameters.AddWithValue("@image", selectedCategoryImage);
                            updateRecordsCmd.ExecuteNonQuery();
                        }
                        else
                        {
                            // Для доходів
                            string updateRecordsQuery = @"UPDATE income 
                                                         SET CategoryNameIncome = @name, 
                                                             CategoryImageIncome = @image 
                                                         WHERE IDuser = @userid AND IDcategory = @categoryid";

                            MySqlCommand updateRecordsCmd = new MySqlCommand(updateRecordsQuery, connection);
                            updateRecordsCmd.Parameters.AddWithValue("@userid", currentUserId);
                            updateRecordsCmd.Parameters.AddWithValue("@categoryid", selectedCategoryId);
                            updateRecordsCmd.Parameters.AddWithValue("@name", txtCategoryName.Text.Trim());
                            updateRecordsCmd.Parameters.AddWithValue("@image", selectedCategoryImage);
                            updateRecordsCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Категорію успішно оновлено", "Успіх",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Оновлюємо список категорій
                        LoadCategories();

                        // Скидаємо вибір
                        ResetSelection();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення категорії: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (isEditing)
            {
                MessageBoxResult result = MessageBox.Show("У вас є незбережені зміни. Вийти без збереження?", "Підтвердження",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            this.Close();
        }
    }
}