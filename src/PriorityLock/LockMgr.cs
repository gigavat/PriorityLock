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
        internal bool IsSyncUse;

        internal readonly SemaphoreSlim Low = new SemaphoreSlim(1);
        internal readonly SemaphoreSlim High = new SemaphoreSlim(1);
        private SpinWait LowSpin = new SpinWait();
        private SpinWait HighSpin = new SpinWait();

        public Locker HighLock()
        {
            Wait(true);
            return new HighLocker(this);
        }
        public Locker Lock(bool high = false)
        {
            Wait(high);
            return new Locker(this, high);
        }

        public async Task<Locker> HighLockAsync()
        {
            await WaitAsync(true);
            return new HighLocker(this);
        }
        public async Task<Locker> LockAsync(bool high = false)
        {
            await WaitAsync(high);
            return new Locker(this, high);
        }

        private void Wait(bool high = false)
        {
            if (CurThread == Thread.CurrentThread && IsSyncUse)
            {
                RecursionCount++;
                return;
            }
            if (high)
            {
                Interlocked.Increment(ref HighCount);
                High.Wait();
                while (Interlocked.CompareExchange(ref LowCount, 0, 0) != 0) HighSpin.SpinOnce();
            }
            else
            {
                Low.Wait();
                while (Interlocked.CompareExchange(ref HighCount, 0, 0) != 0) LowSpin.SpinOnce();
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
            IsSyncUse = true;
        }

        private async Task WaitAsync(bool high = false)
        {
            if (high)
            {
                Interlocked.Increment(ref HighCount);
                await High.WaitAsync();
                while (Interlocked.CompareExchange(ref LowCount, 0, 0) != 0) HighSpin.SpinOnce();
            }
            else
            {
                await Low.WaitAsync();
                while (Interlocked.CompareExchange(ref HighCount, 0, 0) != 0) LowSpin.SpinOnce();
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
            CurThread = Thread.CurrentThread;
        }
    }
}