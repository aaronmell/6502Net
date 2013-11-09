using System.Collections.Generic;

namespace Simulator.Model
{
	/// <summary>
	/// A Representation of a Breakpoint
	/// </summary>
	public class Breakpoint
	{
		/// <summary>
		/// Is the Breakpoint enabled or disabled
		/// </summary>
		public bool IsEnabled { get; set; }

		/// <summary>
		/// The Value of the Breakpoint
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// The Type of breakpoint being set
		/// </summary>
		public string Type { get; set; }

		public static List<string> AllTypes
		{
			get { return BreakpointType.AllTypes; }
		} 
	}
}
