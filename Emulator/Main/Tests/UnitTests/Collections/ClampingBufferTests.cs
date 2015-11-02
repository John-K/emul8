//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using Emul8.Utilities.Collections;
using NUnit.Framework;

namespace UnitTests.Collections
{
	[TestFixture]
	public class ClampingBufferTests
	{
		[Test]
		public void ShouldSaveWithoutOverflow()
		{
			var buffer = new CircularBuffer<int>(5);
			buffer.Add(1);
			buffer.Add(2);
			buffer.Add(3);
			CollectionAssert.AreEqual(new [] { 1, 2, 3 }, buffer);
		}

		[Test]
		public void ShouldSaveWithoutOverflowArray()
		{
			var buffer = new CircularBuffer<int>(5);
			var array = new [] { 1, 2, 3, -1, 0 };
			for(var i = 0; i < 3; i++)
			{
				buffer.Add(array[i]);
			}
			var copy = new int[5];
			copy[3] = -1;
			buffer.CopyTo(copy, 0);
			CollectionAssert.AreEqual(array, copy);
		}

		[Test]
		public void ShouldSaveWithOverflow()
		{
			var buffer = new CircularBuffer<int>(4);
			for(var i = 0; i < 6; i++)
			{
				buffer.Add(i);
			}
			Assert.AreEqual(buffer, new [] { 3, 4, 5 });
		}

		[Test]
		public void ShouldSaveWithOverflowArray()
		{
			var buffer = new CircularBuffer<int>(4);
			for(var i = 0; i < 6; i++)
			{
				buffer.Add(i);
			}
			var result = new [] { 3, 4, 5 };
			var copy = new int[3];
			buffer.CopyTo(copy, 0);
			Assert.AreEqual(result, copy);
		}
	}
}

