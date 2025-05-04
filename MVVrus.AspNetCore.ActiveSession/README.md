# ActiveSession library.

## About the library
The ActiveSession library is designed to execute code in the background that provides results for several logically related HTTP requests and to share data between them. This set of logically related requests, as well as the code executing between them and their shared data, will be further referred to as an *active session* (or just session). 

Background execution of code using the ActiveSession library is initiated by a handler of a request associated with an active session. 
Typically, but not necessarily, this initiating query is the first one in the session. It returns a subset of the result for the portion (range) of the full execution result requested through its parameters. After the initiating request has completed, execution of the session code may continue in the background.  Typically the result returned by the initiating request is intermediate, but if all execution falls within the requested range, the result will be final and there will be no further background code execution. Subsequent requests in the session, if any, may return results obtained in the background between requests to the client - intermediate or, if execution completes, final. In addition, any subsequent request in the session may terminate the code running in the background. The background execution may also be interrupted when subsequent client requests expire (timeout).

Unlike the other background execution mechanism in the ASP.NET Core application, Background Services, which is global to the entire application, each active session is associated with a single client, meaning it is different for each client. The binding of the active session to the client is based on the Sessions feature of ASP.NET Core: each active session is associated with a specific session supported by this mechanism. An active session becomes available when processing a request, if the request is made within the framework of the corresponding ASP.NET Core session.

