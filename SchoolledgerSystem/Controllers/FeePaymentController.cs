using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolledgerSystem.DAO;
using SchoolledgerSystem.Models;
using System.Text;

namespace SchoolledgerSystem.Controllers
{
    public class FeePaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeePaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET ALL PAYMENTS (Grouped by Invoice for History)
        [HttpGet]
        public async Task<IActionResult> GetAllPayments()
        {
            try
            {
                var payments = await _context.FeePayments
                    .Include(x => x.Student)
                        .ThenInclude(s => s.ClassType)
                    .Where(x => !x.IsDeleted)
                    .GroupBy(x => x.InvoiceNo)
                    .Select(g => new
                    {
                        InvoiceNo = g.Key,
                        PaymentDate = g.First().PaymentDate,
                        StudentName = g.First().Student.StudentName,
                        RollNo = g.First().Student.RollNo,
                        ClassName = g.First().Student.ClassType.TypeName,
                        TotalAmount = g.Sum(x => x.PaidAmount),
                        Discount = g.First().Discount,
                        DueAfter = g.First().DueAmount
                    })
                    .OrderByDescending(x => x.PaymentDate)
                    .ToListAsync();

                return Json(payments);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // SEARCH STUDENTS
        [HttpGet]
        public async Task<IActionResult> SearchStudents(string searchTerm)
        {
            try
            {
                var query = _context.Students
                    .Include(x => x.ClassType)
                    .Where(x => !x.IsDeleted && x.IsActive);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(x =>
                        x.StudentName.Contains(searchTerm) ||
                        (x.RollNo != null && x.RollNo.Contains(searchTerm)) ||
                        (x.FatherName != null && x.FatherName.Contains(searchTerm))
                    );
                }

                var students = await query
                    .OrderBy(x => x.StudentName)
                    .Select(x => new
                    {
                        x.StudentID,
                        x.StudentName,
                        x.RollNo,
                        x.FatherName,
                        ClassName = x.ClassType.TypeName,
                        ClassTypeID = x.ClassTypeID
                    })
                    .Take(50)
                    .ToListAsync();

                return Json(students);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET STUDENT DETAILS WITH FEE STRUCTURES
        [HttpGet]
        public async Task<IActionResult> GetStudentFeeDetails(int studentId)
        {
            try
            {
                var student = await _context.Students
                    .Include(x => x.ClassType)
                    .Where(x => x.StudentID == studentId && !x.IsDeleted)
                    .Select(x => new
                    {
                        x.StudentID,
                        x.StudentName,
                        x.RollNo,
                        x.FatherName,
                        x.MotherName,
                        x.ContactNo,
                        ClassName = x.ClassType.TypeName,
                        x.ClassTypeID
                    })
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                var feeStructures = await _context.FeeStructures
                    .Include(x => x.FeeType)
                    .Where(x => x.ClassTypeID == student.ClassTypeID && !x.IsDeleted && x.IsActive && x.Amount > 0)
                    .Select(x => new
                    {
                        x.FeeStructureID,
                        FeeName = x.FeeType.FeeName,
                        FeeCode = x.FeeType.FeeCode,
                        x.Amount,
                        x.AcademicYear,
                        x.Description
                    })
                    .ToListAsync();

                if (feeStructures == null || !feeStructures.Any())
                {
                    return Json(new { success = false, message = $"No fee structures configured for {student.ClassName}" });
                }

                var feesWithStatus = new List<object>();
                decimal totalFeeAmount = 0;
                decimal totalPaidOverall = 0;

                foreach (var fee in feeStructures)
                {
                    var totalPaid = await _context.FeePayments
                        .Where(x => x.StudentID == studentId && x.FeeStructureID == fee.FeeStructureID && !x.IsDeleted)
                        .SumAsync(x => x.PaidAmount);

                    var dueAmount = fee.Amount - totalPaid;
                    if (dueAmount < 0) dueAmount = 0;

                    feesWithStatus.Add(new
                    {
                        fee.FeeStructureID,
                        fee.FeeName,
                        fee.Amount,
                        totalPaid = totalPaid,
                        dueAmount = dueAmount,
                        isFullyPaid = dueAmount <= 0,
                        academicYear = fee.AcademicYear
                    });

                    totalFeeAmount += fee.Amount;
                    totalPaidOverall += totalPaid;
                }

                var totalDueOverall = totalFeeAmount - totalPaidOverall;
                if (totalDueOverall < 0) totalDueOverall = 0;

                var paymentHistory = await _context.FeePayments
                    .Include(x => x.FeeStructure)
                        .ThenInclude(f => f.FeeType)
                    .Where(x => x.StudentID == studentId && !x.IsDeleted)
                    .OrderByDescending(x => x.PaymentDate)
                    .Select(x => new
                    {
                        x.FeePaymentID,
                        x.InvoiceNo,
                        FeeName = x.FeeStructure.FeeType.FeeName,
                        x.PaidAmount,
                        x.Discount,
                        x.PaymentDate,
                        x.PaymentMethod,
                        x.PaymentStatus,
                        x.DueAmount
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    student = student,
                    feeStructures = feesWithStatus,
                    totalFeeAmount = totalFeeAmount,
                    totalPaidOverall = totalPaidOverall,
                    totalDueOverall = totalDueOverall,
                    paymentHistory = paymentHistory
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // SAVE BULK PAYMENT
        [HttpPost]
        public async Task<IActionResult> SaveBulkPayment([FromBody] BulkPaymentModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (model == null || model.StudentID <= 0)
                    return Json(new { success = false, message = "Invalid data" });

                if (model.FeeStructureIds == null || !model.FeeStructureIds.Any())
                    return Json(new { success = false, message = "Please select at least one fee" });

                if (model.PaidAmount <= 0 && model.AdvanceAmount <= 0)
                    return Json(new { success = false, message = "Please enter payment amount" });

                // Generate invoice number
                string invoiceNo = string.IsNullOrEmpty(model.InvoiceNo) ?
                    $"INV-{DateTime.Now:yyyyMMddHHmmss}" : model.InvoiceNo;

                var payments = new List<FeePayment>();
                decimal totalPaid = 0;
                int monthsToPay = model.MonthsToPay > 0 ? model.MonthsToPay : 1;
                decimal monthlyAmount = model.PaidAmount / monthsToPay;
                decimal remainingAmount = model.PaidAmount;

                // Handle Advance Payment
                if (model.AdvanceAmount > 0)
                {
                    var advancePayment = new FeePayment
                    {
                        StudentID = model.StudentID,
                        FeeStructureID = 0,
                        TotalAmount = model.AdvanceAmount,
                        PaidAmount = model.AdvanceAmount,
                        DueAmount = 0,
                        Discount = 0,
                        Fine = 0,
                        NetAmount = model.AdvanceAmount,
                        InvoiceNo = invoiceNo,
                        PaymentMethod = model.PaymentMethod,
                        PaymentDate = model.PaymentDate,
                        AcademicYear = DateTime.Now.Year + "-" + (DateTime.Now.Year + 1),
                        PaymentStatus = "Advance",
                        IsDeleted = false,
                        CreatedDate = DateTime.Now
                    };
                    payments.Add(advancePayment);
                    totalPaid += model.AdvanceAmount;
                }

                // Handle Normal/Monthly Payments
                foreach (var feeId in model.FeeStructureIds)
                {
                    var feeDetail = model.SelectedFees.FirstOrDefault(x => x.FeeStructureID == feeId);
                    if (feeDetail != null && feeDetail.DueAmount > 0)
                    {
                        for (int month = 1; month <= monthsToPay; month++)
                        {
                            decimal amountToPay = Math.Min(remainingAmount, monthlyAmount);
                            if (amountToPay > 0.01m) // Only add if amount > 0
                            {
                                var payment = new FeePayment
                                {
                                    StudentID = model.StudentID,
                                    FeeStructureID = feeId,
                                    TotalAmount = feeDetail.Amount,
                                    PaidAmount = amountToPay,
                                    DueAmount = feeDetail.DueAmount - amountToPay,
                                    Discount = model.Discount / monthsToPay,
                                    Fine = 0,
                                    NetAmount = amountToPay,
                                    InvoiceNo = invoiceNo,
                                    PaymentMethod = model.PaymentMethod,
                                    PaymentDate = model.PaymentDate.AddMonths(month - 1),
                                    AcademicYear = feeDetail.AcademicYear,
                                    PaymentStatus = "Paid",
                                    IsDeleted = false,
                                    CreatedDate = DateTime.Now
                                };

                                payments.Add(payment);
                                remainingAmount -= amountToPay;
                                totalPaid += amountToPay;
                            }
                            if (remainingAmount <= 0.01m) break;
                        }
                    }
                    if (remainingAmount <= 0.01m) break;
                }

                if (payments.Any())
                {
                    await _context.FeePayments.AddRangeAsync(payments);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = "Payment processed successfully",
                    invoiceNo = invoiceNo,
                    totalPaid = totalPaid
                });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Database error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // EDIT PAYMENT
        [HttpPost]
        public async Task<IActionResult> EditPayment([FromBody] EditPaymentModel model)
        {
            try
            {
                var payment = await _context.FeePayments
                    .FirstOrDefaultAsync(x => x.FeePaymentID == model.FeePaymentID && !x.IsDeleted);

                if (payment == null)
                    return Json(new { success = false, message = "Payment not found" });

                // Update payment details
                payment.PaidAmount = model.PaidAmount;
                payment.Discount = model.Discount;
                payment.PaymentMethod = model.PaymentMethod;
                payment.NetAmount = model.PaidAmount;
                payment.DueAmount = payment.TotalAmount - model.PaidAmount;
                payment.PaymentStatus = payment.DueAmount <= 0 ? "Paid" : "Partial";
                payment.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Payment updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // DELETE PAYMENT
        [HttpPost]
        public async Task<IActionResult> DeletePayment([FromBody] int id)
        {
            try
            {
                var payment = await _context.FeePayments
                    .FirstOrDefaultAsync(x => x.FeePaymentID == id && !x.IsDeleted);

                if (payment == null)
                    return Json(new { success = false, message = "Payment not found" });

                payment.IsDeleted = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Payment deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET RECEIPT BY INVOICE NUMBER
        [HttpGet]
        public async Task<IActionResult> GetReceiptByInvoice(string invoiceNo)
        {
            try
            {
                var payments = await _context.FeePayments
                    .Include(x => x.Student)
                        .ThenInclude(s => s.ClassType)
                    .Include(x => x.FeeStructure)
                        .ThenInclude(f => f.FeeType)
                    .Where(x => x.InvoiceNo == invoiceNo && !x.IsDeleted)
                    .ToListAsync();

                if (payments == null || !payments.Any())
                    return Json(new { success = false, message = "Receipt not found" });

                var firstPayment = payments.First();
                var receipt = new
                {
                    InvoiceNo = invoiceNo,
                    PaymentDate = firstPayment.PaymentDate,
                    StudentName = firstPayment.Student.StudentName,
                    RollNo = firstPayment.Student.RollNo,
                    ClassName = firstPayment.Student.ClassType.TypeName,
                    FatherName = firstPayment.Student.FatherName,
                    PaymentMethod = firstPayment.PaymentMethod,
                    AcademicYear = firstPayment.AcademicYear,
                    TotalPaid = payments.Sum(x => x.PaidAmount),
                    TotalDiscount = payments.Sum(x => x.Discount),
                    AdvanceAmount = payments.Where(x => x.FeeStructureID == 0).Sum(x => x.PaidAmount),
                    PaymentType = payments.Any(x => x.FeeStructureID == 0) ? "Advance" : "Normal",
                    MonthsPaid = payments.Count(x => x.FeeStructureID != 0),
                    FeeDetails = payments.Where(x => x.FeeStructureID != 0).Select(x => new
                    {
                        FeeName = x.FeeStructure.FeeType.FeeName,
                        Amount = x.PaidAmount,
                        Month = x.PaymentDate.ToString("MMMM yyyy"),
                        Discount = x.Discount,
                        Status = x.PaymentStatus
                    }).ToList()
                };

                return Json(receipt);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET TODAY'S PAYMENTS
        [HttpGet]
        public async Task<IActionResult> GetTodayPayments()
        {
            try
            {
                var today = DateTime.Today;
                var payments = await _context.FeePayments
                    .Include(x => x.Student)
                    .Where(x => x.PaymentDate.Date == today && !x.IsDeleted)
                    .GroupBy(x => x.InvoiceNo)
                    .Select(g => new
                    {
                        InvoiceNo = g.Key,
                        PaymentDate = g.First().PaymentDate,
                        StudentName = g.First().Student.StudentName,
                        RollNo = g.First().Student.RollNo,
                        TotalAmount = g.Sum(x => x.PaidAmount),
                        Discount = g.Sum(x => x.Discount),
                        PaymentMethod = g.First().PaymentMethod
                    })
                    .ToListAsync();

                return Json(payments);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class BulkPaymentModel
    {
        public int StudentID { get; set; }
        public List<int> FeeStructureIds { get; set; }
        public List<SelectedFeeModel> SelectedFees { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal AdvanceAmount { get; set; }
        public int MonthsToPay { get; set; }
        public string PaymentType { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public string InvoiceNo { get; set; }
        public decimal TotalDue { get; set; }
        public decimal AfterDiscount { get; set; }
    }

    public class SelectedFeeModel
    {
        public int FeeStructureID { get; set; }
        public string FeeName { get; set; }
        public decimal Amount { get; set; }
        public decimal DueAmount { get; set; }
        public string AcademicYear { get; set; }
    }

    public class EditPaymentModel
    {
        public int FeePaymentID { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Discount { get; set; }
        public string PaymentMethod { get; set; }
    }
}