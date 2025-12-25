namespace EnterpriseFinance.Models
{
    public class PlannedTransactionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Yangi qo'shilgan
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; }
        public string PlannedDate { get; set; } = string.Empty;
        public string ExecutedDate { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryColor { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Pending";
        public bool IsRecurringGenerated { get; set; }
    }
}
