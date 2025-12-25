namespace EnterpriseFinance.Models
{
    
    public class BudgetViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
        public decimal PlannedIncome { get; set; }
        public decimal PlannedExpense { get; set; }
        public decimal Balance => PlannedIncome - PlannedExpense;
        public string? Notes { get; set; }
    }
}
