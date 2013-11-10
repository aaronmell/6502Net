using System.ComponentModel;
using NUnit.Framework;

namespace Processor.UnitTests
{
	[TestFixture]
	public class ProcessorTests
	{
		#region Initialization Tests
		[Test]
// ReSharper disable InconsistentNaming
		public void Processor_Status_Flags_Initialized_Correctly()
		{
			var processor = new Processor();
			Assert.That(processor.CarryFlag, Is.False);
			Assert.That(processor.ZeroFlag, Is.False);
			Assert.That(processor.DisableInterruptFlag, Is.False);
			Assert.That(processor.DecimalFlag, Is.False);
			Assert.That(processor.OverflowFlag, Is.False);
			Assert.That(processor.NegativeFlag, Is.False);
		}

		[Test]
		public void Processor_Registers_Initialized_Correctly()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0));
			Assert.That(processor.XRegister, Is.EqualTo(0));
			Assert.That(processor.YRegister, Is.EqualTo(0));
			Assert.That(processor.CurrentOpCode, Is.EqualTo(0));
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));
		}

		[Test]
		public void ProgramCounter_Correct_When_Program_Loaded()
		{
			var processor = new Processor();
			processor.LoadProgram(0, new byte[1], 0x01);
			Assert.That(processor.ProgramCounter, Is.EqualTo(0x01));
		}

		[Test, ExpectedException(typeof(InvalidEnumArgumentException))]
		public void Throws_Exception_When_OpCode_Is_Invalid()
		{
			var processor = new Processor();
			processor.LoadProgram(0x00, new byte[] { 0xFF}, 0x00);
			processor.NextStep();
		}
		
		[Test]
		public void Stack_Pointer_Initializes_To_Default_Value_After_Reset()
		{
			var processor = new Processor();
			processor.Reset();

			Assert.That(processor.StackPointer, Is.EqualTo(0xFD));
		}
		#endregion

		#region ADC - Add with Carry Tests
		[TestCase(0, 0, false, 0)]
		[TestCase(0, 1, false, 1)]
		[TestCase(1, 2, false, 3)]
		[TestCase(255, 1, false, 0)]
		[TestCase(254, 1, false, 255)]
		[TestCase(255, 0, false, 255)]
		[TestCase(0, 0, true, 1)]
		[TestCase(0, 1, true, 2)]
		[TestCase(1, 2, true, 4)]
		[TestCase(254, 1, true, 0)]
		[TestCase(253, 1, true, 255)]
		[TestCase(254, 0, true, 255)]
		[TestCase(255, 255, true, 255)]
		public void ADC_Accumulator_Correct_When_Not_In_BDC_Mode(byte accumlatorIntialValue, byte amountToAdd, bool CarryFlagSet, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			if (CarryFlagSet)
			{ 
				processor.LoadProgram(0, new byte[] { 0x38, 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00); 
				processor.NextStep();
			}
			else
				processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);
			
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));
			
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0, 0, false, 0)]
		[TestCase(0, 1, false, 1)]
		[TestCase(1, 2, false, 3)]
		[TestCase(99, 1, false, 0)]
		[TestCase(98, 1, false, 99)]
		[TestCase(99, 0, false, 99)]
		[TestCase(0, 0, true, 1)]
		[TestCase(0, 1, true, 2)]
		[TestCase(1, 2, true, 4)]
		[TestCase(98, 1, true, 0)]
		[TestCase(97, 1, true, 99)]
		[TestCase(98, 0, true, 99)]
		public void ADC_Accumulator_Correct_When_In_BDC_Mode(byte accumlatorIntialValue, byte amountToAdd,
		                                                               bool setCarryFlag, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			if (setCarryFlag)
			{
				processor.LoadProgram(0, new byte[] { 0x38, 0xF8, 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);
				processor.NextStep();
			}
			else
				processor.LoadProgram(0, new byte[] { 0xF8, 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);

			processor.NextStep();
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(254, 1, false, false)]
		[TestCase(254, 1, true, true)]
		[TestCase(253, 1, true, false)]
		[TestCase(255, 1, false, true)]
		[TestCase(255, 1, true, true)]
		public void ADC_Carry_Correct_When_Not_In_BDC_Mode(byte accumlatorIntialValue, byte amountToAdd, bool setCarryFlag,
		                                                             bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));
			
			if (setCarryFlag)
			{
				processor.LoadProgram(0, new byte[] { 0x38, 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);
				processor.NextStep();
			}
			else
				processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);

			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.CarryFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(98, 1, false, false)]
		[TestCase(98, 1, true, false)]
		[TestCase(99, 1, false, true)]
		[TestCase(99, 1, true, true)]
		public void ADC_Carry_Correct_When_In_BDC_Mode(byte accumlatorIntialValue, byte amountToAdd, bool setCarryFlag,
																	 bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xF8, 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);

			processor.NextStep();
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.CarryFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0, 0, true)]
		[TestCase(255, 1, true)]
		[TestCase(0, 1, false)]
		[TestCase(1, 0, false)]
		public void ADC_Zero_Flag_Correct_When_Not_In_BDC_Mode(byte accumlatorIntialValue, byte amountToAdd, bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);

			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(126, 1, false)]
		[TestCase(1, 126, false)]
		[TestCase(1, 127, true)]
		[TestCase(127, 1, true)]
		[TestCase(1, 254, true)]
		[TestCase(254, 1, true)]
		[TestCase(1, 255, false)]
		[TestCase(255, 1, false)]
		public void ADC_Negative_Flag_Correct(byte accumlatorIntialValue, byte amountToAdd, bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));


			processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);

			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0, 127, false, false)]
		[TestCase(0, 128, false, false)]
		[TestCase(1, 127, false, true)]
		[TestCase(1, 128, false, false)]
		[TestCase(127, 1, false, true)]
		[TestCase(127, 127, false, true)]
		[TestCase(128, 127, false, false)]
		[TestCase(128, 128, false, true)]
		[TestCase(128, 129, false, true)]
		[TestCase(128, 255, false, true)]
		[TestCase(255, 0, false, false)]
		[TestCase(255, 1, false, false)]
		[TestCase(255, 127, false, false)]
		[TestCase(255, 128, false, true)]
		[TestCase(255, 255, false, false)]
		[TestCase(0, 127, true, true)]
		[TestCase(0, 128, true, false)]
		[TestCase(1, 127, true, true)]
		[TestCase(1, 128, true, false)]
		[TestCase(127, 1, true, true)]
		[TestCase(127, 127, true, true)]
		[TestCase(128, 127, true, false)]
		[TestCase(128, 128, true, true)]
		[TestCase(128, 129, true, true)]
		[TestCase(128, 255, true, false)]
		[TestCase(255, 0, true, false)]
		[TestCase(255, 1, true, false)]
		[TestCase(255, 127, true, false)]
		[TestCase(255, 128, true, false)]
		[TestCase(255, 255, true, false)]
		public void ADC_Overflow_Flag_Correct(byte accumlatorIntialValue, byte amountToAdd, bool setCarryFlag, bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			if (setCarryFlag)
			{
				processor.LoadProgram(0, new byte[] { 0x38, 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);
				processor.NextStep();
			}
			else
				processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0x69, amountToAdd }, 0x00);

			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.OverflowFlag, Is.EqualTo(expectedValue));
		}
		#endregion

		#region AND - Compare Memory with Accumulator
		[TestCase(0,0,0)]
		[TestCase(255, 255, 255)]
		[TestCase(255, 254, 254)]
		[TestCase(170, 85, 0)]
		public void AND_Accumulator_Correct(byte accumlatorIntialValue, byte amountToAnd, byte expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0x29, amountToAnd }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedResult));
		}
		#endregion

		#region ASL - Arithmetic Shift Left

		[TestCase(0x0A, 109, 218, 0)] // ASL Accumulator
		[TestCase(0x0A, 108, 216, 0)] // ASL Accumulator
		[TestCase(0x06, 109, 218, 0x01)] // ASL Zero Page
		[TestCase(0x16, 109, 218, 0x01)] // ASL Zero Page X
		[TestCase(0x0E, 109, 218, 0x01)] // ASL Absolute
		[TestCase(0x1E, 109, 218, 0x01)] // ASL Absolute X
		public void ASL_Correct_Value_Stored(byte operation, byte valueToShift, byte expectedValue, byte expectedLocation)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0, new byte[] { 0xA9, valueToShift, operation, expectedLocation }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(operation == 0x0A
				? processor.Accumulator
				: processor.Memory.ReadValue(expectedLocation),
						Is.EqualTo(expectedValue));
		}

		[TestCase(127, false)]
		[TestCase(128, true)]
		[TestCase(255, true)]
		[TestCase(0, false)]
		public void ASL_Carry_Set_Correctly(byte valueToShift, bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0, new byte[] { 0xA9, valueToShift, 0x0A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(63, false)]
		[TestCase(64, true)]
		[TestCase(127, true)]
		[TestCase(128, false)]
		[TestCase(0, false)]
		public void ASL_Negative_Set_Correctly(byte valueToShift, bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0, new byte[] { 0xA9, valueToShift, 0x0A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(127, false)]
		[TestCase(128, true)]
		[TestCase(0, true)]
		public void ASL_Zero_Set_Correctly(byte valueToShift, bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0, new byte[] { 0xA9, valueToShift, 0x0A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedValue));
		}
		#endregion

		#region BCC - Branch On Carry Clear

		[TestCase(0, 1, 3)]
		[TestCase(0x80, 0x80, 2)]
		[TestCase(0, 0xFD, 0xFFFF)]
		[TestCase(0x7D, 0x80, 0xFFFF)]
		public void BCC_Program_Counter_Correct(int programCounterInitalValue, byte offset, int expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(programCounterInitalValue, new byte[] { 0x90, offset }, programCounterInitalValue);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedValue));
		}
		#endregion

		#region BCS - Branch on Carry Set
		[TestCase(0, 1, 4)]
		[TestCase(0x80, 0x80, 3)]
		[TestCase(0, 0xFC, 0xFFFF)]
		[TestCase(0x7C, 0x80, 0xFFFF)]
		public void BCS_Program_Counter_Correct(int programCounterInitalValue, byte offset, int expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(programCounterInitalValue, new byte[] { 0x38, 0xB0, offset }, programCounterInitalValue);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedValue));
		}
		#endregion

		#region BEQ - Branch on Zero Set
		[TestCase(0, 1, 5)]
		[TestCase(0x80, 0x80, 4)]
		[TestCase(0, 0xFB, 0xFFFF)]
		[TestCase(0x7B, 0x80, 0xFFFF)]
		[TestCase(2, 0xFE, 4)]
		public void BEQ_Program_Counter_Correct(int programCounterInitalValue, byte offset, int expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(programCounterInitalValue, new byte[] { 0xA9, 0x00 ,0xF0, offset }, programCounterInitalValue);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedValue));
		}
		
		#endregion

		#region BIT - Compare Memory with Accumulator

		[TestCase(0x24, 0x7f, 0x7F, false)] // BIT Zero Page
		[TestCase(0x24, 0x80, 0x7F, false)] // BIT Zero Page
		[TestCase(0x24, 0x7F, 0x80, false)] // BIT Zero Page
		[TestCase(0x24, 0x80, 0xFF, true)] // BIT Zero Page
		[TestCase(0x24, 0xFF, 0x80, true)] // BIT Zero Page
		[TestCase(0x2C, 0x7F, 0x7F, false)] // BIT Absolute
		[TestCase(0x2C, 0x80, 0x7F, false)] // BIT Absolute
		[TestCase(0x2C, 0x7F, 0x80, false)] // BIT Absolute
		[TestCase(0x2C, 0x80, 0xFF, true)] // BIT Absolute
		[TestCase(0x2C, 0xFF, 0x80, true)] // BIT Absolute
		public void BIT_Negative_Set_When_Comparison_Is_Negative_Number(byte operation, byte accumulatorValue, byte valueToTest, bool expectedResult)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0x00, new byte[] { 0xA9, accumulatorValue, operation, 0x06, 0x00, 0x00, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x24, 0x3F, 0x3F, false)] // BIT Zero Page
		[TestCase(0x24, 0x3F, 0x40, false)] // BIT Zero Page
		[TestCase(0x24, 0x40, 0x3F, false)] // BIT Zero Page
		[TestCase(0x24, 0x40, 0x7F, true)] // BIT Zero Page
		[TestCase(0x24, 0x7F, 0x40, true)] // BIT Zero Page
		[TestCase(0x24, 0x7F, 0x80, false)] // BIT Zero Page
		[TestCase(0x24, 0x80, 0x7F, false)] // BIT Zero Page
		[TestCase(0x24, 0xC0, 0xDF, true)] // BIT Zero Page
		[TestCase(0x24, 0xDF, 0xC0, true)] // BIT Zero Page
		[TestCase(0x24, 0x3F, 0x3F, false)] // BIT Zero Page
		[TestCase(0x24, 0xC0, 0xFF, true)] // BIT Zero Page
		[TestCase(0x24, 0xFF, 0xC0, true)] // BIT Zero Page
		[TestCase(0x24, 0x40, 0xFF, true)] // BIT Zero Page
		[TestCase(0x24, 0xFF, 0x40, true)] // BIT Zero Page
		[TestCase(0x24, 0xC0, 0x7F, true)] // BIT Zero Page
		[TestCase(0x24, 0x7F, 0xC0, true)] // BIT Zero Page
		[TestCase(0x2C, 0x3F, 0x3F, false)] // BIT Absolute
		[TestCase(0x2C, 0x3F, 0x40, false)] // BIT Absolute
		[TestCase(0x2C, 0x40, 0x3F, false)] // BIT Absolute
		[TestCase(0x2C, 0x40, 0x7F, true)] // BIT Absolute
		[TestCase(0x2C, 0x7F, 0x40, true)] // BIT Absolute
		[TestCase(0x2C, 0x7F, 0x80, false)] // BIT Absolute
		[TestCase(0x2C, 0x80, 0x7F, false)] // BIT Absolute
		[TestCase(0x2C, 0xC0, 0xDF, true)] // BIT Absolute
		[TestCase(0x2C, 0xDF, 0xC0, true)] // BIT Absolute
		[TestCase(0x2C, 0x3F, 0x3F, false)] // BIT Absolute
		[TestCase(0x2C, 0xC0, 0xFF, true)] // BIT Absolute
		[TestCase(0x2C, 0xFF, 0xC0, true)] // BIT Absolute
		[TestCase(0x2C, 0x40, 0xFF, true)] // BIT Absolute
		[TestCase(0x2C, 0xFF, 0x40, true)] // BIT Absolute
		[TestCase(0x2C, 0xC0, 0x7F, true)] // BIT Absolute
		[TestCase(0x2C, 0x7F, 0xC0, true)] // BIT Absolute
		public void BIT_Overflow_Set_By_Bit_Six(byte operation, byte accumulatorValue, byte valueToTest, bool expectedResult)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0x00, new byte[] { 0xA9, accumulatorValue, operation, 0x06, 0x00, 0x00, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.OverflowFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x24, 0x00, 0x00, true)] // BIT Zero Page
		[TestCase(0x24, 0xFF, 0xFF, false)] // BIT Zero Page
		[TestCase(0x24, 0xAA, 0x55, true)] // BIT Zero Page
		[TestCase(0x24, 0x55, 0xAA, true)] // BIT Zero Page
		[TestCase(0x2C, 0x00, 0x00, true)] // BIT Absolute
		[TestCase(0x2C, 0xFF, 0xFF, false)] // BIT Absolute
		[TestCase(0x2C, 0xAA, 0x55, true)] // BIT Absolute
		[TestCase(0x2C, 0x55, 0xAA, true)] // BIT Absolute
		public void BIT_Zero_Set_When_Comparison_Is_Zero(byte operation, byte accumulatorValue, byte valueToTest, bool expectedResult)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0x00, new byte[] { 0xA9, accumulatorValue, operation, 0x06, 0x00, 0x00, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region BMI - Branch if Negative Set
		[TestCase(0, 1, 5)]
		[TestCase(0x80, 0x80, 4)]
		[TestCase(0, 0xFB, 0xFFFF)]
		[TestCase(0x7B, 0x80, 0xFFFF)]
		public void BMI_Program_Counter_Correct(int programCounterInitalValue, byte offset, int expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(programCounterInitalValue, new byte[] { 0xA9, 0x80, 0x30, offset }, programCounterInitalValue);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedValue));
		}
		#endregion

		#region BNE - Branch On Result Not Zero

		[TestCase(0, 1, 5)]
		[TestCase(0x80, 0x80, 4)]
		[TestCase(0, 0xFB, 0xFFFF)]
		[TestCase(0x7B, 0x80, 0xFFFF)]
		public void BNE_Program_Counter_Correct(int programCounterInitalValue, byte offset, int expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(programCounterInitalValue, new byte[] { 0xA9, 0x01, 0xD0, offset }, programCounterInitalValue);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedValue));
		}

		#endregion

		#region BPL - Branch if Negative Clear
		[TestCase(0, 1, 5)]
		[TestCase(0x80, 0x80, 4)]
		[TestCase(0, 0xFB, 0xFFFF)]
		[TestCase(0x7B, 0x80, 0xFFFF)]
		public void BPL_Program_Counter_Correct(int programCounterInitalValue, byte offset, int expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(programCounterInitalValue, new byte[] { 0xA9, 0x79, 0x10, offset }, programCounterInitalValue);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedValue));
		}
		#endregion

		#region BRK - Simulate Interrupt Request (IRQ)

		[Test]
		public void BRK_Program_Counter_Set_To_Address_At_Break_Vector_Address()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x00 }, 0x00);

			//Manually Write the Break Address
			processor.Memory.WriteValue(0xFFFE, 0xBC);
			processor.Memory.WriteValue(0xFFFF, 0xCD);
			
			processor.NextStep();
		
			Assert.That(processor.ProgramCounter, Is.EqualTo(0xCDBC));
		}

		[Test]
		public void BRK_Program_Counter_Stack_Correct()
		{
			var processor = new Processor();

			processor.LoadProgram(0xABCD, new byte[] { 0x00 }, 0xABCD);
			
			var stackLocation = processor.StackPointer;
			processor.NextStep();

			Assert.That(processor.Memory.ReadValue(stackLocation + 0x100), Is.EqualTo(0xAB));
			Assert.That(processor.Memory.ReadValue(stackLocation + 0x100 - 1), Is.EqualTo(0xCF));
		}

		[Test]
		public void BRK_Stack_Pointer_Correct()
		{
			var processor = new Processor();

			processor.LoadProgram(0xABCD, new byte[] { 0x00 }, 0xABCD);

			var stackLocation = processor.StackPointer;
			processor.NextStep();

			Assert.That(processor.StackPointer, Is.EqualTo(stackLocation - 3));
		}

		[TestCase(0x038, 0x31)] //SEC Carry Flag Test
		[TestCase(0x0F8, 0x38)] //SED Decimal Flag Test
		[TestCase(0x078, 0x34)] //SEI Interrupt Flag Test
		public void BRK_Stack_Set_Flag_Operations_Correctly(byte operation, byte expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x58, operation, 0x00 }, 0x00);

			var stackLocation = processor.StackPointer;
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.Memory.ReadValue(stackLocation + 0x100 - 2), Is.EqualTo(expectedValue));
		}

		[TestCase(0x01, 0x80, 0xB0)] //Negative
		[TestCase(0x01, 0x7F, 0xF0)] //Overflow + Negative
		[TestCase(0x00, 0x00, 0x32)] //Zero
		public void BRK_Stack_Non_Set_Flag_Operations_Correctly(byte accumulatorValue, byte memoryValue, byte expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x58, 0xA9, accumulatorValue, 0x69, memoryValue, 0x00 }, 0x00);

			var stackLocation = processor.StackPointer;
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.Memory.ReadValue(stackLocation + 0x100 - 2), Is.EqualTo(expectedValue));
		}


		#endregion

		#region BVC - Branch if Overflow Clear
		[TestCase(0, 1, 3)]
		[TestCase(0x80, 0x80, 2)]
		[TestCase(0, 0xFD, 0xFFFF)]
		[TestCase(0x7D, 0x80, 0xFFFF)]
		public void BVC_Program_Counter_Correct(int programCounterInitalValue, byte offset, int expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(programCounterInitalValue, new byte[] { 0x50, offset }, programCounterInitalValue);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedValue));
		}
		#endregion

		#region BVS - Branch if Overflow Set
		[TestCase(0, 1, 7)]
		[TestCase(0x80, 0x80, 6)]
		[TestCase(0, 0xF9, 0xFFFF)]
		[TestCase(0x79, 0x80, 0xFFFF)]
		public void BVS_Program_Counter_Correct(int programCounterInitalValue, byte offset, int expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(programCounterInitalValue, new byte[] { 0xA9, 0x01, 0x69, 0x7F, 0x70, offset }, programCounterInitalValue);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedValue));
		}
		#endregion

		#region CLC - Clear Carry Flag

		[Test]
		public void CLC_Carry_Flag_Cleared_Correctly()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x18 }, 0x00);
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(false));
		}

		#endregion

		#region CLD - Clear Decimal Flag

		[Test]
		public void CLD_Carry_Flag_Set_And_Cleared_Correctly()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xF8, 0xD8 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.DecimalFlag, Is.EqualTo(false));
		}

		#endregion

		#region CLI - Clear Interrupt Flag

		[Test]
		public void CLI_Interrup_Flag_Cleared_Correctly()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x58 }, 0x00);
			processor.NextStep();

			Assert.That(processor.DisableInterruptFlag, Is.EqualTo(false));
		}

		#endregion

		#region CLV - Clear Overflow Flag

		[Test]
		public void CLV_Overflow_Flag_Cleared_Correctly()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xB8 }, 0x00);
			processor.NextStep();

			Assert.That(processor.OverflowFlag, Is.EqualTo(false));
		}

		#endregion

		#region CMP - Compare Memory With Accumulator

		[TestCase(0x00, 0x00, true)]
		[TestCase(0xFF, 0x00, false)]
		[TestCase(0x00, 0xFF, false)]
		[TestCase(0xFF, 0xFF, true)]
		public void CMP_Zero_Flag_Set_When_Values_Match(byte accumulatorValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0xC9, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}


		[TestCase(0x00, 0x00, true)]
		[TestCase(0xFF, 0x00, true)]
		[TestCase(0x00, 0xFF, false)]
		[TestCase(0x00, 0x01, false)]
		[TestCase(0xFF, 0xFF, true)]
		public void CMP_Carry_Flag_Set_When_Accumulator_Is_Greater_Than_Or_Equal(byte accumulatorValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0xC9, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0xFE, 0xFF, true)]
		[TestCase(0x81, 0x1, true)]
		[TestCase(0x81, 0x2, false)]
		[TestCase(0x79, 0x1, false)]
		[TestCase(0x00, 0x1, true)]
		public void CMP_Negative_Flag_Set_When_Result_Is_Negative(byte accumulatorValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0xC9, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}

		#endregion

		#region CPX - Compare Memory With X Register
		[TestCase(0x00, 0x00, true)]
		[TestCase(0xFF, 0x00, false)]
		[TestCase(0x00, 0xFF, false)]
		[TestCase(0xFF, 0xFF, true)]
		public void CPX_Zero_Flag_Set_When_Values_Match(byte xValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, xValue, 0xE0, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x00, 0x00, true)]
		[TestCase(0xFF, 0x00, true)]
		[TestCase(0x00, 0xFF, false)]
		[TestCase(0x00, 0x01, false)]
		[TestCase(0xFF, 0xFF, true)]
		public void CPX_Carry_Flag_Set_When_Accumulator_Is_Greater_Than_Or_Equal(byte xValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, xValue, 0xE0, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0xFE, 0xFF, true)]
		[TestCase(0x81, 0x1, true)]
		[TestCase(0x81, 0x2, false)]
		[TestCase(0x79, 0x1, false)]
		[TestCase(0x00, 0x1, true)]
		public void CPX_Negative_Flag_Set_When_Result_Is_Negative(byte xValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, xValue, 0xE0, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region CPY - Compare Memory With X Register
		[TestCase(0x00, 0x00, true)]
		[TestCase(0xFF, 0x00, false)]
		[TestCase(0x00, 0xFF, false)]
		[TestCase(0xFF, 0xFF, true)]
		public void CPY_Zero_Flag_Set_When_Values_Match(byte xValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, xValue, 0xC0, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x00, 0x00, true)]
		[TestCase(0xFF, 0x00, true)]
		[TestCase(0x00, 0xFF, false)]
		[TestCase(0x00, 0x01, false)]
		[TestCase(0xFF, 0xFF, true)]
		public void CPY_Carry_Flag_Set_When_Accumulator_Is_Greater_Than_Or_Equal(byte xValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, xValue, 0xC0, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0xFE, 0xFF, true)]
		[TestCase(0x81, 0x1, true)]
		[TestCase(0x81, 0x2, false)]
		[TestCase(0x79, 0x1, false)]
		[TestCase(0x00, 0x1, true)]
		public void CPY_Negative_Flag_Set_When_Result_Is_Negative(byte xValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, xValue, 0xC0, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region DEC - Decrement Memory by One
		
		[TestCase(0x00,0xFF)]
		[TestCase(0xFF, 0xFE)]
		public void DEC_Memory_Has_Correct_Value(byte initalMemoryValue, byte expectedMemoryValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xC6, 0x03, 0x00, initalMemoryValue }, 0x00);
			processor.NextStep();
			
			Assert.That(processor.Memory.ReadValue(0x03), Is.EqualTo(expectedMemoryValue));
		}

		[TestCase(0x00, false)]
		[TestCase(0x01, true)]
		[TestCase(0x02, false)]
		public void DEC_Zero_Has_Correct_Value(byte initalMemoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xC6, 0x03, 0x00, initalMemoryValue }, 0x00);
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x80, false)]
		[TestCase(0x81, true)]
		[TestCase(0x00, true)]
		public void DEC_Negative_Has_Correct_Value(byte initalMemoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xC6, 0x03, 0x00, initalMemoryValue }, 0x00);
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region DEX - Decrement X by One

		[TestCase(0x00, 0xFF)]
		[TestCase(0xFF, 0xFE)]
		public void DEX_XRegister_Has_Correct_Value(byte initialXRegisterValue, byte expectedMemoryValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, initialXRegisterValue, 0xCA }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.XRegister, Is.EqualTo(expectedMemoryValue));
		}

		[TestCase(0x00, false)]
		[TestCase(0x01, true)]
		[TestCase(0x02, false)]
		public void DEX_Zero_Has_Correct_Value(byte initialXRegisterValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, initialXRegisterValue, 0xCA }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x80, false)]
		[TestCase(0x81, true)]
		[TestCase(0x00, true)]
		public void DEX_Negative_Has_Correct_Value(byte initialXRegisterValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, initialXRegisterValue, 0xCA }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region DEY - Decrement Y by One

		[TestCase(0x00, 0xFF)]
		[TestCase(0xFF, 0xFE)]
		public void DEY_YRegister_Has_Correct_Value(byte initialYRegisterValue, byte expectedMemoryValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, initialYRegisterValue, 0x88 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.YRegister, Is.EqualTo(expectedMemoryValue));
		}

		[TestCase(0x00, false)]
		[TestCase(0x01, true)]
		[TestCase(0x02, false)]
		public void DEY_Zero_Has_Correct_Value(byte initialYRegisterValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, initialYRegisterValue, 0x88 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x80, false)]
		[TestCase(0x81, true)]
		[TestCase(0x00, true)]
		public void DEY_Negative_Has_Correct_Value(byte initialYRegisterValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, initialYRegisterValue, 0x88 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region EOR - Exclusive OR Compare Accumulator With Memory
		
		[TestCase(0x00, 0x00, 0x00)]
		[TestCase(0xFF, 0x00, 0xFF)]
		[TestCase(0x00, 0xFF, 0xFF)]
		[TestCase(0x55, 0xAA, 0xFF)]
		[TestCase(0xFF, 0xFF, 0x00)]
		public void EOR_Accumulator_Correct(byte accumulatorValue, byte memoryValue, byte expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x49, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedResult));
		}

		[TestCase(0xFF, 0xFF, false)]
		[TestCase(0x80, 0x7F, true)]
		[TestCase(0x40, 0x3F, false)]
		[TestCase(0xFF, 0x7F, true)]
		public void EOR_Negative_Flag_Correct(byte accumulatorValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x49, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0xFF, 0xFF, true)]
		[TestCase(0x80, 0x7F, false)]
		public void EOR_Zero_Flag_Correct(byte accumulatorValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x49, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		#endregion

		#region INC - Increment Memory by One

		[TestCase(0x00, 0x01)]
		[TestCase(0xFF, 0x00)]
		public void INC_Memory_Has_Correct_Value(byte initalMemoryValue, byte expectedMemoryValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xE6, 0x03, 0x00, initalMemoryValue }, 0x00);
			processor.NextStep();

			Assert.That(processor.Memory.ReadValue(0x03), Is.EqualTo(expectedMemoryValue));
		}

		[TestCase(0x00, false)]
		[TestCase(0xFF, true)]
		[TestCase(0xFE, false)]
		public void INC_Zero_Has_Correct_Value(byte initalMemoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xE6, 0x03, 0x00, initalMemoryValue }, 0x00);
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x78, false)]
		[TestCase(0x80, true)]
		[TestCase(0x00, false)]
		public void INC_Negative_Has_Correct_Value(byte initalMemoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xE6, 0x02, initalMemoryValue }, 0x00);
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region INX - Increment X by One

		[TestCase(0x00, 0x01)]
		[TestCase(0xFF, 0x00)]
		public void INX_XRegister_Has_Correct_Value(byte initialXRegister, byte expectedMemoryValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, initialXRegister, 0xE8 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.XRegister, Is.EqualTo(expectedMemoryValue));
		}

		[TestCase(0x00, false)]
		[TestCase(0xFF, true)]
		[TestCase(0xFE, false)]
		public void INX_Zero_Has_Correct_Value(byte initialXRegister, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, initialXRegister, 0xE8 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x78, false)]
		[TestCase(0x80, true)]
		[TestCase(0x00, false)]
		public void INX_Negative_Has_Correct_Value(byte initialXRegister, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, initialXRegister, 0xE8 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region INY - Increment Y by One

		[TestCase(0x00, 0x01)]
		[TestCase(0xFF, 0x00)]
		public void INY_YRegisgter_Has_Correct_Value(byte initialYRegister, byte expectedMemoryValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, initialYRegister, 0xC8 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.YRegister, Is.EqualTo(expectedMemoryValue));
		}

		[TestCase(0x00, false)]
		[TestCase(0xFF, true)]
		[TestCase(0xFE, false)]
		public void INY_Zero_Has_Correct_Value(byte initialYRegister, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, initialYRegister, 0xC8 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x78, false)]
		[TestCase(0x80, true)]
		[TestCase(0x00, false)]
		public void INY_Negative_Has_Correct_Value(byte initialYRegister, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, initialYRegister, 0xC8 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}

		#endregion

		#region JMP - Jump to New Location

		[Test]
		public void JMP_Program_Counter_Set_Correctly_After_Jump()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x4C, 0x08, 0x00 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(0x08));
		}

		[Test]
		public void JMP_Program_Counter_Set_Correctly_After_Indirect_Jump()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x6C, 0x03, 0x00, 0x08, 0x00 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(0x08));
		}
		#endregion

		#region JSR - Jump to SubRoutine

		[Test]
		public void JSR_Stack_Loads_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0xBBAA, new byte[] { 0x20, 0xCC, 0xCC }, 0xBBAA);

			var stackLocation = processor.StackPointer;
			processor.NextStep();
			
		
			Assert.That(processor.Memory.ReadValue(stackLocation + 0x100), Is.EqualTo(0xBB));
			Assert.That(processor.Memory.ReadValue(stackLocation + 0x100 - 1), Is.EqualTo(0xAC));
		}

		[Test]
		public void JSR_Program_Counter_Correct()
		{
			var processor = new Processor();

			processor.LoadProgram(0xBBAA, new byte[] { 0x20, 0xCC, 0xCC }, 0xBBAA);
			processor.NextStep();


			Assert.That(processor.ProgramCounter, Is.EqualTo(0xCCCC));
		}


		[Test]
		public void JSR_Stack_Pointer_Correct()
		{
			var processor = new Processor();

			processor.LoadProgram(0xBBAA, new byte[] { 0x20, 0xCC, 0xCC }, 0xBBAA);

			var stackLocation = processor.StackPointer;
			processor.NextStep();


			Assert.That(processor.StackPointer, Is.EqualTo(stackLocation - 2));
		}
		#endregion

		#region LDA - Load Accumulator with Memory

		[Test]
		public void LDA_Accumulator_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[TestCase(0x0,true)]
		[TestCase(0x3, false)]
		public void LDA_Zero_Set_Correctly(byte valueToLoad, bool expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, valueToLoad }, 0x00);
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0x00, false)]
		[TestCase(0x79, false)]
		[TestCase(0x80, true)]
		[TestCase(0xFF, true)]
		public void LDA_Negative_Set_Correctly(byte valueToLoad, bool expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, valueToLoad }, 0x00);
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}

		#endregion

		#region LDX - Load X with Memory

		[Test]
		public void LDX_XRegister_Value_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.XRegister, Is.EqualTo(0x03));
		}

		[TestCase(0x00, false)]
		[TestCase(0x79, false)]
		[TestCase(0x80, true)]
		[TestCase(0xFF, true)]
		public void LDX_Negative_Flag_Set_Correctly(byte valueToLoad, bool expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, valueToLoad }, 0x00);
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0x0, true)]
		[TestCase(0x3, false)]
		public void LDX_Zero_Set_Correctly(byte valueToLoad, bool expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, valueToLoad }, 0x00);
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedValue));
		}

		#endregion

		#region LDY - Load Y with Memory

		[Test]
		public void STY_YRegister_Value_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.YRegister, Is.EqualTo(0x03));
		}

		[TestCase(0x00, false)]
		[TestCase(0x79, false)]
		[TestCase(0x80, true)]
		[TestCase(0xFF, true)]
		public void LDY_Negative_Flag_Set_Correctly(byte valueToLoad, bool expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, valueToLoad }, 0x00);
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0x0, true)]
		[TestCase(0x3, false)]
		public void LDY_Zero_Set_Correctly(byte valueToLoad, bool expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, valueToLoad }, 0x00);
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedValue));
		}

		#endregion

		#region LSR - Logical Shift Right

		[TestCase(0xFF, false, false)]
		[TestCase(0xFE, false, false)]
		[TestCase(0xFF, true, false)]
		[TestCase(0x00, true, false)]
		public void LSR_Negative_Set_Correctly(byte accumulatorValue, bool carryBitSet, bool expectedValue)
		{
			var processor = new Processor();

			var carryOperation = carryBitSet ? 0x38 : 0x18;

			processor.LoadProgram(0, new byte[] { (byte)carryOperation, 0xA9, accumulatorValue, 0x4A }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0x1, true)]
		[TestCase(0x2, false)]
		public void LSR_Zero_Set_Correctly(byte accumulatorValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x4A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x1, true)]
		[TestCase(0x2, false)]
		public void LSR_Carry_Flag_Set_Correctly(byte accumulatorValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x4A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x4A, 0xFF, 0x7F, 0x00)] // LSR Accumulator
		[TestCase(0x4A, 0xFD, 0x7E, 0x00)] // LSR Accumulator
		[TestCase(0x46, 0xFF, 0x7F, 0x01)] // LSR Zero Page
		[TestCase(0x56, 0xFF, 0x7F, 0x01)] // LSR Zero Page X
		[TestCase(0x4E, 0xFF, 0x7F, 0x01)] // LSR Absolute
		[TestCase(0x5E, 0xFF, 0x7F, 0x01)] // LSR Absolute X
		public void LSR_Correct_Value_Stored(byte operation, byte valueToShift, byte expectedValue, byte expectedLocation)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0, new byte[] { 0xA9, valueToShift, operation, expectedLocation }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(operation == 0x4A
				? processor.Accumulator
				: processor.Memory.ReadValue(expectedLocation),
						Is.EqualTo(expectedValue));
		}
		#endregion

		#region ORA - Bitwise OR Compare Memory with Accumulator

		[TestCase(0x00, 0x00, 0x00)]
		[TestCase(0xFF, 0xFF, 0xFF)]
		[TestCase(0x55, 0xAA, 0xFF)]
		[TestCase(0xAA, 0x55, 0xFF)]
		public void ORA_Accumulator_Correct(byte accumulatorValue, byte memoryValue, byte expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x09, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedResult));
		}

		[TestCase(0x00, 0x00, true)]
		[TestCase(0xFF, 0xFF, false)]
		[TestCase(0x00, 0x01, false)]
		public void ORA_Zero_Flag_Correct(byte accumulatorValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x09, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x7F, 0x80, true)]
		[TestCase(0x79, 0x00, false)]
		[TestCase(0xFF, 0xFF, true)]
		public void ORA_Negative_Flag_Correct(byte accumulatorValue, byte memoryValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x09, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region PHA - Push Accumulator Onto Stack

		[Test]
		public void PHA_Stack_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, 0x03, 0x48 }, 0x00);

			var stackLocation = processor.StackPointer;
			
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.Memory.ReadValue(stackLocation + 0x100), Is.EqualTo(0x03));
		}
		
		[Test]
		public void PHA_Stack_Pointer_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, 0x03, 0x48 }, 0x00);

			var stackLocation = processor.StackPointer;
			processor.NextStep();
			processor.NextStep();

			//A Push will decrement the Pointer by 1
			Assert.That(processor.StackPointer, Is.EqualTo(stackLocation - 1));
		}

		[Test]
		public void PHA_Stack_Pointer_Has_Correct_Value_When_Wrapping()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x9A, 0x48 }, 0x00);
			processor.NextStep();
			processor.NextStep();


			Assert.That(processor.StackPointer, Is.EqualTo(0xFF));
		}
		#endregion

		#region PHP - Push Flags Onto Stack 
		[TestCase(0x038,0x31)] //SEC Carry Flag Test
		[TestCase(0x0F8,0x38)] //SED Decimal Flag Test
		[TestCase(0x078, 0x34)] //SEI Interrupt Flag Test
		public void PHP_Stack_Set_Flag_Operations_Correctly(byte operation, byte expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x58, operation, 0x08}, 0x00);

			var stackLocation = processor.StackPointer;
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.Memory.ReadValue(stackLocation + 0x100), Is.EqualTo(expectedValue));
		}

		[TestCase(0x01,0x80,0xB0)] //Negative
		[TestCase(0x01, 0x7F, 0xF0)] //Overflow + Negative
		[TestCase(0x00, 0x00, 0x32)] //Zero
		public void PHP_Stack_Non_Set_Flag_Operations_Correctly( byte accumulatorValue, byte memoryValue, byte expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x58, 0xA9, accumulatorValue, 0x69, memoryValue, 0x08 }, 0x00);

			var stackLocation = processor.StackPointer;
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.Memory.ReadValue(stackLocation + 0x100), Is.EqualTo(expectedValue));
		}

		[Test]
		public void PHP_Stack_Pointer_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x08 }, 0x00);

			var stackLocation = processor.StackPointer;
			processor.NextStep();

			//A Push will decrement the Pointer by 1
			Assert.That(processor.StackPointer, Is.EqualTo(stackLocation - 1));
		}

		#endregion

		#region PLA - Pull From Stack to Accumulator

		[Test]
		public void PLA_Accumulator_Has_Correct_Value()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Read From stack
			processor.LoadProgram(0, new byte[] { 0xA9, 0x03, 0x48, 0xA9, 0x00, 0x68 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[TestCase(0x00, true)]
		[TestCase(0x01, false)]
		[TestCase(0xFF, false)]
		public void PLA_Zero_Flag_Has_Correct_Value(byte valueToLoad, bool expectedResult)
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Read From stack
			processor.LoadProgram(0, new byte[] { 0xA9, valueToLoad, 0x48, 0x68 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();
			
			//Accounting for the Offest in memory
			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x7F, false)]
		[TestCase(0x80, true)]
		[TestCase(0xFF, true)]
		public void PLA_Negative_Flag_Has_Correct_Value(byte valueToLoad, bool expectedResult)
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Read From stack
			processor.LoadProgram(0, new byte[] { 0xA9, valueToLoad, 0x48, 0x68 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		#endregion

		#region PLP - Pull From Stack to Flags

		[Test]
		public void PLP_Carry_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Read From stack
			processor.LoadProgram(0, new byte[] { 0xA9, 0x01, 0x48, 0x28 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.CarryFlag, Is.EqualTo(true));	
		}

		[Test]
		public void PLP_Zero_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Read From stack
			processor.LoadProgram(0, new byte[] { 0xA9, 0x02, 0x48, 0x28 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.ZeroFlag, Is.EqualTo(true));	
		}

		[Test]
		public void PLP_Decimal_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Read From stack
			processor.LoadProgram(0, new byte[] { 0xA9, 0x08, 0x48, 0x28 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.DecimalFlag, Is.EqualTo(true));
		}

		[Test]
		public void PLP_Interrupt_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Read From stack
			processor.LoadProgram(0, new byte[] { 0xA9, 0x04, 0x48, 0x28 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.DisableInterruptFlag, Is.EqualTo(true));
		}

		[Test]
		public void PLP_Overflow_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Read From stack
			processor.LoadProgram(0, new byte[] { 0xA9, 0x40, 0x48, 0x28 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.OverflowFlag, Is.EqualTo(true));
		}

		[Test]
		public void PLP_Negative_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Read From stack
			processor.LoadProgram(0, new byte[] { 0xA9, 0x80, 0x48, 0x28 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.NegativeFlag, Is.EqualTo(true));
		}

		#endregion

		#region ROL - Rotate Left

		[TestCase(0x40, true)]
		[TestCase(0x3F, false)]
		[TestCase(0x80, false)]
		public void ROL_Negative_Set_Correctly(byte accumulatorValue, bool expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x2A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(true, false)]
		[TestCase(false, true)]
		public void ROL_Zero_Set_Correctly(bool carryFlagSet, bool expectedResult)
		{
			var processor = new Processor();

			var carryOperation = carryFlagSet ? 0x38 : 0x18;

			processor.LoadProgram(0, new byte[] { (byte)carryOperation, 0x2A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x80, true)]
		[TestCase(0x7F, false)]
		public void ROL_Carry_Flag_Set_Correctly(byte accumulatorValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x2A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x2A, 0x55, 0xAA, 0x00)] // ROL Accumulator
		[TestCase(0x2A, 0x55, 0xAA, 0x00)] // ROL Accumulator
		[TestCase(0x26, 0x55, 0xAA, 0x01)] // ROL Zero Page
		[TestCase(0x36, 0x55, 0xAA, 0x01)] // ROL Zero Page X
		[TestCase(0x2E, 0x55, 0xAA, 0x01)] // ROL Absolute
		[TestCase(0x3E, 0x55, 0xAA, 0x01)] // ROL Absolute X
		public void ROL_Correct_Value_Stored(byte operation, byte valueToRotate, byte expectedValue, byte expectedLocation)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0, new byte[] { 0xA9, valueToRotate, operation, expectedLocation }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(operation == 0x2A
				? processor.Accumulator
				: processor.Memory.ReadValue(expectedLocation),
						Is.EqualTo(expectedValue));
		}

		#endregion

		#region ROR - Rotate Left

		[TestCase(0xFF, false, false)]
		[TestCase(0xFE, false, false)]
		[TestCase(0xFF, true, true)]
		[TestCase(0x00, true, true)]
		public void ROR_Negative_Set_Correctly(byte accumulatorValue, bool carryBitSet, bool expectedValue)
		{
			var processor = new Processor();

			var carryOperation = carryBitSet ? 0x38 : 0x18;

			processor.LoadProgram(0, new byte[] { (byte)carryOperation, 0xA9, accumulatorValue, 0x6A }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0x00, false, true)]
		[TestCase(0x00, true, false)]
		[TestCase(0x01, false, true)]
		[TestCase(0x01, true, false)]
		public void ROR_Zero_Set_Correctly(byte accumulatorValue, bool carryBitSet, bool expectedResult)
		{
			var processor = new Processor();

			var carryOperation = carryBitSet ? 0x38 : 0x18;

			processor.LoadProgram(0, new byte[] { (byte)carryOperation, 0xA9, accumulatorValue, 0x6A }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x01, true)]
		[TestCase(0x02, false)]
		public void ROR_Carry_Flag_Set_Correctly(byte accumulatorValue, bool expectedResult)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorValue, 0x6A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(expectedResult));
		}

		[TestCase(0x6A, 0xAA, 0x55, 0x00)] // ROR Accumulator
		[TestCase(0x6A, 0xAA, 0x55, 0x00)] // ROR Accumulator
		[TestCase(0x66, 0xAA, 0x55, 0x01)] // ROR Zero Page
		[TestCase(0x76, 0xAA, 0x55, 0x01)] // ROR Zero Page X
		[TestCase(0x6E, 0xAA, 0x55, 0x01)] // ROR Absolute
		[TestCase(0x7E, 0xAA, 0x55, 0x01)] // ROR Absolute X
		public void ROR_Correct_Value_Stored(byte operation, byte valueToRotate, byte expectedValue, byte expectedLocation)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0, new byte[] { 0xA9, valueToRotate, operation, expectedLocation }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(operation == 0x6A
				? processor.Accumulator
				: processor.Memory.ReadValue(expectedLocation),
						Is.EqualTo(expectedValue));
		}

		#endregion

		#region RTI - Return from Interrupt

		[Test]
		public void RTI_Program_Counter_Correct()
		{
			var processor = new Processor();

			processor.LoadProgram(0xABCD, new byte[] { 0x00 }, 0xABCD);
			//The Reset Vector Points to 0x0000 by default, so load the RTI instruction there.
			processor.Memory.WriteValue(0x00, 0x40);
			
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(0xABCF));
		}

		[Test]
		public void RTI_Carry_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Return from Interrupt
			processor.LoadProgram(0, new byte[] { 0xA9, 0x01, 0x48, 0x40 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.CarryFlag, Is.EqualTo(true));
		}

		[Test]
		public void RTI_Zero_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Return from Interrupt
			processor.LoadProgram(0, new byte[] { 0xA9, 0x02, 0x48, 0x40 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.ZeroFlag, Is.EqualTo(true));
		}

		[Test]
		public void RTI_Decimal_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Return from Interrupt
			processor.LoadProgram(0, new byte[] { 0xA9, 0x08, 0x48, 0x40 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.DecimalFlag, Is.EqualTo(true));
		}

		[Test]
		public void RTI_Interrupt_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Return from Interrupt
			processor.LoadProgram(0, new byte[] { 0xA9, 0x04, 0x48, 0x40 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.DisableInterruptFlag, Is.EqualTo(true));
		}

		[Test]
		public void RTI_Overflow_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Return from Interrupt
			processor.LoadProgram(0, new byte[] { 0xA9, 0x40, 0x48, 0x40 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.OverflowFlag, Is.EqualTo(true));
		}

		[Test]
		public void RTI_Negative_Flag_Set_Correctly()
		{
			var processor = new Processor();

			//Load Accumulator and Transfer to Stack, Clear Accumulator, and Return from Interrupt
			processor.LoadProgram(0, new byte[] { 0xA9, 0x80, 0x48, 0x40 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			//Accounting for the Offest in memory
			Assert.That(processor.NegativeFlag, Is.EqualTo(true));
		}
		#endregion

		#region RTS - Return from SubRoutine

		[Test]
		public void RTS_Program_Counter_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0x00, new byte[] { 0x20, 0x04, 0x00, 0x00, 0x60 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(0x03));
		}

		[Test]
		public void RTS_Stack_Pointer_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0xBBAA, new byte[] { 0x60}, 0xBBAA);

			var stackLocation = processor.StackPointer;
			processor.NextStep();


			Assert.That(processor.StackPointer, Is.EqualTo(stackLocation + 2));
		}
		#endregion

		#region SBC - Subtraction With Borrow

		[TestCase(0, 0, false, 0)]
		[TestCase(0, 1, false, 0xFF)]
		[TestCase(1, 1, false, 0)]
		[TestCase(0xFF, 0xFF, false, 0)]
		[TestCase(0, 0, true, 0xFF)]
		[TestCase(2, 1, true, 0)]
		[TestCase(255, 255, true, 255)]
		public void SBC_Accumulator_Correct_When_Not_In_BDC_Mode(byte accumlatorIntialValue, byte amountToSubtract, bool CarryFlagSet, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			if (CarryFlagSet)
			{
				processor.LoadProgram(0, new byte[] { 0x38, 0xA9, accumlatorIntialValue, 0xE9, amountToSubtract }, 0x00);
				processor.NextStep();
			}
			else
				processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0xE9, amountToSubtract }, 0x00);

			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0, 0, false, 0)]
		[TestCase(0, 1, false, 0x63)]
		[TestCase(1, 1, false, 0)]
		[TestCase(0, 0, true, 0x63)]
		[TestCase(2, 1, true, 0)]
		public void SBC_Accumulator_Correct_When_In_BDC_Mode(byte accumlatorIntialValue, byte amountToAdd,
																	   bool setCarryFlag, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			if (setCarryFlag)
			{
				processor.LoadProgram(0, new byte[] { 0x38, 0xF8, 0xA9, accumlatorIntialValue, 0xE9, amountToAdd }, 0x00);
				processor.NextStep();
			}
			else
				processor.LoadProgram(0, new byte[] { 0xF8, 0xA9, accumlatorIntialValue, 0xE9, amountToAdd }, 0x00);

			processor.NextStep();
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}


		[TestCase(0xFF, 1, false, true)]
		[TestCase(0xFF, 0, false, true)]
		[TestCase(0x80, 0, false, true)]
		[TestCase(0x80, 0, true, false)]
		[TestCase(0x81, 1, false, true)]
		[TestCase(0x81, 1, true, false)]
		[TestCase(0, 0x80, false, false)]
		[TestCase(0, 0x80, true, true)]
		[TestCase(1, 0x80, true, false)]
		[TestCase(1, 0x7F, false, false)]
		public void SBC_Overflow_Correct_When_Not_In_BDC_Mode(byte accumlatorIntialValue, byte amountToSubtact, bool setCarryFlag,
																	 bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			if (setCarryFlag)
			{
				processor.LoadProgram(0, new byte[] { 0x38, 0xA9, accumlatorIntialValue, 0xE9, amountToSubtact }, 0x00);
				processor.NextStep();
			}
			else
				processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0xE9, amountToSubtact }, 0x00);

			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.OverflowFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(99, 1, false, false)]
		[TestCase(99, 0, false, false)]
		[TestCase(0, 1, false, true)]
		[TestCase(1, 1, true, true)]
		[TestCase(2, 1, true, false)]
		[TestCase(1, 1, false, false)]
		public void SBC_Overflow_Correct_When_In_BDC_Mode(byte accumlatorIntialValue, byte amountToSubtract, bool setCarryFlag,
																	 bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			if (setCarryFlag)
			{
				processor.LoadProgram(0, new byte[] { 0x38, 0xF8, 0xA9, accumlatorIntialValue, 0xE9, amountToSubtract }, 0x00);
				processor.NextStep();
			}
			else
				processor.LoadProgram(0, new byte[] { 0xF8, 0xA9, accumlatorIntialValue, 0xE9, amountToSubtract }, 0x00);

			

			processor.NextStep();
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(accumlatorIntialValue));

			processor.NextStep();
			Assert.That(processor.OverflowFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0, 0, true)]
		[TestCase(0, 1, false)]
		[TestCase(1, 0, true)]
		[TestCase(2, 1, true)]
		public void SBC_Carry_Correct(byte accumlatorIntialValue, byte amountToSubtract, bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0xE9, amountToSubtract }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0, 0, true)]
		[TestCase(0, 1, false)]
		[TestCase(1, 0, false)]
		[TestCase(1, 1, true)]
		public void SBC_Zero_Correct(byte accumlatorIntialValue, byte amountToSubtract, bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0xE9, amountToSubtract }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0x80, 0x01, false)]
		[TestCase(0x81, 0x01, true)]
		[TestCase(0x00, 0x01, true)]
		[TestCase(0x01, 0x01, false)]
		public void SBC_Negative_Correct(byte accumlatorIntialValue, byte amountToSubtract, bool expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xA9, accumlatorIntialValue, 0xE9, amountToSubtract }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}
		#endregion

		#region SEC - Set Carry Flag

		[Test]
		public void SEC_Carry_Flag_Set_Correctly()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x38 }, 0x00);
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(true));
		}

		#endregion

		#region SED - Set Decimal Mode

		[Test]
		public void SED_Decimal_Mode_Set_Correctly()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xF8 }, 0x00);
			processor.NextStep();

			Assert.That(processor.DecimalFlag, Is.EqualTo(true));
		}

		#endregion

		#region SEI - Set Interrup Flag

		[Test]
		public void SEI_Interrupt_Flag_Set_Correctly()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x78 }, 0x00);
			processor.NextStep();

			Assert.That(processor.DisableInterruptFlag, Is.EqualTo(true));
		}

		#endregion

		#region STA - Store Accumulator In Memory

		[Test]
		public void STA_Memory_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA9, 0x03, 0x85, 0x05 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Memory.ReadValue(0x05), Is.EqualTo(0x03));
		}

		#endregion

		#region STX - Set Memory To X

		[Test]
		public void STX_Memory_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] {0xA2, 0x03, 0x86, 0x05  }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Memory.ReadValue(0x05), Is.EqualTo(0x03));
		}

		#endregion

		#region STY - Set Memory To Y

		[Test]
		public void STY_Memory_Has_Correct_Value()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, 0x03, 0x84, 0x05 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Memory.ReadValue(0x05), Is.EqualTo(0x03));
		}

		#endregion

		#region TAX, TAY, TSX, TSY Tests
		
		[TestCase(0xAA, RegisterMode.Accumulator, RegisterMode.XRegister)]
		[TestCase(0xA8, RegisterMode.Accumulator, RegisterMode.YRegister)]
		[TestCase(0x8A, RegisterMode.XRegister, RegisterMode.Accumulator)]
		[TestCase(0x98, RegisterMode.YRegister, RegisterMode.Accumulator)]
		public void Transfer_Correct_Value_Set(byte operation, RegisterMode transferFrom, RegisterMode transferTo)
		{
			var processor = new Processor();
			byte loadOperation;

			switch (transferFrom)
			{
				case RegisterMode.Accumulator:
					loadOperation = 0xA9;
					break;
				case RegisterMode.XRegister:
					loadOperation = 0xA2;
					break;
				default:
					loadOperation = 0xA0;
					break;
			}

			processor.LoadProgram(0, new[] { loadOperation, (byte)0x03, operation }, 0x00);
			processor.NextStep();
			processor.NextStep();


			switch (transferTo)
			{

				case RegisterMode.Accumulator:
					Assert.That(processor.Accumulator, Is.EqualTo(0x03));
					break;
				case RegisterMode.XRegister:
					Assert.That(processor.XRegister, Is.EqualTo(0x03));
					break;
				default:
					Assert.That(processor.YRegister, Is.EqualTo(0x03));
					break;
			}
		}

		[TestCase(0xAA, 0x80, RegisterMode.Accumulator, true)]
		[TestCase(0xA8, 0x80, RegisterMode.Accumulator, true)]
		[TestCase(0x8A, 0x80, RegisterMode.XRegister, true)]
		[TestCase(0x98, 0x80, RegisterMode.YRegister, true)]
		[TestCase(0xAA, 0xFF, RegisterMode.Accumulator, true)]
		[TestCase(0xA8, 0xFF, RegisterMode.Accumulator, true)]
		[TestCase(0x8A, 0xFF, RegisterMode.XRegister, true)]
		[TestCase(0x98, 0xFF, RegisterMode.YRegister, true)]
		[TestCase(0xAA, 0x7F, RegisterMode.Accumulator, false)]
		[TestCase(0xA8, 0x7F, RegisterMode.Accumulator, false)]
		[TestCase(0x8A, 0x7F, RegisterMode.XRegister, false)]
		[TestCase(0x98, 0x7F, RegisterMode.YRegister, false)]
		[TestCase(0xAA, 0x00, RegisterMode.Accumulator, false)]
		[TestCase(0xA8, 0x00, RegisterMode.Accumulator, false)]
		[TestCase(0x8A, 0x00, RegisterMode.XRegister, false)]
		[TestCase(0x98, 0x00, RegisterMode.YRegister, false)]
		public void Transfer_Negative_Value_Set(byte operation, byte value, RegisterMode transferFrom, bool expectedResult)
		{
			var processor = new Processor();
			byte loadOperation;

			switch (transferFrom)
			{
				case RegisterMode.Accumulator:
					loadOperation = 0xA9;
					break;
				case RegisterMode.XRegister:
					loadOperation = 0xA2;
					break;
				default:
					loadOperation = 0xA0;
					break;
			}

			processor.LoadProgram(0, new[] { loadOperation, value, operation }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedResult));
		}
		
		[TestCase(0xAA, 0xFF, RegisterMode.Accumulator, false)]
		[TestCase(0xA8, 0xFF, RegisterMode.Accumulator, false)]
		[TestCase(0x8A, 0xFF, RegisterMode.XRegister, false)]
		[TestCase(0x98, 0xFF, RegisterMode.YRegister, false)]
		[TestCase(0xAA, 0x00, RegisterMode.Accumulator, true)]
		[TestCase(0xA8, 0x00, RegisterMode.Accumulator, true)]
		[TestCase(0x8A, 0x00, RegisterMode.XRegister, true)]
		[TestCase(0x98, 0x00, RegisterMode.YRegister, true)]
		public void Transfer_Zero_Value_Set(byte operation, byte value, RegisterMode transferFrom, bool expectedResult)
		{
			var processor = new Processor();
			byte loadOperation;

			switch (transferFrom)
			{
				case RegisterMode.Accumulator:
					loadOperation = 0xA9;
					break;
				case RegisterMode.XRegister:
					loadOperation = 0xA2;
					break;
				default:
					loadOperation = 0xA0;
					break;
			}

			processor.LoadProgram(0, new[] { loadOperation, value, operation }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedResult));
		}

		#endregion

		#region TSX - Transfer Stack Pointer to X Register

		[Test]
		public void TSX_XRegister_Set_Correctly()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xBA }, 0x00);

			var stackPointer = processor.StackPointer;
			processor.NextStep();

			Assert.That(processor.XRegister, Is.EqualTo(stackPointer));
		}
		
		[TestCase(0x00, false)]
		[TestCase(0x7F, false)]
		[TestCase(0x80, true)]
		[TestCase(0xFF, true)]
		public void TSX_Negative_Set_Correctly(byte valueToLoad, bool expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] {  0xA2, valueToLoad, 0x9A, 0xBA }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NegativeFlag, Is.EqualTo(expectedValue));
		}

		[TestCase(0x00, true)]
		[TestCase(0x01, false)]
		[TestCase(0xFF, false)]
		public void TSX_Zero_Set_Correctly(byte valueToLoad, bool expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, valueToLoad, 0x9A, 0xBA }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(expectedValue));
		}
		#endregion
		
		#region TXS - Transfer X Register to Stack Pointer

		[Test]
		public void TXS_Stack_Pointer_Set_Correctly()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA2, 0xAA, 0x9A }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.StackPointer, Is.EqualTo(0xAA));
		}
		#endregion

		#region Accumulator Address Tests
		[TestCase(0x69, 0x01, 0x01, 0x02)] // ADC
		[TestCase(0x29, 0x03, 0x03, 0x03)] // AND
		[TestCase(0xA9, 0x04, 0x03, 0x03)] // LDA
		[TestCase(0x49, 0x55, 0xAA, 0xFF)] // EOR
		[TestCase(0x09, 0x55, 0xAA, 0xFF)] // ORA
		[TestCase(0xE9, 0x03, 0x01, 0x02)] // SBC
		public void Immediate_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorInitialValue, operation, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x65, 0x01, 0x01, 0x02)] // ADC
		[TestCase(0x25, 0x03, 0x03, 0x03)] // AND
		[TestCase(0xA5, 0x04, 0x03, 0x03)] // LDA
		[TestCase(0x45, 0x55, 0xAA, 0xFF)] // EOR
		[TestCase(0x05, 0x55, 0xAA, 0xFF)] // ORA
		[TestCase(0xE5, 0x03, 0x01, 0x02)] // SBC
		public void ZeroPage_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorInitialValue, operation, 0x05, 0x00, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}
	
		[TestCase(0x75, 0x00, 0x03, 0x03)] // ADC
		[TestCase(0x35, 0x03, 0x03, 0x03)] // AND
		[TestCase(0xB5, 0x04, 0x03, 0x03)] // LDA
		[TestCase(0x55, 0x55, 0xAA, 0xFF)] // EOR
		[TestCase(0x15, 0x55, 0xAA, 0xFF)] // ORA
		[TestCase(0xF5, 0x03, 0x01, 0x02)] // SBC
		public void ZeroPageX_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			//Just remember that my value's for the STX and ADC were added to the end of the byte array. In a real program this would be invalid, as an opcode would be next and 0x03 would be somewhere else
			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorInitialValue, 0xA2, 0x01, operation, 0x06, 0x00, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x6D, 0x00, 0x03, 0x03)] // ADC
		[TestCase(0x2D, 0x03, 0x03, 0x03)] // AND
		[TestCase(0xAD, 0x04, 0x03, 0x03)] // LDA
		[TestCase(0x4D, 0x55, 0xAA, 0xFF)] // EOR
		[TestCase(0x0D, 0x55, 0xAA, 0xFF)] // ORA
		[TestCase(0xED, 0x03, 0x01, 0x02)] // SBC
		public void Absolute_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorInitialValue, operation, 0x06, 0x00, 0x00, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x7D, 0x01, 0x01, false, 0x02)] // ADC
		[TestCase(0x3D, 0x03, 0x03, false, 0x03)] // AND
		[TestCase(0xBD, 0x04, 0x03, false, 0x03)] // LDA
		[TestCase(0x5D, 0x55, 0xAA, false, 0xFF)]  // EOR
		[TestCase(0x1D, 0x55, 0xAA, false, 0xFF)] // ORA
		[TestCase(0xFD, 0x03, 0x01, false, 0x02)] // SBC
		[TestCase(0x7D, 0x01, 0x01, true, 0x02)] // ADC
		[TestCase(0x3D, 0x03, 0x03, true, 0x03)] // AND
		[TestCase(0xBD, 0x04, 0x03, true, 0x03)] // LDA
		[TestCase(0x5D, 0x55, 0xAA, true, 0xFF)]  // EOR
		[TestCase(0x1D, 0x55, 0xAA, true, 0xFF)] // ORA
		[TestCase(0xFD, 0x03, 0x01, true, 0x02)] // SBC
		public void AbsoluteX_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, bool addressWraps, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, addressWraps
									  ? new byte[] { 0xA9, accumulatorInitialValue, 0xA2, 0x09, operation, 0xff, 0xff, 0x00, valueToTest }
									  : new byte[] { 0xA9, accumulatorInitialValue, 0xA2, 0x01, operation, 0x07, 0x00, 0x00, valueToTest }, 0x00);

			processor.NextStep();
			processor.NextStep();
			processor.NextStep();
			
			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x79, 0x01, 0x01, false, 0x02)] // ADC
		[TestCase(0x39, 0x03, 0x03, false, 0x03)] // AND
		[TestCase(0xB9, 0x04, 0x03, false, 0x03)] // LDA
		[TestCase(0x59, 0x55, 0xAA, false, 0xFF)]  // EOR
		[TestCase(0x19, 0x55, 0xAA, false, 0xFF)] // ORA
		[TestCase(0xF9, 0x03, 0x01, false, 0x02)] // SBC
		[TestCase(0x79, 0x01, 0x01, true, 0x02)] // ADC
		[TestCase(0x39, 0x03, 0x03, true, 0x03)] // AND
		[TestCase(0xB9, 0x04, 0x03, true, 0x03)] // LDA
		[TestCase(0x59, 0x55, 0xAA, true, 0xFF)]  // EOR
		[TestCase(0x19, 0x55, 0xAA, true, 0xFF)] // ORA
		[TestCase(0xF9, 0x03, 0x01, true, 0x02)] // SBC
		public void AbsoluteY_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, bool addressWraps, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, addressWraps
									  ? new byte[] { 0xA9, accumulatorInitialValue, 0xA0, 0x09, operation, 0xff, 0xff, 0x00, valueToTest }
									  : new byte[] { 0xA9, accumulatorInitialValue, 0xA0, 0x01, operation, 0x07, 0x00, 0x00, valueToTest }, 0x00);

			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x61, 0x01, 0x01, false, 0x02)] // ADC
		[TestCase(0x21, 0x03, 0x03, false, 0x03)] // AND
		[TestCase(0xA1, 0x04, 0x03, false, 0x03)] // LDA
		[TestCase(0x41, 0x55, 0xAA, false, 0xFF)]  // EOR
		[TestCase(0x01, 0x55, 0xAA, false, 0xFF)] // ORA
		[TestCase(0xE1, 0x03, 0x01, false, 0x02)] // SBC
		[TestCase(0x61, 0x01, 0x01, true, 0x02)] // ADC
		[TestCase(0x21, 0x03, 0x03, true, 0x03)] // AND
		[TestCase(0xA1, 0x04, 0x03, true, 0x03)] // LDA
		[TestCase(0x41, 0x55, 0xAA, true, 0xFF)]  // EOR
		[TestCase(0x01, 0x55, 0xAA, true, 0xFF)] // ORA
		[TestCase(0xE1, 0x03, 0x01, true, 0x02)] // SBC
		public void Indexed_Indirect_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, bool addressWraps, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0,
			                      addressWraps
									  ? new byte[] { 0xA9, accumulatorInitialValue, 0xA6, 0x06, operation, 0xff, 0x08, 0x9, 0x00, valueToTest }
				                      : new byte[] { 0xA9, accumulatorInitialValue, 0xA6, 0x06, operation, 0x01, 0x06, 0x9, 0x00, valueToTest},
			                      0x00);

			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x71, 0x01, 0x01, false, 0x02)] // ADC
		[TestCase(0x31, 0x03, 0x03, false, 0x03)] // AND
		[TestCase(0xB1, 0x04, 0x03, false, 0x03)] // LDA
		[TestCase(0x51, 0x55, 0xAA, false, 0xFF)]  // EOR
		[TestCase(0x11, 0x55, 0xAA, false, 0xFF)] // ORA
		[TestCase(0xF1, 0x03, 0x01, false, 0x02)] // SBC
		[TestCase(0x71, 0x01, 0x01, true, 0x02)] // ADC
		[TestCase(0x31, 0x03, 0x03, true, 0x03)] // AND
		[TestCase(0xB1, 0x04, 0x03, true, 0x03)] // LDA
		[TestCase(0x51, 0x55, 0xAA, true, 0xFF)]  // EOR
		[TestCase(0x11, 0x55, 0xAA, true, 0xFF)] // ORA
		[TestCase(0xF1, 0x03, 0x01, true, 0x02)] // SBC
		public void Indirect_Indexed_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, bool addressWraps, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0,
								  addressWraps
									  ? new byte[] { 0xA9, accumulatorInitialValue, 0xA0, 0x0A, operation, 0x07, 0x00, 0xFF, 0xFF, valueToTest }
									  : new byte[] { 0xA9, accumulatorInitialValue, 0xA0, 0x01, operation, 0x07, 0x00, 0x08, 0x00, valueToTest },
								  0x00);

			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}
		#endregion

		#region Index Address Tests
		[TestCase(0xA6, 0x03, true)] // LDX Zero Page
		[TestCase(0xB6, 0x03, true)] // LDX Zero Page Y
		[TestCase(0xA4, 0x03, false)] // LDY Zero Page
		[TestCase(0xB4, 0x03, false)] // LDY Zero Page X
		public void ZeroPage_Mode_Index_Has_Correct_Result(byte operation, byte valueToLoad, bool testXRegister)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { operation, 0x03, 0x00, valueToLoad }, 0x00);
			processor.NextStep();

			Assert.That(testXRegister ? processor.XRegister : processor.YRegister, Is.EqualTo(valueToLoad));
		}

		
		[TestCase(0xB6, 0x03, true)] // LDX Zero Page Y
		[TestCase(0xB4, 0x03, false)] // LDY Zero Page X
		public void ZeroPage_Mode_Index_Has_Correct_Result_When_Wrapped(byte operation, byte valueToLoad, bool testXRegister)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { testXRegister ? (byte)0xA0 : (byte)0xA2, 0xFF, operation, 0x06, 0x00, valueToLoad }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(testXRegister ? processor.XRegister : processor.YRegister, Is.EqualTo(valueToLoad));
		}

		[TestCase(0xAE, 0x03, true)] // LDX Absolute
		[TestCase(0xAC, 0x03, false)] // LDY Absolute
		public void Absolute_Mode_Index_Has_Correct_Result(byte operation, byte valueToLoad, bool testXRegister)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { operation, 0x04, 0x00, 0x00, valueToLoad }, 0x00);
			processor.NextStep();
			

			Assert.That(testXRegister ? processor.XRegister : processor.YRegister, Is.EqualTo(valueToLoad));
		}
		#endregion 

		#region Compare Address Tests
		[TestCase(0xC9, 0xFF, 0x00, RegisterMode.Accumulator)] //CMP Immediate
		[TestCase(0xE0, 0xFF, 0x00, RegisterMode.XRegister)] //CPX Immediate
		[TestCase(0xC0, 0xFF, 0x00, RegisterMode.YRegister)] //CPY Immediate
		public void Immediate_Mode_Compare_Operation_Has_Correct_Result(byte operation, byte accumulatorValue, byte memoryValue, RegisterMode mode)
		{
			var processor = new Processor();
			byte loadOperation;

			switch (mode)
			{
				case RegisterMode.Accumulator:
					loadOperation = 0xA9;
					break;
				case RegisterMode.XRegister:
					loadOperation = 0xA2;
					break;
				default:
					loadOperation = 0xA0;
					break;
			}

			processor.LoadProgram(0, new [] { loadOperation, accumulatorValue, operation, memoryValue }, 0x00);
			
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(false));
			Assert.That(processor.NegativeFlag, Is.EqualTo(true));
			Assert.That(processor.CarryFlag, Is.EqualTo(true));
		}

		[TestCase(0xC5, 0xFF, 0x00, RegisterMode.Accumulator)] //CMP Zero Page
		[TestCase(0xD5, 0xFF, 0x00, RegisterMode.Accumulator)] //CMP Zero Page X
		[TestCase(0xE4, 0xFF, 0x00, RegisterMode.XRegister)] //CPX Zero Page
		[TestCase(0xC4, 0xFF, 0x00, RegisterMode.YRegister)] //CPY Zero Page
		public void ZeroPage_Modes_Compare_Operation_Has_Correct_Result(byte operation, byte accumulatorValue, byte memoryValue, RegisterMode mode)
		{
			var processor = new Processor();

			byte loadOperation;

			switch (mode)
			{
				case RegisterMode.Accumulator:
					loadOperation = 0xA9;
					break;
				case RegisterMode.XRegister:
					loadOperation = 0xA2;
					break;
				default:
					loadOperation = 0xA0;
					break;
			}

			processor.LoadProgram(0, new byte[] { loadOperation, accumulatorValue, operation, 0x04, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(false));
			Assert.That(processor.NegativeFlag, Is.EqualTo(true));
			Assert.That(processor.CarryFlag, Is.EqualTo(true));
		}

		[TestCase(0xCD, 0xFF, 0x00, RegisterMode.Accumulator)] //CMP Absolute
		[TestCase(0xDD, 0xFF, 0x00, RegisterMode.Accumulator)] //CMP Absolute X
		[TestCase(0xEC, 0xFF, 0x00, RegisterMode.XRegister)] //CPX Absolute
		[TestCase(0xCC, 0xFF, 0x00, RegisterMode.YRegister)] //CPY Absolute
		public void Absolute_Modes_Compare_Operation_Has_Correct_Result(byte operation, byte accumulatorValue, byte memoryValue, RegisterMode mode)
		{
			var processor = new Processor();

			byte loadOperation;

			switch (mode)
			{
				case RegisterMode.Accumulator:
					loadOperation = 0xA9;
					break;
				case RegisterMode.XRegister:
					loadOperation = 0xA2;
					break;
				default:
					loadOperation = 0xA0;
					break;
			}

			processor.LoadProgram(0, new byte[] { loadOperation, accumulatorValue, operation, 0x05, 0x00, memoryValue }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(false));
			Assert.That(processor.NegativeFlag, Is.EqualTo(true));
			Assert.That(processor.CarryFlag, Is.EqualTo(true));
		}

		[TestCase(0xC1, 0xFF, 0x00, true)] 
		[TestCase(0xC1, 0xFF, 0x00, false)] 
		public void Indexed_Indirect_Mode_CMP_Operation_Has_Correct_Result(byte operation, byte accumulatorValue, byte memoryValue, bool addressWraps)
		{
			var processor = new Processor();

			processor.LoadProgram(0,
									  addressWraps
										  ? new byte[] { 0xA9, accumulatorValue, 0xA6, 0x06, operation, 0xff, 0x08, 0x9, 0x00, memoryValue }
										  : new byte[] { 0xA9, accumulatorValue, 0xA6, 0x06, operation, 0x01, 0x06, 0x9, 0x00, memoryValue },
									  0x00);


			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(false));
			Assert.That(processor.NegativeFlag, Is.EqualTo(true));
			Assert.That(processor.CarryFlag, Is.EqualTo(true));
		}

		[TestCase(0xD1, 0xFF, 0x00, true)] 
		[TestCase(0xD1, 0xFF, 0x00, false)] 
		public void Indirect_Indexed_Mode_CMP_Operation_Has_Correct_Result(byte operation, byte accumulatorValue, byte memoryValue, bool addressWraps)
		{
			var processor = new Processor();

			processor.LoadProgram(0,
							  addressWraps
								  ? new byte[] { 0xA9, accumulatorValue, 0x84, 0x06, operation, 0x07, 0x0A, 0xFF, 0xFF, memoryValue }
								  : new byte[] { 0xA9, accumulatorValue, 0x84, 0x06, operation, 0x07, 0x01, 0x08, 0x00, memoryValue },
							  0x00);

			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.ZeroFlag, Is.EqualTo(false));
			Assert.That(processor.NegativeFlag, Is.EqualTo(true));
			Assert.That(processor.CarryFlag, Is.EqualTo(true));
		}
		#endregion

		#region Decrement/Increment Address Tests
		[TestCase(0xC6,0xFF, 0xFE)] //DEC Zero Page
		[TestCase(0xD6, 0xFF, 0xFE)] //DEC Zero Page X
		[TestCase(0xE6, 0xFF, 0x00)] //INC Zero Page
		[TestCase(0xF6, 0xFF, 0x00)] //INC Zero Page X
		public void Zero_Page_DEC_INC_Has_Correct_Result(byte operation, byte memoryValue, byte expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { operation, 0x02, memoryValue }, 0x00);
			processor.NextStep();

			Assert.That(processor.Memory.ReadValue(0x02), Is.EqualTo(expectedValue));
		}

		[TestCase(0xCE, 0xFF, 0xFE)] //DEC Zero Page
		[TestCase(0xDE, 0xFF, 0xFE)] //DEC Zero Page X
		[TestCase(0xEE, 0xFF, 0x00)] //INC Zero Page
		[TestCase(0xFE, 0xFF, 0x00)] //INC Zero Page X
		public void Absolute_DEC_INC_Has_Correct_Result(byte operation, byte memoryValue, byte expectedValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { operation, 0x03, 0x00, memoryValue }, 0x00);
			processor.NextStep();

			Assert.That(processor.Memory.ReadValue(0x03), Is.EqualTo(expectedValue));
		}
		#endregion

		#region Store In Memory Address Tests

		[TestCase(0x85, RegisterMode.Accumulator)] // STA Zero Page
		[TestCase(0x95, RegisterMode.Accumulator)] // STA Zero Page X
		[TestCase(0x86, RegisterMode.XRegister)] // STX Zero Page
		[TestCase(0x96, RegisterMode.XRegister)] // STX Zero Page Y
		[TestCase(0x84, RegisterMode.YRegister)] // STY Zero Page
		[TestCase(0x94, RegisterMode.YRegister)] // STY Zero Page X
		public void ZeroPage_Mode_Memory_Has_Correct_Result(byte operation, RegisterMode mode)
		{
			var processor = new Processor();

			byte loadOperation;
			switch (mode)
			{
				case RegisterMode.Accumulator:
					loadOperation = 0xA9;
					break;
				case RegisterMode.XRegister:
					loadOperation = 0xA2;
					break;
				default:
					loadOperation = 0xA0;
					break;
			}

			processor.LoadProgram(0, new byte[] { loadOperation, 0x04, operation, 0x00, 0x05 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Memory.ReadValue(0x04), Is.EqualTo(0x05));
		}

		[TestCase(0x8D, 0x03, RegisterMode.Accumulator)] // STA Absolute
		[TestCase(0x9D, 0x03, RegisterMode.Accumulator)] // STA Absolute X
		[TestCase(0x99, 0x03, RegisterMode.Accumulator)] // STA Absolute X
		[TestCase(0x8E, 0x03, RegisterMode.XRegister)] // STX Zero Page
		[TestCase(0x8C, 0x03, RegisterMode.YRegister)] // STY Zero Page
		public void Absolute_Mode_Memory_Has_Correct_Result(byte operation, byte valueToLoad, RegisterMode mode)
		{
			var processor = new Processor();

			byte loadOperation;
			switch (mode)
			{
				case RegisterMode.Accumulator:
					loadOperation = 0xA9;
					break;
				case RegisterMode.XRegister:
					loadOperation = 0xA2;
					break;
				default:
					loadOperation = 0xA0;
					break;
			}

			processor.LoadProgram(0, new byte[] { loadOperation, valueToLoad, operation, 0x04 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Memory.ReadValue(0x04), Is.EqualTo(valueToLoad));
		}

		#endregion

		#region Cycle Tests
		[TestCase(0x69, 2)] // ADC Immediate
		[TestCase(0x65, 3)] // ADC Zero Page
		[TestCase(0x75, 4)] // ADC Zero Page X
		[TestCase(0x6D, 4)] // ADC Absolute
		[TestCase(0x7D, 4)] // ADC Absolute X
		[TestCase(0x79, 4)] // ADC Absolute Y
		[TestCase(0x61, 6)] // ADC Indrect X
		[TestCase(0x71, 5)] // ADC Indirect Y
		[TestCase(0x29, 2)] // AND Immediate
		[TestCase(0x25, 2)] // AND Zero Page
		[TestCase(0x35, 3)] // AND Zero Page X
		[TestCase(0x2D, 4)] // AND Absolute
		[TestCase(0x3D, 4)] // AND Absolute X
		[TestCase(0x39, 4)] // AND Absolute Y
		[TestCase(0x21, 6)] // AND Indirect X
		[TestCase(0x31, 5)] // AND Indirect Y
		[TestCase(0x0A, 2)] // ASL Accumulator
		[TestCase(0x06, 5)] // ASL Zero Page
		[TestCase(0x16, 6)] // ASL Zero Page X
		[TestCase(0x0E, 6)] // ASL Absolute
		[TestCase(0x1E, 7)] // ASL Absolute X
		[TestCase(0x24, 3)] // BIT Zero Page
		[TestCase(0x2C, 4)] // BIT Absolute
		[TestCase(0x00, 7)] // BRK Implied
		[TestCase(0x18, 2)] // CLC Implied
		[TestCase(0xD8, 2)] // CLD Implied
		[TestCase(0x58, 2)] // CLI Implied
		[TestCase(0xB8, 2)] // CLV Implied
		[TestCase(0xC9, 2)] // CMP Immediate
		[TestCase(0xC5, 3)] // CMP ZeroPage
		[TestCase(0xD5, 4)] // CMP Zero Page X
		[TestCase(0xCD, 4)] // CMP Absolute
		[TestCase(0xDD, 4)] // CMP Absolute X
		[TestCase(0xD9, 4)] // CMP Absolute Y
		[TestCase(0xC1, 6)] // CMP Indirect X
		[TestCase(0xD1, 5)] // CMP Indirect Y
		[TestCase(0xE0, 2)] // CPX Immediate
		[TestCase(0xE4, 3)] // CPX ZeroPage
		[TestCase(0xEC, 4)] // CPX Absolute
		[TestCase(0xC0, 2)] // CPY Immediate
		[TestCase(0xC4, 3)] // CPY ZeroPage
		[TestCase(0xCC, 4)] // CPY Absolute
		[TestCase(0xC6, 5)] // DEC Zero Page
		[TestCase(0xD6, 6)] // DEC Zero Page X
		[TestCase(0xCE, 6)] // DEC Absolute
		[TestCase(0xDE, 7)] // DEC Absolute X
		[TestCase(0xCA, 2)] // DEX Implied
		[TestCase(0x88, 2)] // DEY Implied
		[TestCase(0x49, 2)] // EOR Immediate
		[TestCase(0x45, 3)] // EOR Zero Page
		[TestCase(0x55, 4)] // EOR Zero Page X
		[TestCase(0x4D, 4)] // EOR Absolute
		[TestCase(0x5D, 4)] // EOR Absolute X
		[TestCase(0x59, 4)] // EOR Absolute Y
		[TestCase(0x41, 6)] // EOR Indrect X
		[TestCase(0x51, 5)] // EOR Indirect Y
		[TestCase(0xE6, 5)] // INC Zero Page
		[TestCase(0xF6, 6)] // INC Zero Page X
		[TestCase(0xE8, 2)] // INX Implied
		[TestCase(0xC8, 2)] // INY Implied
		[TestCase(0xEE, 6)] // INC Absolute
		[TestCase(0xFE, 7)] // INC Absolute X
		[TestCase(0x4C, 3)] // JMP Absolute
		[TestCase(0x6C, 5)] // JMP Indirect
		[TestCase(0x20, 6)] // JSR Absolute
		[TestCase(0xA9, 2)] // LDA Immediate
		[TestCase(0xA5, 3)] // LDA Zero Page
		[TestCase(0xB5, 4)] // LDA Zero Page X
		[TestCase(0xAD, 4)] // LDA Absolute
		[TestCase(0xBD, 4)] // LDA Absolute X
		[TestCase(0xB9, 4)] // LDA Absolute Y
		[TestCase(0xA1, 6)] // LDA Indirect X
		[TestCase(0xB1, 5)] // LDA Indirect Y
		[TestCase(0xA2, 2)] // LDX Immediate
		[TestCase(0xA6, 3)] // LDX Zero Page
		[TestCase(0xB6, 4)] // LDX Zero Page Y
		[TestCase(0xAE, 4)] // LDX Absolute
		[TestCase(0xBE, 4)] // LDX Absolute Y
		[TestCase(0xA0, 2)] // LDY Immediate
		[TestCase(0xA4, 3)] // LDY Zero Page
		[TestCase(0xB4, 4)] // LDY Zero Page Y
		[TestCase(0xAC, 4)] // LDY Absolute
		[TestCase(0xBC, 4)] // LDY Absolute Y
		[TestCase(0x4A, 2)] // LSR Accumulator
		[TestCase(0x46, 5)] // LSR Zero Page
		[TestCase(0x56, 6)] // LSR Zero Page X
		[TestCase(0x4E, 6)] // LSR Absolute
		[TestCase(0x5E, 7)] // LSR Absolute X
		[TestCase(0xEA, 2)] // NOP Implied
		[TestCase(0x09, 2)] // ORA Immediate
		[TestCase(0x05, 2)] // ORA Zero Page
		[TestCase(0x15, 3)] // ORA Zero Page X
		[TestCase(0x0D, 4)] // ORA Absolute
		[TestCase(0x1D, 4)] // ORA Absolute X
		[TestCase(0x19, 4)] // ORA Absolute Y
		[TestCase(0x01, 6)] // ORA Indirect X
		[TestCase(0x11, 5)] // ORA Indirect Y
		[TestCase(0x48, 3)] // PHA Implied
		[TestCase(0x08, 3)] // PHP Implied
		[TestCase(0x68, 4)] // PLA Implied
		[TestCase(0x28, 4)] // PLP Implied
		[TestCase(0x2A, 2)] // ROL Accumulator
		[TestCase(0x26, 5)] // ROL Zero Page
		[TestCase(0x36, 6)] // ROL Zero Page X
		[TestCase(0x2E, 6)] // ROL Absolute
		[TestCase(0x3E, 7)] // ROL Absolute X
		[TestCase(0x6A, 2)] // ROR Accumulator
		[TestCase(0x66, 5)] // ROR Zero Page
		[TestCase(0x76, 6)] // ROR Zero Page X
		[TestCase(0x6E, 6)] // ROR Absolute
		[TestCase(0x7E, 7)] // ROR Absolute X
		[TestCase(0x40, 6)] // RTI Implied
		[TestCase(0x60, 6)] // RTS Implied
		[TestCase(0xE9, 2)] // SBC Immediate
		[TestCase(0xE5, 3)] // SBC Zero Page
		[TestCase(0xF5, 4)] // SBC Zero Page X
		[TestCase(0xED, 4)] // SBC Absolute
		[TestCase(0xFD, 4)] // SBC Absolute X
		[TestCase(0xF9, 4)] // SBC Absolute Y
		[TestCase(0xE1, 6)] // SBC Indrect X
		[TestCase(0xF1, 5)] // SBC Indirect Y
		[TestCase(0x38, 2)] // SEC Implied
		[TestCase(0xF8, 2)] // SED Implied
		[TestCase(0x78, 2)] // SEI Implied
		[TestCase(0x85, 3)] // STA ZeroPage
		[TestCase(0x95, 4)] // STA Zero Page X
		[TestCase(0x8D, 4)] // STA Absolute
		[TestCase(0x9D, 5)] // STA Absolute X
		[TestCase(0x99, 5)] // STA Absolute Y
		[TestCase(0x81, 6)] // STA Indirect X
		[TestCase(0x91, 6)] // STA Indirect Y
		[TestCase(0x86, 3)] // STX Zero Page
		[TestCase(0x96, 4)] // STX Zero Page Y
		[TestCase(0x8E, 4)] // STX Absolute
		[TestCase(0x84, 3)] // STY Zero Page
		[TestCase(0x94, 4)] // STY Zero Page X
		[TestCase(0x8C, 4)] // STY Absolute
		[TestCase(0xAA, 2)] // TAX Implied
		[TestCase(0xA8, 2)] // TAY Implied
		[TestCase(0xBA, 2)] // TSX Implied
		[TestCase(0x8A, 2)] // TXA Implied
		[TestCase(0x9A, 2)] // TXS Implied
		[TestCase(0x98, 2)] // TYA Implied
		public void NumberOfCyclesRemaining_Correct_After_Operations_That_Do_Not_Wrap(byte operation, int numberOfCyclesUsed)
		{
			var processor = new Processor();
			processor.LoadProgram(0, new byte[] { operation, 0x00 }, 0x00);
			
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0x07D, true, 5)] // ADC Absolute X
		[TestCase(0x079, false, 5)] // ADC Absolute Y
		[TestCase(0x03D, true, 5)] // AND Absolute X
		[TestCase(0x039, false, 5)] // AND Absolute Y
		[TestCase(0x1E, true, 7)] // ASL Absolute X
		[TestCase(0xDD, true, 5)] // CMP Absolute X
		[TestCase(0xD9, false, 5)] // CMP Absolute Y
		[TestCase(0xDE, true, 7)] // DEC Absolute X
		[TestCase(0x05D, true, 5)] // EOR Absolute X
		[TestCase(0x059, false, 5)] // EOR Absolute Y
		[TestCase(0xFE, true, 7)] // INC Absolute X
		[TestCase(0xBD, true, 5)] // LDA Absolute X
		[TestCase(0xB9, false, 5)] // LDA Absolute Y
		[TestCase(0xBE, false, 5)] // LDX Absolute Y
		[TestCase(0xBC, true, 5)] // LDY Absolute X
		[TestCase(0x5E, true, 7)] // LSR Absolute X
		[TestCase(0x1D, true, 5)] // ORA Absolute X
		[TestCase(0x19, false, 5)] // ORA Absolute Y
		[TestCase(0x3E, true, 7)] // ROL Absolute X
		[TestCase(0x7E, true,  7)] // ROR Absolute X
		[TestCase(0xFD, true, 5)] // SBC Absolute X
		[TestCase(0xF9, false, 5)] // SBC Absolute Y
		[TestCase(0x9D, true, 5)] // STA Absolute X
		[TestCase(0x99, true, 5)] // STA Absolute Y
		public void NumberOfCyclesRemaining_Correct_When_In_AbsoluteX_Or_AbsoluteY_And_Wrap(byte operation, bool isAbsoluteX, int numberOfCyclesUsed)
		{
			var processor = new Processor();

			processor.LoadProgram(0, isAbsoluteX
				                      ? new byte[] {0xA6, 0x06, operation, 0xff, 0xff, 0x00, 0x03}
				                      : new byte[] {0xA4, 0x06, operation, 0xff, 0xff, 0x00, 0x03}, 0x00);

			processor.NextStep();

			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0x071, 6)] // ADC Indirect Y
		[TestCase(0x031, 6)] // AND Indirect Y
		[TestCase(0xB1, 6)] // LDA Indirect Y
		[TestCase(0xD1, 6)] // CMP Indirect Y
		[TestCase(0x51, 6)] // EOR Indirect Y
		[TestCase(0x11, 6)] // ORA Indirect Y
		[TestCase(0xF1, 6)] // SBC Indirect Y
		[TestCase(0x91, 6)] // STA Indirect Y
		public void NumberOfCyclesRemaining_Correct_When_In_IndirectIndexed_And_Wrap(byte operation, int numberOfCyclesUsed)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xA0, 0x04, operation, 0x05, 0x08, 0xFF, 0xFF, 0x03 }, 0x00);
			processor.NextStep();
			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}
		
		[TestCase(0x90, 2 , true)] //BCC
		[TestCase(0x90, 3, false)] //BCC
		[TestCase(0xB0, 2, false)] //BCS
		[TestCase(0xB0, 3, true)]  //BCS
		public void NumberOfCyclesRemaining_Correct_When_Relative_And_Branch_On_Carry(byte operation, int numberOfCyclesUsed, bool isCarrySet )
		{
			var processor = new Processor();


			processor.LoadProgram(0, isCarrySet
				                         ? new byte[] {0x38, operation, 0x00}
				                         : new byte[] {0x18, operation, 0x00}, 0x00);
			processor.NextStep();


			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0x90, 4, false, true)]  //BCC
		[TestCase(0x90, 4, false, false)] //BCC
		[TestCase(0xB0, 4, true, true)]  //BCC
		[TestCase(0xB0, 4, true, false)] //BCC
		public void NumberOfCyclesRemaining_Correct_When_Relative_And_Branch_On_Carry_And_Wrap(byte operation, int numberOfCyclesUsed, bool isCarrySet,  bool wrapRight)
		{
			var processor = new Processor();

			var carryOperation = isCarrySet ? 0x38 : 0x18;
			var initialAddress = wrapRight ? 0xFFF0 : 0x00;
			var amountToMove = wrapRight ? 0x0F : 0x84;

			processor.LoadProgram(initialAddress, new byte[] { (byte)carryOperation, operation, (byte)amountToMove, 0x00 }, initialAddress);
			processor.NextStep();

			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0xF0, 3, true)]  //BEQ
		[TestCase(0xF0, 2, false)] //BEQ
		[TestCase(0xD0, 3, false)]  //BNE
		[TestCase(0xD0, 2, true)] //BNE
		public void NumberOfCyclesRemaining_Correct_When_Relative_And_Branch_On_Zero(byte operation, int numberOfCyclesUsed, bool isZeroSet)
		{
			var processor = new Processor();

			processor.LoadProgram(0, isZeroSet 
				? new byte[] {0xA9, 0x00, operation, 0x00} 
				: new byte[] {0xA9, 0x01, operation, 0x00}, 0x00);

			processor.NextStep();


			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0xF0, 4, true, true)]  //BEQ
		[TestCase(0xF0, 4, true, false)] //BEQ
		[TestCase(0xD0, 4, false, true)]  //BNE
		[TestCase(0xD0, 4, false, false)] //BNE
		public void NumberOfCyclesRemaining_Correct_When_Relative_And_Branch_On_Zero_And_Wrap(byte operation, int numberOfCyclesUsed, bool isZeroSet, bool wrapRight)
		{
			var processor = new Processor();

			var newAccumulatorValue = isZeroSet ? 0x00 : 0x01;
			var initialAddress = wrapRight ? 0xFFF0 : 0x00;
			var amountToMove = wrapRight ? 0x0D : 0x84;

			processor.LoadProgram(initialAddress, new byte[] { 0xA9, (byte)newAccumulatorValue, operation, (byte)amountToMove, 0x00 }, initialAddress);
			processor.NextStep();

			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0x30, 3, true)]  //BEQ
		[TestCase(0x30, 2, false)] //BEQ
		[TestCase(0x10, 3, false)]  //BNE
		[TestCase(0x10, 2, true)] //BNE
		public void NumberOfCyclesRemaining_Correct_When_Relative_And_Branch_On_Negative(byte operation, int numberOfCyclesUsed, bool isNegativeSet)
		{
			var processor = new Processor();

			processor.LoadProgram(0, isNegativeSet
				? new byte[] { 0xA9, 0x80, operation, 0x00 }
				: new byte[] { 0xA9, 0x79, operation, 0x00 }, 0x00);

			processor.NextStep();


			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0x30, 4, true, true)]  //BEQ
		[TestCase(0x30, 4, true, false)] //BEQ
		[TestCase(0x10, 4, false, true)]  //BNE
		[TestCase(0x10, 4, false, false)] //BNE
		public void NumberOfCyclesRemaining_Correct_When_Relative_And_Branch_On_Negative_And_Wrap(byte operation, int numberOfCyclesUsed, bool isNegativeSet, bool wrapRight)
		{
			var processor = new Processor();

			var newAccumulatorValue = isNegativeSet ? 0x80 : 0x79;
			var initialAddress = wrapRight ? 0xFFF0 : 0x00;
			var amountToMove = wrapRight ? 0x0D : 0x84;

			processor.LoadProgram(initialAddress, new byte[] { 0xA9, (byte)newAccumulatorValue, operation, (byte)amountToMove, 0x00 }, initialAddress);
			processor.NextStep();

			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0x50, 3, false)]  //BVC
		[TestCase(0x50, 2, true)] //BVC
		[TestCase(0x70, 3, true)]  //BVS
		[TestCase(0x70, 2, false)] //BVS
		public void NumberOfCyclesRemaining_Correct_When_Relative_And_Branch_On_Overflow(byte operation, int numberOfCyclesUsed, bool isOverflowSet)
		{
			var processor = new Processor();

			processor.LoadProgram(0, isOverflowSet
				? new byte[] { 0xA9, 0x01, 0x69, 0x7F, operation, 0x00 }
				: new byte[] { 0xA9, 0x01, 0x69, 0x01, operation, 0x00 }, 0x00);

			processor.NextStep();
			processor.NextStep();
			
			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0x50, 4, false, true)]  //BVC
		[TestCase(0x50, 4, false, false)] //BVC
		[TestCase(0x70, 4, true, true)]  //BVS
		[TestCase(0x70, 4, true, false)] //BVS
		public void NumberOfCyclesRemaining_Correct_When_Relative_And_Branch_On_Overflow_And_Wrap(byte operation, int numberOfCyclesUsed, bool isOverflowSet, bool wrapRight)
		{
			var processor = new Processor();

			var newAccumulatorValue = isOverflowSet ? 0x7F : 0x00;
			var initialAddress = wrapRight ? 0xFFF0 : 0x00;
			var amountToMove = wrapRight ? 0x0B : 0x86;

			processor.LoadProgram(initialAddress, new byte[] { 0xA9, (byte)newAccumulatorValue, 0x69, 0x01, operation, (byte)amountToMove, 0x00 }, initialAddress);
			processor.NextStep();
			processor.NextStep();

			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}
		#endregion

		#region Program Counter Tests
		[TestCase(0x69, 2)] // ADC Immediate
		[TestCase(0x65, 2)] // ADC ZeroPage
		[TestCase(0x75, 2)] // ADC Zero Page X
		[TestCase(0x6D, 3)] // ADC Absolute
		[TestCase(0x7D, 3)] // ADC Absolute X
		[TestCase(0x79, 3)] // ADC Absolute Y
		[TestCase(0x61, 2)] // ADC Indirect X
		[TestCase(0x71, 2)] // ADC Indirect Y
		[TestCase(0x29, 2)] // AND Immediate
		[TestCase(0x25, 2)] // AND Zero Page
		[TestCase(0x35, 2)] // AND Zero Page X
		[TestCase(0x2D, 3)] // AND Absolute
		[TestCase(0x3D, 3)] // AND Absolute X
		[TestCase(0x39, 3)] // AND Absolute Y
		[TestCase(0x21, 2)] // AND Indirect X
		[TestCase(0x31, 2)] // AND Indirect Y
		[TestCase(0x0A, 1)] // ASL Accumulator
		[TestCase(0x06, 2)] // ASL Zero Page
		[TestCase(0x16, 2)] // ASL Zero Page X
		[TestCase(0x0E, 3)] // ASL Absolute
		[TestCase(0x1E, 3)] // ASL Absolute X
		[TestCase(0x24, 2)] // BIT Zero Page
		[TestCase(0x2C, 3)] // BIT Absolute
		[TestCase(0x18, 1)] // CLC Implied
		[TestCase(0xD8, 1)] // CLD Implied
		[TestCase(0x58, 1)] // CLI Implied
		[TestCase(0xB8, 1)] // CLV Implied
		[TestCase(0xC9, 2)] // CMP Immediate
		[TestCase(0xC5, 2)] // CMP ZeroPage
		[TestCase(0xD5, 2)] // CMP Zero Page X
		[TestCase(0xCD, 3)] // CMP Absolute
		[TestCase(0xDD, 3)] // CMP Absolute X
		[TestCase(0xD9, 3)] // CMP Absolute Y
		[TestCase(0xC1, 2)] // CMP Indirect X
		[TestCase(0xD1, 2)] // CMP Indirect Y
		[TestCase(0xE0, 2)] // CPX Immediate
		[TestCase(0xE4, 2)] // CPX ZeroPage
		[TestCase(0xEC, 3)] // CPX Absolute
		[TestCase(0xC0, 2)] // CPY Immediate
		[TestCase(0xC4, 2)] // CPY ZeroPage
		[TestCase(0xCC, 3)] // CPY Absolute
		[TestCase(0xC6, 2)] // DEC Zero Page
		[TestCase(0xD6, 2)] // DEC Zero Page X
		[TestCase(0xCE, 3)] // DEC Absolute
		[TestCase(0xDE, 3)] // DEC Absolute X
		[TestCase(0xCA, 1)] // DEX Implied
		[TestCase(0x88, 1)] // DEY Implied
		[TestCase(0x49, 2)] // EOR Immediate
		[TestCase(0x45, 2)] // EOR ZeroPage
		[TestCase(0x55, 2)] // EOR Zero Page X
		[TestCase(0x4D, 3)] // EOR Absolute
		[TestCase(0x5D, 3)] // EOR Absolute X
		[TestCase(0x59, 3)] // EOR Absolute Y
		[TestCase(0x41, 2)] // EOR Indirect X
		[TestCase(0x51, 2)] // EOR Indirect Y
		[TestCase(0xE6, 2)] // INC Zero Page
		[TestCase(0xF6, 2)] // INC Zero Page X
		[TestCase(0xEE, 3)] // INC Absolute
		[TestCase(0xFE, 3)] // INC Absolute X
		[TestCase(0xE8, 1)] // INX Implied
		[TestCase(0xC8, 1)] // INY Implied
		[TestCase(0xA9, 2)] // LDA Immediate
		[TestCase(0xA5, 2)] // LDA Zero Page
		[TestCase(0xB5, 2)] // LDA Zero Page X
		[TestCase(0xAD, 3)] // LDA Absolute
		[TestCase(0xBD, 3)] // LDA Absolute X
		[TestCase(0xB9, 3)] // LDA Absolute Y
		[TestCase(0xA1, 2)] // LDA Indirect X
		[TestCase(0xB1, 2)] // LDA Indirect Y
		[TestCase(0xA2, 2)] // LDX Immediate
		[TestCase(0xA6, 2)] // LDX Zero Page
		[TestCase(0xB6, 2)] // LDX Zero Page Y
		[TestCase(0xAE, 3)] // LDX Absolute
		[TestCase(0xBE, 3)] // LDX Absolute Y
		[TestCase(0xA0, 2)] // LDY Immediate
		[TestCase(0xA4, 2)] // LDY Zero Page
		[TestCase(0xB4, 2)] // LDY Zero Page Y
		[TestCase(0xAC, 3)] // LDY Absolute
		[TestCase(0xBC, 3)] // LDY Absolute Y
		[TestCase(0x4A, 1)] // LSR Accumulator
		[TestCase(0x46, 2)] // LSR Zero Page
		[TestCase(0x56, 2)] // LSR Zero Page X
		[TestCase(0x4E, 3)] // LSR Absolute
		[TestCase(0x5E, 3)] // LSR Absolute X
		[TestCase(0xEA, 1)] // NOP Implied
		[TestCase(0x09, 2)] // ORA Immediate
		[TestCase(0x05, 2)] // ORA Zero Page
		[TestCase(0x15, 2)] // ORA Zero Page X
		[TestCase(0x0D, 3)] // ORA Absolute
		[TestCase(0x1D, 3)] // ORA Absolute X
		[TestCase(0x19, 3)] // ORA Absolute Y
		[TestCase(0x01, 2)] // ORA Indirect X
		[TestCase(0x11, 2)] // ORA Indirect Y
		[TestCase(0x48, 1)] // PHA Implied
		[TestCase(0x08, 1)] // PHP Implied
		[TestCase(0x68, 1)] // PLA Implied
		[TestCase(0x28, 1)] // PLP Implied
		[TestCase(0x2A, 1)] // ROL Accumulator
		[TestCase(0x26, 2)] // ROL Zero Page
		[TestCase(0x36, 2)] // ROL Zero Page X
		[TestCase(0x2E, 3)] // ROL Absolute
		[TestCase(0x3E, 3)] // ROL Absolute X
		[TestCase(0x6A, 1)] // ROR Accumulator
		[TestCase(0x66, 2)] // ROR Zero Page
		[TestCase(0x76, 2)] // ROR Zero Page X
		[TestCase(0x6E, 3)] // ROR Absolute
		[TestCase(0x7E, 3)] // ROR Absolute X
		[TestCase(0xE9, 2)] // SBC Immediate
		[TestCase(0xE5, 2)] // SBC Zero Page
		[TestCase(0xF5, 2)] // SBC Zero Page X
		[TestCase(0xED, 3)] // SBC Absolute
		[TestCase(0xFD, 3)] // SBC Absolute X
		[TestCase(0xF9, 3)] // SBC Absolute Y
		[TestCase(0xE1, 2)] // SBC Indrect X
		[TestCase(0xF1, 2)] // SBC Indirect Y
		[TestCase(0x38, 1)] // SEC Implied
		[TestCase(0xF8, 1)] // SED Implied
		[TestCase(0x78, 1)] // SEI Implied
		[TestCase(0x85, 2)] // STA ZeroPage
		[TestCase(0x95, 2)] // STA Zero Page X
		[TestCase(0x8D, 3)] // STA Absolute
		[TestCase(0x9D, 3)] // STA Absolute X
		[TestCase(0x99, 3)] // STA Absolute Y
		[TestCase(0x81, 2)] // STA Indirect X
		[TestCase(0x91, 2)] // STA Indirect Y
		[TestCase(0x86, 2)] // STX Zero Page
		[TestCase(0x96, 2)] // STX Zero Page Y
		[TestCase(0x8E, 3)] // STX Absolute
		[TestCase(0x84, 2)] // STY Zero Page
		[TestCase(0x94, 2)] // STY Zero Page X
		[TestCase(0x8C, 3)] // STY Absolute
		[TestCase(0xAA, 1)] // TAX Implied
		[TestCase(0xA8, 1)] // TAY Implied
		[TestCase(0xBA, 1)] // TSX Implied
		[TestCase(0x8A, 1)] // TXA Implied
		[TestCase(0x9A, 1)] // TXS Implied
		[TestCase(0x98, 1)] // TYA Implied
		public void Program_Counter_Correct(byte operation, int expectedProgramCounter)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));


			processor.LoadProgram(0, new byte[] { operation, 0x0}, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedProgramCounter));
		}

		[TestCase(0x90, true, 2)]  //BCC
		[TestCase(0xB0, false, 2)] //BCS
		public void Branch_On_Carry_Program_Counter_Correct_When_NoBranch_Occurs(byte operation, bool carrySet, byte expectedOutput)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0,
			                      carrySet
					                      ? new byte[] {0x38, operation, 0x48 }
				                      : new byte[] {0x18, operation, 0x48 }, 0x00);

			processor.NextStep();
			var currentProgramCounter = processor.ProgramCounter;
			
			processor.NextStep();
			Assert.That(processor.ProgramCounter, Is.EqualTo(currentProgramCounter + expectedOutput));

		}

		[TestCase(0xF0, false, 2)]  //BEQ
		[TestCase(0xD0, true, 2)]  //BNE
		public void Branch_On_Zero_Program_Counter_Correct_When_NoBranch_Occurs(byte operation, bool zeroSet, byte expectedOutput)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0,
								  zeroSet
										  ? new byte[] { 0xA9, 0x00, operation }
									  : new byte[] { 0xA9, 0x01, operation }, 0x00);

			processor.NextStep();
			var currentProgramCounter = processor.ProgramCounter;

			processor.NextStep();
			Assert.That(processor.ProgramCounter, Is.EqualTo(currentProgramCounter + expectedOutput));

		}

		[TestCase(0x30, false, 2)]  //BMI
		[TestCase(0x10, true, 2)]  //BPL
		public void Branch_On_Negative_Program_Counter_Correct_When_NoBranch_Occurs(byte operation, bool negativeSet, byte expectedOutput)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0,
								  negativeSet
										  ? new byte[] { 0xA9, 0x80, operation }
									  : new byte[] { 0xA9, 0x79, operation }, 0x00);

			processor.NextStep();
			var currentProgramCounter = processor.ProgramCounter;

			processor.NextStep();
			Assert.That(processor.ProgramCounter, Is.EqualTo(currentProgramCounter + expectedOutput));

		}

		[TestCase(0x50, true, 2)]  //BVC
		[TestCase(0x70, false, 2)]  //BVS
		public void Branch_On_Overflow_Program_Counter_Correct_When_NoBranch_Occurs(byte operation, bool overflowSet, byte expectedOutput)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0, overflowSet
				? new byte[] { 0xA9, 0x01, 0x69, 0x7F, operation, 0x00 }
				: new byte[] { 0xA9, 0x01, 0x69, 0x01, operation, 0x00 }, 0x00);

			processor.NextStep();
			processor.NextStep();
			var currentProgramCounter = processor.ProgramCounter;

			processor.NextStep();
			Assert.That(processor.ProgramCounter, Is.EqualTo(currentProgramCounter + expectedOutput));
		}

		[Test]
		public void Program_Counter_Wraps_Correctly()
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));

			processor.LoadProgram(0xFFFF, new byte[] {0x38}, 0xFFFF);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(0));
		}
		#endregion
	}
}