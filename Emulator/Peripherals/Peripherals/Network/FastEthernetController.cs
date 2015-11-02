//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
﻿using System;
using Emul8.Peripherals.Bus;
using Emul8.Logging;

namespace Emul8.Peripherals.Network
{
    public class FastEthernetController : IDoubleWordPeripheral, IKnownSize
    {
        public uint ReadDoubleWord(long offset)
        {
            switch((Register)offset)
            {
            case Register.InterruptEvent:
                return interruptRegisterValue;
            case Register.MiiManagementFrame:
                return miiManagementValue;
            case Register.ControlRegister:
                return 1;
            }
            this.LogUnhandledRead(offset);
            return 0;
        }

        public void WriteDoubleWord(long offset, uint value)
        {
            switch((Register)offset)
            {
            case Register.ControlRegister:
                if(value != 0)
                {
                    this.Log(LogLevel.Warning, "Unhandled value 0x{0:X} written to control register.", value);
                }
                break;
            case Register.InterruptEvent:
                interruptRegisterValue &= ~value;
                break;
            case Register.MiiManagementFrame:
                interruptRegisterValue |= 0x00800000;
                miiManagementValue = value;
                break;
            default:
                this.LogUnhandledWrite(offset, value);
                break;
            }
        }

        public void Reset()
        {
            interruptRegisterValue = 0;
            miiManagementValue = 0;
        }

        public long Size
        {
            get
            {
                return 0x4000;
            }
        }

        private enum Register
        {
            InterruptEvent = 0x4,
            ControlRegister = 0x24,
            MiiManagementFrame = 0x40
        }

        private uint interruptRegisterValue;
        private uint miiManagementValue;
    }
}

