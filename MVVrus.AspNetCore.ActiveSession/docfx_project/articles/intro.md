# ActiveSession library.

## About the library
The ActiveSession library is designed to execute code in the background that provides results for several logically related HTTP requests and to share data between them. This set of logically related requests, as well as the code executing between them and their shared data, will be further referred to as an *active session* (or just session). 

Background execution of code using the ActiveSession library is initiated by a handler of a request associated with an active session. 
Typically, but not necessarily, this initiating query is the first one in the session. It returns a subset of the result for the portion (range) of the full execution result requested through its parameters. After the initiating request has completed, execution of the session code may continue in the background.  Typically the result returned by the initiating request is intermediate, but if all execution falls within the requested range, the result will be final and there will be no further background code execution. Subsequent requests in the session, if any, may return results obtained in the background between requests to the client - intermediate or, if execution completes, final. In addition, any subsequent request in the session may terminate the code running in the background. The background execution may also be interrupted when subsequent client requests expire (timeout).

Unlike the other background execution mechanism in the ASP.NET Core application, Background Services, which is global to the entire application, each active session is associated with a single client, meaning it is different for each client. The binding of the active session to the client is based on the Sessions feature of ASP.NET Core: each active session is associated with a specific session supported by this mechanism. An active session becomes available when processing a request, if the request is made within the framework of the corresponding ASP.NET Core session.

## References
1. [The repository with the ActiveSession library source.](https://github.com/mvvrus/ActiveSession)
2. [The library NuGet Package.](https://www.nuget.org/packages/MVVrus.AspNetCore.ActiveSession/) 
3. [The repository with examples of the library usage.](https://github.com/mvvrus/ActiveSessionExamples) Most îf exmamples in this document (namely those, for which file names are specified) are taken from SampleApplication project in this repository. These file names are shown relative to this project's directory.

## Prerequisites

The ActiveSession library uses session state feature of ASP.NET Core framework, which in turn is based upon .NET distributed caching. To begin using ActiveSession Library these features must first be initialized and configured.



