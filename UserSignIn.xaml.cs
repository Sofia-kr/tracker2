using MySql.Data.MySqlClient;
using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace t
{
    public partial class UserSignIn : Window
    {
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";

        public UserSignIn()
        {
            InitializeComponent();
        }

        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            string pattern = @"^[a-zA-Z0-9._%+-]+@gmail\.com$";
            return Regex.IsMatch(email, pattern);
        }

        private bool ValidatePassword(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Length <= 8;
        }

        private void GmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Ваша логіка валідації
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            GeneralErrorText.Visibility = Visibility.Collapsed;
            PasswordErrorText.Visibility = Visibility.Collapsed;

            string email = GmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Будь ласка, заповніть всі поля");
                return;
            }

            if (!ValidateEmail(email))
            {
                ShowError("Некоректний формат Gmail");
                return;
            }

            if (!ValidatePassword(password))
            {
                PasswordErrorText.Text = "Пароль повинен містити до 8 символів";
                PasswordErrorText.Visibility = Visibility.Visible;
                return;
            }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string checkUserQuery = "SELECT COUNT(*) FROM userdata WHERE Gmail = @Gmail";
                    MySqlCommand checkUserCmd = new MySqlCommand(checkUserQuery, connection);
                    checkUserCmd.Parameters.AddWithValue("@Gmail", email);

                    int userCount = Convert.ToInt32(checkUserCmd.ExecuteScalar());

                    if (userCount == 0)
                    {
                        ShowError("Ви не зареєстровані!");
                        return;
                    }

                    string checkPasswordQuery = "SELECT Password FROM userdata WHERE Gmail = @Gmail";
                    MySqlCommand checkPasswordCmd = new MySqlCommand(checkPasswordQuery, connection);
                    checkPasswordCmd.Parameters.AddWithValue("@Gmail", email);

                    string storedPassword = checkPasswordCmd.ExecuteScalar()?.ToString();

                    if (storedPassword != password)
                    {
                        ShowError("Пароль або електронна пошта введені не правильно");
                        return;
                    }
                    string query = "SELECT IDuser FROM userdata WHERE Gmail = @Gmail AND Password = @Password";

                    int IDuser = -1; // Declare and initialize IDuser

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Gmail", email);
                        command.Parameters.AddWithValue("@Password", password);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                IDuser = reader.GetInt32(reader.GetOrdinal("IDuser"));
                            }
                            else
                            {
                                ShowError("Не вдалося знайти користувача.");
                                return;
                            }
                        }
                    }

                    NavigateToMainScreen(IDuser);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Помилка бази даних: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            GeneralErrorText.Text = message;
            GeneralErrorText.Visibility = Visibility.Visible;
        }

        private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var passwordRecovery = new PasswordRecovery();
                passwordRecovery.Owner = this;
                passwordRecovery.ShowDialog();
                this.Close();
            }
            catch
            {
                MessageBox.Show("Форма відновлення пароля тимчасово недоступна", "Інформація",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var userRegistration = new UserRegistration();
                userRegistration.Owner = this;
                userRegistration.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Форма реєстрації тимчасово недоступна", "Інформація",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void NavigateToMainScreen(int IDuser)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    mainWindow = new MainWindow(IDuser);
                    Application.Current.MainWindow = mainWindow;
                }
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка переходу: {ex.Message}", "Помилка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
