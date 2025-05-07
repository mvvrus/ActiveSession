# Using the ActiveSession library for handling web requests

The common pattern of using ActiveSession library in web request handlers is the following.

First, the handler obtains reference to the active session to which the request to be processed belongs.
Then depending whether the endpoint that processes the request should start a new runner to perform the request or it should work with an existing one, it creates or finds the exiting runner, extracts (possibly asynchronously) its state and result (partial/intermediate or full/final) and returns the result along with the state as a result of the request.
Both these steps are described in the [Using active sessions](UsingActiveSessions.md) article.

Alternatively the handler may terminate the current active session as it described in the same [Using active sessions](UsingActiveSessions.md) article.

To pass runner identifiers from a front-end application part to request handlers applications should use [external runner identifiers](UsingExternalRunnerIdentifiers.md).

To work with data common to the whole user session applications may use [active session groups](UsingActiveSessionGroups.md).

Applications may associate arbitrary data with active sessions or active session groups as it is described in the [Associate data with an active session or a group](AssociateDataWithASession.md) article.

Active sessions and their groups both support their own scopes for service containers and allow obtaining services with the Scoped lifetime from these containers. Scoped services obtained this way exist throughout the entire lifetime of the active session or the group of active sessions, respectively.

The ActiveSession library has features that make it easy to consume scoped services from active session-scoped service containers via dependency injection. These features are described in the [Using dependency injection with active sessions](UsingDependencyInjection.md) article.