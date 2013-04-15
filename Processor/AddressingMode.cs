namespace Processor
{
	/// <summary>
	/// The addressing modes used by the 6502 Processor
	/// </summary>
	public enum AddressingMode
	{
		/// <summary>
		/// In this mode a full address is given to operation on IE: Memory byte[] { 0x60, 0x00, 0xFF } 
		/// would perform an ADC operation and Add the value at ADDRESS 0xFF00 to the accumulator.
		/// The address is always LSB first
		/// </summary>
		Absolute = 1,
		/// <summary>
		/// In this mode a full address is given to operation on IE: Memory byte[] { 0x7D, 0x00, 0xFF } The full value would then be added to the X Register.
		/// If the X register was 0x01 then the address would be 0xFF01. and the value stored there would have an ADC operation performed on it and the value would
		/// be added to the accumulator.
		/// </summary>
		AbsoluteX = 2,
		/// <summary>
		/// In this mode a full address is given to operation on IE: Memory byte[] { 0x79, 0x00, 0xFF } The full value would then be added to the Y Register.
		/// If the Y register was 0x01 then the address would be 0xFF01. and the value stored there would have an ADC operation performed on it and the value would
		/// be added to the accumulator
		/// </summary>
		AbsoluteY = 3,
		/// <summary>
		/// In this mode the instruction operates on the accumulator. No operands are needed. 
		/// </summary>
		Accumulator = 4,
		/// <summary>
		/// In this mode, the value to operate on immediately follows the instruction. IE: Memory byte[] { 0x69, 0x01 } 
		/// would perform an ADC operation and Add 0x01 directly to the accumulator
		/// </summary>
		Immediate = 5,
		/// <summary>
		/// No address is needed for this mode. EX: BRK (Break), CLC (Clear Carry Flag) etc
		/// </summary>
		Implied = 6,
		/// <summary>
		/// In this mode assume the following
		/// Memory = { 0x61, 0x02, 0x04, 0x00, 0x03 }
		/// RegisterX = 0x01
		/// 1. Take the sum of the X Register and the value after the opcode 0x01 + 0x01 = 0x02. 
		/// 2. Starting at position 0x02 get an address (0x04,0x00) = 0x0004
		/// 3. Perform the ADC operation and Add the value at 0x0005 to the accumulator
		/// Note: if the Zero Page address is greater than 0xff then roll over the value. IE 0x101 rolls over to 0x01
		/// </summary>
		IndirectX = 7,
		/// <summary>
		/// In this mode assume the following
		/// Memory = { 0x61, 0x02, 0x04, 0x00, 0x03 }
		/// RegisterY = 0x01
		/// 1. Starting at position 0x02 get an address (0x04,0x00) = 0x0004 
		/// 2. Take the sum of the Y Register and the absolute address 0x01+0x0004 = 0x0005
		/// 3. Perform the ADC operation and Add the value at 0x0005 to the accumulator
		/// Note: if the address is great that 0xffff then roll over IE: 0x10001 rolls over to 0x01
		/// </summary>
		IndirectY = 8,
		/// <summary>
		/// JMP is the only operation that uses this mode. In this mode an absolute address is specified that points to the location of the absolute address we want to jump to.
		/// </summary>
		Indirect = 9,
		/// <summary>
		/// This Mode Changes the PC. It allows the program to change the location of the PC by 127 in either direction.
		/// </summary>
		Relative = 10,
		/// <summary>
		/// In this mode, a zero page address of the value to operate on is specified. This mode can only operation on values between 0x0 and 0xFF, or those that sit on the zero page of memory. IE: Memory byte[] { 0x69, 0x02, 0x01 } 
		/// would perform an ADC operation and Add 0x01 directly to the Accumulator
		/// </summary>
		ZeroPage = 11,
		/// <summary>
		/// In this mode, a zero page address of the value to operate on is specified, however the value of the X register is added to the address IE: Memory byte[] { 0x86, 0x02, 0x01, 0x67, 0x04, 0x01 } 
		/// In this example we store a value of 0x01 into the X register, then we would perform an ADC operation using the addres of 0x04+0x01=0x05 and Add the result of 0x01 directly to the Accumulator
		/// </summary>
		ZeroPageX = 12,
		/// <summary>
		/// This works the same as ZeroPageX except it uses the Y register instead of the X register.
		/// </summary>
		ZeroPageY = 13,
	}
}
