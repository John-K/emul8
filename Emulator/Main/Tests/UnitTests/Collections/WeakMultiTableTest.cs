//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Emul8.Utilities.Collections;
using System.Linq;
using NUnit.Framework;

namespace UnitTests.Collections
{
    public class WeakMultiTableTest
    {
        [Test]
        public void ShouldHandleManyRightValuesForLeft()
        {
            var table = new WeakMultiTable<int, int>();
            table.Add(0, 5);
            table.Add(0, 6);
            Assert.AreEqual(2, table.GetAllForLeft(0).Count());
        }

        [Test]
        public void ShouldHandleManyLeftValuesForRight()
        {
            var table = new WeakMultiTable<int, int>();
            table.Add(5, 0);
            table.Add(6, 0);
            Assert.AreEqual(2, table.GetAllForRight(0).Count());
        }

        [Test,Ignore]
        public void ShouldRemovePair()
        {
            var table = new WeakMultiTable<int, int>();
            table.Add(0, 0);
            table.Add(1, 1);

            Assert.AreEqual(1, table.GetAllForLeft(0).Count());
            Assert.AreEqual(1, table.GetAllForLeft(1).Count());

            table.RemovePair(0, 0);

            Assert.AreEqual(0, table.GetAllForLeft(0).Count());
            Assert.AreEqual(1, table.GetAllForLeft(1).Count());
        }

        [Test]
        public void ShouldHandleRemoveOfUnexistingItem()
        {
            var table = new WeakMultiTable<int, int>();

            Assert.AreEqual(0, table.GetAllForLeft(0).Count());
            table.RemovePair(0, 0);
            Assert.AreEqual(0, table.GetAllForLeft(0).Count());
        }

        [Test]
        public void ShouldHoldWeakReference()
        {
            if (GC.MaxGeneration == 0)
            {
                Assert.Inconclusive("Not working on boehm");
            }

            var table = new WeakMultiTable<NotSoWeakClass, int>();
            var wr = GenerateWeakReferenceAndInsertIntoTable(table);

            GC.Collect();
            Assert.IsFalse(wr.IsAlive);
        }

        private WeakReference GenerateWeakReferenceAndInsertIntoTable(WeakMultiTable<NotSoWeakClass, int> table)
        {
            var item = new NotSoWeakClass();
            table.Add(item, 0);
            return new WeakReference(item);
        }

        private class NotSoWeakClass
        {
        }
    }
}

