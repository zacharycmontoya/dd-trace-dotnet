using Datadog.Trace;
using Datadog.Trace.Configuration;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCore31.NoTraces
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var settings = TracerSettings.FromDefaultSources();
            Tracer.Instance = new Tracer(settings);
            services.AddSingleton(Tracer.Instance);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/test"),
                        appBuilder =>
                        {
                            appBuilder.UseMiddleware<CustomMiddleware>();
                        }
            );

            app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync($"Request.Path = \"{context.Request.Path}\"");
                    });
        }
    }

    public class CustomMiddleware
    {
        private readonly RequestDelegate _nextDelegate;

        public CustomMiddleware(RequestDelegate nextDelegate)
        {
            _nextDelegate = nextDelegate;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Console.WriteLine("CustomMiddlewareNoInterface.InvokeAsync()");
            await context.Response.WriteAsync($"Request.Path = \"{context.Request.Path}\" (detected /test)");
        }
    }
}
