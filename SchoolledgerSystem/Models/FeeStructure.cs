using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolledgerSystem.Models
{
    public class FeeStructure
    {
        // Primary Key
        public int FeeStructureID { get; set; }

        // 🔗 ClassType (because you already created it)
        public int ClassTypeID { get; set; }

        [ForeignKey("ClassTypeID")]
        public ClassType ClassType { get; set; }

        // 🔗 FeeType
        public int FeeTypeID { get; set; }

        [ForeignKey("FeeTypeID")]
        public FeeType FeeType { get; set; }

        // 💰 Amount (can override FeeType amount if needed)
        public decimal Amount { get; set; }

        // 📅 Academic Year (optional but recommended)
        public string? AcademicYear { get; set; }

        // 📝 Extra description
        public string? Description { get; set; }

        // 🔘 Status
        public bool IsActive { get; set; } = true;

        // 🗑 Soft delete
        public bool IsDeleted { get; set; } = false;

        // 🕒 System tracking
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }
    }
}