

using Microsoft.AspNetCore.Mvc;
using SchoolledgerSystem.Models;
using SchoolledgerSystem.Services;

namespace SchoolledgerSystem.Controllers
{
    public class StudentController : Controller
    {
        StudentService _service;

        public StudentController(StudentService service)
        {
            _service = service;
        }

        // PAGE
        public IActionResult Index()
        {
            return View();
        }

        // GET ALL
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Json(data);
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Student student)
        {
            await _service.AddAsync(student);
            return Json(new { success = true });
        }

        // UPDATE
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] Student student)
        {
            await _service.UpdateAsync(student);
            return Json(new { success = true });
        }

        // DELETE
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Json(new { success = true });
        }
    }
}