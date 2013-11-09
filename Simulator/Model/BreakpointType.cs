using System.Collections.Generic;

namespace Simulator.Model
{
	/// <summary>
	/// The Type of Breakpoint
	/// </summary>
	public static class BreakpointType
	{
		/// <summary>
		/// A Listing of all of the Current Types
		/// </summary>
		public static List<string> AllTypes = new List<string>
			{
				ProgramCounterType,
				NumberOfCycleType
			};

		/// <summary>
		/// The ProgamCounter Breakpoint Type
		/// </summary>
		public const string ProgramCounterType = "Program Counter";

		/// <summary>
		/// The CycleCount Breakpoint Type
		/// </summary>
		public const string NumberOfCycleType = "Number of Cycles";

	}
}