## References
1. [The repository with the ActiveSession library source.](https://github.com/mvvrus/ActiveSession)
2. [The AciveSession library documentation site.](https://mvvrus.github.io/) \(under construction\).
3. [The repository with examples of the ActiveSession library usage.](https://github.com/mvvrus/ActiveSessionExamples) Most оf exmamples in this document (namely those, for which file names are specified) are taken from SampleApplication project in this repository. These file names are shown relative to this project's directory.

## Components of the library and its extensions

### Runners

*Runners* are instances of classes containing code of an operation that may execute itself in the background. To interact with an application and with another parts of the library runners must implement `IRunner<TResult>` generic interface. There is a number of *standard runners* that are implemented by the ActiveSession library. One can also implement its own custom runner class by implementing `IRunner<TResult>` generic interface. 

The runner interface contains a number of properties and methods that do not depend on a runner result type. Those members and properties are collected in a  type-agnostic (non-generic) runner interface IRunner. Technically, the full runner interface `IRunner<TResult>` is inherited from this type-agnostic interface, adding result-dependent methods to it.

Each runner is entirely responsible for the execution of its operation:
- starting the operation;
- passing the initial result of the execution of the started operation and the current state of the runner to the HTTP request handler that launched the operation;
- executing the operation in the background;
- passing information about the state of the operation and the result obtained in the background to handlers of subsequent requests from the same session;
- completing the operation - naturally, upon its execution, or forcibly, either due to a call from a handler of a request belonging to the operation's session, or upon expiration of the waiting time for the next request.

### Active session objects 
*Active session objects* represent currently existing active sessions. An HTTP request handler receives a reference to the active session object of which it is a part. The active session object implements the IActiveSession interface, which allows the request handler to interact with the ActiveSession library. In particular, the handler can:
- create new runners in this session;
- get references to runners running in this session;
- get services from the service container associated with this session;
- read and write shared data of this session;
- terminate this session.

Starting from v.1.1 the ActiveSession library supports multiple simultaneous active sessions associated with a single ASP.NET Core session, or, starting from version 1.2 - a single session group. Each such an active session is represented by its own active session object. These objects are distinguished by active session identifier suffixes assigned to them. Scopes of these active sessions are determined by ActiveSession library infrastructure, based the library setup during application startup, as described in the documentation. 

### ActiveSession library infrastructure

The *ActiveSession library infrastructure* executes all internal operations required for the library to perform its work; applications do not interact directly with the infrastructure.
In particular, the ActiveSession library infrastructure performs the following functions:
- creates or finds an existing active session object to which the processed request belongs and provides a reference to this object to request handlers;
- stores, tracks an expiration of a timeout and terminates (after the expiration or upon call from the application) active sessions, disposing their objects;
- terminates all runners that were working in the terminated active session and disposes its active session object;
- creates, maintains make available and disposes after their expiration active session group objects (since version 1.2);
- creates new runners using runner factories registered in the application service container, according to calls from active session objects to work in those sessions; 
- stores runners, finds runners requested by active session objects and returns references to them;
- tracks an expiration of a runner timeout and terminates runners and remove them from the store: upon runner completion or after the timeout expiration;
- cleans up runners that have been terminated for one reason or another, if such cleaning is provided for the runner class;

### Runner factories

*Runner factories* are objects that are designed to create runners. The ActiveSession library infrastructure receives runner factories from the service container. Therefore, runner factory classes must be registered during application initialization in the application services container (AKA DI container) as implementations of the interface that the infrastructure will request from the container - the specialization of the generic interface `IRunnerFactory<TRequest,TResult>`. This interface has two type parameters: TRequest is a type of an argument that is passed by the application to create the runner, TResult is a result type of the created runner. 

To register standard runner factories, the ActiveSession library has extension methods for the IServiceCollection interface intended for this purpose. How to use these methods is shown below in the in the example of initialization code. To facilitate an implementation of custom runners, the library also defines auxiliary classes and extension methods intended for this purpose.

## Prerequisites

The ActiveSession library uses session state feature of ASP.NET Core framework, which in turn is based upon .NET distributed caching. To begin using ActiveSession Library these features must first be initialized and configured.

## Application initialization

The initialization of an application that uses the ActiveSession library must first set up the above mentioned prerequisites. Then the initialization code must add all runner factories used to the application services container. The first addition of a runner factory also implicitly adds infrastructure services to the container. Finally, the initialization code must add the ActiveSession middleware to the application's middleware pipeline. The simplest way to do all of this is shown in the following example (this and all other examples are taken from a project, demonstrating use of standard runners from the ActiveSessions library):

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

    WebApplication app = builder.Build();

    //Configure prerequisites in the middleware pipeline
    app.UseSession();
    //Add ActiveSessions middleware to the middleware pipeline
    app.UseActiveSessions();
    //... configure usage of other features in the middleware pipeline

    app.Run();
````

## How to use the ActiveSession library from handlers of HTTP requests

### Accessing the active session object
It is well known that when processing a request, the middleware pipeline receives a request context - an instance of a class derived from HttpContext as input to the pipeline. Generally this context is available to request handler in most frameworks - as a property named HttpContext of the class containing handlers in MVC and Razor Pages frameworks, as a binding to a parameter of type HttpContext in Minimal API framework etc. And to access the active session object associated with the request from a request handler one can use the extension method GetActiveSession() of the HttpContext class. The example of accessing an active session associated with a request is a part of the example in the "Creating a new runner" section below.

### Using the IActiveSession - active session object interface.
Before using IActiveSession interface methods one should ensure that the active session is available. To do this one need to check that the IsAvailable property of the received IActiveSession interface contains true. The example in the next section demonstrate, among other things, how to obtain a reference to the active session object for a request and verify that the active session is available. 

#### Creating a new runner
Use the generic `CreateRunner<TRequest,TResult>(TResult, HttpContext)` method of the IActiveSession interface to create a new runner to run in this active session. Being generic, this method has two type parameters: TRequest, which is the type of the first parameter to pass to this method, and TResult, which is the type of the result that will be returned by the new runner. The value of the first parameter of the CreateRunner method is passed to the Create method of the appropriate runner factory and is used when creating a new runner. The second parameter is the context of the request in whose handler the runner is created. This method returns a generic record struct `KeyedRunner<TResult>` with two fields: `IRunner<TResult> Runner`, that references the newly created runner, and `int RunnerNumber`, that contains the number assigned to this runner in the active session. 

None of the parameters passed to this method depend on the second type parameter, so the type parameters of this method cannot be inferred by a compiler and must be specified explicitly. Additionally, the result type of a standard runner may be rather complex. Because of these inconveniences,the ActiveSession library defines a number of extension methods for the IActiveSession interface, namely CreateSequenceRunner, CreateTimeSeriesRunner, and CreateSessionProcessRunner, which create the ActiveSession library's standard runners. Although these methods are also generic, they are more convenient to use: they have only one type parameter (the same as the type parameter of the standard runner class to be created), which can often be inferred from their parameters, and they make it easy to specify the result type by using this type parameter to specify actual runner result type.
These methods also return a generic record struct described earlier with type parameter set appropriately.

The following example demonstrates creation of a runner as well as accessing the active session object for the request and checking that the active session is available (the Razor Pages framework is used in the example):

**Pages\SequenceAdapterParams.cshtml.cs**
````
        //Come here after the data are entered to the form on the page 
        //(the page template is beyond the scope of the example)
        public ActionResult OnPost() 
        {
            if(ModelState.IsValid) { 
                //Make some input processing
                // IEnumerable<SimSeqData> sync_source;
                //...
                
                //Obtain a reference on the active session object for this request
                IActiveSession session= HttpContext.GetActiveSession();
                
                //Check that the active session is available
                if(session.IsAvailable) {
                
                    //Create a new runner
                    (var runner, int runner_number)= session.CreateSequenceRunner(sync_source, HttpContext);
                    
                    //This part will be explained later in a section 
                    //dedicated to an external runner identifier usage
                    ExtRunnerKey key = (session, runner_number); //Make an external identifier
                    //Pass the external identifier by a part of the redirection URL path
                    return RedirectToPage("SequenceShowResults", new { key }); 
                }
                else //An active session is unavailable 
                    return StatusCode(StatusCodes.Status500InternalServerError);
            }
            //Repeat input due to invalid data entered
            return Page();
        }
````
#### Obtaining the existing runner
Use the generic `GetRunner<TResult>(int, HttpContext)` method of the IActiveSession interface to obtain a reference to an existing runner with a result type TResult which executes within current active session and is registered under a runner number passed as the first parameter. If no runner registered under the specified number exists, or if the runner registered has incompatible result type the method returns null. 

Parameters of GetRunner generic method do not depend on its type parameter, therefore the type parameter cannot be inferred and must be specified.

The ActiveSession library defines a number of extension methods for the IActiveSession interface to make it easier to specify the type parameter while obtaining standard runners. These extension methods - GetSequenceRunner and GetTimeSeriesRunner - are generic too. They have the same type parameters as their corresponding standard runner classes.

Use the GetNonTypedRunner(int, HttpContext) method of the IActiveSession interface to obtain a reference to a type-agnostic runner interface (IRunner) of an existing runner.

Examples of obtaining references to existing runners may be found below in the "Using an external runner identifier" section.

#### Terminate the active session

Call Terminate method of the ActiveSession interface to terminate the active session associated with this request. No active session object will be available in this request and any runners executing in the terminated session will also be terminated. If a new request comes which would be a part of the terminated active session a new active session object with a new value of its Generation property will be created for this request.

An example of using the IActiveSession.Terminate method in an action method of a MVC API Controller :


**APIControllers\SampleController.cs**
````
    [HttpPost("[action]")]
    public IActionResult TerminateSession()
    {
        IActiveSession session = HttpContext.GetActiveSession();
        if(session.IsAvailable) {
            session.Terminate(HttpContext);
            return StatusCode(StatusCodes.Status204NoContent);
        }
        else return StatusCode(StatusCodes.Status500InternalServerError);
    }

````
#### Associate data with the active session and track the active session completion and cleanup.

The Properties property of the active session interface IActiveSession allows to associate arbitrary objects with the active session. This property is a dictionary with a string as a key and an arbitrary object as a value. Objects can be added with their corresponding keys and retrieved by those keys. Concurent access to the Properties dictionary is allowed. If an active session object is disposable (i.e. implement the IDisposable and/or IAsyncDisposable interfaces),  then disposing it switches Properties to read-only mode: all existing objects at the corresponding keys will remain accessible, but no more objects can be added, and no existing objects can be removed from the Properties dictionary.

If the objects added to Properties are disposable, one should monitor the completion and cleanup of the active session to perform proper cleanup of these associated objects. There are two events in the lifecycle of an active session that you can monitor. First, the IActiveSession interface contains a CompletedToken property of type CancellationToken that will be canceled when the active session is completed and ready to be cleaned up. Second, CleanupCompletionTask contains a task that completes when the active session is finished cleaning up.

The following example shows how one can associate objects with an active session and track the session completion and cleanup. The example associates a RunnerRegistry object (whose functionality is beyond the scope of the example) with an active session, retrieves the associated object and cleans it up after the active session is finished cleaning up:

**RunnerRegistry.cs**
````
    public class RunnerRegistry: IDisposable
    {
        //...
        public void Dispose()
        {
            //...
        }

    }
````

**Sources\RunnerRegistryActiveSessionsExtensions.cs**
````
    public static class RunnerRegistryActiveSessionsExtensions
    {
        public const string REGISTRY_NAME = "RunnerRegistry";
        
        public static RunnerRegistry GetRegistry(this IActiveSession ActiveSession)
        {
            Object? cached_result;
            RunnerRegistry? result = null;
            //First just try to get the existing registry (fast path)
            if(!ActiveSession.Properties.TryGetValue(REGISTRY_NAME, out cached_result)) { //Fast path isn't available
                lock(ActiveSession.Properties) { //Lock shareable resource - Properties dictionary
                    result = new RunnerRegistry(); //Create new registry to add it to Properties
                    if(ActiveSession.Properties.TryAdd(REGISTRY_NAME, result)) //Use "double check pattern" with second check within lock block
                        //Addition was successful
                        ActiveSession.CleanupCompletionTask.ContinueWith((_) => result.Dispose()); //Plan disposing the registry added after end of ActiveSession
                    else {
                        //Somebody added a registry instance already between checks
                        cached_result = ActiveSession.Properties[REGISTRY_NAME]; //Get previously added registry
                        result.Dispose();
                        result=null; //Dispose and clear result
                    }
                }
            }
            return result??(RunnerRegistry)cached_result!; //cached_result is not null here;
        }
        //...
    }
````

#### Using an external runner identifier to pass runner identification to a front-end part

One does not simply pass the runner number into the front-end part of a web application to make a reference suitable for obtaining the same runner while handling the next request to the back-end. This is because, although the runner number fully identifies a runner within single active session, there is no guarantee that the next request will belong to the same active session. So one should pass to the front-end not only the runner number, but the information that uniquely identifies current active session among all ever existing  or will ever be created active sessions. To perform this task the ActiveSession library defines the ExtRunnerKey class. In addition to the runner number, it contains data that uniquely identifies the active session. The instance method IsForSession(IActiveSession) checks whether the active session passed as an argument is the same one for which this instance was created. 

There are two ways to use the ExtRunnerKey object. The first way is to pass its value back as an object. An object may be passed as a whole being serialized into JSON or XML in an API call parameters. Or it may be passed as a set of form values with appropriate names and bound to a handler parameter of type ExtRunnerKey via the MVC or Minimal API binding process. This method of passing an external runner identifier is well suited for processing API calls from scripts and handling input from forms.

Another way to pass a value of ExtRunnerKey is to serialize it into a string via ExtRunnerKey.ToString(). This method is well suited to pass an external runner identifier as part of an URL path or query values, because the serialized value does not contain symbols, that may not be used in those URL parts. The passed value may be de-serialized using ExtRunnerKey.TryParse method.

Here are two examples of passing runner identification between server (back end) and browser(front end) parts of a web application. Each example comprises a number of files. Obtaining references to runners is also covered by these examples. Code snippets for this example are taken from an example project for ActiveSession library.


##### Passing an external runner identifier as an object for API call

The example contains the following steps:

1.Save an external runner identifier into the `_key` field of page model class accessible to the corresponding Razor Page template

**Pages\TimeSeriesResults.cshtml.cs**
````
    public class TimeSeriesResultsModel : PageModel
    {
        internal ExtRunnerKey _key;
        //...
        public async Task OnGetAsync(ExtRunnerKey Key)
        {
            _key=Key;
            //...
        }
        //...
    }
````

2.Initialize a global JavaScript variable in the resulting HTML page to contain the external identifier of its associated runner by means of the Razor Page template by a value of the above mentioned field of the page model class. Then, from the HTML page unload handler, pass the external runner identifier to the Abort API endpoint to terminate the associated runner.

**Pages\TimeSeriesResults.cshtml**
````
@page "{key}"
@model SampleApplication.Pages.TimeSeriesResultsModel
<!-- ... -->

<script>
    var pollInterval = @Model._timeoutMsecs;
    var runner_key = { 
        RunnerNumber: @Model._key.RunnerNumber,
        Generation: @Model._key.Generation,
        _ActiveSessionId: "@(WebUtility.UrlEncode( Model._key.ActiveSessionId))",
        get ActiveSessionId() { return decodeURI(this._ActiveSessionId); }
    }, 

    window.onunload = function () {
        let request = {
            RunnerKey: runner_key,
        }
        fetch("@Model._AbortEndpoint", {
            method: "POST",
            headers: {
               "Content-type": "application/json;charset=utf-8"
            },
            keepalive: true,
            body: JSON.stringify(request)
        });
    }
</script>
````

3.Check that the identifier passed is for the same active session in the API handler. If so, get a type-agnostic interface of the runner and call its Abort method. This example also demonstrates how to obtain of a type-agnostic interface of an existing runner - the topic mentioned earlier.

**APIControllers\SampleController.cs**
````
    [HttpPost("[action]")]
    public ActionResult<AbortResponse> Abort(AbortRequest Request)
    {
        IActiveSession session = HttpContext.GetActiveSession();
        if(session.IsAvailable && Request.RunnerKey.IsForSession(session)) {
            var runner = session.GetNonTypedRunner(Request.RunnerKey.RunnerNumber, HttpContext);
            if(runner!=null) {
                AbortResponse response = new AbortResponse();
                response.runnerStatus=runner.Abort(HttpContext.TraceIdentifier).ToString();
                return response;
            }
        }
        return StatusCode(StatusCodes.Status410Gone);
    }
````

##### Passing an external runner identifier serialized to a string in the URL 

The example contains the following steps:

1.Serialize an external runner identifier and pass it as segment of a URL path in an HTTP redirect result
**Pages\SequenceAdapterParams.cshtml.cs**
````
        //This is part of the previous example from "Creating a new runner" section
        public ActionResult OnPost() 
        {
            //...
                    //Create a new runner
                    (var runner, int runner_number)= session.CreateSequenceRunner(sync_source, HttpContext);
                    //Make an external identifier
                    ExtRunnerKey key = (session, runner_number); 
                    //Pass the external identifier by a part of the redirection URL path.
                    //A value of an external identifier will be serialized by the Razor Pages framework using the key.ToString() method call
                    return RedirectToPage("SequenceShowResults", new { key }); 
            //...
        }
````

2.Bind the value of the path segment to the parameter of the GET request handler for the target Razor Page, the binder class being specified via `[ModelBinder]` attribute, the C# code snippet also provides a previously mentioned example of obtaining an existing runner via the IActiveSession interface:

**Pages\SequenceShowResults.cshtml**
````
@page "{key}" 
@model SapmleApplication.Pages.SequenceShowResultsModel
<!-- ... -->
````

**Pages\SequenceShowResults.cshtml.cs**
````
    public class SequenceShowResultsModel : PageModel
    {
        //...
        public async Task OnGetAsync([ModelBinder<ExtRunnerKeyMvcModelBinder>]ExtRunnerKey Key)
        {
            //...
            IActiveSession active_session = HttpContext.GetActiveSession();
            if(!active_session.IsAvailable) {
                //... Write a message about the situation
            }
            else {
                if(!Key.IsForSession(active_session)) {
                    //... Write a message about the situation
                }
                else {
                    //Obtain the runner
                    var runner = active_session.GetSequenceRunner<SimSeqData>(Key.RunnerNumber, HttpContext);
                    //...
                }
            }
        }
        //...
    }
````

3.Binding is performed using the following helper class that uses ExtRunnerKey.TryParse method:

**ExtRunnerKeyMvcModelBinder.cs**
````
    public class ExtRunnerKeyMvcModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext Context)
        {
            String name = Context.ModelName;
            String? key_string = Context.ValueProvider.GetValue(name).FirstOrDefault();
            if(key_string != null) {
                ExtRunnerKey key;
                if(ExtRunnerKey.TryParse(key_string, out key)) {
                    Context.ModelState.SetModelValue(name, key, key_string);
                    Context.Result=ModelBindingResult.Success(key);
                }
            }
            return Task.CompletedTask;
        }
    }
````

### ActiveSession and dependency injection.

#### Obtaining Scoped services for use by runners.

Dependency injection is often used by ASP.NET Core-based frameworks (MVC, etc.) to pass a reference to a service to be used by a request handler.
Such services should be used with caution with ActiveSession library: if the service is registered with Scoped lifetime, one should not pass this service to a runner. 

This is because objects implementing such services are intact only during the current request processing and are generally unavailable for the whole duration of the active session. Technically, such services are resolved form `HttpContext.RequestServices` container, which is disposed after the HTTP request processing is completed. Thus, the same limitation applies to services obtained from the above container calling its `IServiceProvider` interface methods manually (using the so-called "service locator pattern"). Such services should be obtained from a container associated with an active session (`IActiveSession.SessionServices`) for use with the ActiveSession library.

A convenient way to obtain services from the active session service container in an HTTP request handler is to use *session container adapter service*
`IActiveSessionService<TService>` as the dependency type instead of TService itself. The `IActiveSessionService<TService>` adapter service is one of the ActiveSession library infrastructure services. When obtained, its Service property contains a reference to the object implementing the actual service TService that was requested. If the actual service cannot be obtained, either because the service is not registered with the container or for some other reason, this property contains null. The session container adapter service itself, `IActiveSessionService<TService>`, is registered with the Scoped lifetime and can be obtained from the request service container, so it is eligible for dependency injection in frameworks supporting dependency injection. If the active session is available, the property Service of an interface contains a reference obtained from the active session container. Otherwise, if an active session is not available, the IActiveSessionService implementation falls back to the request container and resolves the requested service from it. In this way, the reference to a service obtained through this session container adapter may be used by a handler instead of the service itself, regardless of whether an active session is available. The IsFromSession property of this session container adapter service indicates whether the received service implementation came from the active session container and is suitable for use in the active session runners.

#### Working with Scoped services that does not allow a concurrent access to them
Some services that are registered with a Scoped lifetime are not designed to allow concurrent access to their instances. Important examples of such services are database contexts from Entity Framework Core when they are registered as services. Such services are well suited for use by traditional HTTP request processing, where each request has its own scope, so an instance of an object implementing such a service for a particular request cannot be accessed by handlers of other requests. However, using such services within the scope of an active session instead of within the scope of a single HTTP request creates a problem. This is because the instances that implement them can potentially be shared among all request handlers belonging to the same active session, but these instances are not designed to support such sharing.

To solve this problem, the ActiveSession library contains a mutually exclusive service lock feature. An *exclusive service accessor*, represented by the generic interface `ILockedSessionService<TService>`, where TService is a type of a service, provides mutually exclusive access to a service of the specified type obtained from the active session service container. An exclusive service accessor for a service of type TService can be obtained by calling the asynchronous AcquireAsync method of the generic `ISessionServiceLock<TService>` interface specialized by the service type (TService). While one such exclusive service accessor for the service exists within the active session and is not disposed, the AcquireAsync method will not return the next exclusive service accessor for this service, but will wait for the previous one to be disposed.

The reference to the object implementing the service is contained in the Service property of the exclusive service accessor, `ILockedSessionService<TService>`. If the service could not be obtained from the container (because it is not registered or for some other reason), this reference, similar to the session container 
 service,`IActiveSessionService<TService>`, will contain null.

Just like the session container adapter service `IActiveSessionService<TService>`, the mutually exclusive service lock feature can also be used in conditions when an active session is unavailable. In this case, the Service property of the mutually exclusive access object will contain a reference to the object implementing the service obtained from the request service container, similar to the session container adapter service. And since access from handlers of other requests to this service is impossible in this case, the `ISessionServiceLock<TService>.AcquireAsync` method will not wait for anything, but will immediately return the exclusive service accessor to the service. And the IsReallyLocked property of such an accessor will contain false, while for the service obtained from the session service container it will be equal to true. This solution makes the exclusive service accessor universal: the service obtained through it can be used within the handler of one request in the same way, regardless of the availability of the active session.

The `ISessionServiceLock<TService>` interface is one of the services of the ActiveSession library infrastructure. It is registered in the application service container with a Scoped lifetime, and is designed to be obtained from the request service container. Therefore, it can be injected as a dependency into any class or method that uses a framework that supports dependency injection.

The ActiveSession library provides an extension method for creating a runner, which is an analogue of IActiveSession.CreateRunner - with the same name and parameters, but having one additional parameter - an exclusive service accessor, which will be released automatically after the completion and disposing of the runner (it is assumed that the service accessible through the exclusive service accessor is used by the runner). All extension methods for creating standard runners also accept an optional parameter - an exclusive service accessor, which will be released after the completion and cleanup of the corresponding runner created by these methods.

An example of using the exclusive service accessor feature with Razor Pages framework is shown below:

**Pages\ExclusiveParams.cshtml.cs**
```
    public class ExclusiveParamsModel : PageModel
    {
        // ...Other fields and properties
        //A reference to the service used to obtain an exclusive service accessor
        readonly ISessionServiceLock<IExclusiveService> _sessionServiceLock;
        // ...
        //Inject ISessionServiceLock service reference into the page model class constructor
        public ExclusiveParamsModel(ISessionServiceLock<IExclusiveService> SessionServiceLock)
        {
            // ...Initialize other fields and properties
            _sessionServiceLock=SessionServiceLock;
        }
        
        public async Task<ActionResult> OnPostAsync() 
        {
            IActiveSession session = HttpContext.GetActiveSession();
            // ...Perform some initialization and check if the session exists
            
            //An exclusive service accessor. The service represented by it will not be really used by this example however.
            ILockedSessionService<IExclusiveService>? accessor; 
            try {
                // Obtain an exclusive service accessor to lock access to the instance implementing IExclusiveService
                accessor =  await _sessionServiceLock.AcquireAsync(Timeout.InfiniteTimeSpan, HttpContext.RequestAborted);
            }
            catch(ObjectDisposedException) {
                //The active session was terminated, and it and all its associated objects are disposed.
                return RedirectToPage("SessionGone"); //Redirect to a page informing a user about that
            }
            if(accessor == null) return StatusCode(StatusCodes.Status500InternalServerError); //Infinite wait ended? It's impossible!
            
            IAsyncEnumerable<SimSeqData> async_source; //Its initialized is skipped for brevity.
            //Create a runner and pass the exclusive service accessor obtained earlier to be disposed after the runner completion and cleanup
            (IRunner runner, int runner_number) = session.CreateSequenceRunner(async_source, HttpContext, accessor);
            
            //... return RedirectResult to the result page containing an URL with the runner external identifier
            //... see "Passing in URL an external runner identifier serialized into a string" example.          
        }
        
    }

```

### Working with runners

A runner is an object instance that executes a background operation, returns operation results and interacts with other parts of the ActiveSession library supporting its execution. A runner may be considered as consisting of a background part and a foreground part.

A background part is generally outside the scope of this section. All one need to know about it is that the background part performs some (runner-specific) operation passing through a series of execution points, where it return intermediate results to be used by a foreground part. Each execution point has a number which monotonically increases (typically by 1). The background operation may complete naturally (but this is not required) with returning the final result or due to an error, and may be interrupted through its foreground part. The progress of the background execution is reflected by the GetProgress() method and the IsBackgroundExecutionCompleted property of the runner interface, that will be described later.

Before moving on to describing the properties and methods of the runner interface `IRunner<TResult>` (and its result-agnostic part `IRunner`), one should review several concepts that this description relies on.

#### Runner state

A runner state consists of a runner status, a runner position, and possibly an exception occurred during the the background execution of the runner.
A runner state is returned along with a result by result-obtaining methods of the runner(see appropriate section below). The runner state returned along with result is taken at the moment when the result is returned. The current state of a runner is also available through the Status, Position and Exception properties of a runner interface. 

The *runner status*, a value of RunnerStatus enumerable type, describes a stage of runner's life cycle. A runner's existence begins with its initial stage, in which the background operation has not yet started. The status value for this stage is always NotStarted. 

When background execution begins, the runner enters the executing state. There are two status values that correspond to the executing stage. The Stalled value indicates that a result for the last execution point reached by the background operation has already been returned, but the background operation is still executing. The Progressed value indicates that the result for the last execution point reached by the background operation has not yet been returned, regardless of whether the background operation has completed or not. To check whether the status value corresponds to an executing stage, the extension method IsRunning() for the RunnerStatus class may be used.

A runner comes to its final stage after its background operation has completed for any reason and all pending result (if any) have been returned. Status values corresponding to the final stage indicates the reason for the runner's completion. A Completed status value indicates that the background operation was completed naturally and its final result was returned. A Failed status value indicates that the background operation was terminated due to an exception occurred during its execution and the last result obtained before the exception occurred was also returned, the exception becoming a part of the runner state. An Aborted status value indicates that the runner execution was interrupted by an application call or by the library itself due to a termination of the active session to which the runner belongs. Unlike the two previous cases, an interruption the runner execution not only terminates the background operation, but also discards all results that have not yet been returned or are ready to return immediately. Therefore, interruption causes the runner to transition to the final state with status Aborted immediately, rather than after all background results have been returned, as in the two previous cases. 

The state of an runner that has reached the final stage can no longer be changed, and the runner itself is removed from the storage after completion, becoming inaccessible through the active session interface IActiveSession, and is subject to cleanup if its class implies such (implements the IDisposal and/or IAsyncDisposal interface). To check whether the runner status value corresponds to a final stage, the extension method IsFinal() for the RunnerStatus class may be used.

The *runner position* is the execution point number for which results of background execution were returned. When the position is passed along with the returned result, it specifies the execution point for which result is returned. Position property of a runner contains the execution point number of the last execution point for which a result was returned. Typically (and for any standard ActiveSession runner, always), this is also the highest execution point number for which a result was ever returned.

The *exception* is set as a part of the runner state only when the runner enters its final Failed state. In this case, it contains the exception that occurred during background execution. In all other cases, the exception is null.

#### Sequence runners

An important special case of runners are those that receive or produce in the background a sequence of objects of the same type (hereinafter these objects will be referred to as *records*)and return as a result this sequence of records in parts, in the order they were received, and without omissions or repetitions. In what follows, these runners will be referred to as *sequence runners*. 

Sequence runners have a number of common features that distinguish them from other runners.
First, result-obtaining methods of one sequence runner must be executed sequentially, one at a time. Violation of this rule results in an exception. This rule applies to asynchronous result-obtaining methods too: while one such a method is waiting for its completion, no other result-obtaining methods, whether synchronous or asynchronous, may be called. 
Second, the result of a sequence runner has the generic type`IEnumerable<TItem>` where TItem is the type of the record. The result returned by result-obtaining methods of a sequential runner is part of the original background sequence, which contains records that have not been returned yet - from the last execution point for which the  result was returned and to the execution point specified by the method's parameters (see discussion of result-obtaining method parameters below).
Third, the execution point number for a sequence runner is the number of records in the original background sequence - obtained or returned, depending on the context - so far. That is, for a progress of the background execution of a sequence runner, this number is a number of records created or obtained from another source by the background process, and for a position of a sequence runner, it is the total number of records returned as a result so far.
Fourth, each result-obtaining method must start obtaining its result from the current position of the runner (see description of parameters of result-obtaining methods below).

#### Methods for obtaining results.

The runner interface `IRunner<TResult>` defines two result-obtaining methods. These methods have similar parameters and return types, which will be discussed together below, but they are used for different purposes.

The first of these methods, GetRequiredAsync, starts a background process (if it has not already started) and returns the result obtained by advancing to the execution point specified by its parameters. If the specified execution point has not yet been reached by the background process, this method asynchronously waits for it to be reached. If the background process terminates before the specified point is reached, the result for the last execution point reached by the process is returned. This is the method that is typically called by the first request after creating the runner, to get the initial result.

For sequence runners, the above can be stated more simply: the GetRequiredAsync method starts a background process if required and returns (possibly asynchronously waiting for them to be received) the number of records specified by the Advance parameter (see the description of the method parameters below), or, if the background process terminates before the specified number of records are received, the actual number of records received since the last return of the result.

The second of these methods, GetAvailable, returns (synchronously) the background process's already-received result - either everything that has been retrieved, or for the execution point specified through its parameters, if that point has already been passed by the background process that is still executing. Typically, this method is called to retrieve additional results after the first call to GetRequiredAsync.

For sequence runners, this means that the GetAvailable method returns only the records already retrieved by the background process, and no more than the number of records specified in the Advance parameter (with the default Advance parameter value - all records).

##### Parameters of result-obtaining methods.

Let's look at the signatures of the result-obtaining methods.
````
public ValueTask<RunnerResult<TResult>> GetRequiredAsync(
    Int32 Advance = DEFAULT_ADVANCE,
    CancellationToken Token = default,
    Int32 StartPosition =CURRENT_POSITION,
    String? TraceIdentifier=null
);
````
````
public RunnerResult<TResult> GetAvailable(
    Int32 Advance = MAXIMUM_ADVANCE, 
    Int32 StartPosition = CURRENT_POSITION, 
    String? TraceIdentifier = null
);
````

One can see that both methods have a similar set of parameters, none of which is required. 

The Advance and StartPosition parameters determine for which specific execution point numbers of the runner the result should be returned. The value of the StartPosition parameter can be either a number - the number of the starting execution point, or the constant CURRENT_POSITION=-1, meaning that the current position of the runner is used as the starting point number. The value of the Advance parameter can be either a number - the maximum size of the range of execution point numbers for which the result is returned, or the constant DEFAULT_ADVANCE=0, the interpretation of which depends on the runner.

What exactly these parameters mean for specific runners, and what restrictions are imposed on them, is determined by the runner type itself.

In particular, the following rules exist for sequence runners. The StartPosition parameter must be either the CURRENT_POSITION constant or the current position of the runner (the value of its Position property). The Advance parameter is the maximum number of records in the returned result, and the value of this parameter cannot be negative, and if the Advance parameter value is zero (that is, the DEFAULT_ADVANCE constant), then the default value specified in the ActiveSession library configuration is substituted instead (which, in turn, is 20 by default).

For other types of standard library runners, the interpretation of the StartPosition and Advance parameters is specified in the description of these runners.

The TraceIdentifier parameter of both methods is used exclusively for tracing request processing; its value, if specified, is added to each entry sent to a log.

The Token parameter of the GetRequiredAsync method is used to cancel the execution of this method call via the standard .NET coordinated cancellation mechanism. In this case, only this specific method is canceled, the background process continues to execute, and its result can be obtained by subsequent calls to GetAvalable or GetRequiredAsync. It should be noted that for sequence runners, when a call to the GetRequiredAsync method is canceled (as well as when an exception occurs during its execution), no records from this sequence are lost: they will be returned by the next call to GetAvalable or GetRequiredAsync.

##### Obtaining results by result-obtaining methods.

Both result-returning methods return not only the result itself, but also the runner state (discussed earlier) for the moment of return in a structure of type `RunnerResult<TResult>`. The GetRequiredAsync method, since it is executed asynchronously, returns a task with a result of type `RunnerResult<TResult>` } (a value of type `ValueTask<RunnerResult<TResult>>` ) which can be used to wait for completion and obtain the result. The GetAvailable method, since it is executed synchronously, simply returns a structure of type `RunnerResult<TResult>` .

