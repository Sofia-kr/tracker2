using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static Mysqlx.Datatypes.Scalar.Types;

namespace t
{
    public partial class MainWindow : Window
    {
        private string connectionString = "server=sql7.freesqldatabase.com;port=3306;user=sql7811018;password=aBIaRrIe8v;database=sql7811018;Charset=utf8mb4;";
        private List<Transaction> transactions = new List<Transaction>();
        private PeriodType currentPeriod = PeriodType.Day;
        private ViewType currentViewType = ViewType.Expenses;
        private DateTime currentDate = DateTime.Now;
        private DateTime customStartDate = DateTime.Now;
        private DateTime customEndDate = DateTime.Now;
        private int currentUserId;
        public MainWindow(int userId)
        {
           
            InitializeComponent();
            currentUserId = userId;
            cmbType.SelectedIndex = 0;

            dpStartDate.SelectedDate = DateTime.Now;
            dpEndDate.SelectedDate = DateTime.Now;

            UpdateDateDisplay();
            UpdateAmountDisplay();
            UpdateGeneralBalance();
        }
        private int GetCurrentUserId()
        {
            return currentUserId;
        }
        private void UpdateDateDisplay()
        {
            switch (currentPeriod)
            {
                case PeriodType.Day:
                    btnDatePeriod.Content = currentDate.ToString("dd.MM.yyyy");
                    break;
                case PeriodType.Week:
                    DateTime startOfWeek = GetStartOfWeek(currentDate);
                    DateTime endOfWeek = startOfWeek.AddDays(6);
                    btnDatePeriod.Content = $"{startOfWeek:dd.MM.yyyy} - {endOfWeek:dd.MM.yyyy}";
                    break;
                case PeriodType.Month:
                    DateTime startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                    DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                    btnDatePeriod.Content = $"{startOfMonth:dd.MM.yyyy} - {endOfMonth:dd.MM.yyyy}";
                    break;
                case PeriodType.Year:
                    DateTime startOfYear = new DateTime(currentDate.Year, 1, 1);
                    DateTime endOfYear = new DateTime(currentDate.Year, 12, 31);
                    btnDatePeriod.Content = $"{startOfYear:dd.MM.yyyy} - {endOfYear:dd.MM.yyyy}";
                    break;
                case PeriodType.Custom:
                    btnDatePeriod.Content = $"{customStartDate:dd.MM.yyyy} - {customEndDate:dd.MM.yyyy}";
                    break;
            }
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            // Тиждень починається з понеділка
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private void UpdateAmountDisplay()
        {
            decimal amount = CalculatePeriodAmount();

            switch (currentViewType)
            {
                case ViewType.Expenses:
                    txtAmount.Text = $"-{amount:F2}";
                    txtAmount.Foreground = System.Windows.Media.Brushes.Red;
                    break;
                case ViewType.Income:
                    txtAmount.Text = $"+{amount:F2}";
                    txtAmount.Foreground = System.Windows.Media.Brushes.Green;
                    break;
                case ViewType.Savings:
                    txtAmount.Text = $"{amount:F2}";
                    txtAmount.Foreground = System.Windows.Media.Brushes.Blue;
                    break;
            }
        }

        private decimal CalculatePeriodAmount()
        {
            DateTime startDate, endDate;

            switch (currentPeriod)
            {
                case PeriodType.Day:
                    startDate = currentDate.Date;
                    endDate = currentDate.Date.AddDays(1).AddSeconds(-1);
                    break;
                case PeriodType.Week:
                    startDate = GetStartOfWeek(currentDate);
                    endDate = startDate.AddDays(7).AddSeconds(-1);
                    break;
                case PeriodType.Month:
                    startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                    endDate = startDate.AddMonths(1).AddSeconds(-1);
                    break;
                case PeriodType.Year:
                    startDate = new DateTime(currentDate.Year, 1, 1);
                    endDate = new DateTime(currentDate.Year, 12, 31).AddDays(1).AddSeconds(-1);
                    break;
                case PeriodType.Custom:
                    startDate = customStartDate.Date;
                    endDate = customEndDate.Date.AddDays(1).AddSeconds(-1);
                    break;
                default:
                    startDate = currentDate.Date;
                    endDate = currentDate.Date.AddDays(1).AddSeconds(-1);
                    break;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string tableName = "";
                    string amountColumn = "";
                    string dateColumn = "";
                    
                    switch (currentViewType)
                    {
                        case ViewType.Expenses:
                            tableName = "expenses";
                            amountColumn = "AmoutExpenses"; 
                            dateColumn = "ExpenseDate";
                            break;
                        case ViewType.Income:
                            tableName = "income";
                            amountColumn = "AmountIncome";
                            dateColumn = "IncomeDate";
                            break;
                        case ViewType.Savings:
                            tableName = "saving";
                            amountColumn = "AmoutSaving";
                            dateColumn = "SavingDate";
                            break;
                    }

                    if (string.IsNullOrEmpty(tableName))
                        return 0;

                    string query = $@"
                SELECT COALESCE(SUM({amountColumn}), 0) 
                FROM {tableName} 
                WHERE IDuser = @UserId 
                AND {dateColumn} BETWEEN @StartDate AND @EndDate";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", currentUserId);
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);

                        var result = command.ExecuteScalar();
                        return Convert.ToDecimal(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при отриманні даних з бази: {ex.Message}");
                return 0;
            }
        }

