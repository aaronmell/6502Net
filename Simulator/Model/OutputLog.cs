using Processor;

namespace Simulator.Model
{
	/// <summary>
	/// The OutputLog Model. Used by the outputlog grid to show a history of operations performed by the CPU
	/// </summary>
	public class OutputLog : Disassembly
	{
		public OutputLog(Disassembly disassembly)
		{
			DisassemblyOutput = disassembly.DisassemblyOutput;
			HighAddress = disassembly.HighAddress;
			LowAddress = disassembly.LowAddress;
			OpCodeString = disassembly.OpCodeString;
		}

		/// <summary>
		/// The Program Counter Value
		/// </summary>
		public string ProgramCounter { get; set; }
		/// <summary>
		/// The Current Ope Code
		/// </summary>
		public string CurrentOpCode { get; set; }
		/// <summary>
		/// The X Register
		/// </summary>
		public string XRegister { get; set; }
		/// <summary>
		/// The Y Register
		/// </summary>
		public string YRegister { get; set; }
		/// <summary>
		/// The Accummulator
		/// </summary>
		public string Accumulator { get; set; }
		/// <summary>
		/// The Stack Pointer
		/// </summary>
		public string StackPointer { get; set; }
		/// <summary>
		/// The number of cycles executed since the last load or reset
		/// </summary>
		public int NumberOfCycles { get; set; }
	}
}
