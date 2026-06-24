using Microsoft.AspNetCore.Mvc;

namespace SchoolledgerSystem.Controllers
{
    public class BaseController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
