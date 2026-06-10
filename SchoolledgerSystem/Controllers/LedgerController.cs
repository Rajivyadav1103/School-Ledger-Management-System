using Microsoft.AspNetCore.Mvc;

namespace SchoolledgerSystem.Controllers
{
    public class LedgerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
