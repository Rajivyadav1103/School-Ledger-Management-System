using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolledgerSystem.DAO;
using SchoolledgerSystem.Models;

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

        // GET ALL PAYMENTS
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _context.FeePayments
                    .Include(x => x.Student)
                        .ThenInclude(s => s.ClassType)
                    .Include(x => x.FeeStructure)
                        .ThenInclude(f => f.FeeType)
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.FeePaymentID)
                    .Select(x => new
                    {
                        x.FeePaymentID,
                        x.StudentID,
                        StudentName = x.Student.StudentName,
                        RollNo = x.Student.RollNo,
                        ClassName = x.Student.ClassType.TypeName,
                        FeeName = x.FeeStructure.FeeType.FeeName,
                        x.PaidAmount,
                        x.InvoiceNo,
                        x.PaymentMethod,
                        x.PaymentDate,
                        x.PaymentStatus
                    })
                    .ToListAsync();

                return Json(data);
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

        // GET STUDENT DETAILS WITH FEE STRUCTURES FOR THEIR CLASS
        [HttpGet]
        public async Task<IActionResult> GetStudentFeeDetails(int studentId)
        {
            try
            {
                // Get student with their class
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

                // Get ALL fee structures for this student's class only
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

                // If no fee structures found
                if (feeStructures == null || !feeStructures.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = $"No fee structures configured for {student.ClassName}. Please contact administrator."
                    });
                }

                // Calculate payment status for each fee
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

                // Get payment history
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
                        x.PaymentDate,
                        x.PaymentMethod,
                        x.PaymentStatus
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
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data" });
                }

                if (model.StudentID <= 0)
                {
                    return Json(new { success = false, message = "Please select a student" });
                }

                if (model.FeeStructureIds == null || !model.FeeStructureIds.Any())
                {
                    return Json(new { success = false, message = "Please select at least one fee" });
                }

                if (model.PaidAmount <= 0)
                {
                    return Json(new { success = false, message = "Paid amount must be greater than 0" });
                }

                if (model.PaidAmount > model.TotalDue)
                {
                    return Json(new { success = false, message = "Paid amount cannot exceed total due amount" });
                }

                // Generate invoice number
                string invoiceNo = model.InvoiceNo;
                if (string.IsNullOrEmpty(invoiceNo))
                {
                    var lastPayment = await _context.FeePayments
                        .OrderByDescending(x => x.FeePaymentID)
                        .FirstOrDefaultAsync();

                    int lastNumber = 0;
                    if (lastPayment != null && !string.IsNullOrEmpty(lastPayment.InvoiceNo))
                    {
                        string[] parts = lastPayment.InvoiceNo.Split('-');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int num))
                        {
                            lastNumber = num;
                        }
                    }
                    invoiceNo = $"INV-{DateTime.Now:yyyyMM}-{lastNumber + 1:D6}";
                }

                // Distribute payment among selected fees
                decimal remainingAmount = model.PaidAmount;
                var payments = new List<FeePayment>();
                var paymentDistribution = new List<object>();

                foreach (var feeId in model.FeeStructureIds)
                {
                    var feeDetail = model.SelectedFees.FirstOrDefault(x => x.FeeStructureID == feeId);
                    if (feeDetail != null && feeDetail.DueAmount > 0)
                    {
                        decimal amountToPay = Math.Min(remainingAmount, feeDetail.DueAmount);
                        if (amountToPay > 0)
                        {
                            var payment = new FeePayment
                            {
                                StudentID = model.StudentID,
                                FeeStructureID = feeId,
                                TotalAmount = feeDetail.Amount,
                                PaidAmount = amountToPay,
                                DueAmount = feeDetail.DueAmount - amountToPay,
                                Discount = 0,
                                Fine = 0,
                                NetAmount = amountToPay,
                                InvoiceNo = invoiceNo,
                                PaymentMethod = model.PaymentMethod,
                                PaymentDate = model.PaymentDate,
                                AcademicYear = feeDetail.AcademicYear,
                                PaymentStatus = (feeDetail.DueAmount - amountToPay) <= 0 ? "Paid" : "Partial",
                                IsDeleted = false,
                                CreatedDate = DateTime.Now
                            };

                            payments.Add(payment);
                            remainingAmount -= amountToPay;

                            paymentDistribution.Add(new
                            {
                                feeName = feeDetail.FeeName,
                                amount = amountToPay
                            });
                        }

                        if (remainingAmount <= 0) break;
                    }
                }

                if (payments.Any())
                {
                    await _context.FeePayments.AddRangeAsync(payments);
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = "Payment processed successfully",
                    invoiceNo = invoiceNo,
                    totalPaid = model.PaidAmount,
                    payments = paymentDistribution
                });
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
                {
                    return Json(new { success = false, message = "Receipt not found" });
                }

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
                    FeeDetails = payments.Select(x => new
                    {
                        FeeName = x.FeeStructure.FeeType.FeeName,
                        Amount = x.PaidAmount,
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
                var tomorrow = today.AddDays(1);

                var payments = await _context.FeePayments
                    .Include(x => x.Student)
                    .Where(x => x.PaymentDate >= today && x.PaymentDate < tomorrow && !x.IsDeleted)
                    .GroupBy(x => x.InvoiceNo)
                    .Select(g => new
                    {
                        InvoiceNo = g.Key,
                        PaymentDate = g.First().PaymentDate,
                        StudentName = g.First().Student.StudentName,
                        RollNo = g.First().Student.RollNo,
                        TotalAmount = g.Sum(x => x.PaidAmount),
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

        // GET ALL PAYMENTS HISTORY
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
                        TotalAmount = g.Sum(x => x.PaidAmount)
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

        // DELETE PAYMENT
        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            try
            {
                var data = await _context.FeePayments
                    .FirstOrDefaultAsync(x => x.FeePaymentID == id && !x.IsDeleted);

                if (data == null)
                {
                    return Json(new { success = false, message = "Record not found" });
                }

                data.IsDeleted = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payment deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }

    // Models for bulk payment
    public class BulkPaymentModel
    {
        public int StudentID { get; set; }
        public List<int> FeeStructureIds { get; set; }
        public List<SelectedFeeModel> SelectedFees { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public string InvoiceNo { get; set; }
        public decimal TotalDue { get; set; }
    }

    public class SelectedFeeModel
    {
        public int FeeStructureID { get; set; }
        public string FeeName { get; set; }
        public decimal Amount { get; set; }
        public decimal DueAmount { get; set; }
        public string AcademicYear { get; set; }
    }
}