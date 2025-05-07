# Using dependency injection with active sessions.

## Obtaining Scoped services for use by runners.

Dependency injection is often used by ASP.NET Core-based frameworks (MVC, etc.) to pass a reference to a service to be used by a request handler.
Such services should be used with caution with ActiveSession library: if the service is registered with Scoped lifetime, one should not pass this service to a runner. 

This is because objects implementing such services are intact only during the current request processing and are generally unavailable for the whole duration of the active session. Technically, such services are resolved form `HttpContext.RequestServices` container, which is disposed after the HTTP request processing is completed. Thus, the same limitation applies to services obtained from the above container calling its `IServiceProvider` interface methods manually (using the so-called "service locator pattern"). Such services should be obtained from a container associated with an active session (`IActiveSession.SessionServices`) for use with the ActiveSession library.

A convenient way to obtain services from the active session service container in an HTTP request handler is to use *session container adapter service*
`IActiveSessionService<TService>` as the dependency type instead of TService itself. The `IActiveSessionService<TService>` adapter service is one of the ActiveSession library infrastructure services. When obtained, its Service property contains a reference to the object implementing the actual service TService that was requested. If the actual service cannot be obtained, either because the service is not registered with the container or for some other reason, this property contains null. The session container adapter service itself, `IActiveSessionService<TService>`, is registered with the Scoped lifetime and can be obtained from the request service container, so it is eligible for dependency injection in frameworks supporting dependency injection. If the active session is available, the property Service of an interface contains a reference obtained from the active session container. Otherwise, if an active session is not available, the IActiveSessionService implementation falls back to the request container and resolves the requested service from it. In this way, the reference to a service obtained through this session container adapter may be used by a handler instead of the service itself, regardless of whether an active session is available. The IsFromSession property of this session container adapter service indicates whether the received service implementation came from the active session container and is suitable for use in the active session runners.

## Working with Scoped services that does not allow a concurrent access to them
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

