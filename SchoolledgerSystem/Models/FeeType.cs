namespace SchoolledgerSystem.Models
{
    public class FeeType
    {
        // Primary Key
        public int FeeTypeID { get; set; }

        // Fee Info
        public string FeeCode { get; set; }
        public string FeeName { get; set; }

        // Amount
        public decimal Amount { get; set; }

        // Optional
        public string? Description { get; set; }

        // Status
        public bool IsActive { get; set; } = true;

        // Soft Delete
        public bool IsDeleted { get; set; } = false;

        // System
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}