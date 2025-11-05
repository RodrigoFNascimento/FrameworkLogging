# FrameworkLogging

This project is a **minimal example** of how to use **Microsoft's `ILogger<T>`** logging abstraction in a **.NET Framework 4.8.1 Web API**, and how to integrate **Serilog** as the actual logging backend while keeping the `ILogger<T>` interface.  

The project was created using **Visual Studio's .NET Framework Web API template**, with minimal changes — only what is necessary to demonstrate logging. The goal is **clarity**, showing exactly what you need to set up logging in this kind of project.

All logs in this example are sent to the **Visual Studio Debug window** for simplicity.


## Why this approach?

### **`ILogger<T>` abstraction**  
  Using Microsoft’s `ILogger<T>` in your code provides:
  - **Provider-agnostic logging:** your code does not depend on Serilog, NLog, or any other provider.  
  - **Easy swapping of logging providers:** you can change the backend later without touching controllers or services.  
  - **Scoped logging and structured messages:** supports context-aware logging with `BeginScope`.

### **Serilog as the backend**  
  By plugging Serilog into `ILogger`:
  - You can use Serilog’s advanced features (object destructuring, multiple sinks, async sinks, enrichers).  
  - You keep the code consistent with `ILogger<T>` throughout your application.

## Project Breakdown

### Configure SimpleInjector

.NET Framework Web API **does not have a built-in DI container** like ASP.NET Core. Because of that, we use **SimpleInjector** to manage dependencies.

First, add SimpleInjector to the project:

```bash
Install-Package SimpleInjector
Install-Package SimpleInjector.Integration.WebApi
```

Then, configure it on `Global.asax.cs`.

```csharp
// Global.asax.cs

using SimpleInjector;
using SimpleInjector.Integration.WebApi;

protected void Application_Start()
{
    // ...

    GlobalConfiguration.Configuration.DependencyResolver =
        GetDependencyResolver();
}

private System.Web.Http.Dependencies.IDependencyResolver GetDependencyResolver()
{
    var container = new Container();

    container.RegisterWebApiControllers(GlobalConfiguration.Configuration);

    container.Verify();

    return new SimpleInjectorWebApiDependencyResolver(container);
}
```

### Configure Microsoft’s ILogger

We configure an `ILoggerFactory`, register it in SimpleInjector and then register the generic `ILogger<T>` so that it can be injected anywhere.

Add the necessary packages:

```bash
Install-Package Microsoft.Extensions.Logging
Install-Package Microsoft.Extensions.Logging.Abstractions
```

Inject the logger:

```csharp
// Global.asax.cs

using using Microsoft.Extensions.Logging;

// ...

private System.Web.Http.Dependencies.IDependencyResolver GetDependencyResolver()
{
    var container = new Container();

    container.RegisterWebApiControllers(GlobalConfiguration.Configuration);

    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddDebug(); // send logs to Visual Studio Debug window
    });

    container.RegisterInstance(loggerFactory);
    container.Register(typeof(ILogger<>), typeof(Logger<>), Lifestyle.Singleton);

    container.Verify();

    return new SimpleInjectorWebApiDependencyResolver(container);
}
```

Receive through DI and use it:

```csharp
public class ValuesController : ApiController
{
    private readonly ILogger<ValuesController> _logger;

    public ValuesController(ILogger<ValuesController> logger)
    {
        _logger = logger;
    }

    // GET api/values
    public IEnumerable<string> Get()
    {
        var values = new string[] { "value1", "value2" };
        using (_logger.BeginScope(("values", values)))
            _logger.LogInformation("Found {count} values.", values.Length);

        return values;
    }
}
```

### Configure Serilog

We plug Serilog into the Microsoft ILogger system so that all `ILogger<T>` logs are handled by Serilog.

Add the packages:

```bash
Install-Package Serilog
Install-Package Serilog.Extensions.Logging
Install-Package Serilog.Sinks.Debug
```

Inject it:

```csharp
// Global.asax.cs

using Serilog;

protected void Application_Start()
{
    // ...

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
```

### Usage Example: Scoped Logging

With everything setup, we can use the injected instance of Microsoft's `ILogger<T>` while still leveraging Serilog's features, like object destructuring:

```csharp
public class ValuesController : ApiController
{
    private readonly ILogger<ValuesController> _logger;

    public ValuesController(ILogger<ValuesController> logger)
    {
        _logger = logger;
    }

    // GET api/values
    public IEnumerable<string> Get()
    {
        var values = new string[] { "value1", "value2" };
        using (_logger.BeginScope(("@values", values)))
            _logger.LogInformation("Found {count} values.", values.Length);

        return values;
    }
}
```

## Summary

- Use ILogger<T> in your code for flexibility and clean DI-friendly logging.

- Plug in Serilog as the backend to take advantage of advanced logging features.

- SimpleInjector handles injection in a Web API project where there is no built-in container.

- Minimal configuration is needed for a fully working logging system in a .NET Framework 4.8 Web API.

## References

[Microsoft.Extensions.Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line)

[Serilog](https://serilog.net/)

[SimpleInjector](https://simpleinjector.org/)