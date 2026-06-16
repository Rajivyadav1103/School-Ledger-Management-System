using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolledgerSystem.Models
{
    public class FeePayment
    {
        public int FeePaymentID { get; set; }

        public int StudentID { get; set; }

        [ForeignKey("StudentID")]
        public Student Student { get; set; }

        // IMPORTANT: Make FeeStructureID nullable (int?)
        public int? FeeStructureID { get; set; }

        [ForeignKey("FeeStructureID")]
        public FeeStructure FeeStructure { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public decimal Discount { get; set; } = 0;
        public decimal Fine { get; set; } = 0;
        public decimal NetAmount { get; set; }
        public string InvoiceNo { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string AcademicYear { get; set; } = "2025/2026";
        public string PaymentStatus { get; set; } = "Paid";
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}