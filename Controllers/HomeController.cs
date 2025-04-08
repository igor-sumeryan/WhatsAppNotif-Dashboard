using Microsoft.AspNetCore.Mvc;
using WhatsAppNotificationDashboard.Services;

namespace WhatsAppNotificationDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseService _databaseService;
        private readonly IConfiguration _configuration;

        public HomeController(DatabaseService databaseService, IConfiguration configuration)
        {
            _databaseService = databaseService;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            ViewBag.RefreshInterval = _configuration["RefreshInterval"];
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = await _databaseService.GetNotificationsAsync();
            return Json(notifications);
        }
    }
}