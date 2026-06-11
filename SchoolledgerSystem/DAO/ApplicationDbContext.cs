using Microsoft.EntityFrameworkCore;
using SchoolledgerSystem.Models;


namespace SchoolledgerSystem.DAO
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }

        public DbSet<FeeType> FeeTypes { get; set; }

        public DbSet<ClassType> ClassTypes { get; set; }

        public DbSet<FeeStructure> FeeStructures { get; set; }

        public DbSet<FeePayment> FeePayments { get; set; }


    }
}