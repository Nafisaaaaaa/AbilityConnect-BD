using Microsoft.AspNetCore.Mvc;

namespace SDP1.Models
{
    public class VolunteerModels : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
