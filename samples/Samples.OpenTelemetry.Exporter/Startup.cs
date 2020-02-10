using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter.Datadog;
using OpenTelemetry.Trace.Configuration;

namespace Samples.OpenTelemetry.Exporter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddOpenTelemetry((sp, builder) =>
                                      {
                                          builder.UseDatadog(telemetryConfiguration =>
                                                             {
                                                                 // this is already the default
                                                                 // telemetryConfiguration.Tracer = Tracer.Instance;
                                                             })
                                                 .AddRequestCollector()
                                                 .AddDependencyCollector();
                                      });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
                             {
                                 endpoints.MapControllerRoute(
                                     name: "default",
                                     pattern: "{controller=Home}/{action=Index}/{id?}");
                             });
        }
    }
}
