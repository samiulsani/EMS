using System.Diagnostics;
using EMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}


        public IActionResult Index()
        {
            // ?. ??? ????? ???? ??? ????
            if (User.Identity.IsAuthenticated)
            {
                // ?. ??? ??? ??? ??? ?????????? ???
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin"); // Admin/Index ? ?????
                }
                else if (User.IsInRole("Student"))
                {
                    return RedirectToAction("Index", "Student"); // Student/Index ? ????? (??? ???? ??????)
                }
                else if (User.IsInRole("Teacher"))
                {
                    return RedirectToAction("Index", "Teacher"); // Teacher/Index ? ????? (??? ???? ??????)
                }
            }

            // ?. ???? ??? ?? ????? ?????? ??????? ?????
            return View();
        }

        public IActionResult Features()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