        private void UpdateGeneralBalance()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    int currentUserId = GetCurrentUserId();

                    // Використовуємо правильні назви стовпців
                    string query = @"
                SELECT 
                    (SELECT COALESCE(SUM(AmountIncome), 0) FROM income WHERE IDuser = @UserId) -
                    (SELECT COALESCE(SUM(AmoutExpenses), 0) FROM expenses WHERE IDuser = @UserId) AS Balance";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", currentUserId);

                        var result = command.ExecuteScalar();
                        decimal balance = result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;

                        txtGeneralBalance.Text = $"{balance:F2}";
                        txtGeneralBalance.Foreground = balance >= 0 ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при отриманні загального балансу: {ex.Message}");
            }
        }

        private void ReturnToCurrentPeriod()
        {
            DateTime now = DateTime.Now;

            switch (currentPeriod)
            {
                case PeriodType.Day:
                    currentDate = now;
                    break;
                case PeriodType.Week:
                    currentDate = now;
                    break;
                case PeriodType.Month:
                    currentDate = now;
                    break;
                case PeriodType.Year:
                    currentDate = now;
                    break;
                case PeriodType.Custom:
                    // Для власного періоду встановлюємо поточний день
                    customStartDate = now;
                    customEndDate = now;
                    dpStartDate.SelectedDate = now;
                    dpEndDate.SelectedDate = now;
                    break;
            }

            UpdateDateDisplay();
            UpdateAmountDisplay();

            // Показати повідомлення про успішне повернення
            ShowTemporaryMessage("Поточний період встановлено");
        }

        private void ShowTemporaryMessage(string message)
        {
            // Тимчасове повідомлення
            string originalText = btnDatePeriod.Content.ToString();
            btnDatePeriod.Content = message;

            // Повернути оригінальний текст через 2 секунди
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                UpdateDateDisplay();
            };
            timer.Start();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (currentPeriod == PeriodType.Custom)
            {
                // Для власного періоду зміщуємо обидві дати
                int daysDiff = (int)(customEndDate - customStartDate).TotalDays;
                customStartDate = customStartDate.AddDays(-daysDiff - 1);
                customEndDate = customEndDate.AddDays(-daysDiff - 1);
                dpStartDate.SelectedDate = customStartDate;
                dpEndDate.SelectedDate = customEndDate;
            }
            else
            {
                switch (currentPeriod)
                {
                    case PeriodType.Day:
                        currentDate = currentDate.AddDays(-1);
                        break;
                    case PeriodType.Week:
                        currentDate = currentDate.AddDays(-7);
                        break;
                    case PeriodType.Month:
                        currentDate = currentDate.AddMonths(-1);
                        break;
                    case PeriodType.Year:
                        currentDate = currentDate.AddYears(-1);
                        break;
                }
            }
            UpdateDateDisplay();
            UpdateAmountDisplay();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (currentPeriod == PeriodType.Custom)
            {
                // Для власного періоду зміщуємо обидві дати
                int daysDiff = (int)(customEndDate - customStartDate).TotalDays;
                customStartDate = customStartDate.AddDays(daysDiff + 1);
                customEndDate = customEndDate.AddDays(daysDiff + 1);
                dpStartDate.SelectedDate = customStartDate;
                dpEndDate.SelectedDate = customEndDate;
            }
            else
            {
                switch (currentPeriod)
                {
                    case PeriodType.Day:
                        currentDate = currentDate.AddDays(1);
                        break;
                    case PeriodType.Week:
                        currentDate = currentDate.AddDays(7);
                        break;
                    case PeriodType.Month:
                        currentDate = currentDate.AddMonths(1);
                        break;
                    case PeriodType.Year:
                        currentDate = currentDate.AddYears(1);
                        break;
                }
            }
            UpdateDateDisplay();
            UpdateAmountDisplay();
        }

        private void BtnDay_Click(object sender, RoutedEventArgs e)
        {
            currentPeriod = PeriodType.Day;
            HideCustomPeriodControls();
            UpdateDateDisplay();
            UpdateAmountDisplay();
        }

        private void BtnWeek_Click(object sender, RoutedEventArgs e)
        {
            currentPeriod = PeriodType.Week;
            HideCustomPeriodControls();
            UpdateDateDisplay();
            UpdateAmountDisplay();
        }

        private void BtnMonth_Click(object sender, RoutedEventArgs e)
        {
            currentPeriod = PeriodType.Month;
            HideCustomPeriodControls();
            UpdateDateDisplay();
            UpdateAmountDisplay();
        }

        private void BtnYear_Click(object sender, RoutedEventArgs e)
        {
            currentPeriod = PeriodType.Year;
            HideCustomPeriodControls();
            UpdateDateDisplay();
            UpdateAmountDisplay();
        }

        private void BtnCustom_Click(object sender, RoutedEventArgs e)
        {
            currentPeriod = PeriodType.Custom;
            ShowCustomPeriodControls();
            UpdateDateDisplay();
        }

        private void BtnApplyCustomPeriod_Click(object sender, RoutedEventArgs e)
        {
            if (dpStartDate.SelectedDate.HasValue && dpEndDate.SelectedDate.HasValue)
            {
                customStartDate = dpStartDate.SelectedDate.Value;
                customEndDate = dpEndDate.SelectedDate.Value;
                UpdateDateDisplay();
                UpdateAmountDisplay();
            }
        }

        private void BtnEditCategories_Click(object sender, RoutedEventArgs e)
        {
            // Тепер вікно самостійно керує перемиканням між типами категорій
            EditCategory editWindow = new EditCategory(currentUserId);
            editWindow.ShowDialog();

            // Оновити дані після редагування категорій
            UpdateAmountDisplay();
            UpdateGeneralBalance();
        }

        private void BtnDeleteCategories_Click(object sender, RoutedEventArgs e)
        {
            // Тепер вікно самостійно керує перемиканням між типами категорій
            DeleteCategory deleteWindow = new DeleteCategory(currentUserId);
            deleteWindow.ShowDialog();

            // Оновити дані після видалення категорій
            UpdateAmountDisplay();
            UpdateGeneralBalance();
        }
        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentPeriod == PeriodType.Custom && dpStartDate.SelectedDate.HasValue && dpEndDate.SelectedDate.HasValue)
            {
                customStartDate = dpStartDate.SelectedDate.Value;
                customEndDate = dpEndDate.SelectedDate.Value;
                UpdateDateDisplay();
                UpdateAmountDisplay();
            }
        }

        private void ShowCustomPeriodControls()
        {
            pnlCustomPeriod.Visibility = Visibility.Visible;
        }

        private void HideCustomPeriodControls()
        {
            pnlCustomPeriod.Visibility = Visibility.Collapsed;
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbType.SelectedItem is ComboBoxItem item)
            {
                string selectedType = item.Tag.ToString();
                currentViewType = (ViewType)Enum.Parse(typeof(ViewType), selectedType);
                UpdateAmountDisplay();
            }
        }

        private void BtnChecks_Click(object sender, RoutedEventArgs e)
        {
            ChecksData checksWindow = new ChecksData(currentUserId);
            checksWindow.ShowDialog();
            this.Close();
        }

        private void BtnAddRecord_Click(object sender, RoutedEventArgs e)
        {
            DateTime dateToPass;

            // Якщо ви хочете передавати поточну дату з періоду
            switch (currentPeriod)
            {
                case PeriodType.Day:
                    dateToPass = currentDate;
                    break;
                case PeriodType.Week:
                    // Передаємо перший день тижня
                    dateToPass = GetStartOfWeek(currentDate);
                    break;
                case PeriodType.Month:
                    // Передаємо перший день місяця
                    dateToPass = new DateTime(currentDate.Year, currentDate.Month, 1);
                    break;
                case PeriodType.Year:
                    // Передаємо перший день року
                    dateToPass = new DateTime(currentDate.Year, 1, 1);
                    break;
                case PeriodType.Custom:
                    dateToPass = customStartDate;
                    break;
                default:
                    dateToPass = DateTime.Now;
                    break;
            }

            // Передати userId і дату у вікно Expenses
            Expenses addRecordWindow = new Expenses(currentUserId, dateToPass);
            addRecordWindow.ShowDialog();

            // Оновити дані після закриття вікна додавання
            UpdateAmountDisplay();
            UpdateGeneralBalance();
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Savings savingsWindow = new Savings();
            savingsWindow.ShowDialog();
            this.Close();
        }
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            UserSettings settingsWindow = new UserSettings(currentUserId);
            settingsWindow.ShowDialog();
            this.Close();
        }
    }
}

