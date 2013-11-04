using System;
using Processor;
using Proc = Processor.Processor;

namespace Simulator.Model
{
	/// <summary>
	/// Model that contains the required information needed to save the current state of the processor to disk
	/// </summary>
	[Serializable]
	public class SaveFileModel
	{
		#region Simulator Properties
		/// <summary>
		/// The Number of Cycles the Program has Ran so Far
		/// </summary>
		public int NumberOfCycles { get; set; }

		/// <summary>
		/// The Current Program Listing
		/// </summary>
		public string Listing { get; set; }
		
		#endregion

		#region Processor Properties
		/// <summary>
		/// The Processors Current Memory
		/// </summary>
		public byte[] MemoryDump { get; set; }

		/// <summary>
		/// The Current Stack Pointer
		/// </summary>
		public int StackPointer { get; set; }

		/// <summary>
		/// The Current Program Counter
		/// </summary>
		public int ProgramCounter { get; set; }

		/// <summary>
		/// The Current Accumulator Value
		/// </summary>
		public int Accumulator { get; set; }
		/// <summary>
		/// The Current XRegister Value
		/// </summary>
		public int XRegister { get; set; }
		/// <summary>
		/// The Current YRegister Value
		/// </summary>
		public int YRegister { get; set; }
		/// <summary>
		/// The Current OpCode /// </summary>
		public int CurrentOpCode { get; set; }
		/// <summary>
		/// The Current Disassembely 
		/// </summary>
		public Disassembly CurrentDisassembly { get; set; }
		/// <summary>
		/// The Current Interrupt Period
		/// </summary>
		public int InterruptPeriod { get; set; }
		/// <summary>
		/// The Current Number of Cycles Left
		/// </summary>
		public int NumberofCyclesLeft { get; set; }
		/// <summary>
		/// The Current Carry Flag Value
		/// </summary>
		public bool CarryFlag { get; set; }
		/// <summary>
		/// The Current ZeroFlag Value
		/// </summary>
		public bool ZeroFlag { get; set; }
		/// <summary>
		/// The Current DisableInterruptFlag Value
		/// </summary>
		public bool DisableInterruptFlag { get; set; }
		/// <summary>
		/// The Current Decimal Flag Value
		/// </summary>
		public bool DecimalFlag { get; set; }
		/// <summary>
		/// The Current Overflow Flag Value
		/// </summary>
		public bool OverflowFlag { get; set; }
		/// <summary>
		/// The Current Negative Flag Value
		/// </summary>
		public bool NegativeFlag { get; set; }
		#endregion

		
	}
}
