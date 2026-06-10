namespace SchoolledgerSystem.Models
{
    public class Student
    {
        // 🆔 Primary Info
        public int StudentID { get; set; }
        public string RollNo { get; set; }


        // 👤 Personal Info
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string BloodGroup { get; set; }


        // 👨‍👩‍👦 Parent / Guardian Info
        public string FatherName { get; set; }
        public string MotherName { get; set; }


        // 📚 Academic Info
        public string Class { get; set; }
        public string Section { get; set; }


        // 📞 Contact Info
        public string Phone { get; set; }
        public string EmergencyContact { get; set; }
        public string Email { get; set; }


        // 🏠 Address Info
        public string Address { get; set; }


        // 📅 System Info
        public DateTime AdmissionDate { get; set; }


        // ⭐ Status
        public bool IsActive { get; set; } = true;
    }
}