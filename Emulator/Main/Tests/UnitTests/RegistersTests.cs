//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using NUnit.Framework;
using Emul8.Core.Structure.Registers;
using System.Collections.Generic;

namespace Emul8.UnitTests
{
    [TestFixture]
    public class RegistersTests
    {
        [Test]
        public void ShouldNotAcceptOutOfBoundsValues()
        {
            var foundException = false;
            try
            {
                enumRWField.Value = (TwoBitEnum)(1 << 2);
            }
            catch(ArgumentException)
            {
                foundException = true;
            }
            finally
            {
                Assert.IsTrue(foundException);
            }

            foundException = false;
            try
            {
                valueRWField.Value = (1 << 4);
            }
            catch(ArgumentException)
            {
                foundException = true;
            }
            finally
            {
                Assert.IsTrue(foundException);
            }
        }

        [Test]
        public void ShouldNotAcceptNegativeFields()
        {
            var foundException = false;
            var localRegister = new DoubleWordRegister(null);
            try
            {
                localRegister.DefineEnumField<TwoBitEnum>(0, -1); 
            }
            catch(ArgumentException)
            {
                foundException = true;
            }
            finally
            {
                Assert.IsTrue(foundException);
            }

            foundException = false;
            try
            {
                localRegister.DefineValueField(0, -1); 
            }
            catch(ArgumentException)
            {
                foundException = true;
            }
            finally
            {
                Assert.IsTrue(foundException);
            }
        }


        [Test]
        public void ShouldNotExceedRegisterSize()
        {
            var registersAndPositions = new Dictionary<PeripheralRegister, int>
            {
                { new DoubleWordRegister(null), 31 },
                { new WordRegister(null), 15 },
                { new ByteRegister(null), 7 },
            };
            foreach(var registerAndPosition in registersAndPositions)
            {
                var foundException = false;
                var localRegister = registerAndPosition.Key;
                try
                {
                    localRegister.DefineEnumField<TwoBitEnum>(registerAndPosition.Value, 2);
                }
                catch(ArgumentException)
                {
                    foundException = true;
                }
                finally
                {
                    Assert.IsTrue(foundException);
                }
            }
        }

        [Test]
        public void ShouldNotAllowIntersectingFields()
        {
            var foundException = false;
            var localRegister = new DoubleWordRegister(null);
            localRegister.DefineValueField(1, 5);
            try
            {
                localRegister.DefineValueField(0, 2);
            }
            catch(ArgumentException)
            {
                foundException = true;
            }
            finally
            {
                Assert.IsTrue(foundException);
            }
        }

        [Test]
        public void ShouldWriteFieldsWithMaxLength()
        {
            var localRegister = new DoubleWordRegister(null);
            localRegister.DefineValueField(0, 32);
            localRegister.Write(uint.MaxValue);
            Assert.AreEqual(uint.MaxValue, localRegister.Read());
        }

        [Test]
        public void ShouldReadBoolField()
        {
            register.Write(1 << 2);
            Assert.AreEqual(true, flagRWField.Value);
        }

        [Test]
        public void ShouldReadEnumField()
        {
            register.Write(3);
            Assert.AreEqual(TwoBitEnum.D, enumRWField.Value);
        }

        [Test]
        public void ShouldReadValueField()
        {
            register.Write(88); //1011000
            Assert.AreEqual(11, valueRWField.Value); //1011
        }

        [Test]
        public void ShouldWriteBoolField()
        {
            flagRWField.Value = true;
            Assert.AreEqual(1 << 2, register.Read());
        }

        [Test]
        public void ShouldWriteEnumField()
        {
            enumRWField.Value = TwoBitEnum.D;
            Assert.AreEqual((uint)TwoBitEnum.D, register.Read());
        }

        [Test]
        public void ShouldWriteValueField()
        {
            valueRWField.Value = 11;
            Assert.AreEqual(88, register.Read());
        }

        [Test]
        public void ShouldResetComplexRegister()
        {
            register.Reset();
            Assert.AreEqual(0x3780, register.Read());
        }

        [Test]
        public void ShouldNotWriteUnwritableField()
        {
            register.Write(1 << 21);
            Assert.AreEqual(false, flagRField.Value);
        }

