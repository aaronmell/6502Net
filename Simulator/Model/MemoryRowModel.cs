namespace Simulator.Model
{
	/// <summary>
	/// A Model of a Single Page of memory
	/// </summary>
	public class MemoryRowModel
	{
		/// <summary>
		/// The offset of this row. Expressed in hex
		/// </summary>
		public string Offset { get; set; }
		/// <summary>
		/// The memory at the location offset + 00
		/// </summary>
		public string Location00 { get; set; }
		/// <summary>
		/// The memory at the location offset + 01
		/// </summary>
		public string Location01 { get; set; }
		/// <summary>
		/// The memory at the location offset + 02
		/// </summary>
		public string Location02 { get; set; }
		/// <summary>
		/// The memory at the location offset + 03
		/// </summary>
		public string Location03 { get; set; }
		/// <summary>
		/// The memory at the location offset + 04
		/// </summary>
		public string Location04 { get; set; }
		/// <summary>
		/// The memory at the location offset + 05
		/// </summary>
		public string Location05 { get; set; }
		/// <summary>
		/// The memory at the location offset + 06
		/// </summary>
		public string Location06 { get; set; }
		/// <summary>
		/// The memory at the location offset + 07
		/// </summary>
		public string Location07 { get; set; }
		/// <summary>
		/// The memory at the location offset + 08
		/// </summary>
		public string Location08 { get; set; }
		/// <summary>
		/// The memory at the location offset + 09
		/// </summary>
		public string Location09 { get; set; }
		/// <summary>
		/// The memory at the location offset + 0A
		/// </summary>
		public string Location0A { get; set; }
		/// <summary>
		/// The memory at the location offset + 0B
		/// </summary>
		public string Location0B { get; set; }
		/// <summary>
		/// The memory at the location offset + 0C
		/// </summary>
		public string Location0C { get; set; }
		/// <summary>
		/// The memory at the location offset + 0D
		/// </summary>
		public string Location0D { get; set; }
		/// <summary>
		/// The memory at the location offset + 0E
		/// </summary>
		public string Location0E { get; set; }
		/// <summary>
		/// The memory at the location offset + 0F
		/// </summary>
		public string Location0F { get; set; }
	}
}
