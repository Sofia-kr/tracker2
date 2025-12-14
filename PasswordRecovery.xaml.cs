using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace t
{
    public partial class PasswordRecovery : Window
    {
        private string connectionString = "Server=sql7.freesqldatabase.com;Port=3306;user=sql7811018;Pwd=aBIaRrIe8v;Database=sql7811018;CharSet=utf8mb4;";
        private int userId;
        private string userQuestion = "";
        private string correctAnswer = "";

        public PasswordRecovery()
        {
            InitializeComponent();
            txtNewPassword.IsEnabled = false;
            txtConfirmPassword.IsEnabled = false;
            btnSave.IsEnabled = false;
            txtAnswer.IsEnabled = false;
            txtQuestion.Text = "";

            // Підписка на подію зміни тексту email
            txtEmail.TextChanged += TxtEmail_TextChanged;
        }

        private void BtnVerify_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim().ToLower();
            string answer = txtAnswer.Text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Введіть електронну пошту!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            if (!IsValidGmail(email))
            {
                MessageBox.Show("Неправильний формат Gmail!\nПриклад: example@gmail.com", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.SelectAll();
                txtEmail.Focus();
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Перевіряємо чи існує користувач
                    string query = "SELECT IDuser, SpecialQuestion, SpecialAnswer FROM userdata WHERE LOWER(Gmail) = @email";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@email", email);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userId = reader.GetInt32("IDuser");
                            userQuestion = reader["SpecialQuestion"].ToString();
                            correctAnswer = reader["SpecialAnswer"].ToString();

                            txtQuestion.Text = userQuestion;
                            txtAnswer.IsEnabled = true;
                            txtAnswer.Focus();

                            if (!string.IsNullOrEmpty(answer))
                            {
                                // Якщо відповідь вже введена, перевіряємо її
                                CheckAnswer();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Користувача з такою електронною поштою не знайдено!", "Помилка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            txtEmail.SelectAll();
                            txtEmail.Focus();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042) // Cannot connect to server
                {
                    MessageBox.Show("Не вдається підключитися до сервера бази даних.\nПеревірте підключення до інтернету.", "Помилка підключення",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (ex.Number == 1045) // Access denied
                {
                    MessageBox.Show("Помилка автентифікації до бази даних.\nПеревірте налаштування підключення.", "Помилка доступу",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Помилка бази даних: {ex.Message}", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася неочікувана помилка: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckAnswer()
        {
            string answer = txtAnswer.Text.Trim();

            if (string.IsNullOrEmpty(answer))
            {
                MessageBox.Show("Введіть відповідь на питання!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAnswer.Focus();
                return;
            }

            // Перевіряємо відповідь (без хешування, точне співпадіння з урахуванням регістру)
            if (answer.Trim() == correctAnswer.Trim())
            {
                MessageBox.Show("Відповідь вірна! Тепер ви можете встановити новий пароль.", "Успіх",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                txtNewPassword.IsEnabled = true;
                txtConfirmPassword.IsEnabled = true;
                btnSave.IsEnabled = true;
                txtAnswer.IsEnabled = false;
                btnVerify.IsEnabled = false;
                txtNewPassword.Focus();
            }
            else
            {
                MessageBox.Show("Невірна відповідь на питання!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                txtAnswer.SelectAll();
                txtAnswer.Focus();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = txtNewPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("Введіть новий пароль!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewPassword.Focus();
                return;
            }

            if (string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show("Підтвердіть новий пароль!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtConfirmPassword.Focus();
                return;
            }

            // Перевірка довжини пароля (5-8 символів як у базі даних)
            if (newPassword.Length < 5 || newPassword.Length > 8)
            {
                MessageBox.Show("Пароль повинен містити від 5 до 8 символів!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewPassword.SelectAll();
                txtNewPassword.Focus();
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Паролі не співпадають! Будь ласка, перевірте введені дані.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtConfirmPassword.SelectAll();
                txtConfirmPassword.Focus();
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Оновлюємо пароль (без хешування)
                    string query = "UPDATE userdata SET Password = @password WHERE IDuser = @userid";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@password", newPassword);
                    cmd.Parameters.AddWithValue("@userid", userId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Пароль успішно змінено!\nТепер ви можете увійти з новим паролем.", "Успіх",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        UserSignIn signIn = new UserSignIn();
                        signIn.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Не вдалося змінити пароль. Спробуйте ще раз.", "Помилка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1406) // Data too long for column
                {
                    MessageBox.Show("Пароль занадто довгий! Максимум 8 символів.", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    txtNewPassword.SelectAll();
                    txtNewPassword.Focus();
                }
                else
                {
                    MessageBox.Show($"Помилка бази даних при зміні пароля: {ex.Message}", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася неочікувана помилка: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            UserSignIn signIn = new UserSignIn();
            signIn.Show();
            this.Close();
        }

        // Перевірка Gmail
        private bool IsValidGmail(string email)
        {
            return Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@gmail\.com$", RegexOptions.IgnoreCase);
        }

        // Обробники подій клавіатури
        private void TxtEmail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnVerify_Click(sender, e);
            }
        }

        private void TxtAnswer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (txtAnswer.IsEnabled)
                {
                    CheckAnswer();
                }
            }
        }

        private void TxtNewPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtConfirmPassword.Focus();
            }
        }

        private void TxtConfirmPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnSave.IsEnabled)
            {
                BtnSave_Click(sender, e);
            }
        }

        // Обробник зміни тексту для txtAnswer
        private void TxtAnswer_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Нічого не робимо тут, просто залишаємо для XAML
        }

        // Обробник зміни тексту для txtEmail
        private void TxtEmail_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Якщо email змінився, скидаємо стан
            if (!string.IsNullOrEmpty(userQuestion))
            {
                ResetVerificationState();
            }
        }

        private void ResetVerificationState()
        {
            txtQuestion.Text = "";
            txtAnswer.IsEnabled = false;
            txtAnswer.Text = "";
            txtNewPassword.IsEnabled = false;
            txtNewPassword.Password = "";
            txtConfirmPassword.IsEnabled = false;
            txtConfirmPassword.Password = "";
            btnVerify.IsEnabled = true;
            btnSave.IsEnabled = false;
            userId = 0;
            userQuestion = "";
            correctAnswer = "";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtEmail.Focus();
        }
    }
}