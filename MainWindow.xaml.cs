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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace t
{
    public partial class MainWindow : Window
    {
        private List<Transaction> transactions = new List<Transaction>();
        private PeriodType currentPeriod = PeriodType.Day;
        private ViewType currentViewType = ViewType.Expenses;
        private DateTime currentDate = DateTime.Now;
        private DateTime customStartDate = DateTime.Now;
        private DateTime customEndDate = DateTime.Now;
        public MainWindow()
        {
            InitializeComponent();
            // Встановлюємо початкові значення
            cmbType.SelectedIndex = 0;
            cmbAddType.SelectedIndex = 0;

            // Ініціалізуємо DatePicker
            dpStartDate.SelectedDate = DateTime.Now;
            dpEndDate.SelectedDate = DateTime.Now;

            UpdateDateDisplay();
            UpdateAmountDisplay();
            UpdateGeneralBalance();
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

            return transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate && t.Type == currentViewType.ToString())
                .Sum(t => t.Amount);
        }

        private void UpdateGeneralBalance()
        {
            decimal totalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            decimal totalExpenses = transactions.Where(t => t.Type == "Expenses").Sum(t => t.Amount);
            decimal balance = totalIncome - totalExpenses;

            txtGeneralBalance.Text = $"{balance:F2}";
            txtGeneralBalance.Foreground = balance >= 0 ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
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
            ChecksData checksWindow = new ChecksData();
            checksWindow.ShowDialog();
        }

        private void BtnAddRecord_Click(object sender, RoutedEventArgs e)
        {
            Expenses addRecordWindow = new Expenses();
            addRecordWindow.ShowDialog();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            UserSignIn UserSignInWindow = new UserSignIn();
            UserSignInWindow.ShowDialog();
        }
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            UserSettings settingsWindow = new UserSettings();
            settingsWindow.ShowDialog();
        }
    }
}

