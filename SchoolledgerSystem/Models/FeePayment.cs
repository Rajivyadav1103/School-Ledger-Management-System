using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolledgerSystem.Models
{
    public class FeePayment
    {
        // Primary Key
        public int FeePaymentID { get; set; }

        // 👨 Student
        public int StudentID { get; set; }

        [ForeignKey("StudentID")]
        public Student Student { get; set; }

        // 💰 Fee Structure
        public int FeeStructureID { get; set; }

        [ForeignKey("FeeStructureID")]
        public FeeStructure FeeStructure { get; set; }

        // 💵 PAYMENT DETAILS

        // Total fee (from FeeStructure)
        public decimal TotalAmount { get; set; }

        // Amount paid now
        public decimal PaidAmount { get; set; }

        // Remaining amount
        public decimal DueAmount { get; set; }

        // Optional discount
        public decimal Discount { get; set; } = 0;

        // Optional fine (late fee)
        public decimal Fine { get; set; } = 0;

        // FINAL CALCULATION
        public decimal NetAmount { get; set; }

        // 📄 Receipt / Invoice
        public string InvoiceNo { get; set; }

        // 💳 Payment Info
        public string PaymentMethod { get; set; } = "Cash";

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // 📅 Academic Year
        public string AcademicYear { get; set; } = "2025/2026";

        // 🔘 Status
        public string PaymentStatus { get; set; } = "Paid"; // Paid, Partial, Pending

        // 🗑 Soft Delete
        public bool IsDeleted { get; set; } = false;

        // 🕒 System
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}