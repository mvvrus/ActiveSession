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
        const Int32 SMALL_TIMEOUT = 500;

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

        [Fact]
        public void DisposeReadLocked()
        {
            ConcurentSortedDictionary<String, Object?> dict = new ConcurentSortedDictionary<String, Object?>();
            dict._dispose_timeout = -1;
            dict.EnterReadLock();
            Task dispose_task = Task.Run(()=>dict.Dispose());
            Assert.False(dispose_task.Wait(SMALL_TIMEOUT));
            Assert.NotEqual(0, dict._disposed_status);
            Assert.NotEqual(0, dict._can_call_exit);
            dict.ExitReadLock();
            Assert.True(dispose_task.Wait(SMALL_TIMEOUT));
            Assert.Equal(0, dict._can_call_exit);
        }

        [Fact]
        public void DisposeReadLockedHanged()
        {
            ConcurentSortedDictionary<String, Object?> dict = new ConcurentSortedDictionary<String, Object?>();
            dict.EnterReadLock();
            Task dispose_task = Task.Run(() => dict.Dispose());
            Assert.False(dispose_task.Wait(SMALL_TIMEOUT));
            Assert.NotEqual(0, dict._disposed_status);
            Assert.NotEqual(0, dict._can_call_exit);
            Thread.Sleep(2000);
            dict.ExitReadLock();
            Assert.True(dispose_task.Wait(SMALL_TIMEOUT));
            Assert.Equal(0, dict._can_call_exit);
        }

        [Fact]
        public void DisposeWriteLocked()
        {
            ConcurentSortedDictionary<String, Object?> dict = new ConcurentSortedDictionary<String, Object?>();
            dict._dispose_timeout = -1;
            dict.EnterWriteLock();
            Task dispose_task = Task.Run(() => dict.Dispose());
            Assert.False(dispose_task.Wait(SMALL_TIMEOUT));
            Assert.NotEqual(0, dict._disposed_status);
            Assert.NotEqual(0, dict._can_call_exit);
            dict.ExitWriteLock();
            Assert.True(dispose_task.Wait(SMALL_TIMEOUT));
            Assert.Equal(0, dict._can_call_exit);

        }

        [Fact]
        public void DisposeWriteLockedHanged()
        {
            ConcurentSortedDictionary<String, Object?> dict = new ConcurentSortedDictionary<String, Object?>();
            dict.EnterWriteLock();
            Task dispose_task = Task.Run(() => dict.Dispose());
            Assert.False(dispose_task.Wait(SMALL_TIMEOUT));
            Assert.NotEqual(0, dict._disposed_status);
            Assert.NotEqual(0, dict._can_call_exit);
            Thread.Sleep(2000);
            dict.ExitWriteLock();
            Assert.True(dispose_task.Wait(SMALL_TIMEOUT));
            Assert.Equal(0, dict._can_call_exit);
        }
    }
}