        [Test]
        public void ShouldNotReadUnreadableField()
        {
            flagWField.Value = true;
            Assert.AreEqual(0, register.Read());
        }

        [Test]
        public void ShouldWriteZeroToClear()
        {
            flagW0CField.Value = true;
            Assert.AreEqual(1 << 18, register.Read());
            register.Write(0);
            Assert.AreEqual(false, flagW0CField.Value);
            Assert.AreEqual(0, register.Read());
        }

        [Test]
        public void ShouldWriteOneToClear()
        {
            flagW1CField.Value = true;
            Assert.AreEqual(1 << 17, register.Read());
            register.Write(1 << 17);
            Assert.AreEqual(false, flagW0CField.Value);
            Assert.AreEqual(0, register.Read());
        }

        [Test]
        public void ShouldReadToClear()
        {
            flagWRTCField.Value = true;
            Assert.AreEqual(1 << 19, register.Read());
            Assert.AreEqual(false, flagWRTCField.Value);
        }

        [Test]
        public void ShouldCallReadHandler()
        {
            //for the sake of sanity
            Assert.AreEqual(0, enumCallbacks);
            Assert.AreEqual(0, boolCallbacks);
            Assert.AreEqual(0, numberCallbacks);
            register.Read();
            Assert.AreEqual(1, enumCallbacks);
            Assert.AreEqual(1, boolCallbacks);
            Assert.AreEqual(1, numberCallbacks);

            Assert.IsTrue(oldBoolValue == newBoolValue);
            Assert.IsTrue(oldEnumValue == newEnumValue);
            Assert.IsTrue(oldUintValue == newUintValue);
        }

        [Test]
        public void ShouldRetrieveValueFromHandler()
        {
            enableValueProviders = true;
            register.Write(3 << 0x16 | 1 << 0x19);
            Assert.AreEqual(4 << 0x16 | 1 << 0x1A, register.Read());
        }

        [Test]
        public void ShouldCallWriteAndHandler()
        {
            Assert.AreEqual(0, enumCallbacks);
            Assert.AreEqual(0, boolCallbacks);
            Assert.AreEqual(0, numberCallbacks);
            register.Write(0x2880);
            //Two calls for changed registers, 1 call for unchanged register
            Assert.AreEqual(2, enumCallbacks);
            Assert.AreEqual(1, boolCallbacks);
            Assert.AreEqual(2, numberCallbacks);

            Assert.IsTrue(oldBoolValue == newBoolValue);
            Assert.IsTrue(oldEnumValue == TwoBitEnum.A && newEnumValue == TwoBitEnum.B);
            Assert.IsTrue(oldUintValue == 0);
            Assert.IsTrue(newUintValue == 10);
        }

        [Test]
        public void ShouldWorkWithUndefinedEnumValue()
        {
            register.Write(2);
            Assert.AreEqual((TwoBitEnum)2, enumRWField.Value);
        }

        [Test]
        public void ShouldToggleField()
        {
            register.Write(1 << 15);
            Assert.AreEqual(true, flagTRField.Value);
            register.Write(1 << 15);
            Assert.AreEqual(false, flagTRField.Value);
            register.Write(1 << 15);
            Assert.AreEqual(true, flagTRField.Value);
        }

        [Test]
        public void ShouldSetField()
        {
            register.Write(1 << 16);
            Assert.AreEqual(true, flagSRField.Value);
            register.Write(0);
            Assert.AreEqual(true, flagSRField.Value);
        }

