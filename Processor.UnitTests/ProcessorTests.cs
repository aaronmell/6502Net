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
		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			//This program will Set the Accumulator to 1 initiall before adding the value to Accumulate.
			processor.LoadProgram(0, new byte[] { 0x69, 0x01 }, 0x00);
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(0x01));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_Overflow()
		{
			var processor = new Processor();

			//This program will Set the Accumulator to 1 initiall before adding the value to Accumulate.
			processor.LoadProgram(0, new byte[] { 0x69, 0x01, 0x69,0xff }, 0x00 );
			processor.NextStep();
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));
		}

		[TestCase(0xfe, false)]
		[TestCase(0xff, true)]
		public void Overflow_Has_Correct_Value_After_ADC_Operation(byte valueToAccumulate, bool isOverflow)
		{
			var processor = new Processor();

			
			processor.LoadProgram(0, new byte[] {0x69, 0x01, 0x69, valueToAccumulate }, 0x00);
			processor.NextStep();
			processor.NextStep();
			
			Assert.That(processor.IsOverflow, Is.EqualTo(isOverflow));
		}

		[TestCase(0xfe, false)]
		[TestCase(0xff, true)]
		public void Carry_Has_Correct_Value_After_ADC_Operation(byte valueToAccumulate, bool isCarry)
		{
			var processor = new Processor();

		
			processor.LoadProgram(0, new byte[] {0x69, 0x01, 0x69, valueToAccumulate }, 0x00);
			processor.NextStep();
			processor.NextStep();
			
			Assert.That(processor.CarryFlag, Is.EqualTo(isCarry));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_With_Carry()
		{
			var processor = new Processor();

			
			//1 + 255 = 0 + Carry = 1
			processor.LoadProgram(0, new byte[] {0x69, 0x01, 0x69, 0xFF, 0x69, 0x00 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();
			
			Assert.That(processor.Accumulator, Is.EqualTo(0x01));
		}

		[TestCase(0x63, false)]
		[TestCase(0x64, true)]
		public void Carry_Has_Correct_Value_After_ADC_Operation_In_Decimal_Mode(byte valueToAccumulate, bool isCarry)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0xF8, 0x69, valueToAccumulate }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.CarryFlag, Is.EqualTo(isCarry));
		}

		[Test]
		public void ProgramCounter_Has_Correct_Value_After_ADC_Operation_When_In_Absolute_Mode()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x60, 0x03, 0x00, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(3));
		}

		[Test]
		public void ProgramCounter_Has_Correct_Value_After_ADC_Operation_When_In_Immediate_Mode()
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x69, 0x01 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(2));
		}

		[Test]
		public void ProgramCounter_Has_Correct_Value_After_ADC_Operation_When_In_ZeroPage_Mode()
		{
			var processor = new Processor();
			
			processor.LoadProgram(0, new byte[] { 0x65, 0x02, 0x01 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(2));
		}

		[Test]
		public void ProgramCounter_Has_Correct_Value_After_ADC_Operation_When_In_ZeroPageX_Mode()
		{
			var processor = new Processor();

			//Just remember that my value was added to the end of the byte array. In a real program this would be invalid, as an opcode would be next and 0x03 would be somewhere else
			processor.LoadProgram(0, new byte[] { 0x75, 0x02, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(2));
		}

		[Test]
		public void ProgramCounter_Has_Correct_Value_After_ADC_Operation_When_In_AbsoluteX_Mode()
		{
			var processor = new Processor();

			//Just remember that my value was added to the end of the byte array. In a real program this would be invalid, as an opcode would be next and 0x03 would be somewhere else
			processor.LoadProgram(0, new byte[] { 0x7D, 0x02, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(3));
		}

		[Test]
		public void ProgramCounter_Has_Correct_Value_After_ADC_Operation_When_In_AbsoluteY_Mode()
		{
			var processor = new Processor();

			//Just remember that my value was added to the end of the byte array. In a real program this would be invalid, as an opcode would be next and 0x03 would be somewhere else
			processor.LoadProgram(0, new byte[] { 0x79, 0x02, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(3));
		}

		[Test]
		public void ProgramCounter_Has_Correct_Value_After_ADC_Operation_When_In_IndexIndirect_Mode()
		{
			var processor = new Processor();

			//Just remember that my value was added to the end of the byte array. In a real program this would be invalid, as an opcode would be next and 0x03 would be somewhere else
			processor.LoadProgram(0, new byte[] { 0x61, 0x02, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(2));
		}

		[Test]
		public void ProgramCounter_Has_Correct_Value_After_ADC_Operation_When_In_IndirectIndex_Mode()
		{
			var processor = new Processor();

			//Just remember that my value was added to the end of the byte array. In a real program this would be invalid, as an opcode would be next and 0x03 would be somewhere else
			processor.LoadProgram(0, new byte[] { 0x71, 0x02, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(2));
		}

		[TestCase(0x69, 2)]
		[TestCase(0x65, 3)]
		[TestCase(0x75, 4)]
		[TestCase(0x60, 4)]
		[TestCase(0x7D, 4)]
		[TestCase(0x79, 4)]
		[TestCase(0x61, 6)]
		[TestCase(0x71, 5)]
		public void NumberOfCyclesRemaining_Has_Correct_Value_After_ADC_Ooerations_That_Do_Not_Wrap(byte operation, int numberOfCyclesUsed)
		{
			var processor = new Processor();
			var startingNumberOfCycles = processor.NumberofCyclesLeft;

			processor.LoadProgram(0, new byte[] { operation, 0x02, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}
		

		[TestCase(0x086,0x07d)]
		[TestCase(0x084,0x079)]
		public void NumberOfCyclesRemaining_Has_Correct_Value_After_ADC_Operations_When_In_AbsoluteX_Or_AbsoluteY_That_Wrap(byte setRegisterOperation, byte adcperation)
		{
			var processor = new Processor();
			var startingNumberOfCycles = processor.NumberofCyclesLeft;

			processor.LoadProgram(0, new byte[] { 0x84, 0x05, 0x79, 0xff, 0xff, 0x07, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - 9));
		}

		[Test]
		public void NumberOfCyclesRemaining_Has_Correct_Value_After_ADC_Operations_When_In_IndirectIndexed_That_Wrap()
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
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_Immediate_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			
			processor.LoadProgram(0, new byte[] { 0x69, 0x01 }, 0x00);
			processor.NextStep();
			Assert.That(processor.Accumulator, Is.EqualTo(0x01));
		}
		
		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_ZeroPage_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));
			
			processor.LoadProgram(0, new byte[] { 0x65, 0x02, 0x01 }, 0x00);
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x01));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_ZeroPageX_Mode()
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
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_Absolute_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x60, 0x03, 0x00, 0x03}, 0x00);
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_AbsoluteY_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x84, 0x05, 0x79, 0x05, 0x00, 0x01, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();
			
			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_AbsoluteY_Mode_When_Wrapping_Occurs()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x84, 0x05, 0x79, 0xff, 0xff, 0x07, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_AbsoluteX_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x86, 0x05, 0x7D, 0x05, 0x00, 0x01, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_AbsoluteX_Mode_When_Wrapping_Occurs()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x86, 0x05, 0x7D, 0xff, 0xff, 0x07, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_Indexed_Indirect_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x86, 0x04, 0x61, 0x01, 0x04, 0x7, 0x00, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_Indexed_Indirect_Mode_When_Wrapping_Occurs()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x86, 0x04, 0x61, 0xff, 0x06, 0x7, 0x00, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_Indirect_Indexed_Mode()
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0x84, 0x04, 0x71, 0x05, 0x01, 0x6, 0x00, 0x03 }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(0x03));
		}

		[Test]
		public void Accumulator_Has_Correct_Value_After_ADC_Operation_When_In_Indirect_Indexed_Mode_When_Wrapping_Occurs()
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
