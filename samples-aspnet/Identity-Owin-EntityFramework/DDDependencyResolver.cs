using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Async;

namespace Identity_Owin_EntityFramework
{
    public class DDDependencyResolver : IDependencyResolver
    {
        private readonly DDAsyncControllerActionInvoker _actionInvoker = new DDAsyncControllerActionInvoker(new AsyncControllerActionInvoker());
        private readonly IDependencyResolver _fallback;

        public DDDependencyResolver(IDependencyResolver fallback)
        {
            this._fallback = fallback;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAsyncActionInvoker))
            {
                return _actionInvoker;
            }

            return this._fallback.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this._fallback.GetServices(serviceType);
        }
    }
}
