# Setting up the ActiveSession library.

Configuring the ActiveSession library is part of the overall application configuration. It consists of two stages. The first stage - configuring prerequisites and setting up the library configuration and services - is performed during the application services configuration. The second part - addition and configuration of the ActiveSession library middleware - is performed during the setup application's pipeline middleware.

## Configuring prerequisites

The initialization of an application that uses the ActiveSession library must first set up its prerequisites (see [Introduction](/articles/intro.html) ). 

## Setting up the ActiveSession library services and configuration.
To make the ActiveSession library up and running in the ASP.NET Core application, one must add some services to the application's service container (AKA DI_container). The addition of services in an ASP.NET Core application is performed at the service configuration stage via registering services in the service collection of an application builder, which is of type [IServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection). The services registered in this collection are then added to the application's	 DI-container during the application build process. Technically, registration of services is usually performed by extension methods for the IServiceCollection interface.

Here is an example of configuring prerequisites and registering services taken from one of example projects: 
**Program.cs**
````
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    
    //Configure prerequisites in the application services container
    builder.Services.AddMemoryCache();
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession();
    //Add runner factories for each ActiveSession standard runner
    builder.Services.AddEnumAdapter<SimSeqData>();
    builder.Services.AddAsyncEnumAdapter<SimSeqData>();
    builder.Services.AddTimeSeriesRunner<Int32>();
    builder.Services.AddSessionProcessRunner<Int32>();
    //... configure usage of other features in the application services container
````

### Services to be registered for the ActiveSession library.

The ActiveSession library services include infrastructure ones and ActiveSession runner factory ones. The infrastructure services are necessary for the library itself to function and to perform useful job. These services are registered as a single bundle by call of the [AddActiveSessionInfrastructure](/api/MVVrus.AspNetCore.ActiveSession.ActiveSessionServiceCollectionExtensions.AddActiveSessionInfrastructure.html) extension method for IServiceCollection interface. However, applications rarely need to call this method directly because it is called internally by any ActiveSession runner factory registration method discussed later.

An ActiveSession runner factory service must be registered for each runner type to make runners of that type available for the application. Each runner factory service is a specialization of a generic interface [IRunnerFactory&lt;TRequest,TResult&gt;](/api/MVVrus.AspNetCore.ActiveSession.IRunnerFactory-2.html). For each combination of TRequest and TResult used in the application, its own runner factory service must be registered.

There is a number of runner factory registration extension methods in the ActiveSession library. Each standard runner type has corresponding generic runner factory registration method. Those methods (namely, AddAsyncEnumAdapter, AddEnumAdapter, AddSessionProcessRunner and AddTimeSeriesRunner) are defined in the [StdRunnerServiceCollectionExtensions](/api/MVVrus.AspNetCore.ActiveSession.StdRunner.StdRunnerServiceCollectionExtensions.html). To facilitate implementing custom runner a number of overloaded auxiliary [AddActiveSessions](/api/MVVrus.AspNetCore.ActiveSession.ActiveSessionServiceCollectionExtensions.AddActiveSessions.html) methods with different sets of parameters exist too. 

### Setting up the ActiveSession library configuration.

The ActiveSession library configuration is contained in an ActiveSessionOptions record instance, accessible via the [Options Pattern](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options) as a default instance.
The initial source of the ActiveSession library configuration is the "MVVrus.ActiveSessions" section of the application configuration. The contents of this section are set by any of the application configuration setting methods [according to general rules](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/). Configuration parameters that are not set in any of the sources receive default values. The names, descriptions, and default values ​​for the parameters are described in the documentation for the [ActiveSessionOptions](/api/MVVrus.AspNetCore.ActiveSession.ActiveSessionOptions.html) record

For additional configuration customization, all the previously listed ActiveSession library service registration methods have overloaded variants with an additional parameter - a configuration customization delegate of the `Action<ActiveSessionOptions>` type. When creating an instance of a configuration record, these delegates are called in turn in the same order as the registration methods through which they were passed. A reference to a custom configuration object is passed to each such delegate, to which the delegate can make its changes.

## Configuring the middleware pipeline for using the ActiveSession Library.

The goal of configuring the ActiveSession library during the middleware pipeline setup step is to add the ActiveSession middleware to the pipeline and configure it. The ActiveSession middleware adds a reference to an active session object (either an existing one or a newly created one), to which the request belongs, to the HTTP request context ([HttpContext](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.httpcontext)) being processed. The active session object is selected based on the ASP.NET Core session [HttpContext.Session](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.httpcontext.session), to which the request belongs, and possibly other request parameters such as the path in the request URL.

Adding ActiveSession middleware to the middleware pipeline and configuring it is done by one or more calls to one of the overloaded [UseActiveSessions](/api/MVVrus.AspNetCore.ActiveSession.ActiveSessionBuilderExtensions.html) extension methods for the [IApplicationBuilder](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.iapplicationbuilder) interface. The middleware is added to the pipeline at the point of the first call. Arguments of this first and other UseActiveSessions method calls are collected together and after necessary transformations passed to the constructor of the middleware instance to be added.

Here is a simple example of configuring the ActiveSession middleware from the example project: 
**Program.cs**
````
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    
    //...configure prerequisites in the application services container 
    //...configure usage of other features in the application services container

    WebApplication app = builder.Build();

    //Configure prerequisites in the middleware pipeline
    app.UseSession();
    //Add ActiveSessions middleware to the middleware pipeline
    app.UseActiveSessions();
    //... configure usage of other features in the middleware pipeline

    app.Run();
````

Configuring ActiveSession middleware serves two purposes. First, it specifies for which requests a reference to the active session object will be added to the request context at all. Second, since version 1.1, it may specify a suffix that will be used in the active session identifier: since version 1.1, ActiveSession supports multiple active sessions associated with a single active session group (introduced in version 1.2) or an ASP.NET Core session (in version 1.1). The suffix specified allows the ActiveSession middlware to choose the correct active session for the specific request handler from all active sessions associated with the same group or ASP.NET Core sessions.

When building the application middleware pipeline parameters from all calls of the UseActiveSession methods are combined and passed to the ActiveSession middleware constructor. The constructor stores all filters and suffixes to be assigned by them from parameters of calls in the internal data structure of the middleware objects, so when the middleware is invoked to process a request, it effectively acts according to the following set of rules. First, the middleware checks each filter effectively one by one in the order they were specified in UseActiveSession calls to see whether the request passes each filter. If the request passes the filter, then the middleware notes that a reference to the active session object should be set for this request and, if this filter sets a suffix, then the suffix is set to be used for the active session identifier. If the particular filter that accepts the request does not set a suffix at all (returns null as a suffix), then the search for the next filter that also accepts the request and that sets a suffix continues (note: if a filter sets the suffix to an empty string, then the suffix is considered to be set and the search stops). If none of the filters that may accept the request sets a suffix, then an empty string will be used as the active session identifier suffix.

The way the filter works and the suffix to be set is determined by the specific form of the called overloaded method [UseActiveSessions](/api/MVVrus.AspNetCore.ActiveSession.ActiveSessionBuilderExtensions.html). Calling this method in the form that has no parameters means that the filter of this call will be satisfied by any request, and that this filter does not set any suffix.
