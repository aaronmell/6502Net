namespace Processor
{
	/// <summary>
	/// 
	/// </summary>
	public enum StatusRegisters
	{
		/// <summary>
		/// this holds the carry out of the most significant
		/// bit in any arithmetic operation. In subtraction operations however, this
		/// flag is cleared - set to 0 - if a borrow is required, set to 1 - if no
		/// borrow is required. The carry flag is also used in shift and rotate
		/// logical operations.
		/// </summary>
		Carry = 0,
		/// <summary>
		/// this is set to 1 when any arithmetic or logical
		/// operation produces a zero result, and is set to 0 if the result is
		/// non-zero.
		/// </summary>
		Zero = 1,
		/// <summary>
		/// this is an interrupt enable/disable flag. If it is set,
		/// interrupts are disabled. If it is cleared, interrupts are enabled.
		///  </summary>
		Interrupt = 2,
		/// <summary>
		/// this is the decimal mode status flag. When set, and an Add withSign
		/// Carry or Subtract with Carry instruction is executed, the source values are
		/// treated as valid BCD (Binary Coded Decimal, eg. 0x00-0x99 = 0-99) numbers.
		/// The result generated is also a BCD number.
		/// </summary>
		DecimalMode = 3,
		/// <summary>
		/// this is set when a software interrupt (BRK instruction) is
		/// executed.
		/// </summary>
		Break = 4,
		/// <summary>
		/// Not used. Supposed to be logical 1 at all times.
		/// </summary>
		Unused = 5,
		/// <summary>
		/// when an arithmetic operation produces a result
		/// too large to be represented in a byte, V is set.
		/// </summary>
		Overflow = 6,
		/// <summary>
		/// this is set if the result of an operation is
		/// negative, cleared if positive.
		///  </summary>
		Sign = 7,
	}
}
