# Active Session Groups (available since version 1.2).

An *active session group **is a collection of sessions that are based on a single, common user session. From the point of view of the ActiveSession library, a user session is a storage of variables that are saved between requests of one user. The selection of the active session to which the request belongs is made based on these variables along with other request parameters.

In principle, a user session is a somewhat abstract concept. It is implemented by framework facilities external to the ActiveSession library, and it is the framework that determines which user session the current request belongs to.

The current version of the ActiveSession library uses a single implementation of a user session - based on the ASP.NET Core session, which is accessible through the Session property of the HttpContext request context.

For each active session group, the ActiveSession library creates and maintains an active session group object. 

An active session group object is mainly intended for use in a middleware, not in endpoint handlers and because of this lacks convenient extension methods facilitating use of dependency injection of services associated with this objects.