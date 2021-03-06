//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Emul8.Peripherals.CPU;
using Emul8.Core;
using Emul8.Utilities;

namespace Emul8.Hooks
{
    public static class BlockBeginExtensions
    {
        public static void SetHookAtBlockBegin(this ICPUWithBlockBeginHook cpu, [AutoParameter]Machine m, string pythonScript)
        {
            var engine = new BlockPythonEngine(m, cpu, pythonScript);
            cpu.SetHookAtBlockBegin(engine.HookWithSize);
        }
        public static void AddHook(this ICPUWithBlockBeginHook cpu, [AutoParameter]Machine m, uint addr, string pythonScript)
        {
            var engine = new BlockPythonEngine(m, cpu, pythonScript);
            cpu.AddHook(addr, engine.Hook);
        }
    }
}

