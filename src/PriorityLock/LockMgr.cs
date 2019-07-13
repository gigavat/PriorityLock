using System;
using System.Threading;
using System.Threading.Tasks;

namespace PriorityLock
{
    /// <summary>
    /// Use instance of this class instead classic lock. 
    /// For lock some code use construction like:
    /// 
    /// using (_lockMgr.Lock())
    /// {
    ///     // your code
    /// }
    /// for usual priority access to the block of your code, or 
    /// 
    /// using (_lockMgr.HighLock())
    /// {
    ///     // your code
    /// }
    /// for high priority access to the block of your code.
    /// 
    /// Also you may use async version, but it not supports recursion, 
    /// so you should never use any locks (sync or async) into async lock
    /// 
    /// using (await _lockMgr.LockAsync())
    /// {
    ///     // your code
    /// }
    /// for usual priority access to the block of your code, or 
    /// 
    /// using (await _lockMgr.HighLockAsync())
    /// {
    ///     // your code
    /// }
    /// for high priority access to the block of your code.
    /// </summary>
    public class LockMgr
    {
        internal int HighCount;
        internal int LowCount;

        internal Thread CurThread;
        internal int RecursionCount;

        internal readonly SemaphoreSlim Low = new SemaphoreSlim(1, 1);
        internal readonly SemaphoreSlim High = new SemaphoreSlim(1, 1);

        public Locker HighLock()
        {
            Wait(true);
            return new HighLocker(this);
        }
        public Locker Lock(bool isHigh = false)
        {
            Wait(isHigh);
            return isHigh ? new HighLocker(this) : new Locker(this);
        }

        public async Task<Locker> HighLockAsync()
        {
            await WaitAsync(true);
            return new HighLocker(this);
        }
        public async Task<Locker> LockAsync(bool isHigh = false)
        {
            await WaitAsync(isHigh);
            return isHigh ? new HighLocker(this) : new Locker(this);
        }

        private void Wait(bool high = false)
        {
            if (CurThread == Thread.CurrentThread)
            {
                Interlocked.Increment(ref RecursionCount);
                return;
            }
            var spin = new SpinWait();
            if (high)
            {
                Interlocked.Increment(ref HighCount);
                High.Wait();
                while (Volatile.Read(ref LowCount) != 0) spin.SpinOnce();
            }
            else
            {
                Low.Wait();
                while (Volatile.Read(ref HighCount) != 0) spin.SpinOnce();
                try
                {
                    High.Wait();
                    Interlocked.Increment(ref LowCount);
                }
                finally
                {
                    High.Release();
                }
            }
            CurThread = Thread.CurrentThread;
        }

        private async Task WaitAsync(bool high = false)
        {
            var spin = new SpinWait();
            if (high)
            {
                Interlocked.Increment(ref HighCount);
                await High.WaitAsync();
                while (Volatile.Read(ref LowCount) != 0) spin.SpinOnce();
            }
            else
            {
                await Low.WaitAsync();
                while (Volatile.Read(ref HighCount) != 0) spin.SpinOnce();
                try
                {
                    await High.WaitAsync();
                    Interlocked.Increment(ref LowCount);
                }
                finally
                {
                    High.Release();
                }
            }
        }

        internal void Release(Locker locker)
        {
            if (RecursionCount > 0)
            {
                Interlocked.Decrement(ref RecursionCount);
                return;
            }
            RecursionCount = 0;
            CurThread = null;

            if (locker is HighLocker)
            {
                High.Release();
                Interlocked.Decrement(ref HighCount);
            }
            else
            {
                Low.Release();
                Interlocked.Decrement(ref LowCount);
            }
        }
    }
}