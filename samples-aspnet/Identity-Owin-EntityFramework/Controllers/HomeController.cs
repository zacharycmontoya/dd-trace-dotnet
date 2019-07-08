using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Datadog.Trace.ClrProfiler;
using Identity_Owin_EntityFramework.Models;

namespace Identity_Owin_EntityFramework.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public async Task<ActionResult> UsersAsync()
        {
            var attached = Instrumentation.ProfilerAttached;

            var context = new ApplicationDbContext();
            var users = await context.Users.ToListAsync();

            return View("About");
        }
    }
}
