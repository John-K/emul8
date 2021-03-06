//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;

namespace Emul8.Core
{
    public interface IMappedSegment
    {
        IntPtr Pointer { get; }
        long StartingOffset { get; }
        long Size { get; }
        void Touch();
    }
}

