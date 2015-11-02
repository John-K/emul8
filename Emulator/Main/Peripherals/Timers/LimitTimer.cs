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

namespace Emul8.Peripherals.Timers
{
    public class LimitTimer : ITimer, IPeripheral
    {
        public LimitTimer(Machine machine, long frequency, long limit = int.MaxValue, Direction direction = Direction.Descending, bool enabled = false, WorkMode workMode = WorkMode.Periodic)
        {
            if(limit == 0)
            {
                throw new ArgumentException("Limit cannot be zero.");
            }
            irqSync = new object();
            clockSource = machine.ObtainClockSource();

            initialFrequency = frequency;
            initialLimit = limit;
            initialDirection = direction;
            initialEnabled = enabled;
            initialWorkMode = workMode;
            InternalReset();
        }

        public long GetValueAndLimit(out long currentLimit)
        {
            var clockEntry = clockSource.GetClockEntry(OnLimitReached);
            currentLimit = clockEntry.Period;
            return clockEntry.Value;
        }

        public long Frequency
        {
            get
            {
                return frequency;
            }
            set
            {
                frequency = value;
                var effectiveFrequency = frequency / Divider;
                clockSource.ExchangeClockEntryWith(OnLimitReached, oldEntry => oldEntry.With(ratio: ClockEntry.FrequencyToRatio(this, effectiveFrequency)));
            }
        }

        public long Value
        {
            get 
            {
                return clockSource.GetClockEntry(OnLimitReached).Value;
            }
            set
            {
                clockSource.ExchangeClockEntryWith(OnLimitReached, oldEntry => oldEntry.With(value: value));
            }
        }

        public WorkMode Mode
        {
            get
            {
                return clockSource.GetClockEntry(OnLimitReached).WorkMode;
            }
            set
            {
                clockSource.ExchangeClockEntryWith(OnLimitReached, oldEntry => oldEntry.With(workMode: value));
            }
        }

        public int Divider
        {
            get
            {
                return divider;
            }
            set
            {
                if(value == divider)
                {
                    return;
                }
                if(value == 0)
                {
                    throw new ArgumentException("Divider cannot be zero.");
                }
                divider = value;
                var effectiveFrequency = Frequency / divider;
                clockSource.ExchangeClockEntryWith(OnLimitReached, oldEntry => oldEntry.With(ratio: ClockEntry.FrequencyToRatio(this, effectiveFrequency)));
            }
        }

        public long Limit
        {
            get
            {
                return clockSource.GetClockEntry(OnLimitReached).Period;
            }
            set
            {
                clockSource.ExchangeClockEntryWith(OnLimitReached, oldEntry =>
                { 
                    if(AutoUpdate)
                    {
                            return oldEntry.With(period: value, value: value);
                    }
                    return oldEntry.With(period: value);
                }, () =>
                {
                    throw new InvalidOperationException("Should not reach here.");
                });
            }
        }

        public bool AutoUpdate { get; set; }

        public void Enable()
        {
            clockSource.ExchangeClockEntryWith(OnLimitReached, oldEntry => oldEntry.With(enabled: true),
                () => { throw new InvalidOperationException("Should not reach here."); });
            // should not reach here - limit should already be set in ctor
        }

        public void Disable()
        {
            clockSource.ExchangeClockEntryWith(OnLimitReached, oldEntry => oldEntry.With(enabled: false),
                () => { throw new InvalidOperationException("Should not reach here."); });
            // should not reach here - limit should already be set in ctor
        }

        public bool Enabled
        {
            get
            {
                return clockSource.GetClockEntry(OnLimitReached).Enabled;
            }
            set
            {
                if(value)
                {
                    Enable();
                }
                else
                {
                    Disable();
                }
            }
        }

        public bool EventEnabled
        {
            get
            {
                lock(irqSync)
                {
                    return eventEnabled;
                }
            }
            set
            {
                lock(irqSync)
                {
                    eventEnabled = value;
                }
            }
        }

        public bool Interrupt
        {
            get
            {
                lock(irqSync)
                {
                    return rawInterrupt && eventEnabled;
                }
            }
        }

        public bool RawInterrupt
        {
            get
            {
                lock(irqSync)
                {
                    return rawInterrupt;
                }
            }
        }

        public void ClearInterrupt()
        {
            lock(irqSync)
            {
                rawInterrupt = false;
            }
        }

        public void ResetValue()
        {
            clockSource.ExchangeClockEntryWith(OnLimitReached, oldEntry =>
            {
                    if(oldEntry.Direction == Direction.Ascending)
                    {
                        return oldEntry.With(value: 0);
                    }
                    return oldEntry.With(value: oldEntry.Period);
            });
        }

        public Direction Direction
        {
            get
            {
                return clockSource.GetClockEntry(OnLimitReached).Direction;
            }
            set
            {
                clockSource.ExchangeClockEntryWith(OnLimitReached, oldEntry => oldEntry.With(direction: value),
                    () =>
                    {
                        throw new InvalidOperationException("Should not reach here.");
                    });
            }
        }

        public virtual void Reset()
        {
            InternalReset();
        }

        public event Action LimitReached;

        protected virtual void OnLimitReached()
        {
            lock(irqSync)
            {
                rawInterrupt = true;

                if (!eventEnabled)
                {
                    return;
                }

                var alarm = LimitReached;
                if(alarm != null)
                {
                    alarm();
                }
            }
        }

        private void InternalReset()
        {
            frequency = initialFrequency;

            var clockEntry = new ClockEntry(initialLimit, ClockEntry.FrequencyToRatio(this, frequency), OnLimitReached, initialEnabled, initialDirection, initialWorkMode) { Value = initialDirection == Direction.Ascending ? 0 : initialLimit };
            clockSource.ExchangeClockEntryWith(OnLimitReached, x => clockEntry, () => clockEntry);
            divider = 1;
            EventEnabled = false;
        }

        private bool eventEnabled;
        private bool rawInterrupt;
        private readonly long initialFrequency;
        private readonly WorkMode initialWorkMode;
        private readonly long initialLimit;
        private readonly Direction initialDirection;
        private readonly bool initialEnabled;
        private readonly IClockSource clockSource;
        private readonly object irqSync;

        private long frequency;
        private int divider;
    }
}

