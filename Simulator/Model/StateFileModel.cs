using System;
using System.Collections.Generic;
using Proc = Processor.Processor;

namespace Simulator.Model
{
	/// <summary>
	/// Model that contains the required information needed to save the current state of the processor to disk
	/// </summary>
	[Serializable]
	public class StateFileModel
	{
		/// <summary>
		/// The Number of Cycles the Program has Ran so Far
		/// </summary>
		public int NumberOfCycles { get; set; }

		/// <summary>
		/// The Current Program Listing
		/// </summary>
		public string Listing { get; set; }

		/// <summary>
		/// The output of the program
		/// </summary>
		public IList<OutputLog> OutputLog { get; set; }

		/// <summary>
		/// The path that the state was loaded from.
		/// </summary>
		public string FilePath { get; set; }
		
		/// <summary>
		/// The Processor Object that is being saved
		/// </summary>
		public Proc Processor { get; set; }
		

		
	}
}