        [SetUp]
        public void SetUp()
        {
            register = new DoubleWordRegister(null, 0x3780u);
            enumRWField = register.DefineEnumField<TwoBitEnum>(0, 2);
            flagRWField = register.DefineFlagField(2);
            valueRWField = register.DefineValueField(3, 4);
            register.DefineEnumField<TwoBitEnum>(7, 2, readCallback: EnumCallback, writeCallback: EnumCallback, changeCallback: EnumCallback);
            register.DefineFlagField(9, readCallback: BoolCallback, writeCallback: BoolCallback, changeCallback: BoolCallback);
            register.DefineValueField(10, 4, readCallback: NumberCallback, writeCallback: NumberCallback, changeCallback: NumberCallback);
            flagTRField = register.DefineFlagField(15, FieldMode.Read | FieldMode.Toggle);
            flagSRField = register.DefineFlagField(16, FieldMode.Read | FieldMode.Set);
            flagW1CField = register.DefineFlagField(17, FieldMode.Read | FieldMode.WriteOneToClear);
            flagW0CField = register.DefineFlagField(18, FieldMode.Read | FieldMode.WriteZeroToClear);
            flagWRTCField = register.DefineFlagField(19, FieldMode.ReadToClear | FieldMode.Write);
            flagWField = register.DefineFlagField(20, FieldMode.Write);
            flagRField = register.DefineFlagField(21, FieldMode.Read);
            register.DefineValueField(22, 3, valueProviderCallback: ModifyingValueCallback);
            register.DefineFlagField(25, valueProviderCallback: ModifyingFlagCallback);
            register.DefineEnumField<TwoBitEnum>(26, 2, valueProviderCallback: ModifyingEnumCallback);

            enableValueProviders = false;

            enumCallbacks = 0;
            boolCallbacks = 0;
            numberCallbacks = 0;
            oldBoolValue = false;
            newBoolValue = false;
            oldEnumValue = TwoBitEnum.A;
            newEnumValue = TwoBitEnum.A;
            oldUintValue = 0;
            newUintValue = 0;
        }

        private void EnumCallback(TwoBitEnum oldValue, TwoBitEnum newValue)
        {
            enumCallbacks++;
            oldEnumValue = oldValue;
            newEnumValue = newValue;
        }

        private void BoolCallback(bool oldValue, bool newValue)
        {
            boolCallbacks++;
            oldBoolValue = oldValue;
            newBoolValue = newValue;
        }

        private void NumberCallback(uint oldValue, uint newValue)
        {
            numberCallbacks++;
            oldUintValue = oldValue;
            newUintValue = newValue;
        }

        private uint ModifyingValueCallback(uint currentValue)
        {
            if(enableValueProviders)
            {
                return currentValue + 1;
            }
            return currentValue;
        }

        private bool ModifyingFlagCallback(bool currentValue)
        {
            if(enableValueProviders)
            {
                return !currentValue;
            }
            return currentValue;
        }

        private TwoBitEnum ModifyingEnumCallback(TwoBitEnum currentValue)
        {
            if(enableValueProviders)
            {
                return currentValue + 1;
            }
            return currentValue;
        }

        /*Offset  |   Field
          --------------------
          0       |   Enum rw
          1       |
          --------------------
          2       |   Bool rw
          --------------------
          3       |   Value rw
          4       |
          5       |
          6       |
          --------------------
          7       |   Enum rw w/callbacks, reset value 3
          8       |
          --------------------
          9       |   Bool rw w/callbacks, reset value 1
          --------------------
          A       |   Value rw w/callbacks, reset value 13
          B       |
          C       |
          D       |
          --------------------
          F       |   Bool tr
          --------------------
          10      |   Bool sr
          --------------------
          11      |   Bool w1c
          --------------------
          12      |   Bool w0c
          --------------------
          13      |   Bool wrtc
          --------------------
          14      |   Bool w
          --------------------
          15      |   Bool r
          --------------------
          16      |   Value rw w/changing r callback
          17      |   
          18      |   
          --------------------
          19      |   Bool rw w/changing r callback
          --------------------
          1A      |   Enum rw w/changing r callback
          1B      |
        */
        private DoubleWordRegister register;

        private int enumCallbacks;
        private int boolCallbacks;
        private int numberCallbacks;

        private TwoBitEnum oldEnumValue;
        private TwoBitEnum newEnumValue;
        private bool oldBoolValue;
        private bool newBoolValue;
        private uint oldUintValue;
        private uint newUintValue;

        private bool enableValueProviders;

        private IEnumRegisterField<TwoBitEnum> enumRWField;
        private IFlagRegisterField flagRWField;
        private IValueRegisterField valueRWField;
        private IFlagRegisterField flagTRField;
        private IFlagRegisterField flagSRField;
        private IFlagRegisterField flagW1CField;
        private IFlagRegisterField flagW0CField;
        private IFlagRegisterField flagWRTCField;
        private IFlagRegisterField flagWField;
        private IFlagRegisterField flagRField;

        private enum TwoBitEnum
        {
            A = 0,
            B = 1,
            D = 3
        }
    }
}

