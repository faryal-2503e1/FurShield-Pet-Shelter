using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FurShield.Controllers
{
    // Static "Access Care Options" content: categorized articles/tips + FAQs,
    // as required by the SRS. Content is fixed in the view (no DB needed).
    [Authorize(Roles = "PetOwner")]
    public class CareController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
