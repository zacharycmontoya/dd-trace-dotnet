using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Datadog.Trace.ClrProfiler;
using Identity_Owin_EntityFramework.Models;

namespace Identity_Owin_EntityFramework.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var prefixes = new[] { "COR_", "CORECLR_", "DD_" };

            var envVars = (from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                           from prefix in prefixes
                           let key = (envVar.Key as string)?.ToUpperInvariant()
                           let value = envVar.Value as string
                           where key.StartsWith(prefix)
                           orderby key
                           select new KeyValuePair<string, string>(key, value))
               .ToList();

            var attached = Instrumentation.ProfilerAttached;

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
