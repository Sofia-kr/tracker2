using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace t
{
    public partial class UserRegistration : Window
    {
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7803706;password=DrUIbcmB1f;database=sql7803706;Charset=utf8mb4;";

        public UserRegistration()
        {
            InitializeComponent();
        }

        private bool IsEmailRegistered(string email)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM userdata WHERE Gmail = @Email";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string name = NameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string question = SpecialQuestionTextBox.Text.Trim();
            string answer = SpecialAnswerTextBox.Text.Trim();

            // Виправлена умова перевірки заповненості полів
            if (string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(question) ||
                string.IsNullOrEmpty(answer))
            {
                MessageBox.Show("Заповніть всі поля!");
                return;
            }

            if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@gmail\.com$"))
            {
                MessageBox.Show("Неправильний формат Gmail!");
                return;
            }

            if (IsEmailRegistered(email))
            {
                MessageBox.Show("Ця пошта вже зареєстрована!");
                return;
            }

            if (name.Length > 20)
            {
                MessageBox.Show("Ім'я повинно містити не більше 20 символів");
                return;
            }

            // Об'єднання перевірок паролю
            if (password.Length < 5 || password.Length > 8)
            {
                MessageBox.Show("Пароль повинен містити від 5 до 8 символів");
                return;
            }

            // Додаткова перевірка на наявність пробілів в паролі
            if (password.Contains(" "))
            {
                MessageBox.Show("Пароль не повинен містити пробілів");
                return;
            }

            MessageBox.Show("Питання буде використане для відновлення паролю! Переконайтеся, що знатимете відповідь!");

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string insertQuery = @"INSERT INTO userdata (Name, Gmail, Password, SpecialQuestion, SpecialAnswer)
                                           VALUES (@Name, @Gmail, @Password, @SpecialQuestion, @SpecialAnswer)";
                    using (var cmd = new MySqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Gmail", email);
                        cmd.Parameters.AddWithValue("@Password", password);
                        cmd.Parameters.AddWithValue("@SpecialQuestion", question);
                        cmd.Parameters.AddWithValue("@SpecialAnswer", answer);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Успішна реєстрація!");
                UserSignIn signIn = new UserSignIn();
                signIn.Show();
                this.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Помилка бази даних: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}");
            }
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSignInWindow();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSignInWindow();
        }

        // Винесення спільної логіки в окремий метод
        private void OpenSignInWindow()
        {
            UserSignIn signIn = new UserSignIn();
            signIn.Show();
            this.Close();
        }
    }
}

