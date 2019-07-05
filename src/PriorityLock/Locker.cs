using System;
using System.Threading;

namespace PriorityLock
{
    public class Locker : IDisposable
    {
        private LockMgr _mgr;

        public Locker(LockMgr mgr)
        {
            _mgr = mgr;
        }

        public void Dispose()
        {
            _mgr?.Release(this);
            _mgr = null;
        }
    }
}
