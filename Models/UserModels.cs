using Microsoft.AspNetCore.Mvc;

namespace SDP1.Models
{
    public class UserModels : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
