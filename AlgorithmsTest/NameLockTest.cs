using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExpertiseExplorer.Algorithms.RepositoryManagement;
using System.Collections.Concurrent;

namespace ExpertiseExplorer.Algorithms.Test
{
    [TestClass]
    public class NameLockTest
    {
        private class NameLockFactoryForTests : NameLockFactory
        {
            public NameLockFactoryForTests(bool isCaseSensitive = true) : base(isCaseSensitive)
            {
            }

            public ConcurrentDictionary<string, Tuple<int, object>> dictLocksShadow { get { return dictLocks; } }
        }

        [TestMethod]
        public void TestWithSingleLock()
        {
            NameLockFactoryForTests nlf = new NameLockFactoryForTests();

            Thread[] threads = new Thread[10];
            Account acc = new Account(1000, nlf);
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(acc.DoTransactions));
                threads[i] = t;
            }
            for (int i = 0; i < 10; i++)
            {
                threads[i].Start();
            }

                // wait for all threads to terminate
            for (int i = 0; i < 10; i++)
                threads[i].Join();

                // if something goes wrong with locking, Account throws a "Negative Balance" exception
            Assert.IsTrue(nlf.dictLocksShadow.IsEmpty);            
        }

        [TestMethod]
        public void TestMultipleLocks()
        {
            NameLockFactoryForTests nlf = new NameLockFactoryForTests();

            Random rnd = new Random(1402);
            const int NUMBER_OF_ACCOUNTS = 20;
            const int NUMBER_OF_TASKS = 50;

            Account[] allAccounts = new Account[NUMBER_OF_ACCOUNTS];

            for (int i = 0;i<NUMBER_OF_ACCOUNTS;++i)
                allAccounts[i] = new Account(rnd.Next(2000), nlf, "Key " + i);

            Task[] tasks = new Task[NUMBER_OF_TASKS];
            for (int i = 0;i<NUMBER_OF_TASKS;++i)
            {
                Random rnd2 = new Random(i);
                tasks[i] = new Task(delegate()
                    {
                        for (int j = 0; j < 10; ++j)
                            allAccounts[rnd2.Next(NUMBER_OF_ACCOUNTS)].DoTransactions();
                    }
                );
                tasks[i].Start();
            }

            Task.WaitAll(tasks);

            Assert.IsTrue(nlf.dictLocksShadow.IsEmpty);  
        }

        [TestMethod]
        public void TestCaseSensitiveLocks()
        {
            NameLockFactoryForTests nlf = new NameLockFactoryForTests(true);

            Random rnd = new Random(1402);
            const int NUMBER_OF_TASKS = 50;

            Account[] allAccounts = new Account[] {
                    new Account(rnd.Next(2000), nlf, "account"),
                    new Account(rnd.Next(2000), nlf, "ACCOUNT"),
                    new Account(rnd.Next(2000), nlf, "Account")
                };

            Task[] tasks = new Task[NUMBER_OF_TASKS];
            for (int i = 0; i < NUMBER_OF_TASKS; ++i)
            {
                Random rnd2 = new Random(i);
                tasks[i] = new Task(delegate ()
                {
                    for (int j = 0; j < 10; ++j)
                        allAccounts[rnd2.Next(allAccounts.Length)].DoTransactions();
                }
                );
                tasks[i].Start();
            }

            Task.WaitAll(tasks);

            Assert.IsTrue(nlf.dictLocksShadow.IsEmpty);
        }

        #region Code adapted from https://msdn.microsoft.com/en-us/library/c5kehkcz%28v=vs.100%29.aspx
        class Account
        {
            NameLockFactory locks;
            int balance;
            string lockName;

            Random r = new Random();

            public Account(int initial, NameLockFactory lockProvider, string lockName = "myPersonalLock")
            {
                balance = initial;
                locks = lockProvider;
                this.lockName = lockName;
            }

            int Withdraw(int amount)
            {

                // This condition never is true unless the lock statement
                // is commented out.
                if (balance < 0)
                {
                    throw new Exception("Negative Balance");
                }

                // Comment out the next line to see the effect of leaving out 
                // the lock keyword.
                lock (locks.acquireLock(lockName))
                {
                    if (balance >= amount)
                    {
                        Console.WriteLine("Balance before Withdrawal :  " + balance);
                        Console.WriteLine("Amount to Withdraw        : -" + amount);
                        balance = balance - amount;
                        Console.WriteLine("Balance after Withdrawal  :  " + balance);
                        locks.releaseLock(lockName);
                        return amount;
                    }
                    else
                    {
                        locks.releaseLock(lockName);
                        return 0; // transaction rejected
                    }
                }
            }

            public void DoTransactions()
            {
                for (int i = 0; i < 100; i++)
                {
                    Withdraw(r.Next(1, 100));
                }
            }
        }
#endregion Code from https://msdn.microsoft.com/en-us/library/c5kehkcz%28v=vs.100%29.aspx
    }
}
