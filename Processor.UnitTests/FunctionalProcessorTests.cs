using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace Processor.UnitTests
{
	[TestFixture]
	public class FunctionalProcessorTests
	{
		public byte[] _kdTestProgram;

		[Test]
		public void KD_Runs_Successfully()
		{
			var processor = new Processor();
			processor.LoadProgram(0x1000,_kdTestProgram, 0x1000);

			while (true)
			{
				var programCounter = processor.ProgramCounter.ToString("X");
				processor.NextStep();


				Debug.WriteLine("{0}  {9} {10}  A:{2} X:{11} Y:{12} N:{3} V:{4} D:{5} I:{6} Z:{7} C:{8} SP:{1} ", programCounter,
				                processor.StackPointer.ToString("X"), processor.Accumulator.ToString("X"), processor.NegativeFlag,
				                processor.OverflowFlag,
				                processor.DecimalFlag, processor.DisableInterruptFlag, processor.ZeroFlag, processor.CarryFlag,
				                processor.CurrentOpCode.ConvertOpCodeIntoString(),
				                processor.CurrentMemoryAddress.HasValue
					                ? processor.CurrentMemoryAddress.Value.ToString("X")
					                : string.Empty,
								processor.XRegister,
								processor.YRegister);
			}
		}


		[SetUp]
		public void Setup_Programs()
		{
			_kdTestProgram = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "KlausDormann_Test.bin"));
		}
	}



	
}
