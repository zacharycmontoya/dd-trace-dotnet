using System;
using System.Web.Mvc;
using System.Web.Mvc.Async;

namespace Identity_Owin_EntityFramework
{
    public class DDAsyncControllerActionInvoker : IAsyncActionInvoker
    {
        private readonly IAsyncActionInvoker _actionInvoker;

        public DDAsyncControllerActionInvoker(IAsyncActionInvoker actionInvoker)
        {
            _actionInvoker = actionInvoker;
        }

        public bool InvokeAction(ControllerContext controllerContext, string actionName)
        {
            return _actionInvoker.InvokeAction(controllerContext, actionName);
        }

        public IAsyncResult BeginInvokeAction(ControllerContext controllerContext, string actionName, AsyncCallback callback, object state)
        {
            return _actionInvoker.BeginInvokeAction(controllerContext, actionName, callback, state);
        }

        public bool EndInvokeAction(IAsyncResult asyncResult)
        {
            return _actionInvoker.EndInvokeAction(asyncResult);
        }
    }
}
