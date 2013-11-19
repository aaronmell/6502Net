using System.IO;
using NUnit.Framework;

namespace Processor.UnitTests
{
	[TestFixture]
	public class FunctionalProcessorTests
	{
		public byte[] KdTestProgram;

		public byte[] InterruptProgram;

		/// <summary>
		/// Each Test Case in Klaus_Dormann's Functional Test Program. 
		/// See https://github.com/Klaus2m5/6502_65C02_functional_tests
		/// Note: Each test case also runs the tests before it. There wasn't a good way to just run each test case
		/// If a test is failing find the first test that fails. The tests are dumb, they do not catch error traps correctly.
		/// </summary>
		[TestCase(0x01, 0x0432)] // Load Data
		[TestCase(0x02, 0x055e)] // BNE Relative Addressing Test
		[TestCase(0x03, 0x05a5)] // Partial test BNE & CMP, CPX, CPY immediate
		[TestCase(0x04, 0x05d9)] // Testing stack operations PHA PHP PLA PLP
		[TestCase(0x05, 0x0753)] // Testing branch decisions BPL BMI BVC BVS BCC BCS BNE BEQ
		[TestCase(0x06, 0x084f)] //  Test PHA does not alter flags or accumulator but PLA does
		[TestCase(0x07, 0x0883)] // Partial pretest EOR #
		[TestCase(0x08, 0x08cd)] // PC modifying instructions except branches (NOP, JMP, JSR, RTS, BRK, RTI)
		[TestCase(0x09, 0x0923)] // Jump absolute
		[TestCase(0x0A, 0x095f)] // Jump indirect
		[TestCase(0x0B, 0x0996)] // Jump subroutine & return from subroutine
		[TestCase(0x0C, 0x09c8)] // Break and return from RTI
		[TestCase(0x0D, 0x0a6e)] //  Test set and clear flags CLC CLI CLD CLV SEC SEI SED
		[TestCase(0x0E, 0x0d34)] // testing index register increment/decrement and transfer INX INY DEX DEY TAX TXA TAY TYA 
		[TestCase(0x0F, 0x0dfd)] // TSX sets NZ - TXS does not
		[TestCase(0x10, 0x0eb8)] //  Testing index register load & store LDY LDX STY STX all addressing modes LDX / STX - zp,y / abs,y
		[TestCase(0x11, 0x0efa)] // Indexed wraparound test (only zp should wrap)
		[TestCase(0x12, 0x0fb1)] // LDY / STY - zp,x / abs,x
		[TestCase(0x13, 0x0ff1)] // Indexed wraparound test (only zp should wrap)
		[TestCase(0x14, 0x12e7)] // LDX / STX - zp / abs / #
		[TestCase(0x15, 0x15e1)] // LDY / STY - zp / abs / #
		[TestCase(0x16, 0x1692)] // Testing load / store accumulator LDA / STA all addressing modes LDA / STA - zp,x / abs,x
		[TestCase(0x17, 0x17ad)] // LDA / STA - (zp),y / abs,y / (zp,x)
		[TestCase(0x18, 0x1850)] // Indexed wraparound test (only zp should wrap)
		[TestCase(0x19, 0x1b1a)] // LDA / STA - zp / abs / #
		[TestCase(0x1A, 0x1c6e)] // testing bit test & compares BIT CPX CPY CMP all addressing modes BIT - zp / abs
		[TestCase(0x1B, 0x1d7c)] // CPX - zp / abs / # 
		[TestCase(0x1C, 0x1e8a)] // CPY - zp / abs / # 
		[TestCase(0x1D, 0x226e)] // CMP - zp / abs / # 
		[TestCase(0x1E, 0x23b2)] //Testing shifts - ASL LSR ROL ROR all addressing modes shifts - accumulator
		[TestCase(0x1F, 0x2532)] // Shifts - zeropage
		[TestCase(0x20, 0x26d6)] // Shifts - absolute
		[TestCase(0x21, 0x2856)] // Shifts - zp indexed
		[TestCase(0x22, 0x29fa)] // Shifts - abs indexed
		[TestCase(0x23, 0x2aa4)] // testing memory increment/decrement - INC DEC all addressing modes zeropage
		[TestCase(0x24, 0x2b5e)] // absolute memory
		[TestCase(0x25, 0x2c0c)] // zeropage indexed
		[TestCase(0x26, 0x2cca)] // memory indexed
		[TestCase(0x27, 0x2ec0)] // Testing logical instructions - AND EOR ORA all addressing modes AND
		[TestCase(0x28, 0x30b6)] // EOR
		[TestCase(0x29, 0x32ad)] // OR
		[TestCase(0x2A, 0x3312)] // full binary add/subtract test iterates through all combinations of operands and carry input uses increments/decrements to predict result & result flags
		[TestCase(0xF0, 0x33b7)] // decimal add/subtract test  *** WARNING - tests documented behavior only! ***   only valid BCD operands are tested, N V Z flags are ignored iterates through all valid combinations of operands and carry input uses increments/decrements to predict result & carry flag
		// ReSharper disable InconsistentNaming
		public void Klaus_Dorman_Functional_Test(int accumulator, int programCounter)
		// ReSharper restore InconsistentNaming
		{
			var processor = new Processor();
			processor.LoadProgram(0x400, KdTestProgram, 0x400);
			var numberOfCycles = 0;

			while (true)
			{
				processor.NextStep();
				numberOfCycles++;

				if (processor.ProgramCounter == programCounter)
					break;

				if (numberOfCycles > 40037912)
					Assert.Fail("Maximum Number of Cycles Exceeded");
			}

			Assert.That(processor.Accumulator, Is.EqualTo(accumulator));
			// ReSharper disable FunctionNeverReturns
		}
		// ReSharper restore FunctionNeverReturns



		/// <summary>
		/// Each Test Group in Klaus_Dormann's Interrupt Test Program. 
		/// See https://github.com/Klaus2m5/6502_65C02_functional_tests
		/// This tests that the IRQ BRK and NMI all function correctly.
		/// </summary>
		[TestCase(0x04f9)] // IRQ Tests
		[TestCase(0x05b7)] // BRK Tests
		[TestCase(0x068d)] // NMI Tests
		[TestCase(0x06ec)] // Disable Interrupt Tests
		public void Klaus_Dorman_Interrupt_Test(int programCounter)
		{
			var previousInterruptWatchValue = 0;
			//var previousInterruptDisableCleared = false;

			var processor = new Processor();
			processor.LoadProgram(0x400, InterruptProgram, 0x400);
			var numberOfCycles = 0;

			while (true)
			{
				
				var interruptWatch = processor.Memory.ReadValue(0xbffc);
				
				//This is used to simulate the edge triggering of an NMI. If we didn't do this we would get stuck in a loop forever
				if (interruptWatch != previousInterruptWatchValue)
				{
					previousInterruptWatchValue = interruptWatch;

					if ((interruptWatch & 2) != 0)
						processor.NonMaskableInterrupt();
				}
				
				if (!processor.DisableInterruptFlag && (interruptWatch & 1) != 0)
					processor.InterruptRequest();

				processor.NextStep();
				numberOfCycles++;

				if (processor.ProgramCounter == programCounter)
					break;

				if (numberOfCycles > 100000)
					Assert.Fail("Maximum Number of Cycles Exceeded");
			}
		}

		[SetUp]
		public void SetupPrograms()
		{
			KdTestProgram = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Functional Tests", "6502_functional_test.bin"));
			InterruptProgram = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Functional Tests", "6502_interrupt_test.bin"));
		}
	}
}
