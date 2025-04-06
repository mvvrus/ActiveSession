using MVVrus.AspNetCore.ActiveSession.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    public class ConcurentSortedDictionaryTests
    {
        const String KEY = "KEY";
        Object value = new Object();
        const Int32 TIMEOUT = 20000;
        const Int32 SMALL_TIMEOUT = 1000;

        [Fact]
        public void DisposeNormal()
        {
            ConcurentSortedDictionary<String, Object?> dict = new ConcurentSortedDictionary<String, Object?>();
            dict.Add(KEY, value);
            dict.Dispose();
            Assert.NotEqual(0, dict._disposed_status);
            Assert.Equal(0, dict._can_call_exit);
            Assert.Same(value, dict[KEY]);
            Assert.Throws<ObjectDisposedException>(() => dict.Add(KEY+"1",null));
            Assert.Throws<ObjectDisposedException>(() => dict[KEY] = null);
            Assert.Throws<ObjectDisposedException>(() => dict.Remove(KEY));
        }

        class TestSetup: IDisposable
        {
            public ManualResetEventSlim Started { get; init; } = new ManualResetEventSlim(false);
            public ConcurentSortedDictionary<String, Object?> Dict { get; init; } = new ConcurentSortedDictionary<String, Object?>();

            public Task StartDispose()
            {
                return Task.Run(() => { Started.Set(); Dict.Dispose(); });
            }

            public void Dispose()
            {
                Started.Dispose();
            }

        }

        [Fact]
        public void DisposeReadLocked()
        {
            using(TestSetup ts = new TestSetup()) {
                ConcurentSortedDictionary<String, Object?> dict = ts.Dict;
                dict._dispose_timeout = -1;
                dict.EnterReadLock();
                Task dispose_task = ts.StartDispose();
                Assert.True(ts.Started.Wait(TIMEOUT));
                Assert.False(dispose_task.IsCompleted);
                dict.ExitReadLock();
                Assert.True(dispose_task.Wait(TIMEOUT));
                Assert.Equal(0, dict._can_call_exit);
                Assert.False(dict.DisposeTimedOut);
            }
        }

        [Fact]
        public void DisposeReadLockedHanged()
        {
            using(TestSetup ts = new TestSetup()) {
                ConcurentSortedDictionary<String, Object?> dict = ts.Dict;
                dict.EnterReadLock();
                Task dispose_task = ts.StartDispose();
                Assert.True(ts.Started.Wait(TIMEOUT));
                Assert.False(dispose_task.IsCompleted);
                Thread.Sleep(SMALL_TIMEOUT+dict._dispose_timeout);
                Assert.True(dispose_task.Wait(TIMEOUT));
                Assert.Equal(0, dict._can_call_exit);
                Assert.True(dict.DisposeTimedOut);
                dict.ExitReadLock();
            }
        }

        [Fact]
        public void DisposeWriteLocked()
        {
            using(TestSetup ts = new TestSetup()) {
                ConcurentSortedDictionary<String, Object?> dict = ts.Dict;
                dict.EnterWriteLock();
                Task dispose_task = ts.StartDispose();
                Assert.True(ts.Started.Wait(TIMEOUT));
                Assert.False(dispose_task.IsCompleted);
                Thread.Sleep(SMALL_TIMEOUT+dict._dispose_timeout);
                Assert.True(dispose_task.Wait(TIMEOUT));
                Assert.Equal(0, dict._can_call_exit);
                Assert.True(dict.DisposeTimedOut);
                dict.ExitWriteLock();
            }
        }

        [Fact]
        public void DisposeWriteLockedHanged()
        {
            using(TestSetup ts = new TestSetup()) {
                ConcurentSortedDictionary<String, Object?> dict = ts.Dict;
                dict.EnterWriteLock();
                Task dispose_task = ts.StartDispose();
                Assert.True(ts.Started.Wait(TIMEOUT));
                Assert.False(dispose_task.IsCompleted);
                Thread.Sleep(SMALL_TIMEOUT+dict._dispose_timeout);
                Assert.True(dispose_task.Wait(TIMEOUT));
                Assert.Equal(0, dict._can_call_exit);
                Assert.True(dict.DisposeTimedOut);
                dict.ExitWriteLock();
            }
        }
    }
}
