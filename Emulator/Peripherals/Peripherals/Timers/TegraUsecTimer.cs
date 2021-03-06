//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Emul8.Logging;
using Emul8.Peripherals.Bus;
using Emul8.Core;
using Emul8.Time;

namespace Emul8.Peripherals.Timers
{
    public class TegraUsecTimer : LimitTimer, IDoubleWordPeripheral, IKnownSize
    {
        public TegraUsecTimer (Machine machine) : base(machine, 1000000, direction: Direction.Ascending, limit: uint.MaxValue, enabled: true)
        {
            Reset ();
        }

        public uint ReadDoubleWord (long offset)
        {


            switch ((Registers)offset) 
            {
            case Registers.Value:
                return (uint)Value;
            case Registers.Config:
                return (uint)((usecDividend << 8) | usecDivisor);
            case Registers.Freeze:
                return freeze;
            default:
                this.LogUnhandledRead(offset);
                return 0;
            }
        }

        public void WriteDoubleWord (long offset, uint value)
        {
            switch ((Registers)offset) 
            {
            case Registers.Value:
                this.Log(LogLevel.Warning, "Unexpected write to readonly register 0x{0:X}, value 0x{1:X}.", offset, value);
                break;
            case Registers.Config:
                usecDivisor = (byte)(value & 0xF);
                usecDividend = (byte)((value >> 8) & 0xF);
                break;
            case Registers.Freeze:
                freeze = (byte)value;
                break;
            default:
                this.LogUnhandledWrite(offset, value);
                break;
            }
        }

        public long Size {
            get 
            {
                return 64;
            }
        }

        public override void Reset()
        {
            usecDivisor = 0xc;
            usecDividend = 0x0;
        }

        private enum Registers
        {
            Value  = 0x00,
            Config = 0x04,
            Freeze = 0x3c,
        }

        private byte usecDivisor;
        private byte usecDividend;
        private byte freeze;

    }
}

