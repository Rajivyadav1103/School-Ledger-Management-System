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
                        PaymentMethod = g.First().PaymentMethod,
                        FeeCount = g.Count(),
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
                decimal totalDueOverall = 0;

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
                    totalDueOverall += dueAmount;
                }

                if (totalDueOverall < 0) totalDueOverall = 0;

                var advancePayments = await _context.FeePayments
                    .Where(x => x.StudentID == studentId && x.FeeStructureID == null && !x.IsDeleted)
                    .SumAsync(x => x.PaidAmount);

                var paymentHistory = await _context.FeePayments
                    .Include(x => x.FeeStructure)
                        .ThenInclude(f => f.FeeType)
                    .Where(x => x.StudentID == studentId && !x.IsDeleted)
                    .OrderByDescending(x => x.PaymentDate)
                    .Select(x => new
                    {
                        x.FeePaymentID,
                        x.InvoiceNo,
                        FeeName = x.FeeStructureID == null ? "Advance Payment" : x.FeeStructure.FeeType.FeeName,
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
                    advanceBalance = advancePayments,
                    paymentHistory = paymentHistory
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // FIXED: SAVE BULK PAYMENT - Saves fee details correctly
        // ============================================================
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

                string invoiceNo = string.IsNullOrEmpty(model.InvoiceNo) ?
                    $"INV-{DateTime.Now:yyyyMMddHHmmss}" : model.InvoiceNo;

                var payments = new List<FeePayment>();
                decimal totalPaid = 0;
                decimal totalDiscountApplied = model.Discount;

                // Separate one-time and monthly fees
                string[] oneTimeFeeNames = { "Admission", "Registration", "Exam", "Development", "Annual" };

                var oneTimeFees = new List<SelectedFeeModel>();
                var monthlyFees = new List<SelectedFeeModel>();

                foreach (var feeId in model.FeeStructureIds)
                {
                    var fee = model.SelectedFees.FirstOrDefault(x => x.FeeStructureID == feeId);
                    if (fee != null)
                    {
                        bool isOneTime = oneTimeFeeNames.Any(f => fee.FeeName.Contains(f, StringComparison.OrdinalIgnoreCase));

                        var totalPaidSoFar = await _context.FeePayments
                            .Where(x => x.StudentID == model.StudentID && x.FeeStructureID == feeId && !x.IsDeleted)
                            .SumAsync(x => x.PaidAmount);

                        var due = fee.Amount - totalPaidSoFar;
                        if (due < 0) due = 0;

                        var feeModel = new SelectedFeeModel
                        {
                            FeeStructureID = feeId,
                            FeeName = fee.FeeName,
                            Amount = fee.Amount,
                            DueAmount = due,
                            AcademicYear = fee.AcademicYear,
                            IsOneTime = isOneTime
                        };

                        if (isOneTime)
                        {
                            oneTimeFees.Add(feeModel);
                        }
                        else
                        {
                            monthlyFees.Add(feeModel);
                        }
                    }
                }

                // Calculate monthly total (monthly fees × months)
                decimal monthlyTotal = monthlyFees.Sum(x => x.DueAmount);
                decimal monthlyTotalWithMonths = monthlyTotal * model.MonthsToPay;

                // One-time fees total (not multiplied)
                decimal oneTimeTotal = oneTimeFees.Sum(x => x.DueAmount);

                // Grand total
                decimal grandTotal = monthlyTotalWithMonths + oneTimeTotal;
                decimal afterDiscount = grandTotal - totalDiscountApplied;
                if (afterDiscount < 0) afterDiscount = 0;

                // Check if paying more than due (advance)
                bool isAdvancePayment = model.PaidAmount > afterDiscount;
                decimal advanceAmount = isAdvancePayment ? (model.PaidAmount - afterDiscount) : 0;
                decimal actualPayment = isAdvancePayment ? afterDiscount : model.PaidAmount;

                // ============================================================
                // Save regular payments for EACH selected fee
                // ============================================================
                decimal remaining = actualPayment;

                // Save monthly fees (multiplied by months)
                if (monthlyFees.Any())
                {
                    decimal totalMonthlyDue = monthlyFees.Sum(x => x.DueAmount);

                    foreach (var fee in monthlyFees)
                    {
                        if (fee.DueAmount > 0 && remaining > 0)
                        {
                            decimal monthlyDue = fee.DueAmount;
                            decimal totalAmount = monthlyDue * model.MonthsToPay;
                            decimal amountToPay = Math.Min(remaining, totalAmount);

                            if (amountToPay > 0.01m)
                            {
                                decimal discountPortion = totalMonthlyDue > 0 ?
                                    (totalDiscountApplied * (fee.DueAmount / totalMonthlyDue)) : 0;

                                var payment = new FeePayment
                                {
                                    StudentID = model.StudentID,
                                    FeeStructureID = fee.FeeStructureID,
                                    TotalAmount = totalAmount,
                                    PaidAmount = amountToPay,
                                    DueAmount = totalAmount - amountToPay,
                                    Discount = discountPortion,
                                    Fine = 0,
                                    NetAmount = amountToPay,
                                    InvoiceNo = invoiceNo,
                                    PaymentMethod = model.PaymentMethod,
                                    PaymentDate = model.PaymentDate,
                                    AcademicYear = fee.AcademicYear,
                                    PaymentStatus = (totalAmount - amountToPay) <= 0 ? "Paid" : "Partial",
                                    IsDeleted = false,
                                    CreatedDate = DateTime.Now
                                };

                                payments.Add(payment);
                                remaining -= amountToPay;
                                totalPaid += amountToPay;
                            }
                        }
                    }
                }

                // Save one-time fees (not multiplied)
                if (oneTimeFees.Any())
                {
                    foreach (var fee in oneTimeFees)
                    {
                        if (fee.DueAmount > 0 && remaining > 0)
                        {
                            decimal amountToPay = Math.Min(remaining, fee.DueAmount);
                            if (amountToPay > 0.01m)
                            {
                                var payment = new FeePayment
                                {
                                    StudentID = model.StudentID,
                                    FeeStructureID = fee.FeeStructureID,
                                    TotalAmount = fee.Amount,
                                    PaidAmount = amountToPay,
                                    DueAmount = fee.DueAmount - amountToPay,
                                    Discount = 0,
                                    Fine = 0,
                                    NetAmount = amountToPay,
                                    InvoiceNo = invoiceNo,
                                    PaymentMethod = model.PaymentMethod,
                                    PaymentDate = model.PaymentDate,
                                    AcademicYear = fee.AcademicYear,
                                    PaymentStatus = (fee.DueAmount - amountToPay) <= 0 ? "Paid" : "Partial",
                                    IsDeleted = false,
                                    CreatedDate = DateTime.Now
                                };

                                payments.Add(payment);
                                remaining -= amountToPay;
                                totalPaid += amountToPay;
                            }
                        }
                    }
                }

                // Save advance payment if any
                if (advanceAmount > 0)
                {
                    var advancePayment = new FeePayment
                    {
                        StudentID = model.StudentID,
                        FeeStructureID = null,
                        TotalAmount = advanceAmount,
                        PaidAmount = advanceAmount,
                        DueAmount = 0,
                        Discount = 0,
                        Fine = 0,
                        NetAmount = advanceAmount,
                        InvoiceNo = invoiceNo,
                        PaymentMethod = model.PaymentMethod,
                        PaymentDate = model.PaymentDate,
                        AcademicYear = DateTime.Now.Year + "-" + (DateTime.Now.Year + 1),
                        PaymentStatus = "Advance",
                        IsDeleted = false,
                        CreatedDate = DateTime.Now
                    };
                    payments.Add(advancePayment);
                    totalPaid += advanceAmount;
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
                    totalPaid = totalPaid,
                    isAdvance = advanceAmount > 0,
                    advanceAmount = advanceAmount,
                    dueAfter = afterDiscount - actualPayment,
                    totalPayments = payments.Count,
                    discountApplied = totalDiscountApplied,
                    oneTimeTotal = oneTimeTotal,
                    monthlyTotal = monthlyTotal,
                    monthlyTotalWithMonths = monthlyTotalWithMonths,
                    monthsPaid = model.MonthsToPay
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
                var payments = await _context.FeePayments
                    .Where(x => x.InvoiceNo == model.InvoiceNo && !x.IsDeleted)
                    .ToListAsync();

                if (payments == null || !payments.Any())
                    return Json(new { success = false, message = "Payment not found" });

                var regularPayments = payments.Where(x => x.FeeStructureID != null).ToList();
                var advancePayment = payments.FirstOrDefault(x => x.FeeStructureID == null);

                if (regularPayments.Any())
                {
                    foreach (var payment in regularPayments)
                    {
                        payment.PaidAmount = model.PaidAmount / regularPayments.Count;
                        payment.Discount = model.Discount / regularPayments.Count;
                        payment.PaymentMethod = model.PaymentMethod;
                        payment.NetAmount = model.PaidAmount / regularPayments.Count;
                        payment.DueAmount = payment.TotalAmount - payment.PaidAmount;
                        payment.PaymentStatus = payment.DueAmount <= 0 ? "Paid" : "Partial";
                    }
                }

                if (advancePayment != null)
                {
                    advancePayment.PaymentMethod = model.PaymentMethod;
                }

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
        public async Task<IActionResult> DeletePayment([FromBody] string invoiceNo)
        {
            try
            {
                var payments = await _context.FeePayments
                    .Where(x => x.InvoiceNo == invoiceNo && !x.IsDeleted)
                    .ToListAsync();

                if (payments == null || !payments.Any())
                    return Json(new { success = false, message = "Payment not found" });

                foreach (var payment in payments)
                {
                    payment.IsDeleted = true;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Payment deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // FIXED: GET RECEIPT BY INVOICE - Properly loads FeeStructure and FeeType
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetReceiptByInvoice(string invoiceNo)
        {
            try
            {
                // Get all payments for this invoice with proper includes
                var payments = await _context.FeePayments
                    .Include(x => x.Student)
                        .ThenInclude(s => s.ClassType)
                    .Include(x => x.FeeStructure)
                        .ThenInclude(f => f.FeeType) // CRITICAL: Load FeeType to get FeeName
                    .Where(x => x.InvoiceNo == invoiceNo && !x.IsDeleted)
                    .ToListAsync();

                if (payments == null || !payments.Any())
                    return Json(new { success = false, message = "Receipt not found" });

                var firstPayment = payments.First();

                // Separate regular and advance payments
                var regularPayments = payments.Where(x => x.FeeStructureID != null).ToList();
                var advancePayments = payments.Where(x => x.FeeStructureID == null).ToList();

                // Calculate amounts
                var totalFeeAmount = regularPayments.Sum(x => x.TotalAmount);
                var totalPaidAmount = regularPayments.Sum(x => x.PaidAmount);
                var totalDiscountAmount = regularPayments.Sum(x => x.Discount);
                var advanceAmount = advancePayments.Sum(x => x.PaidAmount);
                var totalDue = totalFeeAmount - totalPaidAmount - advanceAmount;
                if (totalDue < 0) totalDue = 0;

                // Calculate months paid
                int monthsPaid = 1;
                if (regularPayments.Any())
                {
                    var groupedByFee = regularPayments.GroupBy(x => x.FeeStructureID);
                    if (groupedByFee.Any())
                    {
                        monthsPaid = groupedByFee.First().Count();
                    }
                }

                var nepaliDate = GetNepaliDateString(firstPayment.PaymentDate);

                // ============================================================
                // Build fee details from regular payments
                // ============================================================
                var feeDetailsList = new List<object>();

                foreach (var payment in regularPayments)
                {
                    string feeName = "Unknown Fee";
                    if (payment.FeeStructure != null && payment.FeeStructure.FeeType != null)
                    {
                        feeName = payment.FeeStructure.FeeType.FeeName;
                    }

                    // Determine if this is a one-time fee
                    bool isOneTime = false;
                    string[] oneTimeFeeNames = { "Admission", "Registration", "Exam", "Development", "Annual" };
                    foreach (var name in oneTimeFeeNames)
                    {
                        if (feeName.Contains(name, StringComparison.OrdinalIgnoreCase))
                        {
                            isOneTime = true;
                            break;
                        }
                    }

                    feeDetailsList.Add(new
                    {
                        FeeName = feeName,
                        TotalAmount = payment.TotalAmount,
                        PaidAmount = payment.PaidAmount,
                        Discount = payment.Discount,
                        Month = isOneTime ? "One-Time" : $"{monthsPaid} months",
                        Status = payment.PaymentStatus,
                        DueAfter = payment.DueAmount,
                        IsOneTime = isOneTime,
                        Count = monthsPaid
                    });
                }

                // Group fees by FeeStructureID to combine multiple months (if needed)
                var groupedFeeDetails = new List<object>();
                var grouped = regularPayments.GroupBy(x => x.FeeStructureID);

                foreach (var group in grouped)
                {
                    var first = group.First();
                    string feeName = "Unknown Fee";
                    if (first.FeeStructure != null && first.FeeStructure.FeeType != null)
                    {
                        feeName = first.FeeStructure.FeeType.FeeName;
                    }

                    bool isOneTime = false;
                    string[] oneTimeFeeNames = { "Admission", "Registration", "Exam", "Development", "Annual" };
                    foreach (var name in oneTimeFeeNames)
                    {
                        if (feeName.Contains(name, StringComparison.OrdinalIgnoreCase))
                        {
                            isOneTime = true;
                            break;
                        }
                    }

                    groupedFeeDetails.Add(new
                    {
                        FeeName = feeName,
                        TotalAmount = group.Sum(x => x.TotalAmount),
                        PaidAmount = group.Sum(x => x.PaidAmount),
                        Discount = group.Sum(x => x.Discount),
                        Month = isOneTime ? "One-Time" : $"{group.Count()} months",
                        Status = group.Sum(x => x.DueAmount) <= 0 ? "Paid" : "Partial",
                        DueAfter = group.Sum(x => x.DueAmount),
                        IsOneTime = isOneTime,
                        Count = group.Count()
                    });
                }

                var receipt = new
                {
                    InvoiceNo = invoiceNo,
                    PaymentDate = firstPayment.PaymentDate,
                    NepaliDate = nepaliDate,
                    StudentName = firstPayment.Student?.StudentName ?? "N/A",
                    RollNo = firstPayment.Student?.RollNo ?? "N/A",
                    ClassName = firstPayment.Student?.ClassType?.TypeName ?? "N/A",
                    FatherName = firstPayment.Student?.FatherName ?? "N/A",
                    PaymentMethod = firstPayment.PaymentMethod,
                    AcademicYear = firstPayment.AcademicYear,

                    // Amounts
                    TotalFee = totalFeeAmount,
                    TotalPaid = totalPaidAmount + advanceAmount,
                    TotalDiscount = totalDiscountAmount,
                    AdvanceAmount = advanceAmount,
                    DueAmount = totalDue,
                    MonthsPaid = monthsPaid,

                    PaymentType = advanceAmount > 0 ? "Advance" : "Normal",
                    IsFullPaid = totalDue <= 0,

                    // Use grouped fee details
                    FeeDetails = groupedFeeDetails.Any() ? groupedFeeDetails : feeDetailsList
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

        // GET NEPALI DATE
        [HttpGet]
        public IActionResult GetNepaliDate()
        {
            try
            {
                var nepaliDate = GetNepaliDateString(DateTime.Now);
                return Json(new { success = true, date = nepaliDate });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetNepaliDateString(DateTime englishDate)
        {
            string[] nepaliMonths = new string[]
            {
                "बैशाख", "जेठ", "असार", "साउन", "भदौ", "असोज",
                "कार्तिक", "मंसिर", "पुस", "माघ", "फाल्गुन", "चैत"
            };

            string[] nepaliWeekdays = new string[]
            {
                "आइतबार", "सोमबार", "मंगलबार", "बुधबार", "बिहिबार", "शुक्रबार", "शनिबार"
            };

            int day = englishDate.Day;
            int month = englishDate.Month;
            int year = englishDate.Year;

            int nepaliYear = year + 56;
            int nepaliMonth = month + 8;
            int nepaliDay = day + 15;

            if (nepaliMonth > 12)
            {
                nepaliMonth -= 12;
                nepaliYear += 1;
            }

            if (nepaliDay > 30)
            {
                nepaliDay -= 30;
                nepaliMonth += 1;
                if (nepaliMonth > 12)
                {
                    nepaliMonth = 1;
                    nepaliYear += 1;
                }
            }

            string weekday = nepaliWeekdays[(int)englishDate.DayOfWeek];
            string monthName = nepaliMonths[nepaliMonth - 1];

            return $"{weekday}, {nepaliDay} {monthName} {nepaliYear}";
        }
    }

    // Models
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
        public bool IsOneTime { get; set; }
    }

    public class EditPaymentModel
    {
        public string InvoiceNo { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Discount { get; set; }
        public string PaymentMethod { get; set; }
    }
}