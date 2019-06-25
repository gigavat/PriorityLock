using PriorityLock;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UsefulFeatures
{
    static class Program
    {
        static int _index = 0;
        static int _count = 0;
        static LockMgr _lockMgr = new LockMgr();

        static void Main(string[] args)
        {
            var tasks = new List<Task>();
            // async
            {
                for (int i = 0; i < 10; i++)
                {
                    var t = Task.Run(async () => await LowPriorityMethodAsync());
                    t.ConfigureAwait(false);
                    tasks.Add(t);
                }

                var ht = Task.Run(async () => await HighPriorityMethodAsync());
                ht.ConfigureAwait(false);
                tasks.Add(ht);

                var ht2 = Task.Run(async () => await HighPriorityMethodAsync());
                ht2.ConfigureAwait(false);
                tasks.Add(ht2);
            }
            // sync
            {
                for (int i = 0; i < 10; i++)
                {
                    var t = Task.Run(() => LowPriorityMethod());
                    t.ConfigureAwait(false);
                    tasks.Add(t);
                }

                var ht = Task.Run(() => HighPriorityMethod());
                ht.ConfigureAwait(false);
                tasks.Add(ht);

                var ht2 = Task.Run(() => HighPriorityMethod());
                ht2.ConfigureAwait(false);
                tasks.Add(ht2);
            }
            Task.WaitAll(tasks.ToArray());

            Console.WriteLine("Test done. Press any key...");
            Console.ReadKey();
        }

        static async Task PrintInLockAsync(int index, string message)
        {
            //using (await _lockMgr.LockAsync()) // - it is not working
            {
                try
                {
                    var count = Interlocked.Increment(ref _count);
                    if (count > 1) throw new Exception("This is worst lock I ever seen!");

                    Console.WriteLine($"{message} lock in {index} ({Thread.CurrentThread.ManagedThreadId})");
                    await Task.Delay(200);
                    Console.WriteLine($"{message} lock out {index} ({Thread.CurrentThread.ManagedThreadId})");
                }
                finally
                {
                    Interlocked.Decrement(ref _count);
                }
            }
        }

        static async Task LowPriorityMethodAsync()
        {
            try
            {
                var index = Interlocked.Increment(ref _index);
                for (int i = 0; i < 50; i++)
                {
                    Console.WriteLine($"low ASYNC lock WAIT {index} ({Thread.CurrentThread.ManagedThreadId})");
                    using (await _lockMgr.LockAsync())
                    {
                        await PrintInLockAsync(index, "low ASYNC");
                    }

                    await Task.Delay(100);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        static async Task HighPriorityMethodAsync()
        {
            try
            {
                var index = Interlocked.Increment(ref _index);
                for (int i = 0; i < 30; i++)
                {
                    Console.WriteLine($"HIGH ASYNC lock WAIT {index} ({Thread.CurrentThread.ManagedThreadId})");
                    using (await _lockMgr.HighLockAsync())
                    {
                        await PrintInLockAsync(index, "HIGH ASYNC");
                    }
                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        static void PrintInLock(int index, string message)
        {
            using (_lockMgr.Lock())
            {
                var count = Interlocked.Increment(ref _count);
                if (count > 1) throw new Exception("This is worst lock I ever seen!");

                Console.WriteLine($"{message} lock in {index} ({Thread.CurrentThread.ManagedThreadId})");
                Thread.Sleep(200);
                Console.WriteLine($"{message} lock out {index} ({Thread.CurrentThread.ManagedThreadId})");

                Interlocked.Decrement(ref _count);
            }
        }

        static void LowPriorityMethod()
        {
            try
            {
                var index = Interlocked.Increment(ref _index);
                for (int i = 0; i < 50; i++)
                {
                    Console.WriteLine($"low lock WAIT {index} ({Thread.CurrentThread.ManagedThreadId})");
                    using (_lockMgr.Lock())
                    {
                        PrintInLock(index, "low");
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        static void HighPriorityMethod()
        {
            try
            {
                var index = Interlocked.Increment(ref _index);
                for (int i = 0; i < 30; i++)
                {
                    Console.WriteLine($"HIGH lock WAIT {index} ({Thread.CurrentThread.ManagedThreadId})");
                    using (_lockMgr.HighLock())
                    {
                        PrintInLock(index, "HIGH");
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
