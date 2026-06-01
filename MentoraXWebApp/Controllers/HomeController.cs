using Microsoft.AspNetCore.Mvc;

namespace MentoraXWebApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (!string.IsNullOrEmpty(role))
            {
                if (role == "Admin")
                    return RedirectToAction("Index", "Admin");
                else if (role == "Tutor")
                    return RedirectToAction("Dashboard", "Tutor");
                else if (role == "Student")
                    return RedirectToAction("Index", "Student");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult TestChat()
        {
            return View();
        }
    }
}