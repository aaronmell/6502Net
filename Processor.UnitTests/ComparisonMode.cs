namespace Processor.UnitTests
{
	/// <summary>
	/// An enum helper, used when testing addressing modes for comparison operations
	/// </summary>
	public enum ComparisonMode
	{
		/// <summary>
		/// CMP Operation
		/// </summary>
		Accumulator = 1,
		/// <summary>
		/// CPX Operation
		/// </summary>
		XRegister = 2,
		/// <summary>
		/// CPY Operation
		/// </summary>
		YRegister = 3
	}
}
