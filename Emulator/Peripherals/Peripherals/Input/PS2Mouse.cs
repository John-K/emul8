//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using Emul8.Logging;
using Emul8.Core;
using Emul8.Utilities;

namespace Emul8.Peripherals.Input
{
    public class PS2Mouse : IPS2Peripheral, IRelativePositionPointerInput
    {
        public PS2Mouse(Machine machine)
        {
            this.machine = machine;
            data = new Queue<byte>();
            Reset();
        }

        public byte Read()
        {
            if(data.Count > 0)
            {
                var result = data.Dequeue();
                NotifyParent();
                return result;
            }
            this.Log(LogLevel.Warning, "Attempted to read while no data in buffer. Returning 0.");
            return 0;
        }

        public void Write(byte value)
        {
            if(lastCommand == Command.None)
            {
                switch((Command)value)
                {
                case Command.Reset:
                    AckAndReset();
                    break;
                case Command.GetDeviceId:
                    lock(data)
                    {
                        SendAck();
                        data.Enqueue(0x00);
                    }
                    break;
                case Command.SetSampleRate:
                case Command.SetResolution:
                    lastCommand = (Command)value;
                    SendAck();
                    break;
                default:
                    this.Log(LogLevel.Warning, "Unhandled PS2 command: {0}", (Command)value);
                    break;
                }
            }
            else
            {
                switch(lastCommand)
                {
                case Command.SetSampleRate:
                case Command.SetResolution:
                    SendAck();
                    break;
                }
                lastCommand = Command.None;
            }
        }

        public void MoveBy(int x, int y)
        {
            byte dataByte = buttonState;
            y = -y;
            if(x < 0)
            {
                dataByte |= 1 << 4;
            }
            if(y < 0)
            {
                dataByte |= 1 << 5;
            }

            x = x.Clamp(-255, 255);
            y = y.Clamp(-255, 255);

            lock(data)
            {
                data.Enqueue(dataByte);
                data.Enqueue((byte)x);
                data.Enqueue((byte)y);
            }
            NotifyParent();
        }

        public void Press(MouseButton button = MouseButton.Left)
        {
            machine.ReportForeignEvent(button, PressInner);
        }

        public void Release(MouseButton button = MouseButton.Left)
        {
            machine.ReportForeignEvent(button, ReleaseInner);
        }

        public void Reset()
        {
            buttonState = 0x08;
            data.Clear();
        }

        public IPS2Controller Controller { get; set; }

        private void PressInner(MouseButton button)
        {
            buttonState |= (byte)button;
            SendButtonState();
        }

        private void ReleaseInner(MouseButton button)
        {
            buttonState &= (byte) ~button;
            SendButtonState();
        }

        private void SendButtonState()
        {
            lock(data)
            {
                data.Enqueue(buttonState);
                data.Enqueue(0x00);
                data.Enqueue(0x00);
            }
            NotifyParent();
        }

        private void AckAndReset()
        {
            Reset();
            lock(data)
            {
                SendAck();
                data.Enqueue((byte) Command.SelfTestPassed);
                data.Enqueue(0x00);
            }
        }

        private void SendAck()
        {
            data.Enqueue((byte)Command.Acknowledge);
            NotifyParent();
        }

        private void NotifyParent()
        {
            if(Controller != null)
            {
                if(data.Count > 0)
                {
                    Controller.Notify();
                }
            }
            else
            {
                this.Log(LogLevel.Noisy, "PS2 device not connected to any controller issued an update.");
            }
        }

        private Command lastCommand;
        private byte buttonState;
        private readonly Queue<byte> data;
        private readonly Machine machine;

        enum Command : byte
        {
            Reset = 0xFF,
            SetSampleRate = 0xF3,
            GetDeviceId = 0xF2,
            SetResolution = 0xE8,
            Acknowledge = 0xFA,
            SelfTestPassed = 0xAA,
            None = 0x00,
        }
    }
}
