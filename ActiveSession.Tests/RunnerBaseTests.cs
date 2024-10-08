﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class RunnerBaseTests
    {
        //Test group: creating and disposing a RunnerBase instance and test  CompletionToken property
        [Fact]
        public void RunnerBaseCreationAndDisposing()
        {
            CancellationTokenSource cts;
            RunnerBase runner_base;
            CancellationToken token;
            //Test case: create RunnerBase instance with internal CancellationTokenSource
            //Arrange and Act
            runner_base=new RunnerBaseImpl(null,false);
            //Assess
            Assert.True(runner_base.CompletionToken.CanBeCanceled);

            //Test case: dispose RunnerBase instance with internal CancellationTokenSource
            //Arrange
            token=runner_base.CompletionToken;
            //Act
            runner_base.Dispose();
            //Assess
            Assert.Throws<ObjectDisposedException>(() => token.WaitHandle);

            //Test case: create RunnerBase instance with external CancellationTokenSource with ownership passed
            //Arrange
            cts=new CancellationTokenSource();
            //Act
            runner_base=new RunnerBaseImpl(cts, true);
            //Assess
            Assert.Equal(cts.Token, runner_base.CompletionToken);

            //Test case: dispose RunnerBase instance with external CancellationTokenSource with ownership passed
            //Arrange
            token=cts.Token;
            //Act
            runner_base.Dispose();
            //Assess
            Assert.Throws<ObjectDisposedException>(() => token.WaitHandle);

            //Test case: create RunnerBase instance with external CancellationTokenSource with no ownership passed
            //Arrange
            cts=new CancellationTokenSource();
            //Act
            runner_base=new RunnerBaseImpl(cts, false);
            //Assess
            Assert.Equal(cts.Token, runner_base.CompletionToken);

            //Test case: dispose RunnerBase instance with external CancellationTokenSource with no ownership passed
            //Arrange
            token=cts.Token;
            //Act
            runner_base.Dispose();
            //Assess
            WaitHandle t = token.WaitHandle;
            cts.Dispose();
        }

        //Test case: getting and setting  Position property
        [Fact]
        public void Position()
        {
            const Int32 NEW_POS = 42;
            //Arrange
            RunnerBaseImpl runner = new RunnerBaseImpl();
            //Act&Assess
            Assert.Equal(0, runner.Position);
            runner.SetPosition(NEW_POS);
            Assert.Equal(NEW_POS, runner.Position);
        }

        //Test group: getting and setting State property
        [Fact]
        public void StateChanges() {
            RunnerBaseImpl runner;
            //Test case: assure the initial state is NotStarted
            runner=new RunnerBaseImpl();
            Assert.Equal(RunnerStatus.NotStarted, runner.Status);

            //Test case: successful start - StartRunning to Stalled when NotStarted
            Assert.True(runner.StartRunningPub(RunnerStatus.Stalled));
            Assert.Equal(RunnerStatus.Stalled, runner.Status);
            Assert.False(runner.CompletionToken.IsCancellationRequested);

            //Test case: false start - StartRunning to Progressed when Stalled
            Assert.False(runner.StartRunningPub(RunnerStatus.Progressed));
            Assert.Equal(RunnerStatus.Stalled, runner.Status);
            Assert.False(runner.CompletionToken.IsCancellationRequested);

            //Test case: attemt to go back to NotStarted - SetState to NotStarted when Stalled
            Assert.False(runner.SetStatePub(RunnerStatus.NotStarted));
            Assert.Equal(RunnerStatus.Stalled, runner.Status);
            Assert.False(runner.CompletionToken.IsCancellationRequested);

            //Test case: change between running states: SetState to Progressed when Stalled
            Assert.True(runner.SetStatePub(RunnerStatus.Progressed));
            Assert.Equal(RunnerStatus.Progressed, runner.Status);
            Assert.False(runner.CompletionToken.IsCancellationRequested);
            Assert.False(runner.CheckCompletionPub());

            //Test case: change running state to final: SetState to Complete when Progressed
            Assert.True(runner.SetStatePub(RunnerStatus.Completed));
            Assert.Equal(RunnerStatus.Completed, runner.Status);
            Assert.True(runner.CompletionToken.IsCancellationRequested);

            //Test case: change final state to running: SetState to Stalled when Complete
            Assert.False(runner.SetStatePub(RunnerStatus.Progressed));
            Assert.Equal(RunnerStatus.Completed, runner.Status);

            //Test case: change one final state to another: SetState to Aborted when Complete
            Assert.False(runner.SetStatePub(RunnerStatus.Aborted));
            Assert.False(runner.CheckCompletionPub());
            Assert.Equal(RunnerStatus.Completed, runner.Status);

            runner.Dispose();
            runner=new RunnerBaseImpl();

            //Test case: change running state to final, do not signal completion: SetState to Complete when Stalled
            Assert.True(runner.StartRunningPub(RunnerStatus.Stalled));
            Assert.Equal(RunnerStatus.Stalled, runner.Status);
            Assert.False(runner.CompletionToken.IsCancellationRequested);
            Assert.True(runner.SetStatePub(RunnerStatus.Completed, true));
            Assert.Equal(RunnerStatus.Completed, runner.Status);
            Assert.False(runner.CompletionToken.IsCancellationRequested);
            Assert.True(runner.CheckCompletionPub());
            Assert.True(runner.CompletionToken.IsCancellationRequested);

            runner.Dispose();

            //Test case: successful start to finish - StartRunning to Aborted when NotStarted
            runner=new RunnerBaseImpl();
            Assert.True(runner.StartRunningPub(RunnerStatus.Completed));
            Assert.Equal(RunnerStatus.Completed, runner.Status);
            Assert.True(runner.CompletionToken.IsCancellationRequested);
            Assert.False(runner.CheckCompletionPub());

            //Test case: call StartRunning on a disposed runner (must throw)
            runner=new RunnerBaseImpl();
            runner.Dispose();
            Assert.Throws<ObjectDisposedException>(() => runner.StartRunningPub(RunnerStatus.Stalled));

            //Test case: call SetState from running to final on a disposed runner (must not throw)
            runner=new RunnerBaseImpl();
            runner.StartRunningPub(RunnerStatus.Stalled);
            runner.Dispose();
            Assert.True(runner.SetStatePub(RunnerStatus.Completed));
            Assert.Equal(RunnerStatus.Completed, runner.Status);
            Assert.False(runner.CompletionToken.IsCancellationRequested);
        }

        //Test group: test Abort method
        [Fact]
        public void Abort()
        {
            RunnerStatus status;
            RunnerBaseImpl runner;
            //Test case: Abort NotStarted runner
            runner=new RunnerBaseImpl();
            status=runner.Abort();
            Assert.True(runner.DoAbortCalled);
            Assert.Equal(RunnerStatus.Aborted, status);
            Assert.True(runner.CompletionToken.IsCancellationRequested);
            runner.Dispose();

            //Test case: Abort running runner
            runner=new RunnerBaseImpl();
            Assert.True(runner.StartRunningPub(RunnerStatus.Stalled));
            status=runner.Abort();
            Assert.True(runner.DoAbortCalled);
            Assert.Equal(RunnerStatus.Aborted, status);
            Assert.True(runner.CompletionToken.IsCancellationRequested);
            runner.Dispose();

            //Test case: Abort runner in the final state
            runner=new RunnerBaseImpl();
            Assert.True(runner.StartRunningPub(RunnerStatus.Completed));
            status=runner.Abort();
            Assert.False(runner.DoAbortCalled);
            Assert.Equal(RunnerStatus.Completed, status);
            runner.Dispose();

            //Test case: Abort runner disposed while running. 
            runner=new RunnerBaseImpl();
            Assert.True(runner.StartRunningPub(RunnerStatus.Stalled));
            runner.Dispose();
            status = runner.Abort();
            Assert.True(runner.DoAbortCalled);
            Assert.Equal(RunnerStatus.Aborted, status);
            Assert.False(runner.CompletionToken.IsCancellationRequested);
        }

        //Test group: SetDisposed and CheckDisposed methods
        [Fact]
        public void SetDisposedAndCheckDisposed()
        {
            RunnerBaseImpl runner=new RunnerBaseImpl();
            //Test case: CheckDisposed on non-disposed runner
            runner.CheckDisposedPub();
            //Test case: SetDisposed on non-disposed runner
            Assert.True(runner.SetDisposedPub());
            //Test case: SetDisposed on disposed runner
            Assert.False(runner.SetDisposedPub());
            //Test case: CheckDisposed on disposed runner
            ObjectDisposedException e = Assert.Throws<ObjectDisposedException>(() => runner.CheckDisposedPub());
            Assert.Equal(typeof(RunnerBaseImpl).Name, e.ObjectName);
        }

        //Test group: Exception thrown while starting background execution
        [Fact]
        public void FailWhileStartingBackgroundExecution()
        {
            RunnerBaseImpl runner = new RunnerBaseImpl(AtStartBkg: () => throw new TestException());
            //Test case: exception thrown in StartBackgroundExecution
            Assert.Throws<TestException>(()=>runner.StartRunning());
            Assert.Equal(RunnerStatus.NotStarted, runner.Status);
            //Test case: exception thrown in StartBackgroundExecutionAsync
            Task task = runner.StartRunningAsync();
            Assert.True(task.IsFaulted);
            Assert.NotNull(task.Exception);
            AggregateException aggregate = Assert.IsType<AggregateException>(task.Exception!);
            Assert.Single(aggregate.InnerExceptions);
            Assert.IsType<TestException>(aggregate.InnerExceptions[0]);
            Assert.Equal(RunnerStatus.NotStarted, runner.Status);
        }

        //Test case: set and access IRunner.ExtraData implemented property
        [Fact]
        public void ExraData()
        {
            RunnerBaseImpl runner = new RunnerBaseImpl();
            Object data = new Object();
            runner.ExtraData=data;
            Assert.Same(data, runner.ExtraData);
        }

        class TestException : Exception { }

        class RunnerBaseImpl : RunnerBase
        {
            readonly Action? _atStartBkg;

            public RunnerBaseImpl(CancellationTokenSource? CompletionTokenSource = null, Boolean PassCtsOwnership = true, Action? AtStartBkg=null):
                base(CompletionTokenSource, PassCtsOwnership, default) { _atStartBkg = AtStartBkg; }

            public Boolean DoAbortCalled { get; private set; }

            public override Boolean IsBackgroundExecutionCompleted => throw new NotImplementedException();

            public void SetPosition(Int32 NewPosition)
            {
                Position=NewPosition;
            }

            public Boolean SetStatePub(RunnerStatus State, Boolean DoNotComplete = false)
            {
                return base.SetStatus(State, DoNotComplete);
            }

            public Boolean CheckCompletionPub()
            {
                return base.CheckCompletion();
            }

            public Boolean StartRunningPub(RunnerStatus NewState)
            {
                return base.StartRunning(NewState);
            }

            public Boolean SetDisposedPub()
            {
                return base.SetDisposed();
            }

            public void CheckDisposedPub()
            {
                base.CheckDisposed();
            }

            protected override void DoAbort(String TraceIdentifier)
            {
                DoAbortCalled=true;
                base.DoAbort(TraceIdentifier);
            }

            protected internal override void StartBackgroundExecution()
            {
                _atStartBkg?.Invoke();
            }

            public override RunnerBkgProgress GetProgress()
            {
                throw new NotImplementedException();
            }
        }

    }
}
