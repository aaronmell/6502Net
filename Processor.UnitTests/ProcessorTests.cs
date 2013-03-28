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
			Assert.That(processor.Zero, Is.False);
			Assert.That(processor.IsInterruptDisabled, Is.False);
			Assert.That(processor.IsInDecimalMode, Is.False);
			Assert.That(processor.IsSoftwareInterrupt, Is.False);
			Assert.That(processor.IsOverflow, Is.False);
			Assert.That(processor.Sign, Is.False);
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
			Assert.That(processor.Zero, Is.EqualTo(expectedValue));
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
			Assert.That(processor.Sign, Is.EqualTo(expectedValue));
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

		#region|Address Mode Tests
		[TestCase(0x69, 0x01, 0x01, 0x02)] // ADC
		[TestCase(0x29, 0x03, 0x03, 0x03)] // AND
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
		public void ZeroPage_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorInitialValue, operation, 0x04, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}
	
		[TestCase(0x75, 0x00, 0x03, 0x03)] // ADC
		[TestCase(0x35, 0x03, 0x03, 0x03)] // AND
		public void ZeroPageX_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			//Just remember that my value's for the STX and ADC were added to the end of the byte array. In a real program this would be invalid, as an opcode would be next and 0x03 would be somewhere else
			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorInitialValue, 0x86, 0x06, operation, 0x06, 0x01, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x60, 0x00, 0x03, 0x03)] // ADC
		[TestCase(0x2D, 0x03, 0x03, 0x03)] // AND
		public void Absolute_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, new byte[] { 0xA9, accumulatorInitialValue, operation, 0x05, 0x00, valueToTest }, 0x00);
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x7D, 0x01, 0x01, false, 0x02)] // ADC
		[TestCase(0x3D, 0x03, 0x03, false, 0x03)] // AND
		[TestCase(0x7D, 0x01, 0x01, true, 0x02)] // ADC
		[TestCase(0x3D, 0x03, 0x03, true, 0x03)] // AND
		public void AbsoluteX_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, bool addressWraps, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, addressWraps
				                      ? new byte[] {0xA9, accumulatorInitialValue, 0x86, 0x07, operation, 0xff, 0xff, 0x09, valueToTest}
				                      : new byte[] {0xA9, accumulatorInitialValue, 0x86, 0x07, operation, 0x07, 0x00, 0x01, valueToTest}, 0x00);

			processor.NextStep();
			processor.NextStep();
			processor.NextStep();
			
			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x79, 0x01, 0x01, false, 0x02)] // ADC
		[TestCase(0x39, 0x03, 0x03, false, 0x03)] // AND
		[TestCase(0x79, 0x01, 0x01, true, 0x02)] // ADC
		[TestCase(0x39, 0x03, 0x03, true, 0x03)] // AND
		public void AbsoluteY_Mode_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, bool addressWraps, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0, addressWraps
									  ? new byte[] { 0xA9, accumulatorInitialValue, 0x84, 0x07, operation, 0xff, 0xff, 0x09, valueToTest }
									  : new byte[] { 0xA9, accumulatorInitialValue, 0x84, 0x07, operation, 0x07, 0x00, 0x01, valueToTest }, 0x00);

			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x61, 0x01, 0x01, false, 0x02)] // ADC
		[TestCase(0x21, 0x03, 0x03, false, 0x03)] // AND
		[TestCase(0x61, 0x01, 0x01, true, 0x02)] // ADC
		[TestCase(0x21, 0x03, 0x03, true, 0x03)] // AND
		public void Indexed_Indirect_Mode_Accumulator_Correct_When_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, bool addressWraps, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0,
			                      addressWraps
									  ? new byte[] { 0xA9, accumulatorInitialValue, 0x86, 0x06, operation, 0xff, 0x08, 0x9, 0x00, valueToTest }
				                      : new byte[] { 0xA9, accumulatorInitialValue, 0x86, 0x06, operation, 0x01, 0x06, 0x9, 0x00, valueToTest},
			                      0x00);

			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}

		[TestCase(0x71, 0x01, 0x01, false, 0x02)] // ADC
		[TestCase(0x31, 0x03, 0x03, false, 0x03)] // AND
		[TestCase(0x71, 0x01, 0x01, true, 0x02)] // ADC
		[TestCase(0x31, 0x03, 0x03, true, 0x03)] // AND
		public void Indirect_Indexed_Mode_Accumulator_Correct_When_Accumulator_Has_Correct_Result(byte operation, byte accumulatorInitialValue, byte valueToTest, bool addressWraps, byte expectedValue)
		{
			var processor = new Processor();
			Assert.That(processor.Accumulator, Is.EqualTo(0x00));

			processor.LoadProgram(0,
								  addressWraps
									  ? new byte[] { 0xA9, accumulatorInitialValue, 0x84, 0x06, operation, 0x07, 0x0A, 0xFF, 0xFF, valueToTest }
									  : new byte[] { 0xA9, accumulatorInitialValue, 0x84, 0x06, operation, 0x07, 0x01, 0x08, 0x00, valueToTest },
								  0x00);

			processor.NextStep();
			processor.NextStep();
			processor.NextStep();

			Assert.That(processor.Accumulator, Is.EqualTo(expectedValue));
		}
		#endregion

		#region Cycle Tests
		[TestCase(0x69, 2)] // ADC Immediate
		[TestCase(0x65, 3)] // ADC Zero Page
		[TestCase(0x75, 4)] // ADC Zero Page X
		[TestCase(0x60, 4)] // ADC Absolute
		[TestCase(0x7D, 4)] // ADC Absolute X
		[TestCase(0x79, 4)] // ADC Absolute Y
		[TestCase(0x61, 6)] // ADC Indrect X
		[TestCase(0x71, 5)] // ADC Indirect Y
		[TestCase(0x29, 2)] // AND Immediate
		[TestCase(0x25, 3)] // AND Zero Page
		[TestCase(0x35, 4)] // AND Zero Page X
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
		public void NumberOfCyclesRemaining_Correct_After_Operations_That_Do_Not_Wrap(byte operation, int numberOfCyclesUsed)
		{
			var processor = new Processor();
			var startingNumberOfCycles = processor.NumberofCyclesLeft;

			processor.LoadProgram(0, new byte[] { operation, 0x02, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0x07d, true, 5)] // ADC Absolute X
		[TestCase(0x079, false, 5)] // ADC Absolute Y
		[TestCase(0x03d, true, 5)] // AND Absolute X
		[TestCase(0x039, false, 5)] // AND Absolute Y
		[TestCase(0x1E, true, 7)] // ASL Absolute X
		public void NumberOfCyclesRemaining_Correct_When_In_AbsoluteX_Or_AbsoluteY_And_Wrap(byte operation, bool isAbsoluteX, int numberOfCyclesUsed)
		{
			var processor = new Processor();

			processor.LoadProgram(0, isAbsoluteX
				                      ? new byte[] {0x86, 0x05, operation, 0xff, 0xff, 0x07, 0x03}
				                      : new byte[] {0x84, 0x05, operation, 0xff, 0xff, 0x07, 0x03}, 0x00);

			processor.NextStep();

			//Get the number of cycles after the register has been loaded, so we can isolate the operation under test
			var startingNumberOfCycles = processor.NumberofCyclesLeft;
			processor.NextStep();

			Assert.That(processor.NumberofCyclesLeft, Is.EqualTo(startingNumberOfCycles - numberOfCyclesUsed));
		}

		[TestCase(0x071, 6)] // ADC Indirect Y
		[TestCase(0x031, 6)] // AND Indirect Y
		public void NumberOfCyclesRemaining_Correct_When_In_IndirectIndexed_And_Wrap(byte operation, int numberOfCyclesUsed)
		{
			var processor = new Processor();

			processor.LoadProgram(0, new byte[] { 0x84, 0x04, 0x71, 0x05, 0x08, 0xFF, 0xFF, 0x03 }, 0x00);
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
		[TestCase(0x60, 3)] // ADC Absolute
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
		public void Program_Counter_Correct(byte operation, int expectedProgramCounter)
		{
			var processor = new Processor();
			Assert.That(processor.ProgramCounter, Is.EqualTo(0));


			processor.LoadProgram(0, new byte[] { operation, 0x02, 0x03 }, 0x00);
			processor.NextStep();

			Assert.That(processor.ProgramCounter, Is.EqualTo(expectedProgramCounter));
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

		[TestCase(127,false)]
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
			
			Assert.That(processor.Sign, Is.EqualTo(expectedValue));
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

			Assert.That(processor.Zero, Is.EqualTo(expectedValue));
		}
		#endregion
	}
}
