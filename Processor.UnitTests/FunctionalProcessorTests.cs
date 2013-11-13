using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace Processor.UnitTests
{
	[TestFixture]
	public class FunctionalProcessorTests
	{
		public byte[] KdTestProgram;

		[Test]
// ReSharper disable InconsistentNaming
		public void Functional_Tests_Minus_Math()
// ReSharper restore InconsistentNaming
		{
			var processor = new Processor();
			processor.LoadProgram(0x400,KdTestProgram, 0x400);
			var numberOfCycles = 0;

			while (true)
			{
				processor.NextStep();
				numberOfCycles++;
			
				if (processor.ProgramCounter == 0x32AD)
					break;

				if (numberOfCycles > 50558)
					break;
			}

			Assert.That(processor.Accumulator == 0x29);
			Assert.That(numberOfCycles, Is.EqualTo(50558));
			Assert.That(processor.ProgramCounter == 0x32AD);
// ReSharper disable FunctionNeverReturns
		}
// ReSharper restore FunctionNeverReturns


		[Test]
		// ReSharper disable InconsistentNaming
		public void Functional_Tests_Minus_BCD_Math()
		// ReSharper restore InconsistentNaming
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();
			var processor = new Processor();
			processor.LoadProgram(0x400, KdTestProgram, 0x400);
			var numberOfCycles = 0;

			while (true)
			{
				processor.NextStep();
				numberOfCycles++;

				if (processor.ProgramCounter == 0x3312)
					break;

				if (numberOfCycles > 26235794)
					break;
			}
			stopWatch.Stop();
			Assert.That(processor.Accumulator == 0x2A);
			Assert.That(numberOfCycles, Is.EqualTo(26235794));
			Assert.That(processor.ProgramCounter == 0x3312);
			Debug.Print("Took '{0} seconds to finish",stopWatch.Elapsed.TotalSeconds);

			//DumpProcessor
			// ReSharper disable FunctionNeverReturns
		}
		// ReSharper restore FunctionNeverReturns

		[Test]
		// ReSharper disable InconsistentNaming
		public void Complete_Functional_Test()
		// ReSharper restore InconsistentNaming
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();
			var processor = new Processor();
			processor.LoadProgram(0x400, KdTestProgram, 0x400);
			var numberOfCycles = 0;

			while (true)
			{
				processor.NextStep();
				numberOfCycles++;

				if (processor.ProgramCounter == 0x33ba)
					break;

				if (numberOfCycles > 30037912)
					break;
			}
			stopWatch.Stop();
			Assert.That(processor.Accumulator == 0xf0);
			Assert.That(numberOfCycles, Is.EqualTo(30037912));
			Assert.That(processor.ProgramCounter == 0x33ba);
			// ReSharper disable FunctionNeverReturns
		}
		// ReSharper restore FunctionNeverReturns

		[SetUp]
		public void SetupPrograms()
		{
			KdTestProgram = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Functional Tests", "6502_functional_test.bin"));
		}
	}



	
}
