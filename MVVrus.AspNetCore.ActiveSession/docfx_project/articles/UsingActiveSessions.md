# Using active sessions
## Accessing the active session object
It is well known that when processing a request, the middleware pipeline receives a request context - an instance of a class derived from HttpContext as input to the pipeline. Generally this context is available to request handler in most frameworks - as a property named HttpContext of the class containing handlers in MVC and Razor Pages frameworks, as a binding to a parameter of type HttpContext in Minimal API framework etc. And to access the active session object associated with the request from a request handler one can use the extension method GetActiveSession() of the HttpContext class. The example of accessing an active session associated with a request is a part of the example in the "Creating a new runner" section below.

## Using the IActiveSession - active session object interface.
Before using IActiveSession interface methods one should ensure that the active session is available. To do this one need to check that the IsAvailable property of the received IActiveSession interface contains true. The example in the next section demonstrate, among other things, how to obtain a reference to the active session object for a request and verify that the active session is available. 

### Creating a new runner
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
### Obtaining a reference to the existing runner
Use the generic `GetRunner<TResult>(int, HttpContext)` method of the IActiveSession interface to obtain a reference to an existing runner with a result type TResult which executes within current active session and is registered under a runner number passed as the first parameter. If no runner registered under the specified number exists, or if the runner registered has incompatible result type the method returns null. 

Parameters of GetRunner generic method do not depend on its type parameter, therefore the type parameter cannot be inferred and must be specified.

The ActiveSession library defines a number of extension methods for the IActiveSession interface to make it easier to specify the type parameter while obtaining standard runners. These extension methods - GetSequenceRunner and GetTimeSeriesRunner - are generic too. They have the same type parameters as their corresponding standard runner classes.

Use the GetNonTypedRunner(int, HttpContext) method of the IActiveSession interface to obtain a reference to a type-agnostic runner interface (IRunner) of an existing runner.

Examples of obtaining references to existing runners may be found below in the "Using an external runner identifier" section.

## Terminating the active session

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

To continue using the ActiveSessions library in a handler that previously called Terminate method the handler may (since version 1.2) call the RefreshActiveSession extension method for the request context and then obtain a reference to the newly created active session object in the usual way by calling GetActiveSession.

## Active session-specific service container.
Each active session has its own service scope with its own service container. This scope exists as long as the active session object exists (and is not disposed). The IActiveSession interface of the active session object contains a reference to the services container specific to the active session in the SessionServices property. Maintaining this specific to the active session scope allows use of services with the Scoped lifetime in multiple request handlers and inside runners giving them the same single copy of each service for the duration of the active session.



