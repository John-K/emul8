//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Antmicro.Migrant;

namespace Emul8.Time
{
    public sealed class SynchronizationDomain : ISynchronizationDomain
    {
        public SynchronizationDomain()
        {
            participantsSync = new object();
            postPhaseSync = new object();
            postPhaseQueue = new ConcurrentQueue<Action>();
            // normally one would expect value 0 here
            // this, however, does not work on mono before
            // e9b135baca99ff74ece08e9f3e8ab8578a89e2d2
            // bug number in Mono's bugzilla is #25928
            barrier = new Barrier(1, PostPhase);
            SyncUnit = DefaultSyncUnit;
        }

        public long SyncUnit { get; set; }

        public long SynchronizationsCount
        {
            get
            {
                return barrier.CurrentPhaseNumber;
            }
        }

        public ISynchronizer ProvideSynchronizer()
        {
            lock(participantsSync)
            {
                if(!firstParticipantIsHere)
                {
                    firstParticipantIsHere = true;
                }
                else
                {
                    barrier.AddParticipant();
                }
            }
            return new Synchronizer(this, SyncUnit);
        }

        public void ExecuteOnNearestSync(Action action)
        {
            postPhaseQueue.Enqueue(action);
        }

        public void SetHookOnSyncPoint(Action<long> handler)
        {
            lock(postPhaseSync)
            {
                hook = handler;
            }
        }

        public void ClearHookOnSyncPoint()
        {
            lock(postPhaseSync)
            {
                hook = null;
            }
        }

        public bool OnSyncPointThread
        {
            get
            {
                return OnSyncThread;
            }
        }

        // why there is an internal event here (apart from the hook)
        // it actually does the same but is intended to be used by the
        // foreign events recording mechanism
        internal event Action<long> SyncPointReached;

        private void RemoveParticipant()
        {
            lock(participantsSync)
            {
                if(barrier.ParticipantCount == 1)
                {
                    firstParticipantIsHere = false;
                }
                else
                {
                    barrier.RemoveParticipant();
                }
            }
        }

        private void PostPhase(Barrier phaseBarrier)
        {
            // since onSyncThread is a thread static field, the value true
            // can only be read on the thread that is executing this method
            // moreover, no lock is necessary since either we're on the same thread
            // and we can read true or we're not - and then the value naver
            // changes (is always false)
            OnSyncThread = true;
            lock(postPhaseSync)
            {
                Action action;
                while(postPhaseQueue.TryDequeue(out action))
                {
                    action();
                }
                if(hook != null)
                {
                    hook(phaseBarrier.CurrentPhaseNumber);
                }
                var syncPointReached = SyncPointReached;
                if(syncPointReached != null)
                {
                    syncPointReached(phaseBarrier.CurrentPhaseNumber);
                }
            }
            OnSyncThread = false;
        }

        private bool firstParticipantIsHere;
        private Action<long> hook;
        private readonly object participantsSync;
        private readonly object postPhaseSync;
        private readonly ConcurrentQueue<Action> postPhaseQueue;
        private readonly Barrier barrier;

        [ThreadStatic]
        private static bool OnSyncThread;

        private const long DefaultSyncUnit = 1;

        private sealed class Synchronizer : ISynchronizer
        {
            public Synchronizer(SynchronizationDomain domain, long syncUnit)
            {
                this.domain = domain;
                cts = new CancellationTokenSource();
                currentValue = syncUnit;
            }

            public void Sync()
            {
                currentValue--;
                if(currentValue == 0)
                {
                    try
                    {
                        domain.barrier.SignalAndWait(cts.Token);
                    }
                    catch(OperationCanceledException)
                    {
                        currentValue++;
                        throw;
                    }
                    currentValue = domain.SyncUnit;
                }
            }

            public void CancelSync()
            {
                cts.Cancel();
            }

            public void RestoreSync()
            {
                var oldCts = Interlocked.Exchange(ref cts, new CancellationTokenSource());
                oldCts.Dispose();
            }

            public void Exit()
            {
                domain.RemoveParticipant();
            }

            private long currentValue;

            [Constructor]
            private CancellationTokenSource cts;
            private readonly SynchronizationDomain domain;
        }
    }
}