The most convenient way to work with the returned `RunnerResult<TResult>`structure is using the deconstructing assignment, like this (the GetAvailable method is used in this example):
`(TResult result, RunnerStatus status, Int32 position, Exception? exception)=runner.GetAvailable();`

##### Examples of using result-obtaining methods

Example of using GetRequiredAsync and GetProgress methods and IsBackgroundExecutionCompleted property (sequence runner example in the example project):

**Pages\SequenceShowResults.cshtml.cs**
````
    public class SequenceShowResultsModel : PageModel
    {
        //...skip irrelevant code
        internal List<SimSeqData> _results=new List<SimSeqData>();
        internal RunnerStatus _status;
        internal Int32 _position;
        internal Exception? _exception;
        internal String RUNNER_COMPLETED = "The runner is completed.";
        //...skip irrelevant code
        internal Int32 _bkgProgress;
        internal Boolean _bkgIsCompleted;
        //...skip irrelevant code
        public String StartupStatusMessage { get; private set; } = "";
        
        public async Task OnGetAsync([ModelBinder<ExtRunnerKeyMvcModelBinder>]ExtRunnerKey Key)
        {
            //...skip irrelevant code and adjust indentation
            var runner = active_session.GetSequenceRunner<SimSeqData>(Key.RunnerNumber, HttpContext);
            //...skip irrelevant code and adjust indentation
            IEnumerable<SimSeqData> res_enum;
            (res_enum,_status,_position,_exception) = 
                await runner.GetRequiredAsync(_params?.StartCount??IRunner.DEFAULT_ADVANCE, TraceIdentifier: HttpContext.TraceIdentifier);
            _results = res_enum.ToList();
            if(_status.IsFinal()) StartupStatusMessage=RUNNER_COMPLETED;
            else {
                StartupStatusMessage="The runner is running in background.";
            }
            _bkgIsCompleted = runner.IsBackgroundExecutionCompleted;
            _bkgProgress = runner.GetProgress().Progress;
            //...skip irrelevant code and adjust indentation
        }
````

**Pages\SequenceShowResults.cshtml**
````
@page "{key}" 
@model SapmleApplication.Pages.SequenceShowResultsModel
<!-- ... --->
<div style="display:inline-block; min-width:30%">
    <h3 style="text-align:center">Example results.</h3>
    <div>
        <span style="margin-left: 10px; font-weight:600">Status:</span> <span id="runner_status">@Model._status</span>
        <span style="margin-left: 10px; font-weight:600">#Records:</span> <span id="position">@Model._position</span>
    </div>
    <div>
        <span style="margin-left: 10px; font-weight:600">Bkg. progress:</span> <span id="bkg_progress">@Model._bkgProgress</span>
        <span style="margin-left: 10px; font-weight:600">Bkg. is completed:</span> <span id="bkg_completed">@Model._bkgIsCompleted</span>
    </div>
    <div class="records number">Number</div><div class="records name">Name</div><div class="records name">Data</div>
    <table style="display:block;border-style:solid;border-width:thin;height:15em;overflow:auto">
        <tbody id="results_table">
            @for(int row = 0; row<Model._position; row++) {
                <tr>
                    <td class="records number">@(Model._results[row].Number+1)</td>
                    <td class="records name">@Model._results[row].Name</td>
                    <td class="records data">@Model._results[row].Data</td>
                </tr>
            }
        </tbody>
    </table>
    <!-- ... --->
</div>
<!-- ... --->
````

Example of using GetAvailable and GetProgress methods and IsBackgroundExecutionCompleted property (sequence runner example in the example project):

**APIControllers\SampleController.cs**
````
    //public class GetAvailableRequest
    //{
    //    public ExtRunnerKey RunnerKey { get; set; }
    //    public Int32? Advance { get; set; }
    //}

    //public class SampleSequenceResponse
    //{
    //    public Int32 status { get; init; } = StatusCodes.Status200OK;
    //    public String? runnerStatus { get; set; }
    //    public Int32 position { get; set; }
    //    public Exception? exception { get; set; }
    //    public IEnumerable<SimSeqData>? result { get; set; }
    //    public Boolean isBackgroundExecutionCompleted { get; set; }
    //    public Int32 backgroundProgress { get; set; }
    //}

    [Route("api")]
    [ApiController]
    public class SampleController : ControllerBase
    {
        [HttpPost("[action]")]
        public ActionResult<SampleSequenceResponse> GetAvailable(GetAvailableRequest Request)
        {
            //...skip irrelevant code and adjust indentation
            // IEnumerable<SimSeqData> runner; Initialized elsewhere
            SampleSequenceResponse response = new SampleSequenceResponse();
            response.backgroundProgress=runner.GetProgress().Progress;
            response.isBackgroundExecutionCompleted=runner.IsBackgroundExecutionCompleted;
            RunnerStatus runner_status;
            (response.result, runner_status, response.position, response.exception) =
                runner.GetAvailable(Request.Advance??Int32.MaxValue, TraceIdentifier: HttpContext.TraceIdentifier);
            response.runnerStatus=runner_status.ToString();
            return response;
            //...skip irrelevant code and adjust indentation
        }
        //...skip irrelevant code and adjust indentation
    }
````

#### Getting information about the background process of the runner

The GetProgress() method of the runner interface returns information about the execution of the background process. It has no parameters and returns a pair of values combined into a RunnerBkgProgress structure: the number of the last execution point reached by the background process and an estimate for the number of the final execution point of the background process, if this estimate is possible (otherwise - null). The value returned by this method is most conveniently handled using a deconstructing assignment, for example, like this: `(Int32 progress, Int32? end) = runner .GetProgress();`

The Boolean IsBackgroundExecutionCompleted property of the runner interface indicates whether the background execution process of the runner has completed. 

Examples of using GetProgress() and IsBackgroundExecutionCompleted property are shown above along with GetRequiredAsync and GetAvailble examples.

#### Interrupting a runner execution.

The Abort method is used to immediately terminate the execution of the runner. It has a single optional parameter TraceIdentifier, which is used for tracing in a completely similar way to the parameter with the same name in the result-obtaining methods. This method returns the runner status corresponding to the reason for which the runner was actually terminated: it is not necessarily equal to Aborted, because the runner could have terminated earlier for another reason. 

Example of interrupting a runner by a request from the front-end part of a web application via API:

**APIControllers\SampleController.cs**
````
    //    public class AbortRequest
    //{
    //        public ExtRunnerKey RunnerKey { get; set; }
    //}
    //
    //    public class AbortResponse
    //{
    //    public Int32 status { get; init; } = StatusCodes.Status200OK;
    //    public String? runnerStatus { get; set; }
    //}


    public ActionResult<AbortResponse> Abort(AbortRequest Request)
    {
        IActiveSession session = HttpContext.GetActiveSession();
        if(session.IsAvailable && Request.RunnerKey.IsForSession(session)) {
            IRunner runner = session.GetNonTypedRunner(Request.RunnerKey.RunnerNumber, HttpContext);
            if(runner!=null) {
                AbortResponse response = new AbortResponse();
                response.runnerStatus=runner.Abort(HttpContext.TraceIdentifier).ToString();
                return response;
            }
        }
        return StatusCode(StatusCodes.Status410Gone);
    }
````


#### Another properties of a runner.

1. Runner identifier: RunnerId Id. The property type is a RunnerId structure. It contains two fields: String? ID - the identifier of the active session in which the runner is running and Int32 RunnerNumber - the runner number. This property is used primarily for tracing purposes: its value, if specified, is added by the runner and infrastructure code as a parameter to each log entry associated with this runner.
2. Completion token: CancellationToken CompletionToken - is set to the canceled state when the runner comes to its final stage. It is used primarily by the library infrastructure itself to determine when the runner can be removed from the storage and cleaned up. But this property can also be used to track when the runner enters the final stage by a user program.
3. Extra data: Object? ExtraData - arbitrary data associated with the runner. 

One can see the example of using ExtraData to pass data associated with the runner in the code of using sequence runner example in the example project where it is used to pass an example parameters between Razor Pages handlers:

**Pages\SequenceAdapterParams.cshtml.cs**
````
        public ActionResult OnPost() 
        {
            //...skip irrelevant code and adjust indentation
            SequenceParams seq_params=MakeSequenceParams();
            //...skip irrelevant code and adjust indentation
            IEnumerable<SimSeqData> sync_source = new SyncDelayedEnumerble<SimSeqData>(seq_params.Stages, new SimSeqDataProducer().Sample);
            (IRunner runner, int runner_number)= session.CreateSequenceRunner(sync_source, HttpContext);
            //...skip irrelevant code and adjust indentation
            ExtRunnerKey key = (session, runner_number);
            runner.ExtraData=seq_params;
            return RedirectToPage("SequenceShowResults", new { key });
            //...skip irrelevant code and adjust indentation
        }
````

**Pages\SequenceShowResults.cshtml.cs**
````
    public class SequenceShowResultsModel : PageModel
    {
        internal SequenceParams? _params;
        //...skip irrelevant code
        public async Task OnGetAsync([ModelBinder<ExtRunnerKeyMvcModelBinder>]ExtRunnerKey Key)
        {
            //...skip irrelevant code and adjust indentation
            var runner = active_session.GetSequenceRunner<SimSeqData>(Key.RunnerNumber, HttpContext);
            //...skip irrelevant code and adjust indentation
            _params = runner.ExtraData as SequenceParams;
            //...skip irrelevant code and adjust indentation
        }
````

**Pages\SequenceShowResults.cshtml**
````
@page "{key}" 
@model SapmleApplication.Pages.SequenceShowResultsModel
<!-- ... --->
<div style="display:inline-block; min-width:30%">
    <h3>Example parameters.</h3>
    <div style ="background-color: lightgray;">
        <div style="margin-bottom:0.2em"><b>Mode</b>:@(Model._params.Mode)</div>
        <div style="margin-bottom:0.2em"><b>Max #records at start</b>:@(Model._params.StartCount)</div>
        <div style="margin-bottom:0.2em"><b>Poll interval</b>:@(Model._params.PollInterval.TotalSeconds)s</div>
        <div style="margin-bottom:0.2em"><b>Max #records per poll</b>:
            @(Model._params.PollMaxCount.HasValue ? Model._params.PollMaxCount.Value.ToString():"not set")</div>
        <div>
            <div><b>Stages</b>:</div>
            <table style="margin-left:5px">
                <tr><th>#</th><th>Iterations:</th><th>Delay:</th><th>Ends at:</th></tr>
                @if (Model._params.Stages != null) {
                    int num = 0;
                    int end_pos = 0;
                    @foreach (SimStage stage in Model._params.Stages) {
                        <tr><td style="min-width:2em">@(num++)</td><td>@stage.Count</td><td>@(Model.SmartInterval(stage.Delay))</td><td>@(end_pos+=stage.Count)</td></tr>
                    }
                }
            </table>
        </div>
    </div>
</div>
<!-- ... --->
````

It is the sole responsibility of the application to clean up this extra data, if necessary. 

#### Monitoring a runner completion and performing cleanup afterwards.

To monitor a runner completion, an application can associate a callback function with the CompletionToken or wait for the runner's cleanup task, a reference to which can be obtained by the CleanupCompletionTask method of the active session interface IActiveSession, to complete. 

One can see the former approach in the class, associated with the SessionProcessRunner example, in a method that is used as the runner's background task (see also the description of the SessionProcessRunner standard runner below). It is used to cancel this background task then the runner ends its execution:

**Sources\RunnerRegistryObserver.cs**
````
        public async Task Observe(Action<Int32, Int32?> Callback, CancellationToken CompletionToken)
        {
            TaskCompletionSource<Int32> wait_source;
            TaskCompletionSource<Int32> completion_source =new TaskCompletionSource<Int32>();
            Callback.Invoke(_registry.Count, null);
            using(
                CancellationTokenRegistration completion_registration= CompletionToken.Register(
                    ()=>completion_source.SetCanceled(CompletionToken))
            ) {
                while(true) {
                    wait_source = Volatile.Read(in _currentWaitSource);
                    Int32 count = (await Task.WhenAny(wait_source.Task,completion_source.Task)).Result;
                    //One can come here only if wait_source.Task is ran to completion,
                    // because completion_source.Task never runs to completion, it can be completed via an OperationCanceledException only.
                    Callback(count, null);
                }
            }
            //One never come here to run this task to completion, as a loop above can be be terminated by a OperationCanceledException
            //This exception will be intercepted by calling code as an expected one.
        }

    }
````

One can see an example of using the latter approach in the code of ActiveSession library itself, in the CreateRunnerWithExclusive extension method of the IActiveSession interface mentioned earlier:
````
        public static KeyedRunner<TResult> CreateRunnerWithExclusiveService<TRequest,TResult> (
            this IActiveSession Session,
            TRequest Request,
            HttpContext Context,
            IDisposable ExclusiveServiceAccessor)
        {
            return InternalCreateRunnerExcl<TRequest,TResult>(Session, Request, Context, ExclusiveServiceAccessor);
        }

        internal static KeyedRunner<TResult> InternalCreateRunnerExcl<TRequest, TResult>(
            IActiveSession Session,
            TRequest Request,
            HttpContext Context,
            IDisposable? ExclusiveServiceAccessor)
        {
            KeyedRunner<TResult> result = Session.CreateRunner<TRequest, TResult>(Request, Context);
            if(ExclusiveServiceAccessor!=null) {
                (Session.TrackRunnerCleanup(result.RunnerNumber)??Task.CompletedTask)
                    .ContinueWith((_) => ExclusiveServiceAccessor.Dispose(),TaskContinuationOptions.ExecuteSynchronously);
            }
            return result;
        }
````

### Standard runners of the ActiveSession library

Currently, the ActiveSession library implements four standard runner classes. Three of them - the generic classes `EnumAdapterRunner<TItem>`, `AsyncEnumAdapterRunner<TItem>`, and `TimeSeriesRunner<TResult>` - are sequence runner classes. Their common features were discussed above.

#### Classes of enumeration adapter runners.

The first two sequential runner classes listed above, `EnumAdapterRunner<TItem>` and `AsyncEnumAdapterRunner<TItem>`, are very similar and should be discussed together. Both of these classes receive input sequences (or enumerations) of elements (in other words, records) of type TItem through their constructors. They then enumerate these sequences in the background, returning their sub-sequences through calls to the GetAvailable and GetRequiredAsync result methods, as described earlier. 

The difference between these two classes is how they enumerate the input sequences. The `EnumAdapterRunner<TItem>` class  receives an input sequence of type `IEnumreable<TItem>` that can only be enumerated synchronously. Any wait in an enumeration process will block the thread which is performing the enumeration, and this wait is expected because enumeration is considered I/O-bound. And the `AsyncEnumAdapterRunner<TItem>` class receives as a source an asynchronously enumerable sequence of type `IAsyncEnumreable<TItem>`, an enumeration of which does not lead to thread blocking.

To create instances of each of these two runner classes, an input parameter of one of two types can be passed to the IActiveSession.CreateRunner method: either a structure containing an enumeration of the desired type and additional enumeration parameters (they are essentially the same for both classes), or an enumeration itself (in this case,  default values will be assigned to other parameters). The mentioned structures with additional parameters have the `EnumAdapterParams<TItem>` and `AsyncEnumAdapterParams<TItem>` generic types and contain, in addition to the sequence to be enumerated in the Source field, a number of other fields, some of which will be described below.

These fields are as follows:
- The `int? DefaultAdvance` field contains the default value for the Advance parameter of the GetRequiredAsync method (see the description of this parameter in the chapter on runners).
- The `int? EnumAheadLimit` contains the maximum number of records fetched in the background but not yet returned, after which further fetching is blocked until the existing records are passed a result of a result-returning method, by default this value is set via configuration.
- The `bool StartInConstructor` field specifies whether an enumeration will be started immediately after creation of the runner instance, otherwise it will be started on the first call to the GetRequiredAsync method (the latter is the default option).
- The `bool PassSourceOnership` field specifies whether this runner instance is responsible for disposing the object implementing the input sequence during this instance disposing by calling its Dispose or DisposeAsync methods, if it implements the appropriate interface: IAsyncDisposable and/or IDisposable.

To simplify the creation of enumeration adapter runners, the ActiveSession library defines several overloaded extension methods for the IActiveSession interface named CreateSequenceRunner. These methods call IActiveSession.CreateRunner to create the required runner type, according to the parameter type passed to them.

To simplify the search for existing enumeration adapter runners, the ActiveSession library defines an extension method `GetSequenceRunner<TItem>` whose type parameter TItem determines the type of records in the sequence (i.e. the result type of the runner found will be `IEnumerable<TItem>`).

#### Class of the runner that creates time series (time-series runner):
The `TimeSeriesRunner<TResult>` class is another sequence runner class. It performs the background process that creates a time series: a sequence of pairs (measurement_time, measured_value), the measured_value field (the second item) of the pair being of type TResult. I.e. the records of the sequence returning by the background process have type ValueTuple{DateTime,TResult}. The measurement_value from the pair above is obtained by calling at the time of measurement a parameterless function (delegate) that returns a value of type TResult (performs measurement). Measurements begin immediately from the moment of the first call to the GetRequiredAsync method and are performed with the specified time interval. Waiting for the expiration of the next interval occurs asynchronously. Measurements are performed either the number of times specified, if this number is not specified, indefinitely, until the runner is terminated by calling its Abort method  or due to another reason.

To create an instance of the TimeSeriesRunner class, one can use one of the overloaded CreateTimeSeriesRunner extension methods of the IActiveSession interface. These methods accepts either two or three arguments, or a single parameter - structure of the `TimeSeriesParams<TResult>` type. The first argument of overloads with two or three parameters is a function (delegate) that performs measurements, the second is an interval between measurements. The third argument, if it exists, specifies count of measurements performed after an initial one. The `TimeSeriesParams<TResult>` argument for appropriate form of CreateTimeSeriesRunner contains values, corresponding arguments of other forms of this method in the fields Gauge, Interval and Count, respectively. To not specify the third argument, the Count field is set to null. This structure as well contains a number of additional fields, of which the following will be considered here (they are similar to the same fields of the previously considered structures (Async)EnumAdapterParams):
- The `int? DefaultAdvance` field contains the default value for the Advance parameter of the GetRequiredAsync method (see the description of this parameter in the chapter on runners).
- The `int? EnumAheadLimit` field contains the maximum number of records selected in the background but not yet returned; upon reaching this limit, further selection is blocked until the existing records are passed as part of the result of the method returning the results; by default, this value is set via configuration.
- The `bool StartInConstructor` field indicates whether the enumeration will start immediately or at the first call to the GetRequiredAsync method (the latter is the default option).

To simplify a search for existing time-series runners the ActiveSession library defines an extension method GetTimeSeriesRunner{TResult}, the TResult type parameter of which determines the type of values ​​in time-value pairs that are records of the returned sequence (i.e. the result type of the found runner will be IEnumerable{(DateTime, TResult)}).

#### Session process runner class.

Session process runner (its type is the generic class `SessionProcessRunner<TResult>`) does not belong to sequence runners, and therefore works according to slightly different rules. Its result-obtaining methods return single values ​​of type TResult as a result. It has different restrictions on parameters: restrictions on the value of the Advance parameter are the same - it must be greater than or equal to 0, but the value of StartPosition can be either any positive value not less than the current value of the Position property, or CURRENT_POSITION, i.e. -1, in which case this parameter is replaced with the current value of the Position property). This runner has one special interpretation for parameters: if the parameters have a default value for the GetRequiredAsync method - `StartPosition==CURRENT_POSITITION` and `Advance==0` - then Advance is replaced with 1, that is, it is assumed that a call with default parameters requests getting a result for the next execution point. The sum of Advance and StartPosition (taking into account the above parameter interpretations) is the execution point number for which the result is requested. The final difference between a session process runner and sequence runners is that its result-obtaining methods can be called independently of each other: at any time, several GetRequiredAsync methods can be called for this runner, asynchronously waiting for the desired execution point to be reached, and the synchronous GetAvailable method can always be called simultaneously.

