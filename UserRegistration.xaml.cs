using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace t
{
    public partial class UserRegistration : Window
    {
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;"
;

        public UserRegistration()
        {
            InitializeComponent();
        }

        private bool IsEmailRegistered(string email)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM userdata WHERE Gmail=@Email";
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

            // Виправлена умова перевірки порожніх полів
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name)
                || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(question)
                || string.IsNullOrEmpty(answer))
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
                MessageBox.Show("Ви вже зареєстровані!");
                return;
            }

            if (name.Length > 20)
            {
                MessageBox.Show("Ім'я повинне містити не більше 20 символів");
                return;
            }

            if (password.Length < 5)
            {
                MessageBox.Show("Пароль повинен містити більше 5 символів");
                return;
            }

            if (password.Length > 8)
            {
                MessageBox.Show("Пароль повинен містити не більше 8 символів");
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
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка реєстрації: {ex.Message}");
            }
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            UserSignIn signIn = new UserSignIn();
            signIn.Show();
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UserSignIn signIn = new UserSignIn();
            signIn.Show();
            this.Close();
        }
    }
}