
using Microsoft.EntityFrameworkCore;
using SchoolledgerSystem.DAO;
using SchoolledgerSystem.Models;

namespace SchoolledgerSystem.Services
{
    public class StudentService
    {
        ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET ALL
        public async Task<List<Student>> GetAllAsync()
        {
            return await _context.Students
                .OrderByDescending( x => x.StudentID)
                .ToListAsync();
        }

        // GET BY ID
        public async Task<Student> GetByIdAsync(int id)
        {
            return await _context.Students.FindAsync(id);
        }

        // CREATE
        public async Task AddAsync(Student student)
        {
            student.AdmissionDate = DateTime.Now;
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        // UPDATE
        public async Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        // DELETE
        public async Task DeleteAsync(int id)
        {
            var data = await _context.Students.FindAsync(id);
            if (data != null)
            {
                _context.Students.Remove(data);
                await _context.SaveChangesAsync();
            }
        }
    }
}