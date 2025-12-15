using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace t
{
    public partial class EditCategory : Window
    {
        private int currentUserId;
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";

        private Dictionary<int, CategoryInfo> categories = new Dictionary<int, CategoryInfo>();
        private int selectedCategoryId = 0;
        private string selectedCategoryName = "";
        private string selectedCategoryImage = "";
        private bool isEditing = false;
        private bool hasUnsavedChanges = false;

        private class CategoryInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Image { get; set; }
            public string Type { get; set; }
        }

        // Шлях до папки з іконками
        private string iconsPath = @"C:\t\t\Properties\References\Categories\";

        public EditCategory(int userId)
        {
            InitializeComponent();
            currentUserId = userId;

            // Завантажуємо категорії для типу, вибраного в ComboBox
            LoadCategories();

            // Додаємо обробники подій
            txtCategoryName.TextChanged += TxtCategoryName_TextChangedForEditing;
            txtCategoryName.TextChanged += TxtCategoryName_TextChanged;
            txtCategoryName.LostFocus += TxtCategoryName_LostFocus;
            this.Closing += EditCategory_Closing;
        }

        private void EditCategory_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (hasUnsavedChanges)
            {
                MessageBoxResult result = MessageBox.Show("У вас є незбережені зміни. Вийти без збереження?", "Підтвердження",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void CmbCategoryType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadCategories();
            ResetSelection();
        }

        private string GetSelectedCategoryType()
        {
            if (cmbCategoryType.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag.ToString();
            }
            return "Expenses"; // Значення за замовчуванням
        }

        private void ResetSelection()
        {
            selectedCategoryId = 0;
            selectedCategoryName = "";
            selectedCategoryImage = "";
            isEditing = false;
            hasUnsavedChanges = false;

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

        private void LoadCategories()
        {
            try
            {
                string currentCategoryType = GetSelectedCategoryType();
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
                        Text = GetSelectedCategoryType() == "Expenses" ?
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
                string iconPath = Path.Combine(iconsPath, image);
                if (File.Exists(iconPath))
                {
                    Image iconImage = new Image
                    {
                        Width = 60,
                        Height = 60,
                        Margin = new Thickness(0, 0, 0, 10),
                        Source = new BitmapImage(new Uri(iconPath)),
                        Stretch = Stretch.Uniform
                    };
                    contentPanel.Children.Add(iconImage);
                }
                else
                {
                    // Спробувати альтернативний шлях
                    string altPath = $"C:/t/t/Properties/References/Categories/{image}";
                    if (File.Exists(altPath))
                    {
                        Image iconImage = new Image
                        {
                            Width = 60,
                            Height = 60,
                            Margin = new Thickness(0, 0, 0, 10),
                            Source = new BitmapImage(new Uri(altPath)),
                            Stretch = Stretch.Uniform
                        };
                        contentPanel.Children.Add(iconImage);
                    }
                    else
                    {
                        CreateIconPlaceholder(contentPanel, categoryName);
                    }
                }
            }
            catch
            {
                CreateIconPlaceholder(contentPanel, categoryName);
            }

            // Назва категорії
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

        private void CreateIconPlaceholder(StackPanel panel, string categoryName)
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
                    Text = categoryName.Length > 0 ? categoryName[0].ToString() : "?",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                }
            };
            panel.Children.Add(placeholder);
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

            // Завантажуємо іконку
            LoadSelectedIcon();

            btnSave.IsEnabled = true;
            hasUnsavedChanges = false;
        }

        private void LoadSelectedIcon()
        {
            try
            {
                string iconPath = Path.Combine(iconsPath, selectedCategoryImage);
                if (File.Exists(iconPath))
                {
                    imgSelectedIcon.Source = new BitmapImage(new Uri(iconPath));
                }
                else
                {
                    // Спробувати альтернативний шлях
                    string altPath = $"C:/t/t/Properties/References/Categories/{selectedCategoryImage}";
                    if (File.Exists(altPath))
                    {
                        imgSelectedIcon.Source = new BitmapImage(new Uri(altPath));
                    }
                    else
                    {
                        // Створюємо placeholder
                        SetIconPlaceholder();
                    }
                }
            }
            catch
            {
                SetIconPlaceholder();
            }
        }

        private void SetIconPlaceholder()
        {
            // Створюємо placeholder з використанням DrawingVisual
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, 50, 50));

                FormattedText formattedText = new FormattedText(
                    selectedCategoryName.Length > 0 ? selectedCategoryName[0].ToString().ToUpper() : "?",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    24,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                double textWidth = formattedText.Width;
                double textHeight = formattedText.Height;
                drawingContext.DrawText(formattedText, new Point(25 - textWidth / 2, 25 - textHeight / 2));
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(50, 50, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawingVisual);
            imgSelectedIcon.Source = rtb;
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
        }

        private void TxtCategoryName_TextChangedForEditing(object sender, TextChangedEventArgs e)
        {
            hasUnsavedChanges = true;
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
            SelectIconDialog iconDialog = new SelectIconDialog();
            iconDialog.Owner = this;

            if (iconDialog.ShowDialog() == true)
            {
                selectedCategoryImage = iconDialog.SelectedIcon;
                LoadSelectedIcon();
                hasUnsavedChanges = true;
            }
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
                string currentCategoryType = GetSelectedCategoryType();

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
                        if (currentCategoryType == "Expenses")
                        {
                            // Для витрат
                            string updateRecordsQuery = @"UPDATE expenses 
                                                         SET CategoryNameExpenses = @name, 
                                                             CategoryImageExpenses = @image 
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
                        hasUnsavedChanges = false;
                    }
                    else
                    {
                        MessageBox.Show("Не вдалося оновити категорію", "Помилка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                MessageBox.Show($"Помилка бази даних: {mysqlEx.Message}\nКод помилки: {mysqlEx.Number}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення категорії: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        
    }

    // Допоміжний клас для діалогу вибору іконки
    public class SelectIconDialog : Window
    {
        public string SelectedIcon { get; private set; }
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

        public SelectIconDialog()
        {
            Title = "Вибір іконки";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            Grid mainGrid = new Grid();

            ScrollViewer scrollViewer = new ScrollViewer();
            WrapPanel iconsPanel = new WrapPanel
            {
                Margin = new Thickness(10)
            };

            foreach (string icon in availableIcons)
            {
                Button iconButton = new Button
                {
                    Width = 80,
                    Height = 80,
                    Margin = new Thickness(5),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.LightGray,
                    Tag = icon
                };

                StackPanel contentPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                try
                {
                    string iconPath = Path.Combine(iconsPath, icon);
                    if (File.Exists(iconPath))
                    {
                        Image iconImage = new Image
                        {
                            Width = 40,
                            Height = 40,
                            Source = new BitmapImage(new Uri(iconPath)),
                            Stretch = Stretch.Uniform
                        };
                        contentPanel.Children.Add(iconImage);
                    }
                    else
                    {
                        // Спробувати альтернативний шлях
                        string altPath = $"C:/t/t/Properties/References/Categories/{icon}";
                        if (File.Exists(altPath))
                        {
                            Image iconImage = new Image
                            {
                                Width = 40,
                                Height = 40,
                                Source = new BitmapImage(new Uri(altPath)),
                                Stretch = Stretch.Uniform
                            };
                            contentPanel.Children.Add(iconImage);
                        }
                        else
                        {
                            // Створити placeholder
                            Border placeholder = new Border
                            {
                                Width = 40,
                                Height = 40,
                                Background = Brushes.LightGray,
                                CornerRadius = new CornerRadius(20),
                                Child = new TextBlock
                                {
                                    Text = Path.GetFileNameWithoutExtension(icon).Length > 0
                                        ? Path.GetFileNameWithoutExtension(icon)[0].ToString().ToUpper()
                                        : "?",
                                    FontSize = 20,
                                    FontWeight = FontWeights.Bold,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Foreground = Brushes.White
                                }
                            };
                            contentPanel.Children.Add(placeholder);
                        }
                    }
                }
                catch
                {
                    // Створити placeholder при помилці
                    Border placeholder = new Border
                    {
                        Width = 40,
                        Height = 40,
                        Background = Brushes.LightGray,
                        CornerRadius = new CornerRadius(20),
                        Child = new TextBlock
                        {
                            Text = Path.GetFileNameWithoutExtension(icon).Length > 0
                                ? Path.GetFileNameWithoutExtension(icon)[0].ToString().ToUpper()
                                : "?",
                            FontSize = 20,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = Brushes.White
                        }
                    };
                    contentPanel.Children.Add(placeholder);
                }

                TextBlock iconName = new TextBlock
                {
                    Text = Path.GetFileNameWithoutExtension(icon),
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 70
                };
                contentPanel.Children.Add(iconName);

                iconButton.Content = contentPanel;
                iconButton.Click += (s, e) =>
                {
                    SelectedIcon = icon;
                    DialogResult = true;
                    Close();
                };

                iconsPanel.Children.Add(iconButton);
            }

            scrollViewer.Content = iconsPanel;
            mainGrid.Children.Add(scrollViewer);

            Content = mainGrid;
        }
    }
}