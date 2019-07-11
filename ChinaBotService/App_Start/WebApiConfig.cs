using ChinaBotService.Services;
using System;
using System.Web.Http;
using Unity;
using Unity.AspNet.WebApi;

namespace ChinaBotService
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Formatters.JsonFormatter.MediaTypeMappings
                .Add(new System.Net.Http.Formatting.RequestHeaderMapping("Accept",
                              "text/html",
                              StringComparison.InvariantCultureIgnoreCase,
                              true,
                              "application/json"));

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            RegisterContainer(config);
        }

        private static void RegisterContainer(HttpConfiguration config)
        {
            var container = new UnityContainer();

            container.RegisterType<ISearchService, SearchService>();

            config.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}
