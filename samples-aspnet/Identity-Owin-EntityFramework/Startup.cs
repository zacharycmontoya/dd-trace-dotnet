using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Identity_Owin_EntityFramework.Startup))]
namespace Identity_Owin_EntityFramework
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
