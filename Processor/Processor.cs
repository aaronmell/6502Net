using System;
using System.ComponentModel;

namespace Processor
{
	/// <summary>
	/// An Implementation of a 6502 Processor
	/// </summary>
	public class Processor
	{
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
		public int ProgramCounter { get; private set; }
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
		/// The stack
		/// </summary>
		public byte[] Stack { get; private set; }
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
		public bool Zero { get; private set; }
		/// <summary>
		/// Interrupts are disabled if this is true
		/// </summary>
		public bool IsInterruptDisabled { get; private set; }
		/// <summary>
		/// If true, the CPU is in decimal mode.
		/// </summary>
		public bool IsInDecimalMode { get; private set; }
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
		public bool IsOverflow { get; private set; }
		/// <summary>
		/// Set to true if the result of an operation is negative in ADC and SBC operations. 
		/// In shift operations the sign holds the carry.
		/// </summary>
		public bool Sign { get; private set; }
		#endregion

		#region Public Methods
		/// <summary>
		/// Default Constructor, Instantiates a new instance of the processor.
		/// </summary>
		public Processor()
		{
			Stack = new byte[256];
			Memory = new Ram(0xFFFF);
			StackPointer = 0xFF;

			InitalizeStack();

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
		/// Initializes the stack to a default value
		/// </summary>
		private void InitalizeStack()
		{
			for (int i = 0; i < Stack.Length; i++)
				Stack[i] = 0x00;
		}

		/// <summary>
		/// Executes an Opcode
		/// </summary>
		private void ExecuteOpCode()
		{
			//The x+ cycles denotes that if a page wrap occurs, then an additional cycle is consumed.
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
				//LDA Load Accumulator with Memory, Immediate, 2 Bytes, 2 Cycles
				case 0xA9:
					{

						Accumulator = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.Immediate));
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(2);
						break;
					}
				//SEC Set Carry, Implied Mode, 1 Bytes, 2 Cycles
				case 0x38:
					{
						CarryFlag = true;
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;
					}
				//SED Set Decimal, Implied Mode, 1 Bytes, 2 Cycles
				case 0xF8:
					{
						IsInDecimalMode = true;
						NumberofCyclesLeft -= 2;
						IncrementProgramCounter(1);
						break;
					}
				//STX Store Index X, Zero Page Mode, 2 Bytes, 4 Cycles
				case 0x86:
					{
						XRegister = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.ZeroPage));
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
						break;
					}
				//STY Store Index Y, Zero Page Mode, 2 Bytes, 4 Cycles
				case 0x84:
					{
						YRegister = Memory.ReadValue(GetAddressByAddressingMode(AddressingMode.ZeroPage));
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
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
			
			IsOverflow = ( ( accumulator ^ result ) & ( memory ^ result ) & 0x80 ) != 0;

		}

		/// <summary>
		/// Sets the IsSignNegative register
		/// </summary>
		/// <param name="value"></param>
		private void SetIsSignNegative(int value)
		{
			//on the 6502, any value greater than 127 is negative. 128 = 1000000 in Binary. the 8th bit is set, therefore the number is a negative number.
			Sign = value > 127;
		}

		/// <summary>
		/// Sets the IsResultZero register
		/// </summary>
		/// <param name="value"></param>
		private void SetIsResultZero(int value)
		{
			Zero = value == 0;
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
							address = address - 0x10000;
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
							address = address - 0x10000;
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
							address = address - 0x100;

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
							finalAddress = finalAddress - 0x10000;
							//We crossed a page boundry, so decrease the number of cycles by 1.
							NumberofCyclesLeft--;
						}
						return finalAddress;
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

			if (IsInDecimalMode)
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

			SetIsResultZero(newValue);
			SetIsSignNegative(newValue);

			Accumulator = newValue;
		}

		/// <summary>
		/// The AND - Compare Memory with Accumulator operation
		/// </summary>
		/// <param name="addressingMode">The addressing mode being used</param>
		private void AndOperation(AddressingMode addressingMode)
		{
			Accumulator = Memory.ReadValue(GetAddressByAddressingMode(addressingMode)) & Accumulator;

			SetIsResultZero(Accumulator);
			SetIsSignNegative(Accumulator);
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

			SetIsSignNegative(value);
			SetIsResultZero(value);

			if (addressingMode == AddressingMode.Accumulator)
				Accumulator = value;
			else
			{
				Memory.WriteValue(memoryAddress, (byte)value);
			}
		}
		#endregion
		
		#endregion
	}
}
