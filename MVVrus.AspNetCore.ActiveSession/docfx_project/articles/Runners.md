# Runners

*Runners* are instances of classes containing code of an operation that may execute itself in the background. To interact with an application and with another parts of the library runners must implement [`IRunner<TResult>`](/api/MVVrus.AspNetCore.ActiveSession.IRunner-1.html) generic interface. There is a number of *standard runners* that are implemented by the ActiveSession library. One can also implement its own custom runner class by implementing `IRunner<TResult>` generic interface. 

The runner interface contains a number of properties and methods that do not depend on a runner result type. Those members and properties are collected in a  type-agnostic (non-generic) runner interface [`IRunner`](/api/MVVrus.AspNetCore.ActiveSession.IRunner.html). Technically, the full runner interface `IRunner<TResult>` is inherited from this type-agnostic interface, adding result-dependent methods to it.

Each runner is entirely responsible for the execution of its operation:
- starting the operation;
- passing the initial result of the execution of the started operation and the current state of the runner to the HTTP request handler that launched the operation;
- executing the operation in the background;
- passing information about the state of the operation and the result obtained in the background to handlers of subsequent requests from the same session;
- completing the operation - naturally, upon its execution, or forcibly, either due to a call from a handler of a request belonging to the operation's session, or upon expiration of the waiting time for the next request.

