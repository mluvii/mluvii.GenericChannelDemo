using Microsoft.AspNetCore.Mvc;

namespace mluvii.GenericChannelDemo.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
