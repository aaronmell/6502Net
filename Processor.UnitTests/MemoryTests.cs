using System;
using NUnit.Framework;

namespace Processor.UnitTests
{
	[TestFixture]
	public class MemoryTests
	{
		[Test]
// ReSharper disable InconsistentNaming
		public void Ram_Initalizes_To_Correct_Values()
		{
			var ram = new Ram(0xffff);
			
			for (int i = 0; i < 0xffff; i++)
			{
				Assert.That(ram.ReadValue(i), Is.EqualTo(0x00));
			}
		}

		[Test]
		public void Ram_Writes_Correct_Values()
		{
			var ram = new Ram(0xffff);

			for (int i = 0; i < 0xffff; i++)
			{
				ram.WriteValue(i,0xff);
				Assert.That(ram.ReadValue(i), Is.EqualTo(0xff));
			}
		}

		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void Ram_Throws_Exception_If_Program_Is_Too_Large()
		{
			var ram = new Ram(0x01);
			ram.LoadProgram(0,new byte[0x02]);
		}

		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void Ram_Throws_Exception_If_Offset_Is_Too_Large()
		{
			var ram = new Ram(0x01);
			ram.LoadProgram(0x2, new byte[0x01]);
		}

		[Test]
		public void Ram_Loads_Program_Correctly()
		{
			var ram = new Ram(0x05);
			ram.LoadProgram(0x04,new byte[] { 0xab } );

			Assert.That(ram.ReadValue(0x04), Is.EqualTo(0xab));
		}
	}
}
