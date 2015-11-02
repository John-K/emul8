//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Emul8.Core;
using Emul8.Time;
using Emul8.Exceptions;
using Emul8.Logging;
using Emul8.Utilities;

namespace Emul8.Peripherals.Timers
{
    public class ComparingTimer : ITimer, IPeripheral
    {
        public ComparingTimer(Machine machine, long frequency, long compare, long limit)
        {
            if(compare > limit || compare < 0)
            {
                throw new ConstructionException(string.Format(CompareHigherThanLimitMessage, compare, limit));
            }
            this.limit = limit;
            initialCompare = compare;
            clockSource = machine.ObtainClockSource();
            clockSource.AddClockEntry(new ClockEntry(compare, ClockEntry.FrequencyToRatio(this, frequency), CompareReached, false, workMode: WorkMode.OneShot));
        }

        public bool Enabled
        {
            get
            {
                return clockSource.GetClockEntry(CompareReached).Enabled;
            }
            set
            {
                clockSource.ExchangeClockEntryWith(CompareReached, oldEntry => oldEntry.With(enabled: value));
            }
        }

        public long Value
        {
            get
            {
                var currentValue = 0L;
                clockSource.GetClockEntryInLockContext(CompareReached, entry =>
                {
                    currentValue = valueAccumulatedSoFar + entry.Value;
                });
                return currentValue;
                
            }
            set
            {
                clockSource.ExchangeClockEntryWith(CompareReached, entry => { 
                    valueAccumulatedSoFar = value;
                    Compare = compareValue;
                    return entry.With(value: 0);
                });
            }
        }

        public long Compare
        {
            get
            {
                var returnValue = 0L;
                clockSource.ExecuteInLock(() =>
                {
                    returnValue = compareValue;
                });
                return returnValue;
            }
            set
            {
                if(value > limit || value < 0)
                {
                    throw new InvalidOperationException(CompareHigherThanLimitMessage.FormatWith(value, limit));
                }
                clockSource.ExchangeClockEntryWith(CompareReached, entry =>
                {
                    compareValue = value;
                    // here we temporary convert to ulong since negative value will require a ClockEntry to overflow,
                    // which will occur later than reaching
                    var nextEventIn = (long)Math.Min((ulong)(compareValue - valueAccumulatedSoFar), (ulong)(limit - valueAccumulatedSoFar));
                    valueAccumulatedSoFar += entry.Value;
                    return entry.With(period: nextEventIn - entry.Value, value: 0);
                });
            }
        }

        public virtual void Reset()
        {
            clockSource.ExchangeClockEntryWith(CompareReached, entry =>  
            {
                valueAccumulatedSoFar = 0;
                compareValue = initialCompare;
                return entry.With(value: 0, enabled: false, period: initialCompare);
            });
        }

        protected virtual void OnCompare()
        {
        }

        private void CompareReached()
        {
            // since we use OneShot, timer's value is already 0 and it is disabled now
            // first we add old limit to accumulated value:
            valueAccumulatedSoFar += clockSource.GetClockEntry(CompareReached).Period;
            if(valueAccumulatedSoFar >= limit && compareValue != limit)
            {
                // compare value wasn't actually reached, the timer reached its limit
                // we don't trigger an event in such case
                valueAccumulatedSoFar = 0;
                clockSource.ExchangeClockEntryWith(CompareReached, entry => entry.With(period: compareValue, enabled: true));
                return;
            }
            // real compare event - then we reenable the timer with the next event marked by limit
            // which will probably be soon corrected by software
            clockSource.ExchangeClockEntryWith(CompareReached, entry => entry.With(period: limit - valueAccumulatedSoFar, enabled: true));
            if(valueAccumulatedSoFar >= limit)
            {
                valueAccumulatedSoFar = 0;
            }
            OnCompare();
        }
            
        private long valueAccumulatedSoFar;
        private long compareValue;
        private readonly IClockSource clockSource;
        private readonly long limit;
        private readonly long initialCompare;

        private const string CompareHigherThanLimitMessage = "Compare value ({0}) cannot be higher than limit ({1}) nor negative.";
    }
}

