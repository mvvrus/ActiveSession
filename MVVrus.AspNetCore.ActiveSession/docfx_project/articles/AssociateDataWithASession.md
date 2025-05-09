# Associate data with the active session or the group of active sessions and track the active session/session group completion and cleanup.

The Properties property of the active session interface ILocalSession common to active sessions and their groups allows to associate arbitrary objects with the active session or group. This property is a dictionary with a string as a key and an arbitrary object as a value. Objects can be added with their corresponding keys and retrieved by those keys. Concurrent access to the Properties dictionary is allowed. If an active session/group object is disposable (i.e. implement the IDisposable and/or IAsyncDisposable interfaces. the former interface being implemented in the current version of the ActiveSession library by both active session and active session group objects implementations),  then disposing it switches Properties to read-only mode: all existing objects at the corresponding keys will remain accessible, but no more objects can be added, and no existing objects can be removed from the Properties dictionary.

If the objects added to Properties are disposable, one should monitor the completion and cleanup of the active session or the group to perform proper cleanup of these associated objects. There are two events in the lifecycle of an active session that you can monitor. First, the ILocalSession interface contains a CompletedToken property of type CancellationToken that will be canceled when the active session is completed and ready to be cleaned up. Additionally, IActiveSession interface contains the CleanupCompletionTask property containing a task that completes when the active session is finished cleaning up.

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
                lock(ActiveSession.Properties) { //Lock shareable resource - Properties dictionary
                    result = new RunnerRegistry(); //Create new registry to add it to Properties
                    if(ActiveSession.Properties.TryAdd(REGISTRY_NAME, result)) //Use "double check pattern" with second check within lock block
                        //Addition was successful
                        ActiveSession.CleanupCompletionTask.ContinueWith((_) => result.Dispose()); //Plan disposing the registry added after end of ActiveSession
                    else {
                        //Somebody added a registry instance already between checks
                        cached_result = ActiveSession.Properties[REGISTRY_NAME]; //Get previously added registry
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

The ActiveSession library implements the [TakeOwnership](/api/MVVrus.AspNetCore.ActiveSession.LocalSessionExtensions.TakeOwnership.html#MVVrus_AspNetCore_ActiveSession_LocalSessionExtensions_TakeOwnership_MVVrus_AspNetCore_ActiveSession_ILocalSession_System_IDisposable_) extension method for the ILocalSession interface, which is intended to facilitate the timely disposal of objects placed in the Properties dictionary for which disposal is provided (i.e. their class implements the IDisposable interface). This method takes as a parameter a reference to the IDisposable of the object to be disposed and registers a callback for the CompletionToken cancellation token that disposes the object when this token is canceled. The TakeOwnership method returns a reference to the IDisposable implementation for the inner object, which contains a reference to the object to be disposed along with the early mentioned callback registration. Disposing this inner object by calling its Dispose() method allows one to dispose the disposable object and simultaneously unregister the callback for disposing it in the cancellation token, after the object of the active session or group of active sessions is disposed.
