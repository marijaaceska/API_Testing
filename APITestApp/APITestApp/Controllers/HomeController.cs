using Microsoft.AspNetCore.Mvc;

namespace APITestApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Home"; 
            return View();
        }

        public IActionResult Guide()
        {
            return View(); 
        }
    }
}
