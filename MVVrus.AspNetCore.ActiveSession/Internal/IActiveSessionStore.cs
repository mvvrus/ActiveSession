﻿namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionStore
    {
        public IActiveSession? FetchOrCreateSession(ISession Session, String? TraceIdentifier, String? Suffix);
        public KeyedRunner<TResult> CreateRunner<TRequest, TResult>(ISession Session, 
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            TRequest Request, String? TraceIdentifier);
        public IRunner? GetRunner(ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager,
            Int32 RunnerNumber, String? TraceIdentifier);
        public ValueTask<IRunner?> GetRunnerAsync(
            ISession Session,
            IActiveSession ActiveSession,
            IRunnerManager RunnerManager, 
            Int32 RunnerNumber, String? TraceIdentifier, CancellationToken Token
        );
        public Task TerminateSession(ISession Sesion, IActiveSession Session, IRunnerManager RunnerManager, String? TraceIdentifier);
        public IActiveSessionFeature AcquireFeatureObject(ISession? Session, String? TraceIdentier, String? Suffix);
        public void ReleaseFeatureObject(IActiveSessionFeature Feature);
        public ActiveSessionStoreStats? GetCurrentStatistics();
    }
}