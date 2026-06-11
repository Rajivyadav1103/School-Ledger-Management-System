using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolledgerSystem.DAO;
using SchoolledgerSystem.Models;

namespace SchoolledgerSystem.Controllers
{
    public class ClassTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassTypeController(ApplicationDbContext context)
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
                var data = await _context.ClassTypes
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.OrderNo)
                    .ThenBy(x => x.TypeName)
                    .Select(x => new
                    {
                        x.ClassTypeID,
                        x.TypeName,
                        x.Description,
                        x.OrderNo,
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
                var data = await _context.ClassTypes
                    .Where(x => x.ClassTypeID == id && !x.IsDeleted)
                    .Select(x => new
                    {
                        x.ClassTypeID,
                        x.TypeName,
                        x.Description,
                        x.OrderNo,
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
        public async Task<IActionResult> Save([FromBody] ClassType model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data" });
                }

                // Validation
                if (string.IsNullOrWhiteSpace(model.TypeName))
                {
                    return Json(new { success = false, message = "Type Name is required" });
                }

                if (model.OrderNo < 0)
                {
                    return Json(new { success = false, message = "Order number cannot be negative" });
                }

                // Check for duplicate TypeName (excluding current record)
                bool exists = await _context.ClassTypes
                    .AnyAsync(x => x.TypeName.ToLower() == model.TypeName.ToLower()
                        && !x.IsDeleted
                        && x.ClassTypeID != model.ClassTypeID);

                if (exists)
                {
                    return Json(new { success = false, message = "Type Name already exists. Please use a unique name." });
                }

                // Check for duplicate OrderNo (excluding current record)
                bool orderExists = await _context.ClassTypes
                    .AnyAsync(x => x.OrderNo == model.OrderNo
                        && !x.IsDeleted
                        && x.ClassTypeID != model.ClassTypeID);

                if (orderExists)
                {
                    return Json(new { success = false, message = $"Order number {model.OrderNo} is already in use. Please use a different order number." });
                }

                if (model.ClassTypeID == 0)
                {
                    // Create new record
                    var entity = new ClassType
                    {
                        TypeName = model.TypeName.Trim(),
                        Description = model.Description?.Trim(),
                        OrderNo = model.OrderNo,
                        IsActive = model.IsActive,
                        CreatedDate = DateTime.Now,
                        IsDeleted = false
                    };

                    await _context.ClassTypes.AddAsync(entity);
                }
                else
                {
                    // Update existing record
                    var data = await _context.ClassTypes
                        .FirstOrDefaultAsync(x => x.ClassTypeID == model.ClassTypeID && !x.IsDeleted);

                    if (data == null)
                    {
                        return Json(new { success = false, message = "Record not found" });
                    }

                    data.TypeName = model.TypeName.Trim();
                    data.Description = model.Description?.Trim();
                    data.OrderNo = model.OrderNo;
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
                var data = await _context.ClassTypes
                    .FirstOrDefaultAsync(x => x.ClassTypeID == id && !x.IsDeleted);

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

        // BULK UPDATE ORDER NUMBERS (if needed)
        [HttpPost]
        public async Task<IActionResult> Reorder([FromBody] List<ClassType> items)
        {
            try
            {
                foreach (var item in items)
                {
                    var data = await _context.ClassTypes
                        .FirstOrDefaultAsync(x => x.ClassTypeID == item.ClassTypeID && !x.IsDeleted);

                    if (data != null)
                    {
                        data.OrderNo = item.OrderNo;
                        data.UpdatedDate = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Order updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}