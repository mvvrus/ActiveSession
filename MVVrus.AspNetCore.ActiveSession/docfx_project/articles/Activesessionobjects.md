# Active session objects 
*Active session objects* represent currently existing active sessions. An HTTP request handler receives a reference to the active session object of which it is a part. Each active session objects has a unique identifier. The active session object implements the [IActiveSession](/api/MVVrus.AspNetCore.ActiveSession.IActiveSession.html) interface, which allows the request handler to interact with the ActiveSession library. In particular, the handler can:
- obtain the active session identifier;
- create new runners in this session;
- get references to runners running in this session;
- get services from the service container associated with this session;
- read and write shared data of this session in the form of arbitrary objects with string keys assigned to them, keys serves as a means to find the appropriate object;
- terminate this session;
- detect whether the sesson has been finished and/or assign code that will be executed when the session is finished.

Starting from v.1.1 the ActiveSession library supports multiple simultaneous active sessions associated with a single ASP.NET Core session, or, starting from version 1.2 - a single session group. Each such an active session is represented by its own active session object. These objects are distinguished by active session identifier suffixes assigned to them. Scopes of these active sessions are determined by ActiveSession library infrastructure, based the library setup during application startup, as described in the documentation. 