A session process runner starts (immediately upon its creation) the task it created as a background process using a delegate, passed to it as an argument. For information on how this task is created, see the description of delegate parameters of the methods for creating session process runners below.  The runner's callback function (delegate) and the cancellation token from CompletionToken property of the runner are passed to the method that creates the background process task. This cancellation token can be used for coordinated cancellation of the background process task upon completion of the runner.

The background process calls the runner's callback function passed to it at times it chooses. These calls constitutes  the execution points of the session process runner, and the number of calls to the callback function is the execution point number for the session process runner. The background process passes an intermediate result (of type TResult) and an estimate of the number of the final execution point (this is what is returned by the runner's GetProgress method) via the callback function. The estimate passed may be different for different calls to the callback function. If the background process cannot produce such an estimate, then null is passed as an estimate. The callback function checks the cancellation token contained in the runner's CompletionToken property, and if cancellation is requested, it throws an OperationCanceledException.

The background process task may (but is not required to) complete normally, in which case it may return a result of type TResult or return no result at all. Normal completion of the background process is considered an additional execution point: a value one is added to the number of the reached execution point, and the result returned by the task, if any, becomes the final result of the runner. If the background process task does not return a result upon completion, the last returned intermediate result becomes the final result.

Completion of a background process in any way terminates all pending asynchronous calls to GetRequiredAsync. An estimate of the number of the final execution point, returned by the GetProgress method, becomes at the background process completion equal to the number of the last actually reached execution point.

