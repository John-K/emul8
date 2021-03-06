//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Emul8.Peripherals.Bus;
using Emul8.Core;
using Emul8.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Emul8.Utilities;

namespace Emul8.Peripherals.IRQControllers
{
    /// <summary>
    /// EXTI interrupt controller.
    /// To map  number inputs used in JSON to pins from the reference manual, use the following rule:
    /// 0->PA0, 1->PA1, ..., 15->PA15, 16->PB0, ...
    /// This model will accept any number of input pins, but keep in mind that currently System
    /// Configuration Controller (SYSCFG) is able to handle only 16x16 pins in total.
    /// </summary>
    public class EXTI :  IDoubleWordPeripheral, IKnownSize, IIRQController, INumberedGPIOOutput
    {
        static EXTI()
        {
            gpioMapping = new Dictionary<int, int> {
                //Mapping 1<->1 for lines 0..4
                { 0, 0 },
                { 1, 1 },
                { 2, 2 },
                { 3, 3 },
                { 4, 4 },
                //Additional output lines, currently not supported.
                //According to docs:
                //* 16 to PVD output
                //* 17 to RTC Alarm event
                //* 18 to USB OTG FS Wakeup event
                //* 19 to Ethernet Wakeup event
                //* 20 to USB OTG HS (configured in FS) Wakeup event (whatever that means)
                //* 21 to RTC Tamper and TimeStamp events
                //* 22 to RTC Wakeup event
                { 16,7 },
                { 17,8 },
                { 18,9 },
                { 19,10 },
                { 20,11 },
                { 21,12 },
                { 22,13 },
            };
            //Common interrupt for lines 5..9
            for(var i = 5; i < 10; ++i)
            {
                gpioMapping[i] = 5;
            }
            //Common interrupt for lines 10..15
            for(var i = 10; i < 16; ++i)
            {
                gpioMapping[i] = 6;
            }
        }

        public EXTI()
        {
            Reset();
        }

        public void OnGPIO(int number, bool value)
        {
            var lineNumber = (byte)(number % MaxNumberOfPinsPerPort);
            var irqNumber = gpioMapping[lineNumber];
            if(BitHelper.IsBitSet((interruptMask | eventMask), lineNumber) && // irq/event unmasked
               ((BitHelper.IsBitSet(risingTrigger, lineNumber) && value) // rising edge
               || (BitHelper.IsBitSet(fallingTrigger, lineNumber) && !value))) // falling edge
            {
                pending |= (1u << lineNumber);
                Connections[irqNumber].Set();
            }
        }

        public uint ReadDoubleWord(long offset)
        {
            switch((Registers)offset)
            {
            case Registers.InterruptMask:
                return interruptMask;
            case Registers.EventMask:
                return eventMask;
            case Registers.RisingTriggerSelection:
                return risingTrigger;
            case Registers.FallingTriggerSelection:
                return fallingTrigger;
            case Registers.SoftwareInterruptEvent:
                return softwareInterrupt;
            case Registers.PendingRegister:
                return pending;
            default:
                this.LogUnhandledRead(offset);
                break;
            }
            return 0;
        }

        public void WriteDoubleWord(long offset, uint value)
        {
            switch((Registers)offset)
            {
            case Registers.InterruptMask:
                interruptMask = value;
                break;
            case Registers.EventMask:
                eventMask = value;
                break;
            case Registers.RisingTriggerSelection:
                risingTrigger = value;
                break;
            case Registers.FallingTriggerSelection:
                fallingTrigger = value;
                break;
            case Registers.SoftwareInterruptEvent:
                var allNewAndOld = softwareInterrupt | value;
                var bitsToSet = allNewAndOld ^ softwareInterrupt;
                BitHelper.ForeachActiveBit(bitsToSet, (x) =>
                {
                    if(BitHelper.IsBitSet((interruptMask | eventMask), x))
                    {
                        Connections[gpioMapping[x]].Set();
                    }
                });
                break;
            case Registers.PendingRegister:
                pending &= ~value;
                softwareInterrupt &= ~value;
                BitHelper.ForeachActiveBit(value, (x) =>
                {
                    Connections[gpioMapping[x]].Unset();
                });
                break;
            default:
                this.LogUnhandledWrite(offset, value);
                break;
            }
        }

        public void Reset()
        {
            interruptMask = 0;
            eventMask = 0;
            risingTrigger = 0;
            fallingTrigger = 0;
            pending = 0;
            softwareInterrupt = 0;


            var innerConnections = new Dictionary<int, IGPIO>();
            for(var i = 0; i < NumberOfOutputLines; ++i)
            {
                innerConnections[i] = new GPIO();
            }
            Connections = new ReadOnlyDictionary<int, IGPIO>(innerConnections);
        }

        public long Size
        {
            get
            {
                return 0x3FF;
            }
        }

        public IReadOnlyDictionary<int, IGPIO> Connections { get; private set; }

        private static readonly Dictionary<int, int> gpioMapping;

        private uint interruptMask;
        private uint eventMask;
        private uint risingTrigger;
        private uint fallingTrigger;
        private uint pending;
        private uint softwareInterrupt;

        private const int MaxNumberOfPinsPerPort = 16;
        private const int NumberOfOutputLines = 14;

        private enum Registers
        {
            InterruptMask = 0x0,
            EventMask = 0x4,
            RisingTriggerSelection = 0x8,
            FallingTriggerSelection = 0xC,
            SoftwareInterruptEvent = 0x10,
            PendingRegister = 0x14
        }
    }
}

