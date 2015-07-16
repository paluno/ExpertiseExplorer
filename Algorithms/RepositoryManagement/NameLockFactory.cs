using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.RepositoryManagement
{
    /// <summary>
    /// Provides name-based locks, i.e. each string identifies a lock. Locks that are not in use anymore are recycled.
    /// </summary>
    public class NameLockFactory
    {
        /// <summary>
        /// Stores locks for each artifact by artifact name. A counter exists for every artifact and the dicitionary empties itself again if 
        /// the locks are not needed anymore. Access only through acquireArtifactLock and releaseArtifactLock!
        /// </summary>
        protected readonly ConcurrentDictionary<string, Tuple<int, object>> dictLocks = new ConcurrentDictionary<string, Tuple<int, object>>();

        public object acquireLock(string lockName)
        {
            Tuple<int, object> lockWithCounter = dictLocks.AddOrUpdate(
                lockName,
                new Tuple<int, object>(1, new object()),     // this is a new lock pair
                delegate(string theName, Tuple<int, object> lockPair)
                {
                    if (lockPair.Item1 > 0)
                        return new Tuple<int, object>(lockPair.Item1 + 1, lockPair.Item2);  // there are a number of references already, everything's okay, just increase the counter
                    else
                        return lockPair;    // the lock was supposed to be deleted. Proceed with caution!
                }
            );

            if (0 == lockWithCounter.Item1)    // deletion was in progress
                lock (dictLocks)
                {
                    lockWithCounter = dictLocks.AddOrUpdate(
                        lockName,
                        new Tuple<int, object>(1, new object()),     // this is a new lock pair, the old one was deleted already
                        (theName, lockPair) => new Tuple<int, object>(lockPair.Item1 + 1, lockPair.Item2)   // it's okay even to increase a zero counter because "release" will check before deletion if at all
                    );
                }

            return lockWithCounter.Item2;
        }

        public void releaseLock(string lockName)
        {
            Tuple<int, object> remainingLock = dictLocks.AddOrUpdate(
                lockName,
                delegate(string theName) { throw new InvalidOperationException("A lock was released more often than retrieved"); },
                (theName, lockPair) => new Tuple<int, object>(lockPair.Item1 - 1, lockPair.Item2)   // decrease counter
            );

            if (0 == remainingLock.Item1)    // nobody uses the lock anymore, we can delete it
                lock (dictLocks)
                {
                    if (dictLocks[lockName].Item1 > 0)
                        return;     // the lock was recreated in between and must not be released

                    Tuple<int, object> dummy;
                    dictLocks.TryRemove(lockName, out dummy);   // this always succeeds
                }
        }
    }
}
