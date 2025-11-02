using Microsoft.Extensions.Logging;
using Serilog;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace FrameworkLogging
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            GlobalConfiguration.Configuration.DependencyResolver =
                GetDependencyResolver();
        }

        protected void Application_End()
        {
            Log.CloseAndFlush();
        }

        private System.Web.Http.Dependencies.IDependencyResolver GetDependencyResolver()
        {
            var container = new Container();

            container.RegisterWebApiControllers(GlobalConfiguration.Configuration);

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateLogger();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(Log.Logger, dispose: true);
            });

            container.RegisterInstance(loggerFactory);
            container.Register(typeof(ILogger<>), typeof(Logger<>), Lifestyle.Singleton);

            container.Verify();

            return new SimpleInjectorWebApiDependencyResolver(container);
        }
    }
}
