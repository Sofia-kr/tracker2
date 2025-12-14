using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace t
{
    public partial class UserSettings : Window
    {
        private int currentUserId;
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";

        private string originalName = "";
        private string originalEmail = "";
        private string originalQuestion = "";
        private string originalAnswer = "";
        private string originalPassword = "";

        private bool isDataModified = false;
        private bool isPasswordModified = false;
        private bool isQuestionModified = false;

        public UserSettings(int userId)
        {
            InitializeComponent();
            currentUserId = userId;
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT Name, Gmail, Password, SpecialQuestion, SpecialAnswer FROM userdata WHERE IDuser = @userid";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            originalName = reader["Name"].ToString();
                            originalEmail = reader["Gmail"].ToString();
                            originalPassword = reader["Password"].ToString();
                            originalQuestion = reader["SpecialQuestion"].ToString();
                            originalAnswer = reader["SpecialAnswer"].ToString();

                            txtName.Text = originalName;
                            txtEmail.Text = originalEmail;
                            txtQuestion.Text = originalQuestion;
                            txtAnswer.Text = originalAnswer;

                            UpdateCharacterCounts();
                        }
                        else
                        {
                            MessageBox.Show("Користувача не знайдено!", "Помилка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            this.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження даних: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCharacterCounts()
        {
            txtNameCount.Text = $"{txtName.Text.Length}/20 символів";
            txtQuestionCount.Text = $"{txtQuestion.Text.Length}/100 символів";
            txtAnswerCount.Text = $"{txtAnswer.Text.Length}/100 символів";

            if (txtEmail.Text == originalEmail)
            {
                txtEmailStatus.Text = "";
            }
            else
            {
                txtEmailStatus.Text = "Нова пошта";
            }
        }

        private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtName.Text.Length > 20)
            {
                txtName.Text = txtName.Text.Substring(0, 20);
                txtName.CaretIndex = 20;
            }
            UpdateCharacterCounts();
            isDataModified = true;
        }

        private void TxtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            isDataModified = true;
            UpdateCharacterCounts();
        }

        private void TxtQuestion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtQuestion.Text.Length > 100)
            {
                txtQuestion.Text = txtQuestion.Text.Substring(0, 100);
                txtQuestion.CaretIndex = 100;
            }
            UpdateCharacterCounts();
            isQuestionModified = true;
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            // Вмикаємо поля для зміни пароля
            txtCurrentPassword.IsEnabled = true;
            txtNewPassword.IsEnabled = true;
            txtConfirmPassword.IsEnabled = true;

            MessageBox.Show("Введіть поточний пароль, новий пароль та підтвердження.", "Інформація",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            string pattern = @"^[a-zA-Z0-9._%+-]+@gmail\.com$";
            return Regex.IsMatch(email, pattern);
        }

        private bool CheckEmailExists(string email)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT COUNT(*) FROM userdata WHERE Gmail = @email AND IDuser != @userid";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@userid", currentUserId);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            bool hasChanges = false;
            bool updateSuccess = true;

            // Перевірка та оновлення основних даних
            if (isDataModified)
            {
                string newName = txtName.Text.Trim();
                string newEmail = txtEmail.Text.Trim();

                if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newEmail))
                {
                    MessageBox.Show("Ім'я та електронна пошта не можуть бути порожніми!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (newName.Length > 20)
                {
                    MessageBox.Show("Ім'я не може перевищувати 20 символів!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ValidateEmail(newEmail))
                {
                    MessageBox.Show("Неправильний формат електронної пошти!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CheckEmailExists(newEmail))
                {
                    MessageBox.Show("Ця електронна пошта вже використовується іншим користувачем!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "UPDATE userdata SET Name = @name, Gmail = @email WHERE IDuser = @userid";
                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@name", newName);
                        cmd.Parameters.AddWithValue("@email", newEmail);
                        cmd.Parameters.AddWithValue("@userid", currentUserId);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            hasChanges = true;
                        }
                        else
                        {
                            updateSuccess = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка оновлення даних: {ex.Message}", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Перевірка та оновлення пароля
            if (!string.IsNullOrEmpty(txtCurrentPassword.Password) ||
                !string.IsNullOrEmpty(txtNewPassword.Password) ||
                !string.IsNullOrEmpty(txtConfirmPassword.Password))
            {
                string currentPassword = txtCurrentPassword.Password;
                string newPassword = txtNewPassword.Password;
                string confirmPassword = txtConfirmPassword.Password;

                if (string.IsNullOrEmpty(currentPassword))
                {
                    MessageBox.Show("Введіть поточний пароль для зміни!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (currentPassword != originalPassword)
                {
                    MessageBox.Show("Поточний пароль невірний!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (newPassword.Length < 5 || newPassword.Length > 8)
                {
                    MessageBox.Show("Новий пароль повинен містити від 5 до 8 символів!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("Паролі не співпадають!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "UPDATE userdata SET Password = @password WHERE IDuser = @userid";
                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@password", newPassword);
                        cmd.Parameters.AddWithValue("@userid", currentUserId);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            hasChanges = true;
                            originalPassword = newPassword; // Оновлюємо кешований пароль
                        }
                        else
                        {
                            updateSuccess = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка оновлення пароля: {ex.Message}", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Перевірка та оновлення питання безпеки
            if (isQuestionModified)
            {
                string newQuestion = txtQuestion.Text.Trim();
                string newAnswer = txtAnswer.Text.Trim();

                if (string.IsNullOrEmpty(newQuestion) || string.IsNullOrEmpty(newAnswer))
                {
                    MessageBox.Show("Питання та відповідь не можуть бути порожніми!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (newQuestion.Length > 100 || newAnswer.Length > 100)
                {
                    MessageBox.Show("Питання та відповідь не можуть перевищувати 100 символів!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Перевіряємо пароль для зміни питання
                if (string.IsNullOrEmpty(txtCurrentPassword.Password))
                {
                    MessageBox.Show("Для зміни питання необхідно ввести поточний пароль!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (txtCurrentPassword.Password != originalPassword)
                {
                    MessageBox.Show("Поточний пароль невірний!", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "UPDATE userdata SET SpecialQuestion = @question, SpecialAnswer = @answer WHERE IDuser = @userid";
                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@question", newQuestion);
                        cmd.Parameters.AddWithValue("@answer", newAnswer);
                        cmd.Parameters.AddWithValue("@userid", currentUserId);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            hasChanges = true;
                            originalQuestion = newQuestion;
                            originalAnswer = newAnswer;
                        }
                        else
                        {
                            updateSuccess = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка оновлення питання: {ex.Message}", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (hasChanges && updateSuccess)
            {
                MessageBox.Show("Всі зміни успішно збережено!", "Успіх",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Очищаємо поля пароля
                txtCurrentPassword.Clear();
                txtNewPassword.Clear();
                txtConfirmPassword.Clear();

                // Вимкнути редагування
                txtName.IsEnabled = false;
                txtEmail.IsEnabled = false;
                txtCurrentPassword.IsEnabled = false;
                txtNewPassword.IsEnabled = false;
                txtConfirmPassword.IsEnabled = false;
                txtQuestion.IsEnabled = false;
                txtAnswer.IsEnabled = false;

                isDataModified = false;
                isPasswordModified = false;
                isQuestionModified = false;
            }
            else if (!hasChanges)
            {
                MessageBox.Show("Немає змін для збереження.", "Інформація",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (isDataModified || isPasswordModified || isQuestionModified)
            {
                MessageBoxResult result = MessageBox.Show("У вас є незбережені зміни. Вийти без збереження?", "Підтвердження",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            MainWindow mainWindow = new MainWindow(currentUserId);
            mainWindow.Show();
            this.Close();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            BtnCancel_Click(sender, e);
        }
    }
}