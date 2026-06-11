namespace SchoolledgerSystem.Models
{
    public class ClassType
    {
        public int ClassTypeID { get; set; }

        public string TypeName { get; set; } = string.Empty; // Primary, Secondary

        public string? Description { get; set; }

        public int OrderNo { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }
    }
}