using System;
using System.ComponentModel;

namespace Processor
{
	/// <summary>
	/// An Implementation of a 6502 Processor
	/// </summary>
	public class Processor
	{
		private int _programCounter;

		//All of the properties here are public and read only to facilitate ease of debugging and testing.
		#region Properties
		/// <summary>
		/// The Accumulator
		/// </summary>
		public int Accumulator { get; private set; }
		/// <summary>
		/// The X Index Register
		/// </summary>
		public int XRegister { get; private set; }
		/// <summary>
		/// The Y Index Register this is used for 
		/// </summary>
		public int YRegister { get; private set; }
		/// <summary>
		/// The Current Op Code being executed by the system
		/// </summary>
		public int CurrentOpCode { get; private set; }
		/// <summary>
		/// Points to the Current Address of the instruction being executed by the system. 
		/// </summary>
		public int ProgramCounter
		{
			get { return _programCounter; } 
			private set
			{
				if (value > 0xFFFF)
					_programCounter = value - 0x10000;
				else if (value < 0)
					_programCounter = value + 0x10000;
				else
					_programCounter = value;
			}
		}
		/// <summary>
		/// Points to the Current Position of the Stack
		/// </summary>
		public int StackPointer { get; private set; }
		/// <summary>
		/// The number of cycles before the next interrupt
		/// </summary>
		public int InterruptPeriod { get; private set; }
		/// <summary>
		/// The number of cycles left before the next interrupt.
		/// </summary>
		public int NumberofCyclesLeft { get; private set; }
		/// <summary>
		/// The Memory
		/// </summary>
		public Ram Memory { get; private set; }
		//Status Registers
		/// <summary>
		/// This is the carry flag. when adding, if there is a carry, then this bit is enabled. 
		/// In subtraction this is reversed and set to false if a borrow is required.
		/// </summary>
		public bool CarryFlag { get; private set; }
		/// <summary>
		/// Is true if the operation produced a zero result
		/// </summary>
		public bool ZeroFlag { get; private set; }
		/// <summary>
		/// Interrupts are disabled if this is true
		/// </summary>
		public bool InterruptFlag { get; private set; }
		/// <summary>
		/// If true, the CPU is in decimal mode.
		/// </summary>
		public bool Decimal { get; private set; }
		/// <summary>
		/// It true when a BRK instruction is executed
		/// </summary>
		public bool IsSoftwareInterrupt { get; private set; }
		/// <summary>
		/// This property is set when an overflow occurs. An overflow happens if the high bit(7) changes during the operation. Remember that values from 128-256 are negative values
		/// as the high bit is set to 1.
		/// Examples:
		/// 64 + 64 = -128 
		/// -128 + -128 = 0
		/// </summary>
		public bool OverflowFlag { get; private set; }
		/// <summary>
		/// Set to true if the result of an operation is negative in ADC and SBC operations. 
		/// In shift operations the sign holds the carry.
		/// </summary>
		public bool NegativeFlag { get; private set; }
		#endregion

		#region Public Methods
		/// <summary>
		/// Default Constructor, Instantiates a new instance of the processor.
		/// </summary>
		public Processor()
		{
			Memory = new Ram(0x10000);
			StackPointer = 0xFF;

			InterruptPeriod = 20;
			NumberofCyclesLeft = InterruptPeriod;
		}

		/// <summary>
		/// Performs the next step on the processor
		/// </summary>
		public void NextStep()
		{
			CurrentOpCode = Memory.ReadValue(ProgramCounter);
			ProgramCounter++;
			ExecuteOpCode();

			//We want to add here instead of replace because the number of cycles left could be zero.
			if (NumberofCyclesLeft < 0)
				NumberofCyclesLeft += InterruptPeriod;
		}

