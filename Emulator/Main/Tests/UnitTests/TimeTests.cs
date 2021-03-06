//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using NUnit.Framework;
using Emul8.Time;

namespace UnitTests
{
	[TestFixture]
	public class TimeTests
	{
		[Test]
		public void ShouldTickWithOneHandler()
		{
			var clocksource = new BaseClockSource();
			var counter = 0;

            clocksource.AddClockEntry(new ClockEntry(2, 1, () => counter++) { Value = 0 });
			clocksource.Advance(1);
            Assert.AreEqual(0, counter);
			clocksource.Advance(1);
			Assert.AreEqual(1, counter);
			clocksource.Advance(1);
			Assert.AreEqual(1, counter);
			clocksource.Advance(2);
			Assert.AreEqual(2, counter);
		}

		[Test]
		public void ShouldTickWithTwoHandlers()
		{
			var clocksource = new BaseClockSource();
			var counterA = 0;
			var counterB = 0;

            clocksource.AddClockEntry(new ClockEntry(2, 1, () => counterA++) { Value = 0 });
            clocksource.AddClockEntry(new ClockEntry(5, 1, () => counterB++) { Value = 0 });
			clocksource.Advance(2);
			Assert.AreEqual(1, counterA);
			Assert.AreEqual(0, counterB);
			clocksource.Advance(2);
			Assert.AreEqual(2, counterA);
			Assert.AreEqual(0, counterB);
			clocksource.Advance(1);
			Assert.AreEqual(2, counterA);
			Assert.AreEqual(1, counterB);
		}
	}
}

