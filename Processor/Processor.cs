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
		public bool IsResultZero { get; private set; }
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
		/// Is true if an arithmetic operaiton procudes a result larger than a byte
		/// </summary>
		public bool IsOverflow { get; private set; }
		/// <summary>
		/// Set to true if the result of an operation is negative
		/// </summary>
		public bool IsSignNegative { get; private set; }
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

			InterruptPeriod = 10;
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
						AddWithCarry(AddressingMode.Immediate);
						IncrementProgramCounter(2);
						NumberofCyclesLeft -= 2;
						break;
					}
				//ADC Add With Carry, Zero Page, 2 Bytes, 3 Cycles
				case 0x65:
					{
						AddWithCarry(AddressingMode.ZeroPage);
						NumberofCyclesLeft -= 3;
						IncrementProgramCounter(2);
						break;
					}
				//ADC Add With Carry, Zero Page X, 2 Bytes, 4 Cycles
				case 0x75:
					{
						AddWithCarry(AddressingMode.ZeroPageX);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
						break;
					}
				//ADC Add With Carry, Absolute, 3 Bytes, 4 Cycles
				case 0x60:
					{
						AddWithCarry(AddressingMode.Absolute);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//ADC Add With Carry, Absolute X, 3 Bytes, 4+ Cycles
				case 0x7D:
					{
						AddWithCarry(AddressingMode.AbsoluteX);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//ADC Add With Carry, Absolute Y, 3 Bytes, 4+ Cycles
				case 0x79:
					{
						AddWithCarry(AddressingMode.AbsoluteY);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(3);
						break;
					}
				//ADC Add With Carry, Indexed Indirect, 2 Bytes, 6 Cycles
				case 0x61:
					{
						AddWithCarry(AddressingMode.IndexedIndirect);
						NumberofCyclesLeft -= 6;
						IncrementProgramCounter(2);
						break;
					}
				//ADC Add With Carry, Indexed Indirect, 2 Bytes, 5+ Cycles
				case 0x71:
					{
						AddWithCarry(AddressingMode.IndirectIndexed);
						NumberofCyclesLeft -= 5;
						IncrementProgramCounter(2);
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
						XRegister = GetValueFromMemory(AddressingMode.ZeroPage);
						NumberofCyclesLeft -= 4;
						IncrementProgramCounter(2);
						break;
					}
				//STY Store Index Y, Zero Page Mode, 2 Bytes, 4 Cycles
				case 0x84:
					{
						YRegister = GetValueFromMemory(AddressingMode.ZeroPage);
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
		/// The ADC - Add Memory to Accumulator with Carry Operation
		/// </summary>
		/// <param name="addressingMode">The addressing mode used to perform this operation.</param>
		private void AddWithCarry(AddressingMode addressingMode)
		{
			//Accumulator, Carry = Accumulator + ValueInMemoryLocation + Carry 
			var newValue = GetValueFromMemory(addressingMode) + Accumulator + (CarryFlag ? 1 : 0);

			SetIsOverflow(newValue);

			if (IsInDecimalMode)
				CarryFlag = newValue > 99;
			else
				CarryFlag = newValue > 255;

			if (IsOverflow)
				newValue = newValue - 256;

			SetIsResultZero(newValue);
			SetIsSignNegative(newValue);

			Accumulator = newValue;
		}

		/// <summary>
		/// Sets the IsOverflow register
		/// </summary>
		/// <param name="value"></param>
		private void SetIsOverflow(int value)
		{
			IsOverflow = value > 255;
		}

		/// <summary>
		/// Sets the IsSignNegative register
		/// </summary>
		/// <param name="value"></param>
		private void SetIsSignNegative(int value)
		{
			IsSignNegative = value > 127;
		}

		/// <summary>
		/// Sets the IsResultZero register
		/// </summary>
		/// <param name="value"></param>
		private void SetIsResultZero(int value)
		{
			IsResultZero = value == 0;
		}

		/// <summary>
		/// Uses the AddressingMode to return the correct value from memory.
		/// Note: This method will not increment the program counter for any mode.
		/// </summary>
		/// <param name="addressingMode">The addressing Mode to use</param>
		/// <returns>A value from memory</returns>
		private int GetValueFromMemory(AddressingMode addressingMode)
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
						return Memory.ReadValue(address);
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
							NumberofCyclesLeft--;
						}


						return Memory.ReadValue(address);
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

						return Memory.ReadValue(address);
					}
				case (AddressingMode.Immediate):
					{
						return Memory.ReadValue(ProgramCounter);
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
						return Memory.ReadValue(finalAddress);
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

						return Memory.ReadValue(finalAddress);

					}
				case (AddressingMode.ZeroPage):
					{
						address = Memory.ReadValue(ProgramCounter);
						return Memory.ReadValue(address);
					}
				case (AddressingMode.ZeroPageX):
					{
						address = Memory.ReadValue(ProgramCounter);
						return Memory.ReadValue(address + XRegister);
					}
				case (AddressingMode.ZeroPageY):
					{
						address = Memory.ReadValue(ProgramCounter);
						return Memory.ReadValue(address + XRegister);
					}
				default:
					throw new InvalidEnumArgumentException(string.Format("The Addressing Mode {0} has not been implemented", addressingMode));
			}

		}
		#endregion
	}
}
