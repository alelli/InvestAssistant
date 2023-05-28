using Microsoft.AspNetCore.Mvc;

namespace Invest.Controllers
{
    public class RecomendController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
