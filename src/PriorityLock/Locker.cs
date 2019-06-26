using System;
using System.Threading;

namespace PriorityLock
{
    public class Locker : IDisposable
    {
        private readonly bool _isHigh;
        private LockMgr _mgr;

        public Locker(LockMgr mgr, bool isHigh = false)
        {
            _isHigh = isHigh;
            _mgr = mgr;
        }

        public void Dispose()
        {
            if (_mgr.RecursionCount > 0)
            {
                _mgr.RecursionCount--;
                _mgr = null;
                return;
            }
            _mgr.RecursionCount = 0;
            _mgr.CurThread = null;
            if (_isHigh)
            {
                _mgr.High.Release();
                Interlocked.Decrement(ref _mgr.HighCount);
            }
            else
            {
                _mgr.Low.Release();
                Interlocked.Decrement(ref _mgr.LowCount);
            }
            _mgr = null;
        }
    }
}
