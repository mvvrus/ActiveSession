using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ConcurentSortedDictionary<TKey, TValue> : IDisposable, IDictionary<TKey, TValue> where TKey : notnull
    {
        const Int32 DISPOSE_WAIT_MSECS = 500;

        ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        SortedDictionary<TKey, TValue> _base = new SortedDictionary<TKey, TValue>();
        public Boolean DisposeTimedOut { get; private set; } = false;
        internal /*Just for tests*/ Int32 _disposed_status = 0, _can_call_exit = 1;
        internal /*Just for tests*/ Int32 _dispose_timeout = DISPOSE_WAIT_MSECS;
        
        public void Dispose()
        {
            Int32 disposed_status = Interlocked.Exchange(ref _disposed_status, 1);
            if(disposed_status == 0) {
                if(_lock.TryEnterWriteLock(_dispose_timeout)) _lock.ExitWriteLock();
                else DisposeTimedOut=true;
                Volatile.Write(ref _can_call_exit, 0);
                _lock.Dispose();
            }
        }

        void PerformDispose()
        {

        }

        internal /*Just for tests*/ void EnterReadLock()
        {
            if(Volatile.Read(ref _disposed_status)==0) _lock.EnterReadLock();
        }

        internal /*Just for tests*/ void ExitReadLock()
        {
            if(Volatile.Read(ref _can_call_exit)!=0) _lock.ExitReadLock();
        }

        internal /*Just for tests*/ void EnterWriteLock()
        {
            if(Volatile.Read(ref _disposed_status)==0) _lock.EnterWriteLock();
            else throw new ObjectDisposedException(nameof(ConcurentSortedDictionary<TKey,TValue>), "Write access is prohibited.");
        }

        internal /*Just for tests*/ void ExitWriteLock()
        {
            if(Volatile.Read(ref _can_call_exit)!=0) _lock.ExitWriteLock();
        }

        public TValue this[TKey key]
        {
            get
            {
                EnterReadLock(); try { return _base[key]; } finally { ExitReadLock(); }
            }
            set
            {
                EnterWriteLock(); try { _base[key]=value; } finally { ExitWriteLock(); }
            }
        }

        public Int32 Count { get { EnterReadLock(); try { return _base.Count; } finally { ExitReadLock(); } } }

        public void Add(TKey key, TValue value)
        {
            EnterWriteLock(); try { _base.Add(key, value); } finally { ExitWriteLock(); }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            EnterWriteLock(); try { ((ICollection<KeyValuePair<TKey, TValue>>)_base).Add(item); } finally { ExitWriteLock(); }
        }

        public void Clear()
        {
            EnterWriteLock(); try { _base.Clear(); } finally { ExitWriteLock(); }
        }

        public Boolean Contains(KeyValuePair<TKey, TValue> item)
        {
            EnterReadLock(); try { return _base.Contains(item); } finally { ExitReadLock(); }
        }

        public Boolean ContainsKey(TKey key)
        {
            EnterReadLock(); try { return _base.ContainsKey(key); } finally { ExitReadLock(); }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, Int32 arrayIndex)
        {
            EnterWriteLock(); try { ((ICollection<KeyValuePair<TKey, TValue>>)_base).CopyTo(array, arrayIndex); } finally { ExitWriteLock(); }
        }

        public Boolean Remove(TKey key)
        {
            EnterWriteLock(); try { return _base.Remove(key); } finally { ExitWriteLock(); }
        }

        public Boolean Remove(KeyValuePair<TKey, TValue> item)
        {
            EnterWriteLock(); try { return ((ICollection<KeyValuePair<TKey, TValue>>)_base).Remove(item); } finally { ExitWriteLock(); }
        }

        public Boolean TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            EnterReadLock(); try { return _base.TryGetValue(key, out value); } finally { ExitReadLock(); }
        }

        public ICollection<TKey> Keys => _base.Keys;
        public ICollection<TValue> Values => _base.Values;
        public Boolean IsReadOnly => ((IDictionary<TKey, TValue>)_base).IsReadOnly;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _base.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

}