According to a general rule for runners, when a background process terminates normally or due to an error, the session process runner does not come to a final stage until the result for the last execution point reached by the background process is requested and returned. And according the same rule, interruption of the runner by calling the Abort method from the application or by the ActiveSession library itself leads to its immediate termination.

The peculiarity of the background process runner is that it does not keep the obtained intermediate results except the last. Therefore, if the result-obtaining method is called (synchronously) for the execution point already passed by the background process, it returns the result for the last reached execution point, not for the requested. But the runner state (status and position of the runner) is still returned for the requested execution point. So if the application needs, the actual values ​​of the intermediate results of the background process together with the execution point numbers for such previously reached execution points, then the session process runner is not suitable for it. 

To create an instance of the SessionProcessRunner class, one can use one of the overloaded CreateSessionProcessRunner extension methods of the IActiveSession interface. These methods accepts one delegate parameter used to create a task running in the background. Four types of delegates can be used to create a session process runner.

First, these can be either delegates that directly create a background process task (in particular, delegates of methods marked as async) or delegates of ordinary (synchronous) methods used as a body for creating a background process task to be executed by one of the threads from the thread pool - that is, acting as a parameter of the constructor of the task of the corresponding type.

Second, tasks created using delegates can either return a result of type TResult (a task created using them will have type `Task<TResult>`) or not return a result at all (the task created will have type `Task`). To achieve this, delegates that directly create the background process task return a task of the desired type, and task body delegates can either return a TResult or not return any result at all.

## Release Notes

current - Make minor improvements to the EnumAdapterRunner and AsyncEnumAdapterrunner classes implememntations. Add end-to-end tests for these classes.

 Add IActiveSessionFeature.RefreshActiveSession method that allows obtaining a new active session after terminating the current one within the same request handler. Also add a new extension method with the same name to HttpContext class to use this feature.

 Semi-breaking change: move the properties - Id and CompletionToken - into the ILocalSession interface and property BaseId - into the IActiveSession interface. But because the previous versions of the librey always does use these interfaces together, no code using the library is expected to be broken, so I decide that this change does not deserve the new major version.

 Allow read-only access to the Properties dictionary of an active session object after the object is disposed.

1.1.1 - Set correct release notes in the package description.

1.1.0 - Add support for multiple active sessions within one ASP.NET Core session via active session identifier suffixes.
  Add a documentation article concerning ActiveSession configuration, including this new feature.

1.0.1 - Fix race conditions possibility in the EnumerbleRunnerBase.GetRequiredAsync method.

1.0.0 - Initial release