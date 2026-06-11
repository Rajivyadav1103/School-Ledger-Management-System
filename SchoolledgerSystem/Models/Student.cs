using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolledgerSystem.Models
{
    public class Student
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? RollNo { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime AdmissionDate { get; set; } = DateTime.Now;
        public int ClassTypeID { get; set; }

        [ForeignKey("ClassTypeID")]
        public ClassType ClassType { get; set; }

        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}