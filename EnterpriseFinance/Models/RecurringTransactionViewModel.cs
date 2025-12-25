using System.ComponentModel.DataAnnotations;

namespace EnterpriseFinance.Models
{
    public class RecurringTransactionViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Transaction Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Amount")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal Amount { get; set; }

        [Display(Name = "Type")]
        public bool IsIncome { get; set; }

        [Display(Name = "Category")]
        public string CategoryName { get; set; } = string.Empty;

        public string CategoryColor { get; set; } = string.Empty;

        [Display(Name = "Frequency")]
        public string Frequency { get; set; } = string.Empty;

        [Display(Name = "Interval")]
        public int Interval { get; set; }

        [Display(Name = "Day of Week")]
        public DayOfWeek? DayOfWeek { get; set; }

        [Display(Name = "Day of Month")]
        public int? DayOfMonth { get; set; }

        [Display(Name = "Start Date")]
        public string StartDate { get; set; } = string.Empty;

        [Display(Name = "End Date")]
        public string EndDate { get; set; } = string.Empty;

        [Display(Name = "Next Planned Date")]
        public string NextPlannedDate { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}