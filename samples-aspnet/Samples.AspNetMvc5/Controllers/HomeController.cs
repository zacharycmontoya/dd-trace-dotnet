using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using Samples.AspNetMvc5.Models;

namespace Samples.AspNetMvc5.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Type tracerType = Type.GetType("Datadog.Trace.Tracer, Datadog.Trace", throwOnError: false);
            Type instrumentationType = Type.GetType("Datadog.Trace.ClrProfiler.Instrumentation, Datadog.Trace.ClrProfiler.Managed", throwOnError:false);

            if (tracerType == null)
            {
                ViewBag.TracerAssemblyLocation = "Type \"Datadog.Trace.Tracer\" not loaded";
            }
            else
            {
                ViewBag.TracerAssemblyLocation = tracerType.Assembly.Location;

                PropertyInfo property = instrumentationType.GetProperty("ProfilerAttached", BindingFlags.Static | BindingFlags.Public);
                ViewBag.ProfilerAttached = property.GetValue(null)?.ToString() ?? "(null)";
            }

            if (instrumentationType == null)
            {
                ViewBag.ClrProfilerAssemblyLocation = "Type \"Datadog.Trace.ClrProfiler.Instrumentation\" not loaded";
            }
            else
            {
                ViewBag.ClrProfilerAssemblyLocation = instrumentationType.Assembly.Location;
            }

            string[] moduleNames = this.HttpContext.ApplicationInstance.Modules.AllKeys;

            var prefixes = new[] { "COR_", "CORECLR_", "DD_", "DATADOG_" };

            var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                          from prefix in prefixes
                          let key = (envVar.Key as string)?.ToUpperInvariant()
                          let value = envVar.Value as string
                          where key.StartsWith(prefix)
                          orderby key
                          select new KeyValuePair<string, string>(key, value);

            return View(new HomeModel
                        {
                            HttpModules = moduleNames,
                            EnvVars = envVars
                        });
        }

        [Route("delay/{seconds}")]
        public ActionResult Delay(int seconds)
        {
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            return View(seconds);
        }

        [Route("delay-async/{seconds}")]
        public async Task<ActionResult> DelayAsync(int seconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            return View("Delay", seconds);
        }
    }
}
