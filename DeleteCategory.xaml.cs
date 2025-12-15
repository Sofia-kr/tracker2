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
    public partial class DeleteCategory : Window
    {
        private int currentUserId;
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";

        private Dictionary<int, CategoryInfo> categories = new Dictionary<int, CategoryInfo>();
        private int selectedCategoryId = 0;
        private string selectedCategoryName = "";

        private class CategoryInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Image { get; set; }
            public string Type { get; set; }
        }

        public DeleteCategory(int userId)
        {
            InitializeComponent();
            currentUserId = userId;
            cmbCategoryType.SelectedIndex = 0;

            LoadCategories();
        }

        private string GetSelectedCategoryType()
        {
            if (cmbCategoryType.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag.ToString();
            }
            return "Expenses"; // Значення за замовчуванням
        }

        private void CmbCategoryType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadCategories();
            ResetSelection();
        }

        private void ResetSelection()
        {
            selectedCategoryId = 0;
            selectedCategoryName = "";
            txtSelectedCategory.Text = "";
            btnDelete.IsEnabled = false;

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
                            "У вас немає категорій витрат для видалення" :
                            "У вас немає категорій доходів для видалення",
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
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Categories", image);
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
                    // Спробуємо інший шлях
                    iconPath = $"C:/t/t/Properties/References/Categories/{image}";
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
                        // Створюємо placeholder
                        CreateIconPlaceholder(contentPanel, categoryName);
                    }
                }
            }
            catch
            {
                // Створюємо placeholder
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
                MaxWidth = 130
            };
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
            if (sender is Border border && border.Tag != null && int.TryParse(border.Tag.ToString(), out int categoryId))
            {
                selectedCategoryId = categoryId;
                selectedCategoryName = categories[categoryId].Name;

                // Підсвічуємо вибрану категорію
                HighlightSelectedCategory(border);

                // Активуємо кнопку видалення
                btnDelete.IsEnabled = true;
                txtSelectedCategory.Text = $"Вибрано: {selectedCategoryName}";
            }
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

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCategoryId == 0)
            {
                MessageBox.Show("Будь ласка, виберіть категорію для видалення", "Увага",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string message = $"Ви впевнені, що хочете видалити категорію \"{selectedCategoryName}\"?\n\n" +
                           "Увага: Всі записи з цією категорією також будуть видалені!";

            MessageBoxResult result = MessageBox.Show(message, "Підтвердження видалення",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DeleteCategoryFromDatabase();
            }
        }

        private void DeleteCategoryFromDatabase()
        {
            try
            {
                string currentCategoryType = GetSelectedCategoryType();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Визначаємо назви таблиць в залежності від типу категорії
                    string recordsTable = currentCategoryType == "Expenses" ? "expenses" : "income";
                    string categoryTable = currentCategoryType == "Expenses" ? "expensescategory" : "incomecategory";

                    // Видаляємо всі записи з цією категорією
                    string deleteRecordsQuery = $"DELETE FROM {recordsTable} WHERE IDuser = @userid AND IDcategory = @categoryid";
                    MySqlCommand deleteRecordsCmd = new MySqlCommand(deleteRecordsQuery, connection);
                    deleteRecordsCmd.Parameters.AddWithValue("@userid", currentUserId);
                    deleteRecordsCmd.Parameters.AddWithValue("@categoryid", selectedCategoryId);
                    int recordsDeleted = deleteRecordsCmd.ExecuteNonQuery();

                    // Видаляємо саму категорію
                    string deleteCategoryQuery = $"DELETE FROM {categoryTable} WHERE IDuser = @userid AND IDcategory = @categoryid";
                    MySqlCommand deleteCategoryCmd = new MySqlCommand(deleteCategoryQuery, connection);
                    deleteCategoryCmd.Parameters.AddWithValue("@userid", currentUserId);
                    deleteCategoryCmd.Parameters.AddWithValue("@categoryid", selectedCategoryId);

                    int rowsAffected = deleteCategoryCmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Категорію \"{selectedCategoryName}\" успішно видалено!\n" +
                                      $"Також видалено {recordsDeleted} записів з цією категорією.", "Успіх",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Оновлюємо список категорій
                        LoadCategories();

                        // Скидаємо вибір
                        ResetSelection();
                    }
                    else
                    {
                        MessageBox.Show("Не вдалося видалити категорію", "Помилка",
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
                MessageBox.Show($"Помилка видалення категорії: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}