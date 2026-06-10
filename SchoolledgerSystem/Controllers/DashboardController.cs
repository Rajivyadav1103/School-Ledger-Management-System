using Microsoft.AspNetCore.Mvc;

namespace SchoolledgerSystem.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
