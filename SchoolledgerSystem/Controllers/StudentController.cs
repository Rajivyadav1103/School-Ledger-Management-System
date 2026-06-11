using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolledgerSystem.DAO;
using SchoolledgerSystem.Models;

namespace SchoolledgerSystem.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET ALL
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _context.Students
                    .Include(x => x.ClassType)
                    .Where(x => x.IsDeleted == false)
                    .OrderByDescending(x => x.StudentID)
                    .Select(x => new
                    {
                        x.StudentID,
                        x.StudentName,
                        x.RollNo,
                        x.Gender,
                        x.DateOfBirth,
                        x.AdmissionDate,
                        x.ClassTypeID,
                        ClassTypeName = x.ClassType != null ? x.ClassType.TypeName : "",
                        x.FatherName,
                        x.MotherName,
                        x.ContactNo,
                        x.Address,
                        x.IsActive
                    })
                    .ToListAsync();

                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET BY ID
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _context.Students
                    .Include(x => x.ClassType)
                    .Where(x => x.StudentID == id && x.IsDeleted == false)
                    .Select(x => new
                    {
                        x.StudentID,
                        x.StudentName,
                        x.RollNo,
                        x.Gender,
                        x.DateOfBirth,
                        x.AdmissionDate,
                        x.ClassTypeID,
                        x.FatherName,
                        x.MotherName,
                        x.ContactNo,
                        x.Address,
                        x.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (data == null)
                {
                    return Json(new { success = false, message = "Record not found" });
                }

                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET CLASS TYPES for dropdown
        [HttpGet]
        public async Task<IActionResult> GetClassTypes()
        {
            try
            {
                var classTypes = await _context.ClassTypes
                    .Where(x => x.IsDeleted == false && x.IsActive == true)
                    .OrderBy(x => x.OrderNo)
                    .Select(x => new { x.ClassTypeID, x.TypeName })
                    .ToListAsync();

                return Json(classTypes);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // SAVE (CREATE + UPDATE)
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] Student model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data" });
                }

                // Validation
                if (string.IsNullOrWhiteSpace(model.StudentName))
                {
                    return Json(new { success = false, message = "Student Name is required" });
                }

                if (model.ClassTypeID <= 0)
                {
                    return Json(new { success = false, message = "Please select a Class Type" });
                }

                if (string.IsNullOrWhiteSpace(model.ContactNo))
                {
                    return Json(new { success = false, message = "Contact Number is required" });
                }

                // Validate contact number (10 digits)
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.ContactNo, @"^\d{10}$"))
                {
                    return Json(new { success = false, message = "Contact Number must be 10 digits" });
                }

                // Check for duplicate Roll Number if provided
                if (!string.IsNullOrWhiteSpace(model.RollNo))
                {
                    bool rollNoExists = await _context.Students
                        .AnyAsync(x => x.RollNo == model.RollNo && x.IsDeleted == false && x.StudentID != model.StudentID);

                    if (rollNoExists)
                    {
                        return Json(new { success = false, message = "Roll Number already exists" });
                    }
                }

                if (model.StudentID == 0)
                {
                    // Create new record
                    var entity = new Student
                    {
                        StudentName = model.StudentName.Trim(),
                        RollNo = string.IsNullOrWhiteSpace(model.RollNo) ? null : model.RollNo.Trim(),
                        Gender = model.Gender,
                        DateOfBirth = model.DateOfBirth,
                        AdmissionDate = model.AdmissionDate == DateTime.MinValue ? DateTime.Now : model.AdmissionDate,
                        ClassTypeID = model.ClassTypeID,
                        FatherName = string.IsNullOrWhiteSpace(model.FatherName) ? null : model.FatherName.Trim(),
                        MotherName = string.IsNullOrWhiteSpace(model.MotherName) ? null : model.MotherName.Trim(),
                        ContactNo = model.ContactNo?.Trim(),
                        Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                        IsActive = model.IsActive,
                        IsDeleted = false,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = null
                    };

                    await _context.Students.AddAsync(entity);
                }
                else
                {
                    // Update existing record
                    var data = await _context.Students
                        .FirstOrDefaultAsync(x => x.StudentID == model.StudentID && x.IsDeleted == false);

                    if (data == null)
                    {
                        return Json(new { success = false, message = "Record not found" });
                    }

                    data.StudentName = model.StudentName.Trim();
                    data.RollNo = string.IsNullOrWhiteSpace(model.RollNo) ? null : model.RollNo.Trim();
                    data.Gender = model.Gender;
                    data.DateOfBirth = model.DateOfBirth;
                    data.AdmissionDate = model.AdmissionDate == DateTime.MinValue ? DateTime.Now : model.AdmissionDate;
                    data.ClassTypeID = model.ClassTypeID;
                    data.FatherName = string.IsNullOrWhiteSpace(model.FatherName) ? null : model.FatherName.Trim();
                    data.MotherName = string.IsNullOrWhiteSpace(model.MotherName) ? null : model.MotherName.Trim();
                    data.ContactNo = model.ContactNo?.Trim();
                    data.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
                    data.IsActive = model.IsActive;
                    data.UpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Saved Successfully" });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = "Database error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // SOFT DELETE
        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            try
            {
                var data = await _context.Students
                    .FirstOrDefaultAsync(x => x.StudentID == id && x.IsDeleted == false);

                if (data == null)
                {
                    return Json(new { success = false, message = "Record not found" });
                }

                data.IsDeleted = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Deleted Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}