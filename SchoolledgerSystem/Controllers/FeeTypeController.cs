using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolledgerSystem.DAO;
using SchoolledgerSystem.Models;

namespace SchoolledgerSystem.Controllers
{
    public class FeeTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeeTypeController(ApplicationDbContext context)
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
                var data = await _context.FeeTypes
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.FeeTypeID)
                    .Select(x => new
                    {
                        x.FeeTypeID,
                        x.FeeCode,
                        x.FeeName,
                        x.Amount,
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
                var data = await _context.FeeTypes
                    .Where(x => x.FeeTypeID == id && !x.IsDeleted)
                    .Select(x => new
                    {
                        x.FeeTypeID,
                        x.FeeCode,
                        x.FeeName,
                        x.Amount,
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

        // SAVE (CREATE + UPDATE)
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] FeeType model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data" });
                }

                // Validation
                if (string.IsNullOrEmpty(model.FeeCode))
                {
                    return Json(new { success = false, message = "Fee Code is required" });
                }

                if (string.IsNullOrEmpty(model.FeeName))
                {
                    return Json(new { success = false, message = "Fee Name is required" });
                }

                if (model.Amount <= 0)
                {
                    return Json(new { success = false, message = "Amount must be greater than 0" });
                }

                // Check for duplicate FeeCode (excluding current record)
                bool exists = await _context.FeeTypes
                    .AnyAsync(x => x.FeeCode == model.FeeCode
                        && !x.IsDeleted
                        && x.FeeTypeID != model.FeeTypeID);

                if (exists)
                {
                    return Json(new { success = false, message = "Fee Code already exists. Please use a unique code." });
                }

                if (model.FeeTypeID == 0)
                {
                    // Create new record
                    var entity = new FeeType
                    {
                        FeeCode = model.FeeCode.ToUpper(),
                        FeeName = model.FeeName,
                        Amount = model.Amount,
                        Description = model.Description,
                        IsActive = model.IsActive,
                        CreatedDate = DateTime.Now,
                        IsDeleted = false
                    };

                    await _context.FeeTypes.AddAsync(entity);
                }
                else
                {
                    // Update existing record
                    var data = await _context.FeeTypes
                        .FirstOrDefaultAsync(x => x.FeeTypeID == model.FeeTypeID && !x.IsDeleted);

                    if (data == null)
                    {
                        return Json(new { success = false, message = "Record not found" });
                    }

                    data.FeeCode = model.FeeCode.ToUpper();
                    data.FeeName = model.FeeName;
                    data.Amount = model.Amount;
                    data.Description = model.Description;
                    data.IsActive = model.IsActive;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Saved Successfully" });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = "Database error: " + ex.InnerException?.Message ?? ex.Message });
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
                var data = await _context.FeeTypes
                    .FirstOrDefaultAsync(x => x.FeeTypeID == id && !x.IsDeleted);

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