using System.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Processor.UnitTests
{
	[TestFixture]
	public class FunctionalProcessorTests
	{
		public byte[] KdTestProgram;

		public byte[] InterruptProgram;

        public byte[] CycleProgram;

        public List<TestData> CycleTestDataResults;
       

        /// <summary>
        /// Each Test Case in Klaus_Dormann's Functional Test Program. 
        /// See https://github.com/Klaus2m5/6502_65C02_functional_tests
        /// Note: Each test case also runs the tests before it. There wasn't a good way to just run each test case
        /// If a test is failing find the first test that fails. The tests are dumb, they do not catch error traps correctly.
        /// </summary>
        [TestCase(0x01, 0x0461)] // Load Data
        [TestCase(0x02, 0x05aa)] // BNE Relative Addressing Test
        [TestCase(0x03, 0x05f1)] // Partial test BNE & CMP, CPX, CPY immediate
        [TestCase(0x04, 0x0625)] // Testing stack operations PHA PHP PLA PLP
        [TestCase(0x05, 0x079f)] // Testing branch decisions BPL BMI BVC BVS BCC BCS BNE BEQ
        [TestCase(0x06, 0x089b)] //  Test PHA does not alter flags or accumulator but PLA does
        [TestCase(0x07, 0x08cf)] // Partial pretest EOR #
        [TestCase(0x08, 0x0919)] // PC modifying instructions except branches (NOP, JMP, JSR, RTS, BRK, RTI)
        [TestCase(0x09, 0x096f)] // Jump absolute
        [TestCase(0x0A, 0x09ab)] // Jump indirect
        [TestCase(0x0B, 0x09e2)] // Jump subroutine & return from subroutine
        [TestCase(0x0C, 0x0a14)] // Break and return from RTI
        [TestCase(0x0D, 0x0aba)] //  Test set and clear flags CLC CLI CLD CLV SEC SEI SED
        [TestCase(0x0E, 0x0d80)] // testing index register increment/decrement and transfer INX INY DEX DEY TAX TXA TAY TYA 
        [TestCase(0x0F, 0x0e49)] // TSX sets NZ - TXS does not
        [TestCase(0x10, 0x0f04)] //  Testing index register load & store LDY LDX STY STX all addressing modes LDX / STX - zp,y / abs,y
        [TestCase(0x11, 0x0f46)] // Indexed wraparound test (only zp should wrap)
        [TestCase(0x12, 0x0ffd)] // LDY / STY - zp,x / abs,x
        [TestCase(0x13, 0x103d)] // Indexed wraparound test (only zp should wrap)
        [TestCase(0x14, 0x1333)] // LDX / STX - zp / abs / #
        [TestCase(0x15, 0x162d)] // LDY / STY - zp / abs / #
        [TestCase(0x16, 0x16de)] // Testing load / store accumulator LDA / STA all addressing modes LDA / STA - zp,x / abs,x
        [TestCase(0x17, 0x17f9)] // LDA / STA - (zp),y / abs,y / (zp,x)
        [TestCase(0x18, 0x189c)] // Indexed wraparound test (only zp should wrap)
        [TestCase(0x19, 0x1b66)] // LDA / STA - zp / abs / #
        [TestCase(0x1A, 0x1cba)] // testing bit test & compares BIT CPX CPY CMP all addressing modes BIT - zp / abs
        [TestCase(0x1B, 0x1dc8)] // CPX - zp / abs / # 
        [TestCase(0x1C, 0x1ed6)] // CPY - zp / abs / # 
        [TestCase(0x1D, 0x22ba)] // CMP - zp / abs / # 
        [TestCase(0x1E, 0x23fe)] //Testing shifts - ASL LSR ROL ROR all addressing modes shifts - accumulator
        [TestCase(0x1F, 0x257e)] // Shifts - zeropage
        [TestCase(0x20, 0x2722)] // Shifts - absolute
        [TestCase(0x21, 0x28a2)] // Shifts - zp indexed
        [TestCase(0x22, 0x2a46)] // Shifts - abs indexed
        [TestCase(0x23, 0x2af0)] // testing memory increment/decrement - INC DEC all addressing modes zeropage
        [TestCase(0x24, 0x2baa)] // absolute memory
        [TestCase(0x25, 0x2c58)] // zeropage indexed
        [TestCase(0x26, 0x2d16)] // memory indexed
        [TestCase(0x27, 0x2f0e)] // Testing logical instructions - AND EOR ORA all addressing modes AND
        [TestCase(0x28, 0x3106)] // EOR
        [TestCase(0x29, 0x32ff)] // OR
        [TestCase(0x2A, 0x3364)] // full binary add/subtract test iterates through all combinations of operands and carry input uses increments/decrements to predict result & result flags
        [TestCase(0x2B, 0x3408)] // Binary Switch Test
        [TestCase(0xF0, 0x3463)] // decimal add/subtract test  *** WARNING - tests documented behavior only! ***   only valid BCD operands are tested, N V Z flags are ignored iterates through all valid combinations of operands and carry input uses increments/decrements to predict result & carry flag
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
				
				var interruptWatch = processor.ReadMemoryValue(0xbffc);
				
				//This is used to simulate the edge triggering of an NMI. If we didn't do this we would get stuck in a loop forever
				if (interruptWatch != previousInterruptWatchValue)
				{
					previousInterruptWatchValue = interruptWatch;

					if ((interruptWatch & 2) != 0)
						processor.TriggerNmi = true;
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

        [Test]
        public void Cycle_Test()
        {
            var processor = new Processor();
            processor.LoadProgram(0x000, CycleProgram, 0x00);
            var numberofLoops = 1;

            while (true)
            {
                if (numberofLoops == 249 )
                {

                }

                processor.NextStep();

                Assert.AreEqual(CycleTestDataResults[numberofLoops].ProgramCounter, processor.ProgramCounter,
                   string.Format("Step {0} PC: {1}", numberofLoops, processor.ProgramCounter));

                Assert.AreEqual(CycleTestDataResults[numberofLoops].CycleCount, processor.GetCycleCount(),
                   string.Format("Step {0} Cycles: {1}", numberofLoops, processor.GetCycleCount()));


                numberofLoops++;

                if (processor.ProgramCounter == 0x1266)
                    break;

                if (numberofLoops > 500)
                    Assert.Fail("Maximum Number of Cycles Exceeded");
            }

            Assert.AreEqual(1140, processor.GetCycleCount());
        }

        [SetUp]
		public void SetupPrograms()
		{
            const string EnvironmentVariable = "TestDataDirectory";
            string CycleTestDataResultsDir = Environment.GetEnvironmentVariable(EnvironmentVariable);

            if (string.IsNullOrWhiteSpace(CycleTestDataResultsDir))
            {
                CycleTestDataResultsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Functional Tests");
            }

            KdTestProgram = File.ReadAllBytes(Path.Combine(CycleTestDataResultsDir, "6502_functional_test.bin"));
			InterruptProgram = File.ReadAllBytes(Path.Combine(CycleTestDataResultsDir, "6502_interrupt_test.bin"));
            CycleProgram = File.ReadAllBytes(Path.Combine(CycleTestDataResultsDir, "6502_cycle_test.bin"));

            LoadCycleTestResults(CycleTestDataResultsDir, "cycle_test_data.csv");
        }

        private void LoadCycleTestResults(string folder, string filename)
        {
            var path = Path.Combine(folder, filename);

            var reader =
                new StreamReader(File.OpenRead(path));

            CycleTestDataResults = new List<TestData>();
            var lineNumber = 0;

            try
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    if (int.Parse(values[0]) % 2 != 0)
                    {
                        lineNumber++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(values[8]))
                    {
                        continue;
                    }

                    CycleTestDataResults.Add(new TestData
                    {
                        ProgramCounter = Int32.Parse(values[1], System.Globalization.NumberStyles.HexNumber),
                        Accumulator = Int32.Parse(values[2], System.Globalization.NumberStyles.HexNumber),
                        XRegister = Int32.Parse(values[3], System.Globalization.NumberStyles.HexNumber),
                        YRegister = Int32.Parse(values[4], System.Globalization.NumberStyles.HexNumber),
                        Flags = Int32.Parse(values[5], System.Globalization.NumberStyles.HexNumber),
                        StackPointer = Int32.Parse(values[6], System.Globalization.NumberStyles.HexNumber),
                        CycleCount = int.Parse(values[7]),
                    });

                    lineNumber++;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            
            
        }
    }
}
