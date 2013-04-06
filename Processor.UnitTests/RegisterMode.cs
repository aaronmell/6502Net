namespace Processor.UnitTests
{
	/// <summary>
	/// An enum helper, used when testing addressing modes for Comparison and Store operations
	/// </summary>
	public enum RegisterMode
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
