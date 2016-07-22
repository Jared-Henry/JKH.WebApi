using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;

//See the following blog posts:
//http://www.asp.net/web-api/overview/releases/whats-new-in-aspnet-web-api-22
//http://stackoverflow.com/questions/19989023/net-webapi-attribute-routing-and-inheritance

namespace JKH.WebApi
{
    public class AllowInheritanceDirectRouteProvider : DefaultDirectRouteProvider
    {
        protected override IReadOnlyList<IDirectRouteFactory> GetActionRouteFactories(HttpActionDescriptor actionDescriptor)
        {
            // inherit route attributes decorated on base class controller's actions
            return actionDescriptor.GetCustomAttributes<IDirectRouteFactory>(inherit: true);
        }
    }
}
