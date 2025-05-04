# ActiveSession library infrastructure

The *ActiveSession library infrastructure* executes all internal operations required for the library to perform its work; applications do not interact directly with the infrastructure. 

In particular, the ActiveSession library infrastructure performs the following functions:
- creates or finds an existing [active session object](Activesessionobjects.md) to which the processed request belongs and provides a reference to this object to request handlers;
- stores, tracks an expiration of a timeout and terminates (after the expiration or upon call from the application) active sessions, disposing their objects;
- terminates all runners that were working in the terminated active session and disposes its active session object;
- creates, maintains make available and disposes after their expiration [active session group objects](Activesessionobjects.md) (since version 1.2);
- creates  in response to calls to active session objects new [runners](Runners.md) to work in those active sessions making use of [runner factories](Runnerfactories.md) registered in the application service container; 
- stores runners between requests to the active sessions, finds runners requested by active session objects and returns references to them;
- tracks an expiration of a runner timeout and terminates runners and remove them from the store - upon runner completion or after the timeout expiration;
- disposes runners that have been terminated for any reason, if this disposing is provided by the runner class.

