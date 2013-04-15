namespace Processor
{
	/// <summary>
	/// Used to help simulating. This class contains the disassembly properties.
	/// </summary>
	public class Disassembly
	{
		/// <summary>
		/// The low Address
		/// </summary>
		public string LowAddress { get; set; }

		/// <summary>
		/// The High Address
		/// </summary>
		public string HighAddress { get; set; }

		/// <summary>
		/// The string representation of the OpCode
		/// </summary>
		public string OpCodeString { get; set; }

		/// <summary>
		/// The disassembly of the current step
		/// </summary>
		public string DisassemblyOutput { get; set; }
		
	}
}
