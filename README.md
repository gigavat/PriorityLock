# PriorityLock
Asynchronous and synchronous lock with two priorities in C#

## Getting Started

Install the [NuGet package](https://www.nuget.org/packages/PriorityLock/).

## Usage

`PriorityLock` supports synchronous reentrant lock like standart `lock`, but also it supports two levels of access priority like `ReaderWriterLockSlim`.

```C#
private PriorityLock.LockMgr _lockMgr = new PriorityLock.LockMgr();
public void LowPriority()
{
  using (_lockMgr.Lock())
  {
    using (_lockMgr.HighLock())
    {
      //if you entered in PriorityLock, 
      //no matter what prioruty will have a reentrant call of PriorityLock

      Thread.Sleep(1000);
    }
  }
}

public void HighPriority()
{
  using (_lockMgr.HighLock())
  {
    using (_lockMgr.Lock())
    {
      Thread.Sleep(1000);
    }
  }
}
```

`PriorityLock` also supports asynchronous lock operations, but it is NOT reentrant.
```C#
private PriorityLock.LockMgr _lockMgr = new PriorityLock.LockMgr();
public async Task LowPriorityAsync()
{
  using (await _lockMgr.LockAsync())
  {
    await Task.Delay(1000);
  }
}

public async Task HighPriorityAsync()
{
  using (await _lockMgr.HighLockAsync())
  {
    await Task.Delay(1000);
  }
}

```

## Important

Do not mix asynchronous and synchronous calls of `PriorityLock`, because asynchronous calls is not reentrant and will deadlock its self when entering the same lock multiple times.