		/// <summary>
		/// Loads a program into the processors memory
		/// </summary>
		/// <param name="offset">The offset in memory when loading the program.</param>
		/// <param name="program">The program to be loaded</param>
		/// <param name="initialProgramCounter">The initial PC value, this is the entry point of the program</param>
		public void LoadProgram(int offset, byte[] program, int initialProgramCounter)
		{
			Memory.LoadProgram(offset, program);
			ProgramCounter = initialProgramCounter;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Executes an Opcode
		/// </summary>
		private void ExecuteOpCode()
		{
			//The x+ cycles denotes that if a page wrap occurs, then an additional cycle is consumed.
			//The x++ cycles denotes that 1 cycle is added when a branch occurs and it on the same page, and two cycles are added if its on a different page./
			//This is handled inside the GetValueFromMemory Method
			switch (CurrentOpCode)
			{
				//ADC Add With Carry, Immediate, 2 Bytes, 2 Cycles
				case 0x69:
					{
						AddWithCarryOperation(AddressingMode.Immediate);
						IncrementProgramCounter(2);
						NumberofCyclesLeft -= 2;
						break;
					}
				//ADC Add With Carry, Zero Page, 2 Bytes, 3 Cycles
				case 0x65:
					{
						AddWithCarryOperation(AddressingMode.ZeroPage);
						NumberofCyclesLeft -= 3;
						IncrementProgramCounter(2);
						break;
					}
				//ADC Add With Carry, Zero Page X, 2 Bytes, 4 Cycles
				case 0x75:
					{
						AddWithCarryOperation(AddressingMode.ZeroPageX);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
						break;
					}
				//ADC Add With Carry, Absolute, 3 Bytes, 4 Cycles
				case 0x60:
					{
						AddWithCarryOperation(AddressingMode.Absolute);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//ADC Add With Carry, Absolute X, 3 Bytes, 4+ Cycles
				case 0x7D:
					{
						AddWithCarryOperation(AddressingMode.AbsoluteX);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//ADC Add With Carry, Absolute Y, 3 Bytes, 4+ Cycles
				case 0x79:
					{
						AddWithCarryOperation(AddressingMode.AbsoluteY);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//ADC Add With Carry, Indexed Indirect, 2 Bytes, 6 Cycles
				case 0x61:
					{
						AddWithCarryOperation(AddressingMode.IndexedIndirect);
						NumberofCyclesLeft -= 6;
						IncrementProgramCounter(2);
						break;
					}
				//ADC Add With Carry, Indexed Indirect, 2 Bytes, 5+ Cycles
				case 0x71:
					{
						AddWithCarryOperation(AddressingMode.IndirectIndexed);
						NumberofCyclesLeft -= 5;
						IncrementProgramCounter(2);
						break;
					}
				//AND Compare Memory with Accumulator, Immediate, 2 Bytes, 2 Cycles
				case 0x29:
					{
						AndOperation(AddressingMode.Immediate);
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(2);
						break;
					}
				//AND Compare Memory with Accumulator, Zero Page, 2 Bytes, 3 Cycles
				case 0x25:
					{
						AndOperation(AddressingMode.ZeroPage);
						NumberofCyclesLeft -= 3;
						IncrementProgramCounter(2);
						break;
					}
				//AND Compare Memory with Accumulator, Zero PageX, 2 Bytes, 4 Cycles
				case 0x35:
					{
						AndOperation(AddressingMode.ZeroPageX);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
						break;
					}
				//AND Compare Memory with Accumulator, Absolute,  3 Bytes, 4 Cycles
				case 0x2D:
					{
						AndOperation(AddressingMode.Absolute);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//AND Compare Memory with Accumulator, AbsolueteX 3 Bytes, 4+ Cycles
				case 0x3D:
					{
						AndOperation(AddressingMode.AbsoluteX);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//AND Compare Memory with Accumulator, AbsoluteY, 3 Bytes, 4+ Cycles
				case 0x39:
					{
						AndOperation(AddressingMode.AbsoluteY);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//AND Compare Memory with Accumulator, IndexedIndirect, 2 Bytes, 6 Cycles
				case 0x21:
					{
						AndOperation(AddressingMode.IndexedIndirect);
						NumberofCyclesLeft -= 6;
						IncrementProgramCounter(2);
						break;
					}
				//AND Compare Memory with Accumulator, IndirectIndexed, 2 Bytes, 5 Cycles
				case 0x31:
					{
						AndOperation(AddressingMode.IndirectIndexed);
						NumberofCyclesLeft -= 5;
						IncrementProgramCounter(2);
						break;
					}
				//ASL Shift Left 1 Bit Memory or Accumulator, Accumulator, 1 Bytes, 2 Cycles
				case 0x0A:
					{
						ASlOperation(AddressingMode.Accumulator);
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;
					}
				//ASL Shift Left 1 Bit Memory or Accumulator, Zero Page, 2 Bytes, 5 Cycles
				case 0x06:
					{
						ASlOperation(AddressingMode.ZeroPage);
						NumberofCyclesLeft -= 5;
						IncrementProgramCounter(2);
						break;
					}
				//ASL Shift Left 1 Bit Memory or Accumulator, Zero PageX, 2 Bytes, 6 Cycles
				case 0x16:
					{
						ASlOperation(AddressingMode.ZeroPageX);
						NumberofCyclesLeft -= 6;
						IncrementProgramCounter(2);
						break;
					}
				//ASL Shift Left 1 Bit Memory or Accumulator, Absolute, 3 Bytes, 6 Cycles
				case 0x0E:
					{
						ASlOperation(AddressingMode.Absolute);
						NumberofCyclesLeft -= 6;
						IncrementProgramCounter(3);
						break;
					}
				//ASL Shift Left 1 Bit Memory or Accumulator, AbsoluteX, 3 Bytes, 7 Cycles
				case 0x1E:
					{
						ASlOperation(AddressingMode.AbsoluteX);
						NumberofCyclesLeft -= 7;
						IncrementProgramCounter(3);
						break;
					}
				//BCC Branch if Carry is Clear, Relative, 2 Bytes, 2++ Cycles
				case 0x90:
					{

						BranchOperation(!CarryFlag);
						NumberofCyclesLeft -= 2;
						break;

					}
				//BCS Branch if Carry is Set, Relative, 2 Bytes, 2++ Cycles
				case 0xB0:
					{
						BranchOperation(CarryFlag);
						NumberofCyclesLeft -= 2;
						break;
					}
				//BEQ Branch if Zero is Set, Relative, 2 Bytes, 2++ Cycles
				case 0xF0:
					{
						BranchOperation(ZeroFlag);
						NumberofCyclesLeft -= 2;
						break;
					}
				//BIT Compare Memory with Accumulator, Zero Page , 2 Bytes 3 Cycles
				case 0x24:
					{
						BitOperation(AddressingMode.ZeroPage);
						IncrementProgramCounter(2);
						NumberofCyclesLeft -= 3;
						break;
					}
				//BIT Compare Memory with Accumulator, Absolute , 2 Bytes 4 Cycles
				case 0x2C:
					{
						BitOperation(AddressingMode.Absolute);
						IncrementProgramCounter(3);
						NumberofCyclesLeft -= 4;
						break;
					}
				// BMI Branch if Negative Set
				case 0x30:
					{
						BranchOperation(NegativeFlag);
						NumberofCyclesLeft -= 2;
						break;
					}
				//BNE Branch if Zero is Not Set, Relative, 2 Bytes, 2++ Cycles
				case 0xD0:
					{
						BranchOperation(!ZeroFlag);
						NumberofCyclesLeft -= 2;
						break;
					}
				// BPL Branch if Negative Clear, 2 Bytes, 2++ Cycles
				case 0x10:
					{
						BranchOperation(!NegativeFlag);
						NumberofCyclesLeft -= 2;
						break;
					}
				//BRK Simulate IRQ, Implied, 1 Byte, 7 Cycles
				case 0x00:
					{
						//I am skipping this one for now. I am not quite sure how the Stack works, so I will come back to this one when I get a better handle on it.
						throw new NotImplementedException();
					}
				// BVC Branch if Overflow Clear, 2 Bytes, 2++ Cycles
				case 0x50:
					{
						BranchOperation(!OverflowFlag);
						NumberofCyclesLeft -= 2;
						break;
					}
				// BVS Branch if Overflow Set, 2 Bytes, 2++ Cycles
				case 0x70:
					{
						BranchOperation(OverflowFlag);
						NumberofCyclesLeft -= 2;
						break;
					}
				//CLC Clear Carry Flag, Implied, 1 Byte, 2 Cycles
				case 0x18:
					{
						CarryFlag = false;
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;
					}
				//CLD Clear Decimal Flag, Implied, 1 Byte, 2 Cycles
				case 0xD8:
					{
						Decimal = false;
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;

					}
				//CLI Clear Interrupt Flag, Implied, 1 Byte, 2 Cycles
				case 0x58:
					{
						InterruptFlag = false;
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;

					}
				//CMP Compare Accumulator with Memory, Immediate, 2 Bytes, 2 Cycles
				case 0xC9:
					{
						CompareOperation(AddressingMode.Immediate, Accumulator);	
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(2);
						break;
					}
				//CMP Compare Accumulator with Memory, Zero Page, 2 Bytes, 3 Cycles
				case 0xC5:
					{
						CompareOperation(AddressingMode.ZeroPage, Accumulator);
						NumberofCyclesLeft -= 3;
						IncrementProgramCounter(2);
						break;
					}
				//CMP Compare Accumulator with Memory, Zero Page x, 2 Bytes, 4 Cycles
				case 0xD5:
					{
						CompareOperation(AddressingMode.ZeroPageX, Accumulator);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
						break;
					}
				//CMP Compare Accumulator with Memory, Absolute, 3 Bytes, 4 Cycles
				case 0xCD:
					{
						CompareOperation(AddressingMode.Absolute, Accumulator);	
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//CMP Compare Accumulator with Memory, Absolute X, 2 Bytes, 4 Cycles
				case 0xDD:
					{
						CompareOperation(AddressingMode.AbsoluteX, Accumulator);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//CMP Compare Accumulator with Memory, Absolute Y, 2 Bytes, 4 Cycles
				case 0xD9:
					{
						CompareOperation(AddressingMode.AbsoluteY, Accumulator);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//CMP Compare Accumulator with Memory, Indirect X, 2 Bytes, 6 Cycles
				case 0xC1:
					{
						CompareOperation(AddressingMode.IndexedIndirect, Accumulator);
						NumberofCyclesLeft -= 6;
						IncrementProgramCounter(2);
						break;
					}
				//CMP Compare Accumulator with Memory, Indirect Y, 2 Bytes, 5 Cycles
				case 0xD1:
					{
						CompareOperation(AddressingMode.IndexedIndirect, Accumulator);
						NumberofCyclesLeft -= 5;
						IncrementProgramCounter(2);
						break;
					}
				//CLV Clear Overflow Flag, Implied, 1 Byte, 2 Cycles
				case 0xB8:
					{
						OverflowFlag = false;
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;
					}
				//JMP Jump to New Location, Absolute 3 Bytes, 3 Cycles
				case 0x4C:
					{
						ProgramCounter = GetAddressByAddressingMode(AddressingMode.Absolute);
						NumberofCyclesLeft -= 3;
						break;
					}
				//LDA Load Accumulator with Memory, Immediate, 2 Bytes, 2 Cycles
				case 0xA9:
					{

						Accumulator = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.Immediate));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);

						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(2);
						break;
					}
				//LDA Load Accumulator with Memory, Zero Page, 2 Bytes, 3 Cycles
				case 0xA5:
					{
						Accumulator = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.ZeroPage));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);

						NumberofCyclesLeft -= 3;
						IncrementProgramCounter(2);
						break;
					}
				//LDA Load Accumulator with Memory, Zero Page X, 2 Bytes, 4 Cycles
				case 0xB5:
					{
						Accumulator = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.ZeroPageX));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);

						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
						break;
					}
				//LDA Load Accumulator with Memory, Absolute, 3 Bytes, 4 Cycles
				case 0xAD:
					{
						Accumulator = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.Absolute));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);

						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//LDA Load Accumulator with Memory, Absolute X, 3 Bytes, 4+ Cycles
				case 0xBD:
					{
						Accumulator = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.AbsoluteX));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);

						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//LDA Load Accumulator with Memory, Absolute Y, 3 Bytes, 4+ Cycles
				case 0xB9:
					{
						Accumulator = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.AbsoluteY));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);

						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//LDA Load Accumulator with Memory, Index Indirect, 2 Bytes, 6 Cycles
				case 0xA1:
					{
						Accumulator = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.IndexedIndirect));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);

						NumberofCyclesLeft -= 6;
						IncrementProgramCounter(2);
						break;
					}
				//LDA Load Accumulator with Memory, Indirect Index, 2 Bytes, 5+ Cycles
				case 0xB1:
					{
						Accumulator = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.IndirectIndexed));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);

						NumberofCyclesLeft -= 5;
						IncrementProgramCounter(2);
						break;
					}
				//SEC Set Carry, Implied, 1 Bytes, 2 Cycles
				case 0x38:
					{
						CarryFlag = true;
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;
					}
				//SEI Set Interrupt, Implied, 1 Bytes, 2 Cycles
				case 0x78:
					{
						InterruptFlag = true;
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;
					}
				//SED Set Interrupt, Implied, 1 Bytes, 2 Cycles
				case 0xF8:
					{
						Decimal = true;
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;
					}
				//STX Store Index X, Zero Page, 2 Bytes, 3 Cycles
				case 0x86:
					{
						XRegister = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.ZeroPage));
						NumberofCyclesLeft -= 3;
						IncrementProgramCounter(2);
						break;
					}
				//STX Store Index X, Zero Page Y, 2 Bytes, 4 Cycles
				case 0x96:
					{
						XRegister = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.ZeroPageY));
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
						break;
					}
				//STX Store Index X, Absolute, 3 Bytes, 4 Cycles
				case 0x8E:
					{
						XRegister = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.Absolute));
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//STY Store Index Y, Zero Page, 2 Bytes, 3 Cycles
				case 0x84:
					{
						YRegister = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.ZeroPage));
						NumberofCyclesLeft -= 3;
						IncrementProgramCounter(2);
						break;
					}
				//STY Store Index Y, Zero Page X, 2 Bytes, 4 Cycles
				case 0x94:
					{
						YRegister = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.ZeroPageX));
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
						break;
					}
				//STY Store Index Y, Absolute, 2 Bytes, 4 Cycles
				case 0x8C:
					{
						YRegister = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.Absolute));
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				default:
					throw new NotSupportedException(string.Format("The OpCode {0} is not supported", CurrentOpCode));
			}
		}

		/// <summary>
		/// Increments the program Counter. We always Increment by 1 less than the value that is passed in to account for the increment that happens after the current
		/// Op code is retrieved
		/// </summary>
		/// <param name="lengthOfOperation">The lenght of the operation</param>
		private void IncrementProgramCounter(int lengthOfOperation)
		{
			//The PC gets increments after the opcode is retrieved but before the opcode is executed. We want to add the remaining length.
			ProgramCounter += lengthOfOperation - 1;
		}
		
		/// <summary>
		/// Sets the IsOverflow Register Correctly
		/// </summary>
		/// <param name="accumulator">The Value in the accumulator before the operation</param>
		/// <param name="memory">The value that came from memory</param>
		/// <param name="result">The result of the operation between the accumulator and memory</param>
		private void SetOverflow(int accumulator , int memory, int result)
		{
			
			OverflowFlag = ( ( accumulator ^ result ) & ( memory ^ result ) & 0x80 ) != 0;

		}

		/// <summary>
		/// Sets the IsSignNegative register
		/// </summary>
		/// <param name="value"></param>
		private void SetNegativeFlag(int value)
		{
			//on the 6502, any value greater than 127 is negative. 128 = 1000000 in Binary. the 8th bit is set, therefore the number is a negative number.
			NegativeFlag = value > 127;
		}

		/// <summary>
		/// Sets the IsResultZero register
		/// </summary>
		/// <param name="value"></param>
		private void SetZeroFlag(int value)
		{
			ZeroFlag = value == 0;
		}

		/// <summary>
		/// Uses the AddressingMode to return the correct address based on the mode.
		/// Note: This method will not increment the program counter for any mode.
		/// Note: This method will return an error if called for either the immediate or accumulator modes. 
		/// </summary>
		/// <param name="addressingMode">The addressing Mode to use</param>
		/// <returns>The memory Location</returns>
		private int GetAddressByAddressingMode(AddressingMode addressingMode)
		{
			int address;
			switch (addressingMode)
			{
				case (AddressingMode.Absolute):
					{
						//Get the first half of the address
						address = Memory.ReadValue(ProgramCounter);

						//Get the second half of the address. We multiple the value by 256 so we retrieve the right address. 
						//IF the first Value is FF and the second value is FF the address would be FFFF.
						address += 256 * Memory.ReadValue(ProgramCounter + 1);
						return address;
					}
				case AddressingMode.AbsoluteX:
					{
						//Get the first half of the address
						address = Memory.ReadValue(ProgramCounter);

						//Get the second half of the address. We multiple the value by 256 so we retrieve the right address. 
						//IF the first Value is FF and the second value is FF the address would be FFFF.
						//Then add the X Register value to that.
						//We don't increment the program counter here, because it is incremented as part of the operation.
						address += (256 * Memory.ReadValue(ProgramCounter + 1) + XRegister);

						//This address wraps if its greater than 0xFFFF
						if (address > 0xFFFF)
						{
							address-= 0x10000;
							//We crossed a page boundry, so decrease the number of cycles by 1.
							//However, if this is an ASL operation, we do not decrease if by 1.
							if (CurrentOpCode == 0x1E)
								return Memory.ReadValue(address);

							NumberofCyclesLeft--;
						}
						return address;
					}
				case AddressingMode.AbsoluteY:
					{
						//Get the first half of the address
						address = Memory.ReadValue(ProgramCounter);

						//Get the second half of the address. We multiple the value by 256 so we retrieve the right address. 
						//IF the first Value is FF and the second value is FF the address would be FFFF.
						//Then add the Y Register value to that.
						//We don't increment the program counter here, because it is incremented as part of the operation.
						address += (256 * Memory.ReadValue(ProgramCounter + 1) + YRegister);

						//This address wraps if its greater than 0xFFFF
						if (address > 0xFFFF)
						{
							address-= 0x10000;
							//We crossed a page boundry, so decrease the number of cycles by 1.
							NumberofCyclesLeft--;
						}

						return address;
					}
				case AddressingMode.Immediate:
					{
						return ProgramCounter;
					}
				case AddressingMode.IndexedIndirect:
					{
						//Get the location of the address to retrieve
						address = Memory.ReadValue(ProgramCounter) + XRegister;

						//Its a zero page address, so it wraps around if greater than 0xff
						if (address > 0xff)
							address-= 0x100;

						//Now get the final Address. The is not a zero page address either.
						var finalAddress = Memory.ReadValue(address) + (256 * Memory.ReadValue(address + 1));
						return finalAddress;
					}
				case AddressingMode.IndirectIndexed:
					{
						address = Memory.ReadValue(ProgramCounter);

						var finalAddress = Memory.ReadValue(address) + (256 * Memory.ReadValue(address + 1)) + YRegister;

						//This address wraps if its greater than 0xFFFF
						if (finalAddress > 0xFFFF)
						{
							finalAddress-= 0x10000;
							//We crossed a page boundry, so decrease the number of cycles by 1.
							NumberofCyclesLeft--;
						}
						return finalAddress;
					}
				case AddressingMode.Relative:
					{
						return ProgramCounter;
					}
				case (AddressingMode.ZeroPage):
					{
						address = Memory.ReadValue(ProgramCounter);
						return address;
					}
				case (AddressingMode.ZeroPageX):
					{
						address = Memory.ReadValue(ProgramCounter);
						return address + XRegister;
					}
				case (AddressingMode.ZeroPageY):
					{
						address = Memory.ReadValue(ProgramCounter);
						return address + XRegister;
					}
				default:
					throw new InvalidEnumArgumentException(string.Format("The Addressing Mode {0} has not been implemented", addressingMode));
			}
		}
	
		/// <summary>
		/// Moves the ProgramCounter in a given direction based on the value inputted
		/// 
		/// </summary>
		private void MoveProgramCounterByRelativeValue(byte valueToMove)
		{
			
			var newAddress = valueToMove > 127 ? (valueToMove & 0x7f) * -1 : (valueToMove & 0x7f);

			var newProgramCounter = ProgramCounter + newAddress;

			if (newProgramCounter < 0x0 || newProgramCounter > 0xFFFF)
			{
				//We crossed a page boundry, so decrease the number of cycles by 1.
				NumberofCyclesLeft--;
			}
			ProgramCounter = newProgramCounter;

		}
		#region Op Code Operations
		/// <summary>
		/// The ADC - Add Memory to Accumulator with Carry Operation
		/// </summary>
		/// <param name="addressingMode">The addressing mode used to perform this operation.</param>
		private void AddWithCarryOperation(AddressingMode addressingMode)
		{
			//Accumulator, Carry = Accumulator + ValueInMemoryLocation + Carry 
			var memoryValue = Memory.ReadValue(GetAddressByAddressingMode(addressingMode));
			var newValue = memoryValue + Accumulator + (CarryFlag ? 1 : 0);

			SetOverflow(Accumulator, memoryValue, newValue);

			if (Decimal)
			{
				if (newValue > 99)
				{
					CarryFlag = true;
					newValue -= 100;
				}
				else
				{
					CarryFlag = false;
				}
			}
			else
			{
				if (newValue > 255)
				{
					CarryFlag = true;
					newValue -= 256;	
				}
				else
				{
					CarryFlag = false;
				}
			}

			SetZeroFlag(newValue);
			SetNegativeFlag(newValue);

			Accumulator = newValue;
		}

		/// <summary>
		/// The AND - Compare Memory with Accumulator operation
		/// </summary>
		/// <param name="addressingMode">The addressing mode being used</param>
		private void AndOperation(AddressingMode addressingMode)
		{
			Accumulator = Memory.ReadValue(GetAddressByAddressingMode(addressingMode)) & Accumulator;

			SetZeroFlag(Accumulator);
			SetNegativeFlag(Accumulator);
		}

		/// <summary>
		/// The ASL - Shift Left One Bit (Memory or Accumulator)
		/// </summary>
		/// <param name="addressingMode">The addressing Mode being used</param>
		public void ASlOperation(AddressingMode addressingMode)
		{
			int value;
			var memoryAddress = 0;
			if (addressingMode == AddressingMode.Accumulator)
				value = Accumulator;
			else
			{
				memoryAddress = GetAddressByAddressingMode(addressingMode);
				value = Memory.ReadValue(memoryAddress);
			}

			//If the 7th bit is set, then we have a carry
			CarryFlag = ((value & 0x80) != 0);

			value = (value << 1);

			if (value > 255)
				value -= 256;

			SetNegativeFlag(value);
			SetZeroFlag(value);

			if (addressingMode == AddressingMode.Accumulator)
				Accumulator = value;
			else
			{
				Memory.WriteValue(memoryAddress, (byte)value);
			}
		}

		/// <summary>
		/// Performs the different branch operations.
		/// </summary>
		/// <param name="performBranch">Is a branch required</param>
		private void BranchOperation(bool performBranch)
		{
			if (performBranch)
			{
				var value = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.Relative));
				
				MoveProgramCounterByRelativeValue(value);
				//We add a cycle because the branch occured.
				NumberofCyclesLeft -= 1;
			}
			
			IncrementProgramCounter(2);
		}

		private void BitOperation(AddressingMode addressingMode)
		{
			var valueToCompare = Memory.ReadValue(GetAddressByAddressingMode(addressingMode)) & Accumulator;

			OverflowFlag = (valueToCompare & 0x40) != 0;

			SetNegativeFlag(valueToCompare);
			SetZeroFlag(valueToCompare);
		}

		private void CompareOperation(AddressingMode addressingMode, int comparisonValue)
		{
			var memoryValue = Memory.ReadValue(GetAddressByAddressingMode(addressingMode));
			var comparedValue = comparisonValue - memoryValue;

			if (comparedValue < 0)
				comparedValue += 0x10000;

			SetZeroFlag(comparedValue);

			CarryFlag = memoryValue <= comparisonValue;
			SetNegativeFlag(comparedValue);
		}
		#endregion
		
		#endregion
	}
}
