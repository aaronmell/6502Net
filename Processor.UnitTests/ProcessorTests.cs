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
			Assert.That(processor.IsResultZero, Is.False);
			Assert.That(processor.IsInterruptDisabled, Is.False);
			Assert.That(processor.IsInDecimalMode, Is.False);
			Assert.That(processor.IsSoftwareInterrupt, Is.False);
			Assert.That(processor.IsOverflow, Is.False);
			Assert.That(processor.IsSignNegative, Is.False);
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
		public void Processor_Stack_Initialized_Correctly()
		{
			var processor = new Processor();
			Assert.That(processor.StackPointer, Is.EqualTo(0xFF));

			foreach (var value in processor.Stack)
			{
				Assert.That(value, Is.EqualTo(0x00));
			}
		}

		[Test]
		public void ProgramCounter_Correct_When_Program_Loaded()
		{
			var processor = new Processor();
			processor.LoadProgram(0, new byte[1], 0x01);
			Assert.That(processor.ProgramCounter, Is.EqualTo(0x01));
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
			Assert.That(processor.IsResultZero, Is.EqualTo(expectedValue));
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
			Assert.That(processor.IsSignNegative, Is.EqualTo(expectedValue));
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
			Assert.That(processor.IsOverflow, Is.EqualTo(expectedValue));
		}
		
		[TestCase(0x69, 2)] // Immediate
		[TestCase(0x65, 2)] // ZeroPage
		[TestCase(0x75, 2)] // Zero Page X
		[TestCase(0x60, 3)] // Absolute
		[TestCase(0x7D, 3)] // Absolute X
		[TestCase(0x79, 3)] // Absolute Y
		[TestCase(0x61, 2)] // Indirect X
		[TestCase(0x71, 2)] // Indirect Y
		public void ADC_Program_Counter_Correct_After_Operations(byte operation, int expectedProgramCounterValue)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { operation, 0x03, 0x00, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedProgramCounterValue));
		}

		[TestCase(0x69, 2)] // Immediate
		[TestCase(0x65, 3)] // Zero Page
		[TestCase(0x75, 4)] // Zero Page X
		[TestCase(0x60, 4)] // Absolute
		[TestCase(0x7D, 4)] // Absolute X
		[TestCase(0x79, 4)] // Absolute Y
		[TestCase(0x61, 6)] // Indrect X
		[TestCase(0x71, 5)] // Indirect Y
		public void ADC_NumberOfCyclesRemaining_Correct_After_Operations_That_Do_Not_Wrap(byte operation, int numberOfCyclesUsed)
		{
			var processor = new Processor();
			var startingNumberOfCycles = processor.NumberofCyclesLeft;

			processor.LoadProgram(0, new byte[] { operation, 0x02, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}
		
		[TestCase(0x086,0x07d)]
		[TestCase(0x084,0x079)]
		public void ADC_NumberOfCyclesRemaining_Correct_When_In_AbsoluteX_Or_AbsoluteY_And_Wrap(byte setRegisterOperation, byte adcOperation)
		{
			var processor = new Processor();
			var startingNumberOfCycles = processor.NumberofCyclesLeft;

			processor.LoadProgram(0, new byte[] { 0x84, 0x05, 0x79, 0xff, 0xff, 0x07, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - 9));
		}

		[Test]
		public void ADC_NumberOfCyclesRemaining_Correct_When_In_IndirectIndexed_And_Wrap()
		{
			var processor = new Processor();
			var startingNumberOfCycles = processor.NumberofCyclesLeft;

			processor.LoadProgram(0, new byte[] { 0x84, 0x04, 0x71, 0x05, 0x08, 0xFF, 0xFF, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - 10));
		}
		#endregion
		
		//These tests use the ADC op codes to fully test the addressing modes
		#region AddressingMode Tests
		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_Immediate_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			
			processor.LoadProgram(0, new byte[] { 0x69, 0x01 }, 0x00);
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(0x01));
		}
		
		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_ZeroPage_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));
			
			processor.LoadProgram(0, new byte[] { 0x65, 0x02, 0x01 }, 0x00);
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x01));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_ZeroPageX_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			//Just remember that my value's for the STX and ADC were added to the end of the byte array. In a real program this would be invalid, as an opcode would be next and 0x03 would be somewhere else
			processor.LoadProgram(0, new byte[] { 0x86, 0x04, 0x75, 0x04, 0x01, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_Absolute_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x60, 0x03, 0x00, 0x03}, 0x00);
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_AbsoluteY_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x84, 0x05, 0x79, 0x05, 0x00, 0x01, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			
			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_AbsoluteY_Mode_When_Wrapping_Occurs()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x84, 0x05, 0x79, 0xff, 0xff, 0x07, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_AbsoluteX_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x86, 0x05, 0x7D, 0x05, 0x00, 0x01, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_AbsoluteX_Mode_When_Wrapping_Occurs()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x86, 0x05, 0x7D, 0xff, 0xff, 0x07, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_Indexed_Indirect_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x86, 0x04, 0x61, 0x01, 0x04, 0x7, 0x00, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_Indexed_Indirect_Mode_When_Wrapping_Occurs()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x86, 0x04, 0x61, 0xff, 0x06, 0x7, 0x00, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_Indirect_Indexed_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x84, 0x04, 0x71, 0x05, 0x01, 0x6, 0x00, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Correct_After_ADC_Operation_When_In_Indirect_Indexed_Mode_When_Wrapping_Occurs()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x84, 0x04, 0x71, 0x05, 0x08, 0xFF, 0xFF, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}
		#endregion
	
	}
}
