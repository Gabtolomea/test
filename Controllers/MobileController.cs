// Controllers/MobileController.cs
// Serves the mobile app at /Mobile
using Microsoft.AspNetCore.Mvc;

namespace OnlineClearance.Controllers
{
    public class MobileController : Controller
    {
        // GET /Mobile  → serves wwwroot/mobile/index.html
        public IActionResult Index()
        {
            return PhysicalFile(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "mobile", "index.html"),
                "text/html");
        }
    }
}
