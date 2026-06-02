using DirectoryMonitor.Models;
using DirectoryMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DirectoryMonitor.Controllers
{
    public class HomeController : Controller
    {
        private readonly DirectoryMonitorService _service;

        public HomeController(DirectoryMonitorService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View(new AnalyzeViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Analyze(AnalyzeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (!Directory.Exists(model.DirectoryPath))
            {
                ModelState.AddModelError(nameof(model.DirectoryPath), "Adresář neexistuje.");
                return View("Index", model);
            }

            var response = await _service.AnalyzeAsync(model.DirectoryPath);

            if (response.ErrorMessage != null)
            {
                ModelState.AddModelError(string.Empty, response.ErrorMessage);
                return View("Index", model);
            }

            model.Result = response.Result;
            ViewBag.FirstRun = response.IsFirstRun;

            return View("Index", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
