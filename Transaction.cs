using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t
{
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }

        public Transaction()
        {
            Date = DateTime.Now;
        }

    }
    public enum ViewType
    {
        Expenses,
        Income,
        Savings
    }
    public enum PeriodType
    {
        Day = 0,
        Week = 1,
        Month = 2,
        Year = 3,
        Custom = 4
    }
}
