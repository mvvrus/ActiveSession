# ActiveSession library.

## About the library
The ActiveSession library is designed to execute code in the background that provides results for several logically related HTTP requests and to share data between them. This set of logically related requests, as well as the code executing between them and their shared data, will be further referred to as an *active session* (or just session). 

Background execution of code using the ActiveSession library is initiated by a handler of a request associated with an active session. 
Typically, but not necessarily, this initiating query is the first one in the session. It returns a subset of the result for the portion (range) of the full execution result requested through its parameters. After the initiating request has completed, execution of the session code may continue in the background.  Typically the result returned by the initiating request is intermediate, but if all execution falls within the requested range, the result will be final and there will be no further background code execution. Subsequent requests in the session, if any, may return results obtained in the background between requests to the client - intermediate or, if execution completes, final. In addition, any subsequent request in the session may terminate the code running in the background. The background execution may also be interrupted when subsequent client requests expire (timeout).

Unlike the other background execution mechanism in the ASP.NET Core application, Background Services, which is global to the entire application, each active session is associated with a single client, meaning it is different for each client. The binding of the active session to the client is based on the Sessions feature of ASP.NET Core: each active session is associated with a specific session supported by this mechanism. An active session becomes available when processing a request, if the request is made within the framework of the corresponding ASP.NET Core session.

## Components of the library and its extensions

### Runners
*Runners* are instances of classes containing code of an operation that may execute itself in the background. To interact with an application and with another parts of the library runners must implement `IRunner<TResult>` generic interface. There is a number of *standard runners* that are implemented by the ActiveSession library. One can also implement its own custom runner class by implementing IRunner&lt;TResult> generic interface. 

The runner interface contains a number of properties and methods that do not depend on a runner result type. Those members and properties are collected in a  type-agnostic (non-generic) runner interface IRunner. Technically, the full runner interface `IRunner<TResult>` is inherited from this type-agnostic interface, adding result-dependent methods to it.

Each runner is entirely responsible for the execution of its operation:
- starting the operation;
- passing the initial result of the execution of the started operation and the current state of the runner to the HTTP request handler that launched the operation;
- executing the operation in the background;
- passing information about the state of the operation and the result obtained in the background to handlers of subsequent requests from the same session;
- completing the operation - naturally, upon its execution, or forcibly, either due to a call from a handler of a request belonging to the operation's session, or upon expiration of the waiting time for the next request.

### Active Session Objects 
*Active session objects* represent currently existing active sessions. An HTTP request handler receives a reference to the active session object of which it is a part. The active session object implements the IActiveSession interface, which allows the request handler to interact with the ActiveSession library. In particular, the handler can:
- create new runners in this session;
- get references to runners running in this session;
- get services from the service container associated with this session;
- read and write shared data of this session;
- terminate this session.

### ActiveSession library infrastructure

The *ActiveSession library infrastructure* executes all internal operations required for the library to perform its work; applications do not interact directly with the infrastructure.
In particular, the ActiveSession library infrastructure performs the following functions:
- creates or finds an existing active session object to which the processed request belongs and provides a reference to this object to request handlers;
- stores, tracks an expiration of a timeout and terminates (after the expiration or upon call from the application) active session objects;
- terminates all runners that were working in the terminated active session and disposes its active session object;
- using runner factories (see below) registered in the application service container, creates new runners according to calls from active session objects to work in those sessions; 
- stores runners, finds runners requested by active session objects and returns references to them;
- track an expiration of a timeout and terminates the work of the runners after this time; - cleans up runners that have been terminated for one reason or another, if such cleaning is provided for the runner class;

### Runner factories

*Runner factories* are objects that are designed to create runners. The ActiveSession library infrastructure receives runner factories from the service container. Therefore, runner factory classes must be registered during application initialization in the application services container (AKA DI container) as implementations of the interface that the infrastructure will request from the container - the specialization of the generic interface IRunnerFactory&lt;TRequest,TResult>. This interface has two type parameters: TRequest is a type of an argument that is passed by the application to create the runner, TResult is a result type of the created runner. 

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
Before using ActiveSession library one should ensure that the active session is available. To do this one need to check that the IsAvailable property of the received IActiveSession interface contains true. The example in the next section demonstrate, among other things, how to obtain a reference to the active session object for a request and verify that the active session is available. 

#### Creating a new runner
Use the generic CreateRunner&lt;TRequest,TResult>(TResult, HttpContext) method of the IActiveSession interface to create a new runner to run in this active session. This method returns a generic record struct with two fields: Runner, with a reference to the newly created runner, and RunnerNumber, with the number assigned to this runner in the active session. Being generic, this method also has two type parameters: TRequest, which is the type of the first parameter to pass to this method, and TResult, which is the type of the result that will be returned by the new runner. None of the parameters passed to this method depend on the second type parameter, so the type parameters of this method cannot be inferred by a compiler and must be specified explicitly.

Because of this inconvenience, the ActiveSession library defines a number of extension methods for the IActiveSession interface, namely CreateSequenceRunner, CreateTimeSeriesRunner, and CreateSessionProcessRunner, which create the ActiveSession library's standard runners. Although these methods are also generic, they are more convenient to use: they have only one type parameter (the same as the type parameter of the standard runner class to be created), which can be inferred from their parameters. These methods also return a generic record struct described earlier with type parameter set appropriately.

