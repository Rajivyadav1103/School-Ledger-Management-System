using Microsoft.AspNetCore.Mvc;

namespace SchoolledgerSystem.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
