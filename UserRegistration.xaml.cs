using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace t
{
    /// <summary>
    /// Interaction logic for EditingUserData.xaml
    /// </summary>
    public partial class EditingUserData : Window
    {
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";
        private string userEmail;
        public EditingUserData(string email)
        {
            InitializeComponent();
            userEmail = email;
            LoadUserData();
        }
        private void LoadUserData()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Name, Gmail, Password FROM userdata WHERE Gmail=@Email";
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", userEmail);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                NameTextBox.Text = reader["Name"].ToString();
                                EmailTextBox.Text = reader["Gmail"].ToString();
                                PasswordBox.Password = reader["Password"].ToString();
                            }
                            else
                            {
                                MessageBox.Show("Користувача не знайдено!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка при завантаженні даних: " + ex.Message);
                }
            }
        }


        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            //UserSettings settingsWindow = new UserSettings();
            //settingsWindow.Show();
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //UserSettings settingsWindow = new UserSettings();
            //settingsWindow.Show();
            this.Close();
        }

        private void SaveDataButton_Click(object sender, RoutedEventArgs e)
        {
            string newName = NameTextBox.Text.Trim();
            string newPassword = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("Заповніть усі поля!");
                return;
            }

            if (newPassword.Length < 5 || newPassword.Length > 8)
            {
                MessageBox.Show("Пароль повинен містити від 5 до 8 символів!");
                return;
            }

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string updateQuery = @"UPDATE userdata 
                                           SET Name=@Name, Password=@Password 
                                           WHERE Gmail=@Email";

                    using (var cmd = new MySqlCommand(updateQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Name", newName);
                        cmd.Parameters.AddWithValue("@Password", newPassword);
                        cmd.Parameters.AddWithValue("@Email", userEmail);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                            MessageBox.Show("Дані оновлено");
                        else
                            MessageBox.Show("Не вдалося оновити дані");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка при оновленні даних: " + ex.Message);
                }
            }

        }
    }
}