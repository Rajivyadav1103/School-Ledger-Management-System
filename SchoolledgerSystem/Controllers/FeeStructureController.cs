using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolledgerSystem.DAO;
using SchoolledgerSystem.Models;

namespace SchoolledgerSystem.Controllers
{
    public class FeeStructureController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeeStructureController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET ALL with related data
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _context.FeeStructures
                    .Include(x => x.ClassType)
                    .Include(x => x.FeeType)
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.FeeStructureID)
                    .Select(x => new
                    {
                        x.FeeStructureID,
                        x.ClassTypeID,
                        ClassTypeName = x.ClassType.TypeName,
                        x.FeeTypeID,
                        FeeTypeName = x.FeeType.FeeName,
                        FeeCode = x.FeeType.FeeCode,
                        x.Amount,
                        x.AcademicYear,
                        x.Description,
                        x.IsActive,
                        x.CreatedDate
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
                var data = await _context.FeeStructures
                    .Include(x => x.ClassType)
                    .Include(x => x.FeeType)
                    .Where(x => x.FeeStructureID == id && !x.IsDeleted)
                    .Select(x => new
                    {
                        x.FeeStructureID,
                        x.ClassTypeID,
                        ClassTypeName = x.ClassType.TypeName,
                        x.FeeTypeID,
                        FeeTypeName = x.FeeType.FeeName,
                        x.Amount,
                        x.AcademicYear,
                        x.Description,
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
                    .Where(x => !x.IsDeleted && x.IsActive)
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

        // GET FEE TYPES for dropdown
        [HttpGet]
        public async Task<IActionResult> GetFeeTypes()
        {
            try
            {
                var feeTypes = await _context.FeeTypes
                    .Where(x => !x.IsDeleted && x.IsActive)
                    .OrderBy(x => x.FeeName)
                    .Select(x => new { x.FeeTypeID, x.FeeName, x.FeeCode, x.Amount })
                    .ToListAsync();

                return Json(feeTypes);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET FEE TYPE AMOUNT
        [HttpGet]
        public async Task<IActionResult> GetFeeTypeAmount(int feeTypeId)
        {
            try
            {
                var feeType = await _context.FeeTypes
                    .Where(x => x.FeeTypeID == feeTypeId && !x.IsDeleted)
                    .Select(x => x.Amount)
                    .FirstOrDefaultAsync();

                return Json(new { amount = feeType });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // SAVE (CREATE + UPDATE)
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] FeeStructure model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data" });
                }

                // Validation
                if (model.ClassTypeID <= 0)
                {
                    return Json(new { success = false, message = "Please select a Class Type" });
                }

                if (model.FeeTypeID <= 0)
                {
                    return Json(new { success = false, message = "Please select a Fee Type" });
                }

                if (model.Amount <= 0)
                {
                    return Json(new { success = false, message = "Amount must be greater than 0" });
                }

                // Check for duplicate (same ClassType + FeeType + AcademicYear)
                bool exists = await _context.FeeStructures
                    .AnyAsync(x => x.ClassTypeID == model.ClassTypeID
                        && x.FeeTypeID == model.FeeTypeID
                        && x.AcademicYear == model.AcademicYear
                        && !x.IsDeleted
                        && x.FeeStructureID != model.FeeStructureID);

                if (exists)
                {
                    return Json(new { success = false, message = "Fee structure already exists for this Class Type, Fee Type, and Academic Year" });
                }

                if (model.FeeStructureID == 0)
                {
                    // Create new record
                    var entity = new FeeStructure
                    {
                        ClassTypeID = model.ClassTypeID,
                        FeeTypeID = model.FeeTypeID,
                        Amount = model.Amount,
                        AcademicYear = model.AcademicYear?.Trim(),
                        Description = model.Description?.Trim(),
                        IsActive = model.IsActive,
                        CreatedDate = DateTime.Now,
                        IsDeleted = false
                    };

                    await _context.FeeStructures.AddAsync(entity);
                }
                else
                {
                    // Update existing record
                    var data = await _context.FeeStructures
                        .FirstOrDefaultAsync(x => x.FeeStructureID == model.FeeStructureID && !x.IsDeleted);

                    if (data == null)
                    {
                        return Json(new { success = false, message = "Record not found" });
                    }

                    data.ClassTypeID = model.ClassTypeID;
                    data.FeeTypeID = model.FeeTypeID;
                    data.Amount = model.Amount;
                    data.AcademicYear = model.AcademicYear?.Trim();
                    data.Description = model.Description?.Trim();
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
                var data = await _context.FeeStructures
                    .FirstOrDefaultAsync(x => x.FeeStructureID == id && !x.IsDeleted);

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

        // BULK DELETE by ClassType
        [HttpPost]
        public async Task<IActionResult> DeleteByClassType(int classTypeId)
        {
            try
            {
                var records = await _context.FeeStructures
                    .Where(x => x.ClassTypeID == classTypeId && !x.IsDeleted)
                    .ToListAsync();

                foreach (var record in records)
                {
                    record.IsDeleted = true;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"{records.Count} record(s) deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}