The following example demonstrates creation of a runner as well as accessing the active session object for the request and checking that the active session is available (the Razor Pages framework is used in the example):

**Pages\SequenceAdapterParams.cshtml.cs**
````
        //Come here after the data are entered to the form on the page (the page template is beyond the scope of the example)
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
                    
                    //This part will be explained later in a section dedicated to an external runner identifier usage
                    ExtRunnerKey key = (session, runner_number); //Make an external identifier
                    return RedirectToPage("SequenceShowResults", new { key }); //Pass the external identifier by a part of the redirection URL path
                }
                else //An active session is unavailable 
                    return StatusCode(StatusCodes.Status500InternalServerError);
            }
            //Repeat input due to invalid data entered
            return Page();
        }
````
#### Obtaining the existing runner
Use the generic GetRunner&lt;TResult>(int, HttpContext) method of the IActiveSession interface to obtain a reference to an existing runner with a result type TResult which executes within current active session and is registered under a runner number passed as the first parameter. If no runner registered under the specified number exists, or if the runner registered has incompatible result type the method returns null. 

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

The Properties property of the active session interface IActiveSession allows to associate arbitrary objects with the active session. This property is a dictionary with a string as a key and an arbitrary object as a value. Objects can be added with their corresponding keys and retrieved by those keys.

