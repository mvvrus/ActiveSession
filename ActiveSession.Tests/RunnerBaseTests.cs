using System;
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
            Assert.Equal(RunnerState.NotStarted, runner.State);

            //Test case: successful start - StartRunning to Stalled when NotStarted
            Assert.True(runner.StartRunningPub(RunnerState.Stalled));
            Assert.Equal(RunnerState.Stalled, runner.State);
            Assert.False(runner.CompletionToken.IsCancellationRequested);

            //Test case: false start - StartRunning to Progressed when Stalled
            Assert.False(runner.StartRunningPub(RunnerState.Progressed));
            Assert.Equal(RunnerState.Stalled, runner.State);
            Assert.False(runner.CompletionToken.IsCancellationRequested);

            //Test case: attemt to go back to NotStarted - SetState to NotStarted when Stalled
            Assert.False(runner.SetStatePub(RunnerState.NotStarted));
            Assert.Equal(RunnerState.Stalled, runner.State);
            Assert.False(runner.CompletionToken.IsCancellationRequested);

            //Test case: change between running states: SetState to Progressed when Stalled
            Assert.True(runner.SetStatePub(RunnerState.Progressed));
            Assert.Equal(RunnerState.Progressed, runner.State);
            Assert.False(runner.CompletionToken.IsCancellationRequested);

            //Test case: change running state to final: SetState to Complete when Progressed
            Assert.True(runner.SetStatePub(RunnerState.Complete));
            Assert.Equal(RunnerState.Complete, runner.State);
            Assert.True(runner.CompletionToken.IsCancellationRequested);

            //Test case: change final state to running: SetState to Stalled when Complete
            Assert.False(runner.SetStatePub(RunnerState.Progressed));
            Assert.Equal(RunnerState.Complete, runner.State);

            //Test case: change one final state to another: SetState to Aborted when Complete
            Assert.False(runner.SetStatePub(RunnerState.Aborted));
            Assert.Equal(RunnerState.Complete, runner.State);

            runner.Dispose();

            //Test case: successful start to finish - StartRunning to Aborted when NotStarted
            runner=new RunnerBaseImpl();
            Assert.True(runner.StartRunningPub(RunnerState.Complete));
            Assert.Equal(RunnerState.Complete, runner.State);
            Assert.True(runner.CompletionToken.IsCancellationRequested);

            //Test case: call StartRunning on a disposed runner (must throw)
            runner=new RunnerBaseImpl();
            runner.Dispose();
            Assert.Throws<ObjectDisposedException>(() => runner.StartRunningPub(RunnerState.Stalled));

            //Test case: call SetState from running to final on a disposed runner (must not throw)
            runner=new RunnerBaseImpl();
            runner.StartRunningPub(RunnerState.Stalled);
            runner.Dispose();
            Assert.True(runner.SetStatePub(RunnerState.Complete));
            Assert.Equal(RunnerState.Complete, runner.State);
            Assert.False(runner.CompletionToken.IsCancellationRequested);
        }

        //Test group: test Abort method
        [Fact]
        public void Abort()
        {
            RunnerBaseImpl runner;
            //Test case: Abort NotStarted runner
            runner=new RunnerBaseImpl();
            runner.Abort();
            Assert.True(runner.DoAbortCalled);
            Assert.Equal(RunnerState.Aborted, runner.State);
            Assert.True(runner.CompletionToken.IsCancellationRequested);
            runner.Dispose();

            //Test case: Abort running runner
            runner=new RunnerBaseImpl();
            Assert.True(runner.StartRunningPub(RunnerState.Stalled));
            runner.Abort();
            Assert.True(runner.DoAbortCalled);
            Assert.Equal(RunnerState.Aborted, runner.State);
            Assert.True(runner.CompletionToken.IsCancellationRequested);
            runner.Dispose();

            //Test case: Abort runner in the final state
            runner=new RunnerBaseImpl();
            Assert.True(runner.StartRunningPub(RunnerState.Complete));
            runner.Abort();
            Assert.False(runner.DoAbortCalled);
            Assert.Equal(RunnerState.Complete, runner.State);
            runner.Dispose();

            //Test case: Abort runner disposed while running. 
            runner=new RunnerBaseImpl();
            Assert.True(runner.StartRunningPub(RunnerState.Stalled));
            runner.Dispose();
            runner.Abort();
            Assert.True(runner.DoAbortCalled);
            Assert.Equal(RunnerState.Aborted, runner.State);
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
            Assert.Throws<ObjectDisposedException>(() => runner.CheckDisposedPub());
        }


        class RunnerBaseImpl : RunnerBase
        {
            public RunnerBaseImpl(CancellationTokenSource? CompletionTokenSource = null, Boolean PassCtsOwnership = true):
                base(CompletionTokenSource, PassCtsOwnership) { }

            public Boolean DoAbortCalled { get; private set; }

            public void SetPosition(Int32 NewPosition)
            {
                Position=NewPosition;
            }

            public Boolean SetStatePub(RunnerState State)
            {
                return base.SetState(State);
            }

            public Boolean StartRunningPub(RunnerState NewState)
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

            protected override void DoAbort()
            {
                DoAbortCalled=true;
                base.DoAbort();
            }
        }

    }
}
