# Using external runner identifiers to pass runner identification to a front-end part

One does not simply pass the runner number into the front-end part of a web application to make a reference suitable for obtaining the same runner while handling the next request in the back-end. This is because, although the runner number fully identifies a runner within single active session, there is no guarantee that the next request will belong to the same active session. So one should pass to the front-end not only the runner number, but the information that uniquely identifies current active session among all ever existing  or will ever be created active sessions. To perform this task the ActiveSession library defines the ExtRunnerKey class. In addition to the runner number, it contains data that uniquely identifies the active session. The instance method IsForSession(IActiveSession) checks whether the active session passed as an argument is the same one for which this instance was created. 

There are two ways to use the ExtRunnerKey object. The first way is to pass its value back as an object. An object may be passed as a whole being serialized into JSON or XML in an API call parameters. Or it may be passed as a set of form values with appropriate names and bound to a handler parameter of type ExtRunnerKey via the MVC or Minimal API binding process. This method of passing an external runner identifier is well suited for processing API calls from scripts and handling input from forms.

Another way to pass a value of ExtRunnerKey is to serialize it into a string via ExtRunnerKey.ToString(). This method is well suited to pass an external runner identifier as part of an URL path or query values, because the serialized value does not contain symbols, that may not be used in those URL parts. The passed value may be de-serialized using ExtRunnerKey.TryParse method.

Here are two examples of passing runner identification between server (back end) and browser(front end) parts of a web application. Each example comprises a number of files. Obtaining references to runners is also covered by these examples. Code snippets for this example are taken from an example project for ActiveSession library.


## Passing an external runner identifier as an object for API call

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

## Passing an external runner identifier serialized to a string in the URL 

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