If the objects added to Properties are cleanup aware (i.e. implement the IDisposable and/or IAsyncDisposable interfaces), you should monitor the completion and cleanup of the active session to perform proper cleanup of these associated objects. There are two events in the lifecycle of an active session that you can monitor. First, the IActiveSession interface contains a CompletedToken property of type CancellationToken that will be canceled when the active session is completed and ready to be cleaned up. Second, CleanupCompletionTask contains a task that completes when the active session is finished cleaning up.

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
                lock(ActiveSession.Properties) { //Lock shareble resource - Properties dictionary
                    result = new RunnerRegistry(); //Create new registry to add it to Properties
                    if(ActiveSession.Properties.TryAdd(REGISTRY_NAME, result)) //Use "double check pattern" with second check within lock block
                        //Addition was successful
                        ActiveSession.CleanupCompletionTask.ContinueWith((_) => result.Dispose()); //Plan disposing the registry added after ebd of ActiveSession
                    else {
                        //Somebody added registry already between checks
                        cached_result = ActiveSession.Properties[REGISTRY_NAME]; //Get previosly added registry
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

One does not simply pass the runner number into the front-end part of a web application to make a reference suitable for obtaining the same runner while handling the next request to the back-end. This is because, although the runner number fully identifies a runner within single active session, there is no guarantee that the next request will belong to the same active session. So one should pass to the front-end not only the runner number, but the information that uniquely identifies current active session among all ever existing and would created active sessions. To perform this task the ActiveSession library defines the ExtRunnerKey class. In addition to the runner number, it contains data that uniquely identifies the active session. The instance method IsForSession(IActiveSession) checks whether the active session passed as an argument is the same one for which this instance was created. 

There are two ways to use the ExtRunnerKey object. The first way is to pass its value back as an object. An object may be passed as a whole being serialized into JSON or XML in an API call parameters. Or it may be passed as a set of form values with appropriate names and bound to a handler parameter of type ExtRunnerKey via the MVC or Minimal API binding process. This method of passing an external runner identifier is well suited for processing API calls from scripts and handling input from forms.

Another way to pass a value of ExtRunnerKey is to serialize it into a string via ExtRunnerKey.ToString(). This method is well suited to pass an external runner identifier as part of an URL path or query values, because the serialized value does not contain symbols, that may not be used in those URL parts. The passed value may be de-serialized using ExtRunnerKey.TryParse method.

Here are two examples of passing runner identification between server (back end) and browser(front end) parts of a web application. Each example comprises a number of files. Obtaining references to runners is also covered by these examples. Code snippets for this example are taken from an example project for ActiveSession library.


#### Passing an external runner identifier as an object for API call

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

#### Passing in URL an external runner identifier serialized into a string

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

2.Bind the value of the path segment to the parameter of the GET request handler for the target Razor Page, the binder class being specified via `[ModelBinder]` attribute:

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

### ActiveSession and dependency injection

#### Obtaining Scoped services

Dependency injection is often used by ASP.NET Core-based frameworks (MVC, etc.) to pass a reference to a service to be used by a request handler.
Such services should be used with caution with ActiveSession library: if the service is registered with Scoped lifetime, one should not pass this service to a runner. To use such a service with the ActiveSession library, one should use the interface type `IActiveSessionService<TService>` as the type of the service instead of `TService` and get the actual service reference from the Service property of this interface.

This is because objects implementing such services are intact only during the current request processing and are generally unavailable for the whole duration of the active session. Technically, such services are resolved form `HttpContext.RequestServices` child container, which is disposed after the HTTP request processing is completed. Thus, the same limitation applies to services obtained from the above container calling its `IServiceProvider` interface methods manually (using the so-called "service locator pattern"). Such services should be obtained from a child container associated with an active session (`IActiveSession.SessionServices`) for use with the ActiveSession library.

The `IActiveSessionService<TService>` generic service gives a convenient way to use child service container of the active session. A reference to the object implementing the real requested service is contained in the Service property. If the object could not be obtained (either because the service is not registered in the container or for some other reason), this property will contain null. The IActiveSessionService service itself is registered with the Scoped lifetime. But if the active session is available, the property Service of an object implementing IActiveSessionService contains a reference obtained from the active session child container. If an active session is not available, the IActiveSessionService implementation falls back to the request child container, and the Service property contains a reference to the service resolved form the request child container. So the reference may be used by a handler regardless of whether an active session is available. The `IActiveSessionService.IsFromSession` property indicates whether the received service object came from the active session child container and is suitable for use in the active session.

#### Working with Scoped services that does not allow a parallel access to them
Some services intended to be registered with Scoped lifetime are not designed to allow parallel access to their instances. These services are well suited to be used by traditional HTTP request handling process. This is because each request has its own scope, so an object instance implementing such a service for the request cannot be accessed from handlers of other requests. Important examples of such services are database contexts from Entity Framework Core. 
Using such services within an active session scope instead of single HTTP request scope makes a challenge. This is because their implementing instances are potentially shareable between all handlers of requests belonging to the same active session.

Currently the ActiveSession library does not offer any tools that can facilitate application authors to answer this challenge. This state may change (and *is planned* to change), but this version of the library offers little to do with the problem of parallel access to Scoped services that does not allow it. 

The following guidelines should be followed to mitigate this parallel access problem.
- use such a service only for one type of a runner;
- work with only one instance of the runner of this type  at a type;
- do not call a result-obtaining method (see appropriate section below) of the instance while another result-obtaining is executing; this rule is fulfilled automatically for sequence runners (see appropriate section below).

An awkward but effective way to ensure that above rules are followed is to create only one runner in the session, wait for its completion and then terminate the session.

The last but not least way of solving this parallel access problem is to avoid it. For example, one may use DbContextFactory as a service to work with Entity Framework in an environment where the ActiveSession library is used.

### Working with runners

A runner is an object instance that executes a background operation, returns operation results and interacts with other parts of the ActiveSession library supporting its execution. A runner may be considered as consisting of a background part and a foreground part.

A background part is generally outside the scope of this section. All one need to know about it is that the background part performs some (runner-specific) operation passing through a series of execution points, where it return intermediate results to be used by a foreground part. Each execution point has a number which monotonically increases (typically by 1). The background part may complete naturally (but this is not required) with returning the final result or due to an error, and may be interrupted through its foreground part. The progress of the background execution is reflected by the GetProgress() method and the IsBackgroundExecutionCompleted property of the runner interface. The former returns a pair of values: the number of the last execution point reached and an estimation of the number of the last execution point (or null if such an estimation is not available). The latter returns true if the background execution is already completed.

#### Runner state

A runner state consists of a runner status, a runner position, and possibly an exception occurred during background execution.

The runner status, a value of RunnerStatus enumerable type, describes a stage of its life cycle. A runner's existence begins with its initial stage, in which the background operation has not yet started. The status value for this stage is always NotStarted. 

When background execution begins, the runner enters the executing state. There are two status values that correspond to the executing stage. The Stalled value indicates that a result for the last execution point reached by the background operation has already been returned, but the background operation is still executing. The Progressed value indicates that the result for the last execution point reached by the background operation has not yet been returned, regardless of whether the background operation has completed or not. To check whether the status value corresponds to an executing stage, the extension method IsRunning() for the RunnerStatus class may be used.

A runner comes to its final stage after its background operation has completed for any reason and all pending result (if any) have been returned. Status values corresponding to the final stage indicates the reason for the background operation's completion. A Completed value indicates that the background operation was completed naturally. A Failed value indicates that the background operation was terminated due to an exception occurred during its execution, the exception becoming a part of the runner state. An Aborted value indicates that the runner execution was interrupted by an application call or by the library itself due to a termination of the active session to which the runner belongs, any pending results being ignored in this case. To check whether the status value corresponds to a final stage, the extension method IsFinal() for the RunnerStatus class may be used.

The runner position is the last execution point number for which results of background execution have been returned.

The exception is set as a part of the runner state only when the runner enters its final Failed state. In this case, it contains the exception that occurred during background execution. In all other cases, the exception is null.

The current state of a runner is available through the Status, Position and Exception properties of a runner interface. It is also returned along with a result by result-obtaining methods. In this case, the runner state at the time the result was received is returned.

#### Sequence runners 

#### Methods for obtaining results

#### Specifying execution point for which result will be obtained 

#### Aborting a runner execution 

#### Associating an object with a runne and tracking its completion and cleanup



### Standard runners of the ActiveSession library

#### Классы, работающие с последовательностями записей: общие свойства.

#### Классы-адаптеры для внешних последовательностей. 

#### Класс исполнителя создающего временные ряды.

#### Класс исполнителя, фонового процесса.
