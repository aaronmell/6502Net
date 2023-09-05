using NLog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Processor
{
	/// <summary>
	/// An Implementation of a 6502 Processor
	/// </summary>
	[Serializable]
	public class Processor
	{
		#region Fields
        private static readonly ILogger _logger = LogManager.GetLogger("Processor");
		private int _programCounter;
		private int _stackPointer;
	    private int _cycleCount;
        private bool _previousInterrupt;
        private bool _interrupt;
        #endregion

        //All of the properties here are public and read only to facilitate ease of debugging and testing.
        #region Properties
        /// <summary>
        /// The memory
        /// </summary>
        protected byte[] Memory { get; private set; }

		/// <summary>
		/// The Accumulator. This value is implemented as an integer intead of a byte.
		/// This is done so we can detect wrapping of the value and set the correct number of cycles.
		/// </summary>
		public int Accumulator { get; protected set; }
		/// <summary>
		/// The X Index Register
		/// </summary>
		public int XRegister { get; private set; }
		/// <summary>
		/// The Y Index Register
		/// </summary>
		public int YRegister { get; private set; }
		/// <summary>
		/// The Current Op Code being executed by the system
		/// </summary>
		public int CurrentOpCode { get; private set; }
        
		/// <summary>
		/// The disassembly of the current operation. This value is only set when the CPU is built in debug mode.
		/// </summary>
		public Disassembly CurrentDisassembly { get; private set; }
		/// <summary>
		/// Points to the Current Address of the instruction being executed by the system. 
		/// The PC wraps when the value is greater than 65535, or less than 0. 
		/// </summary>
		public int ProgramCounter
		{
			get { return _programCounter; } 
			private set { _programCounter = WrapProgramCounter(value); }
		}
		/// <summary>
		/// Points to the Current Position of the Stack.
		/// This value is a 00-FF value but is offset to point to the location in memory where the stack resides.
		/// </summary>
		public int StackPointer
		{
			get { return _stackPointer; }
			private set
			{
				if (value > 0xFF)
					_stackPointer = value - 0x100;
				else if (value < 0x00)
					_stackPointer = value + 0x100;
				else
					_stackPointer = value;
			}
		}
		
        /// <summary>
        /// An external action that occurs when the cycle count is incremented
        /// </summary>
        public Action CycleCountIncrementedAction { get; set; }

        //Status Registers
		/// <summary>
		/// This is the carry flag. when adding, if the result is greater than 255 or 99 in BCD Mode, then this bit is enabled. 
		/// In subtraction this is reversed and set to false if a borrow is required IE the result is less than 0
		/// </summary>
		public bool CarryFlag { get; protected set; }
		/// <summary>
		/// Is true if one of the registers is set to zero.
		/// </summary>
		public bool ZeroFlag { get; private set; }
		/// <summary>
		/// This determines if Interrupts are currently disabled.
		/// This flag is turned on during a reset to prevent an interrupt from occuring during startup/Initialization.
		/// If this flag is true, then the IRQ pin is ignored.
		/// </summary>
		public bool DisableInterruptFlag { get; private set; }
		/// <summary>
		/// Binary Coded Decimal Mode is set/cleared via this flag.
		/// when this mode is in effect, a byte represents a number from 0-99. 
		/// </summary>
		public bool DecimalFlag { get; private set; }
		/// <summary>
		/// This property is set when an overflow occurs. An overflow happens if the high bit(7) changes during the operation. Remember that values from 128-256 are negative values
		/// as the high bit is set to 1.
		/// Examples:
		/// 64 + 64 = -128 
		/// -128 + -128 = 0
		/// </summary>
		public bool OverflowFlag { get; protected set; }
		/// <summary>
		/// Set to true if the result of an operation is negative in ADC and SBC operations. 
		/// Remember that 128-256 represent negative numbers when doing signed math.
		/// In shift operations the sign holds the carry.
		/// </summary>
		public bool NegativeFlag { get; private set; }

        /// <summary>
        /// Set to true when an NMI should occur
        /// </summary>
        public bool TriggerNmi { get; set; }

        /// Set to true when an IRQ has occurred and is being processed by the CPU
        public bool TriggerIRQ { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Default Constructor, Instantiates a new instance of the processor.
        /// </summary>
        public Processor()
		{
			Memory = new byte[0x10000];
			StackPointer = 0x100;

		    CycleCountIncrementedAction = () => { };
		}

		/// <summary>
		/// Initializes the processor to its default state.
		/// </summary>
		public void Reset()
		{
            ResetCycleCount();
           
			StackPointer = 0x1FD;

			//Set the Program Counter to the Reset Vector Address.
			ProgramCounter = 0xFFFC;
			//Reset the Program Counter to the Address contained in the Reset Vector
			ProgramCounter = ( Memory[ProgramCounter] | ( Memory[ProgramCounter + 1] << 8));;

            CurrentOpCode = Memory[ProgramCounter];
			
            //SetDisassembly();

			DisableInterruptFlag = true;
            _previousInterrupt = false;
            TriggerNmi = false;
            TriggerIRQ = false;
		}

		/// <summary>
		/// Performs the next step on the processor
		/// </summary>
		public void NextStep()
		{
            SetDisassembly();

            //Have to read this first otherwise it causes tests to fail on a NES
            CurrentOpCode = ReadMemoryValue(ProgramCounter);

            ProgramCounter++;
		    
            ExecuteOpCode();

            if (_previousInterrupt)
            {
                if (TriggerNmi)
                {
                    ProcessNMI();
                    TriggerNmi = false;
                }
                else if (TriggerIRQ)
                {
                    ProcessIRQ();
                    TriggerIRQ = false;
                }                
            }  
        }

		/// <summary>
		/// Loads a program into the processors memory
		/// </summary>
		/// <param name="offset">The offset in memory when loading the program.</param>
		/// <param name="program">The program to be loaded</param>
		/// <param name="initialProgramCounter">The initial PC value, this is the entry point of the program</param>
		public void LoadProgram(int offset, byte[] program, int initialProgramCounter)
		{
			LoadProgram(offset, program);

			var bytes = BitConverter.GetBytes(initialProgramCounter);

			//Write the initialProgram Counter to the reset vector
			WriteMemoryValue(0xFFFC,bytes[0]);
			WriteMemoryValue(0xFFFD, bytes[1]);
			
			//Reset the CPU
			Reset();
		}

        /// <summary>
        /// Loads a program into the processors memory
        /// </summary>
        /// <param name="offset">The offset in memory when loading the program.</param>
        /// <param name="program">The program to be loaded</param>
        public void LoadProgram(int offset, byte[] program)
        {
            if (offset > Memory.Length)
                throw new InvalidOperationException("Offset '{0}' is larger than memory size '{1}'");

            if (program.Length + offset > Memory.Length)
                throw new InvalidOperationException(string.Format("Program Size '{0}' Cannot be Larger than Memory Size '{1}' plus offset '{2}'", program.Length, Memory.Length, offset));

            for (var i = 0; i < program.Length; i++)
            {
                Memory[i + offset] = program[i];
            }

            Reset();
        }
		
		/// <summary>
		/// The InterruptRequest or IRQ
		/// </summary>
		public void InterruptRequest()
		{
		    TriggerIRQ = true;
		}

		        /// <summary>
        /// Clears the memory
        /// </summary>
        public void ClearMemory()
        {
            for (var i = 0; i < Memory.Length; i++)
                Memory[i] = 0x00;
        }

        /// <summary>
        /// Returns the byte at the given address.
        /// </summary>
        /// <param name="address">The address to return</param>
        /// <returns>the byte being returned</returns>
        public virtual byte ReadMemoryValue(int address)
        {
            var value  = Memory[address];
            IncrementCycleCount();
            return value;
        }

        /// <summary>
        /// Returns the byte at a given address without incrementing the cycle. Useful for test harness. 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public virtual byte ReadMemoryValueWithoutCycle(int address)
        {
            var value = Memory[address];
            return value;
        }

        /// <summary>
        /// Writes data to the given address.
        /// </summary>
        /// <param name="address">The address to write data to</param>
        /// <param name="data">The data to write</param>
        public virtual void WriteMemoryValue(int address, byte data)
        {
            IncrementCycleCount();
            Memory[address] = data;
        }

        /// <summary>
        /// Gets the Number of Cycles that have elapsed
        /// </summary>
        /// <returns>The number of elapsed cycles</returns>
	    public int GetCycleCount()
	    {
	        return _cycleCount;
	    }

        /// <summary>
        /// Increments the Cycle Count, causes a CycleCountIncrementedAction to fire.
        /// </summary>
        protected void IncrementCycleCount()
        {
            _cycleCount++;
            CycleCountIncrementedAction();

            _previousInterrupt = _interrupt;
            _interrupt = TriggerNmi || (TriggerIRQ && !DisableInterruptFlag); 
        }

        /// <summary>
        /// Resets the Cycle Count back to 0
        /// </summary>
	    public void ResetCycleCount()
	    {
	        _cycleCount = 0;
	    }

        /// <summary>
        /// Dumps the entire memory object. Used when saving the memory state
        /// </summary>
        /// <returns></returns>
        public byte[] DumpMemory()
        {
            return Memory;
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
				#region Add / Subtract Operations
				//ADC Add With Carry, Immediate, 2 Bytes, 2 Cycles
				case 0x69:
					{
						AddWithCarryOperation(AddressingMode.Immediate);
						break;
					}
                //ADC Add With Carry, Zero Page, 2 Bytes, 3 Cycles
				case 0x65:
					{
						AddWithCarryOperation(AddressingMode.ZeroPage);
						break;
					}
                //ADC Add With Carry, Zero Page X, 2 Bytes, 4 Cycles
				case 0x75:
					{
						AddWithCarryOperation(AddressingMode.ZeroPageX);						
						break;
					}
                //ADC Add With Carry, Absolute, 3 Bytes, 4 Cycles
				case 0x6D:
					{
						AddWithCarryOperation(AddressingMode.Absolute);
						break;
					}
                //ADC Add With Carry, Absolute X, 3 Bytes, 4+ Cycles
				case 0x7D:
					{
						AddWithCarryOperation(AddressingMode.AbsoluteX);
						break;
					}
                //ADC Add With Carry, Absolute Y, 3 Bytes, 4+ Cycles
				case 0x79:
					{
						AddWithCarryOperation(AddressingMode.AbsoluteY);
						break;
					}
                //ADC Add With Carry, Indexed Indirect, 2 Bytes, 6 Cycles
				case 0x61:
					{
						AddWithCarryOperation(AddressingMode.IndirectX);
						break;
					}
                //ADC Add With Carry, Indexed Indirect, 2 Bytes, 5+ Cycles
				case 0x71:
					{
						AddWithCarryOperation(AddressingMode.IndirectY);
						break;
					}
				//SBC Subtract with Borrow, Immediate, 2 Bytes, 2 Cycles
				case 0xE9:
					{
						SubtractWithBorrowOperation(AddressingMode.Immediate);
						break;
					}
				//SBC Subtract with Borrow, Zero Page, 2 Bytes, 3 Cycles
				case 0xE5:
					{
						SubtractWithBorrowOperation(AddressingMode.ZeroPage);
						break;
					}
				//SBC Subtract with Borrow, Zero Page X, 2 Bytes, 4 Cycles
				case 0xF5:
					{
						SubtractWithBorrowOperation(AddressingMode.ZeroPageX);
						break;
					}
				//SBC Subtract with Borrow, Absolute, 3 Bytes, 4 Cycles
				case 0xED:
					{
						SubtractWithBorrowOperation(AddressingMode.Absolute);
						break;
					}
				//SBC Subtract with Borrow, Absolute X, 3 Bytes, 4+ Cycles
				case 0xFD:
					{
						SubtractWithBorrowOperation(AddressingMode.AbsoluteX);
						break;
					}
				//SBC Subtract with Borrow, Absolute Y, 3 Bytes, 4+ Cycles
				case 0xF9:
					{
						SubtractWithBorrowOperation(AddressingMode.AbsoluteY);
						break;
					}
				//SBC Subtract with Borrow, Indexed Indirect, 2 Bytes, 6 Cycles
				case 0xE1:
					{
						SubtractWithBorrowOperation(AddressingMode.IndirectX);
						break;
					}
				//SBC Subtract with Borrow, Indexed Indirect, 2 Bytes, 5+ Cycles
				case 0xF1:
					{
						SubtractWithBorrowOperation(AddressingMode.IndirectY);
						break;
					}
				#endregion
				
				#region Branch Operations
				//BCC Branch if Carry is Clear, Relative, 2 Bytes, 2++ Cycles
				case 0x90:
					{
						BranchOperation(!CarryFlag);
						break;

					}
				//BCS Branch if Carry is Set, Relative, 2 Bytes, 2++ Cycles
				case 0xB0:
					{
						BranchOperation(CarryFlag);
						break;
					}
				//BEQ Branch if Zero is Set, Relative, 2 Bytes, 2++ Cycles
				case 0xF0:
					{
						BranchOperation(ZeroFlag);
						break;
					}

				// BMI Branch if Negative Set
				case 0x30:
					{
						BranchOperation(NegativeFlag);
						break;
					}
				//BNE Branch if Zero is Not Set, Relative, 2 Bytes, 2++ Cycles
				case 0xD0:
					{
						BranchOperation(!ZeroFlag);
						break;
					}
				// BPL Branch if Negative Clear, 2 Bytes, 2++ Cycles
				case 0x10:
					{
						BranchOperation(!NegativeFlag);
						break;
					}
				// BVC Branch if Overflow Clear, 2 Bytes, 2++ Cycles
				case 0x50:
					{
						BranchOperation(!OverflowFlag);
						break;
					}
				// BVS Branch if Overflow Set, 2 Bytes, 2++ Cycles
				case 0x70:
					{
						BranchOperation(OverflowFlag);
						break;
					}
				#endregion

				#region BitWise Comparison Operations
				//AND Compare Memory with Accumulator, Immediate, 2 Bytes, 2 Cycles
				case 0x29:
					{
						AndOperation(AddressingMode.Immediate);
						break;
					}
				//AND Compare Memory with Accumulator, Zero Page, 2 Bytes, 3 Cycles
				case 0x25:
					{
						AndOperation(AddressingMode.ZeroPage);
						break;
					}
				//AND Compare Memory with Accumulator, Zero PageX, 2 Bytes, 3 Cycles
				case 0x35:
					{
						AndOperation(AddressingMode.ZeroPageX);
						break;
					}
				//AND Compare Memory with Accumulator, Absolute,  3 Bytes, 4 Cycles
				case 0x2D:
					{
						AndOperation(AddressingMode.Absolute);
						break;
					}
				//AND Compare Memory with Accumulator, AbsolueteX 3 Bytes, 4+ Cycles
				case 0x3D:
					{
						AndOperation(AddressingMode.AbsoluteX);
						break;
					}
				//AND Compare Memory with Accumulator, AbsoluteY, 3 Bytes, 4+ Cycles
				case 0x39:
					{
						AndOperation(AddressingMode.AbsoluteY);
						break;
					}
				//AND Compare Memory with Accumulator, IndexedIndirect, 2 Bytes, 6 Cycles
				case 0x21:
					{
						AndOperation(AddressingMode.IndirectX);
						break;
					}
				//AND Compare Memory with Accumulator, IndirectIndexed, 2 Bytes, 5 Cycles
				case 0x31:
					{
						AndOperation(AddressingMode.IndirectY);
						break;
					}
				//BIT Compare Memory with Accumulator, Zero Page, 2 Bytes, 3 Cycles
				case 0x24:
					{
						BitOperation(AddressingMode.ZeroPage);
						break;
					}
				//BIT Compare Memory with Accumulator, Absolute, 2 Bytes, 4 Cycles
				case 0x2C:
					{
						BitOperation(AddressingMode.Absolute);
						break;
					}
				//EOR Exclusive OR Memory with Accumulator, Immediate, 2 Bytes, 2 Cycles
				case 0x49:
					{
						EorOperation(AddressingMode.Immediate);
						break;
					}
				//EOR Exclusive OR Memory with Accumulator, Zero Page, 2 Bytes, 3 Cycles
				case 0x45:
					{
						EorOperation(AddressingMode.ZeroPage);
						break;
					}
				//EOR Exclusive OR Memory with Accumulator, Zero Page X, 2 Bytes, 4 Cycles
				case 0x55:
					{
						EorOperation(AddressingMode.ZeroPageX);
						break;
					}
				//EOR Exclusive OR Memory with Accumulator, Absolute, 3 Bytes, 4 Cycles
				case 0x4D:
					{
						EorOperation(AddressingMode.Absolute);
						break;
					}
				//EOR Exclusive OR Memory with Accumulator, Absolute X, 3 Bytes, 4+ Cycles
				case 0x5D:
					{
						EorOperation(AddressingMode.AbsoluteX);
						break;
					}
				//EOR Exclusive OR Memory with Accumulator, Absolute Y, 3 Bytes, 4+ Cycles
				case 0x59:
					{
						EorOperation(AddressingMode.AbsoluteY);
						break;
					}
				//EOR Exclusive OR Memory with Accumulator, IndexedIndirect, 2 Bytes 6 Cycles
				case 0x41:
					{
						EorOperation(AddressingMode.IndirectX);
						break;
					}
				//EOR Exclusive OR Memory with Accumulator, IndirectIndexed, 2 Bytes 5 Cycles
				case 0x51:
					{
						EorOperation(AddressingMode.IndirectY);
						break;
					}
				//ORA Compare Memory with Accumulator, Immediate, 2 Bytes, 2 Cycles
				case 0x09:
					{
						OrOperation(AddressingMode.Immediate);
						break;
					}
				//ORA Compare Memory with Accumulator, Zero Page, 2 Bytes, 2 Cycles
				case 0x05:
					{
						OrOperation(AddressingMode.ZeroPage);
						break;
					}
				//ORA Compare Memory with Accumulator, Zero PageX, 2 Bytes, 4 Cycles
				case 0x15:
					{
						OrOperation(AddressingMode.ZeroPageX);
						break;
					}
				//ORA Compare Memory with Accumulator, Absolute,  3 Bytes, 4 Cycles
				case 0x0D:
					{
						OrOperation(AddressingMode.Absolute);
						break;
					}
				//ORA Compare Memory with Accumulator, AbsolueteX 3 Bytes, 4+ Cycles
				case 0x1D:
					{
						OrOperation(AddressingMode.AbsoluteX);
						break;
					}
				//ORA Compare Memory with Accumulator, AbsoluteY, 3 Bytes, 4+ Cycles
				case 0x19:
					{
						OrOperation(AddressingMode.AbsoluteY);
						break;
					}
				//ORA Compare Memory with Accumulator, IndexedIndirect, 2 Bytes, 6 Cycles
				case 0x01:
					{
						OrOperation(AddressingMode.IndirectX);
						break;
					}
				//ORA Compare Memory with Accumulator, IndirectIndexed, 2 Bytes, 5 Cycles
				case 0x11:
					{
						OrOperation(AddressingMode.IndirectY);
						break;
					}
				#endregion

				#region Clear Flag Operations
				//CLC Clear Carry Flag, Implied, 1 Byte, 2 Cycles
				case 0x18:
					{
						CarryFlag = false;
					    IncrementCycleCount();
						break;
					}
				//CLD Clear Decimal Flag, Implied, 1 Byte, 2 Cycles
				case 0xD8:
					{
						DecimalFlag = false;
                        IncrementCycleCount();
						break;

					}
				//CLI Clear Interrupt Flag, Implied, 1 Byte, 2 Cycles
				case 0x58:
					{
						DisableInterruptFlag = false;
                        IncrementCycleCount();
						break;

					}
				//CLV Clear Overflow Flag, Implied, 1 Byte, 2 Cycles
				case 0xB8:
					{
						OverflowFlag = false;
                        IncrementCycleCount();
						break;
					}

				#endregion

				#region Compare Operations
				//CMP Compare Accumulator with Memory, Immediate, 2 Bytes, 2 Cycles
				case 0xC9:
					{
						CompareOperation(AddressingMode.Immediate, Accumulator);
						break;
					}
				//CMP Compare Accumulator with Memory, Zero Page, 2 Bytes, 3 Cycles
				case 0xC5:
					{
						CompareOperation(AddressingMode.ZeroPage, Accumulator);
						break;
					}
				//CMP Compare Accumulator with Memory, Zero Page x, 2 Bytes, 4 Cycles
				case 0xD5:
					{
						CompareOperation(AddressingMode.ZeroPageX, Accumulator);
						break;
					}
				//CMP Compare Accumulator with Memory, Absolute, 3 Bytes, 4 Cycles
				case 0xCD:
					{
						CompareOperation(AddressingMode.Absolute, Accumulator);
						break;
					}
				//CMP Compare Accumulator with Memory, Absolute X, 2 Bytes, 4 Cycles
				case 0xDD:
					{
						CompareOperation(AddressingMode.AbsoluteX, Accumulator);
						break;
					}
				//CMP Compare Accumulator with Memory, Absolute Y, 2 Bytes, 4 Cycles
				case 0xD9:
					{
						CompareOperation(AddressingMode.AbsoluteY, Accumulator);
						break;
					}
				//CMP Compare Accumulator with Memory, Indirect X, 2 Bytes, 6 Cycles
				case 0xC1:
					{
						CompareOperation(AddressingMode.IndirectX, Accumulator);
						break;
					}
				//CMP Compare Accumulator with Memory, Indirect Y, 2 Bytes, 5 Cycles
				case 0xD1:
					{
						CompareOperation(AddressingMode.IndirectY, Accumulator);
						break;
					}
				//CPX Compare Accumulator with X Register, Immediate, 2 Bytes, 2 Cycles
				case 0xE0:
					{
						CompareOperation(AddressingMode.Immediate, XRegister);
						break;
					}
				//CPX Compare Accumulator with X Register, Zero Page, 2 Bytes, 3 Cycles
				case 0xE4:
					{
						CompareOperation(AddressingMode.ZeroPage, XRegister);
						break;
					}
				//CPX Compare Accumulator with X Register, Absolute, 3 Bytes, 4 Cycles
				case 0xEC:
					{
						CompareOperation(AddressingMode.Absolute, XRegister);
						break;
					}
				//CPY Compare Accumulator with Y Register, Immediate, 2 Bytes, 2 Cycles
				case 0xC0:
					{
						CompareOperation(AddressingMode.Immediate, YRegister);
						break;
					}
				//CPY Compare Accumulator with Y Register, Zero Page, 2 Bytes, 3 Cycles
				case 0xC4:
					{
						CompareOperation(AddressingMode.ZeroPage, YRegister);
						break;
					}
				//CPY Compare Accumulator with Y Register, Absolute, 3 Bytes, 4 Cycles
				case 0xCC:
					{
						CompareOperation(AddressingMode.Absolute, YRegister);
						break;
					}
				#endregion

				#region Increment/Decrement Operations
				//DEC Decrement Memory by One, Zero Page, 2 Bytes, 5 Cycles
				case 0xC6:
					{
						ChangeMemoryByOne(AddressingMode.ZeroPage, true);
						break;
					}
				//DEC Decrement Memory by One, Zero Page X, 2 Bytes, 6 Cycles
				case 0xD6:
					{
						ChangeMemoryByOne(AddressingMode.ZeroPageX, true);
						break;
					}
				//DEC Decrement Memory by One, Absolute, 3 Bytes, 6 Cycles
				case 0xCE:
					{
						ChangeMemoryByOne(AddressingMode.Absolute, true);
						break;
					}
				//DEC Decrement Memory by One, Absolute X, 3 Bytes, 7 Cycles
				case 0xDE:
					{
						ChangeMemoryByOne(AddressingMode.AbsoluteX, true);
                        IncrementCycleCount();
						break;
					}
				//DEX Decrement X Register by One, Implied, 1 Bytes, 2 Cycles
				case 0xCA:
					{
						ChangeRegisterByOne(true, true);
						break;
					}
				//DEY Decrement Y Register by One, Implied, 1 Bytes, 2 Cycles
				case 0x88:
					{
						ChangeRegisterByOne(false, true);
						break;
					}
				//INC Increment Memory by One, Zero Page, 2 Bytes, 5 Cycles
				case 0xE6:
					{
						ChangeMemoryByOne(AddressingMode.ZeroPage, false);
						break;
					}
				//INC Increment Memory by One, Zero Page X, 2 Bytes, 6 Cycles
				case 0xF6:
					{
						ChangeMemoryByOne(AddressingMode.ZeroPageX, false);
						break;
					}
				//INC Increment Memory by One, Absolute, 3 Bytes, 6 Cycles
				case 0xEE:
					{
						ChangeMemoryByOne(AddressingMode.Absolute, false);
						break;
					}
				//INC Increment Memory by One, Absolute X, 3 Bytes, 7 Cycles
				case 0xFE:
					{
						ChangeMemoryByOne(AddressingMode.AbsoluteX, false);
                        IncrementCycleCount();
						break;
					}
				//INX Increment X Register by One, Implied, 1 Bytes, 2 Cycles
				case 0xE8:
					{
						ChangeRegisterByOne(true, false);
						break;
					}
				//INY Increment Y Register by One, Implied, 1 Bytes, 2 Cycles
				case 0xC8:
					{
						ChangeRegisterByOne(false, false);
						break;
					}
				#endregion

				#region GOTO and GOSUB Operations
				//JMP Jump to New Location, Absolute 3 Bytes, 3 Cycles
				case 0x4C:
					{
						ProgramCounter = GetAddressByAddressingMode(AddressingMode.Absolute);
						break;
					}
				//JMP Jump to New Location, Indirect 3 Bytes, 5 Cycles
				case 0x6C:
                    {
                        ProgramCounter = GetAddressByAddressingMode(AddressingMode.Absolute);

                        if ((ProgramCounter & 0xFF) == 0xFF)
                        {
                            //Get the first half of the address
                            int address = ReadMemoryValue(ProgramCounter);

                            //Get the second half of the address, due to the issue with page boundary it reads from the wrong location!
                            address += 256 * ReadMemoryValue(ProgramCounter - 255);
                            ProgramCounter = address;
                        }
                        else
                        {
                            ProgramCounter = GetAddressByAddressingMode(AddressingMode.Absolute);
                        }

                        break;
                    }
				//JSR Jump to SubRoutine, Absolute, 3 Bytes, 6 Cycles
				case 0x20:
					{
						JumpToSubRoutineOperation();
						break;
					}
				//BRK Simulate IRQ, Implied, 1 Byte, 7 Cycles
				case 0x00:
					{
						BreakOperation(true, 0xFFFE);
						break;
					}
				//RTI Return From Interrupt, Implied, 1 Byte, 6 Cycles
				case 0x40:
					{
						ReturnFromInterruptOperation();
						break;
					}
				//RTS Return From Subroutine, Implied, 1 Byte, 6 Cycles
				case 0x60:
					{
						ReturnFromSubRoutineOperation();
                        break;
					}
				#endregion

				#region Load Value From Memory Operations
				//LDA Load Accumulator with Memory, Immediate, 2 Bytes, 2 Cycles
				case 0xA9:
					{
						Accumulator =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.Immediate));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);
						break;
					}
				//LDA Load Accumulator with Memory, Zero Page, 2 Bytes, 3 Cycles
				case 0xA5:
					{
						Accumulator =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPage));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);
						break;
					}
				//LDA Load Accumulator with Memory, Zero Page X, 2 Bytes, 4 Cycles
				case 0xB5:
					{
						Accumulator =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPageX));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);
						break;
					}
				//LDA Load Accumulator with Memory, Absolute, 3 Bytes, 4 Cycles
				case 0xAD:
					{
						Accumulator =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.Absolute));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);
						break;
					}
				//LDA Load Accumulator with Memory, Absolute X, 3 Bytes, 4+ Cycles
				case 0xBD:
					{
						Accumulator =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.AbsoluteX));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);
						break;
					}
				//LDA Load Accumulator with Memory, Absolute Y, 3 Bytes, 4+ Cycles
				case 0xB9:
					{
						Accumulator =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.AbsoluteY));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);
						break;
					}
				//LDA Load Accumulator with Memory, Index Indirect, 2 Bytes, 6 Cycles
				case 0xA1:
					{
						Accumulator =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.IndirectX));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);
						break;
					}
				//LDA Load Accumulator with Memory, Indirect Index, 2 Bytes, 5+ Cycles
				case 0xB1:
					{
						Accumulator =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.IndirectY));
						SetZeroFlag(Accumulator);
						SetNegativeFlag(Accumulator);
						break;
					}
				//LDX Load X with memory, Immediate, 2 Bytes, 2 Cycles
				case 0xA2:
					{
						XRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.Immediate));
						SetZeroFlag(XRegister);
						SetNegativeFlag(XRegister);
						break;
					}
				//LDX Load X with memory, Zero Page, 2 Bytes, 3 Cycles
				case 0xA6:
					{
						XRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPage));
						SetZeroFlag(XRegister);
						SetNegativeFlag(XRegister);
						break;
					}
				//LDX Load X with memory, Zero Page Y, 2 Bytes, 4 Cycles
				case 0xB6:
					{
						XRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPageY));
						SetZeroFlag(XRegister);
						SetNegativeFlag(XRegister);
						break;
					}
				//LDX Load X with memory, Absolute, 3 Bytes, 4 Cycles
				case 0xAE:
					{
						XRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.Absolute));
						SetZeroFlag(XRegister);
						SetNegativeFlag(XRegister);
						break;
					}
				//LDX Load X with memory, Absolute Y, 3 Bytes, 4+ Cycles
				case 0xBE:
					{
						XRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.AbsoluteY));
						SetZeroFlag(XRegister);
						SetNegativeFlag(XRegister);
						break;
					}
				//LDY Load Y with memory, Immediate, 2 Bytes, 2 Cycles
				case 0xA0:
					{
						YRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.Immediate));
						SetZeroFlag(YRegister);
						SetNegativeFlag(YRegister);
						break;
					}
				//LDY Load Y with memory, Zero Page, 2 Bytes, 3 Cycles
				case 0xA4:
					{
						YRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPage));
						SetZeroFlag(YRegister);
						SetNegativeFlag(YRegister);
						break;
					}
				//LDY Load Y with memory, Zero Page X, 2 Bytes, 4 Cycles
				case 0xB4:
					{
						YRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPageX));
						SetZeroFlag(YRegister);
						SetNegativeFlag(YRegister);
						break;
					}
				//LDY Load Y with memory, Absolute, 3 Bytes, 4 Cycles
				case 0xAC:
					{
						YRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.Absolute));
						SetZeroFlag(YRegister);
						SetNegativeFlag(YRegister);
						break;
					}
				//LDY Load Y with memory, Absolue X, 3 Bytes, 4+ Cycles
				case 0xBC:
					{
						YRegister =ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.AbsoluteX));
						SetZeroFlag(YRegister);
						SetNegativeFlag(YRegister);
						break;
					}
				#endregion

				#region Push/Pull Stack
				//PHA Push Accumulator onto Stack, Implied, 1 Byte, 3 Cycles
				case 0x48:
			        {
                        ReadMemoryValue(ProgramCounter + 1);

                        PokeStack((byte)Accumulator);
					    StackPointer--;
			            IncrementCycleCount();
						break;

					}
				//PHP Push Flags onto Stack, Implied, 1 Byte, 3 Cycles
				case 0x08:
			        {
                        ReadMemoryValue(ProgramCounter + 1);

                        PushFlagsOperation();
						StackPointer--;
						IncrementCycleCount();
						break;
					}
				//PLA Pull Accumulator from Stack, Implied, 1 Byte, 4 Cycles
				case 0x68:
			        {
                        ReadMemoryValue(ProgramCounter + 1);
						StackPointer++;
                        IncrementCycleCount();

						Accumulator = PeekStack();
						SetNegativeFlag(Accumulator);
						SetZeroFlag(Accumulator);

                        IncrementCycleCount();
						break;
					}
				//PLP Pull Flags from Stack, Implied, 1 Byte, 4 Cycles
				case 0x28:
					{
                        ReadMemoryValue(ProgramCounter + 1);

						StackPointer++;
                        IncrementCycleCount();

						PullFlagsOperation();
                        
                        IncrementCycleCount(); 
						break;
					}
				//TSX Transfer Stack Pointer to X Register, 1 Bytes, 2 Cycles
				case 0xBA:
					{
						XRegister = StackPointer;

						SetNegativeFlag(XRegister);
						SetZeroFlag(XRegister);
					    IncrementCycleCount();
						break;
					}
				//TXS Transfer X Register to Stack Pointer, 1 Bytes, 2 Cycles
				case 0x9A:
					{
						StackPointer = (byte)XRegister;
					    IncrementCycleCount();
						break;
					}
				#endregion

				#region Set Flag Operations
				//SEC Set Carry, Implied, 1 Bytes, 2 Cycles
				case 0x38:
					{
						CarryFlag = true;
                        IncrementCycleCount();
						break;
					}
				//SED Set Interrupt, Implied, 1 Bytes, 2 Cycles
				case 0xF8:
					{
						DecimalFlag = true;
                        IncrementCycleCount();
						break;
					}
				//SEI Set Interrupt, Implied, 1 Bytes, 2 Cycles
				case 0x78:
					{
						DisableInterruptFlag = true;
					    IncrementCycleCount();
                        break;
					}
				#endregion

				#region Shift/Rotate Operations
				//ASL Shift Left 1 Bit Memory or Accumulator, Accumulator, 1 Bytes, 2 Cycles
				case 0x0A:
					{
						AslOperation(AddressingMode.Accumulator);
						break;
					}
				//ASL Shift Left 1 Bit Memory or Accumulator, Zero Page, 2 Bytes, 5 Cycles
				case 0x06:
					{
						AslOperation(AddressingMode.ZeroPage);
						break;
					}
				//ASL Shift Left 1 Bit Memory or Accumulator, Zero PageX, 2 Bytes, 6 Cycles
				case 0x16:
					{
						AslOperation(AddressingMode.ZeroPageX);
						break;
					}
				//ASL Shift Left 1 Bit Memory or Accumulator, Absolute, 3 Bytes, 6 Cycles
				case 0x0E:
					{
						AslOperation(AddressingMode.Absolute);
						break;
					}
				//ASL Shift Left 1 Bit Memory or Accumulator, AbsoluteX, 3 Bytes, 7 Cycles
				case 0x1E:
					{
						AslOperation(AddressingMode.AbsoluteX);
                        IncrementCycleCount();
						break;
					}
				//LSR Shift Left 1 Bit Memory or Accumulator, Accumulator, 1 Bytes, 2 Cycles
				case 0x4A:
					{
						LsrOperation(AddressingMode.Accumulator);
						break;
					}
				//LSR Shift Left 1 Bit Memory or Accumulator, Zero Page, 2 Bytes, 5 Cycles
				case 0x46:
					{
						LsrOperation(AddressingMode.ZeroPage);
						break;
					}
				//LSR Shift Left 1 Bit Memory or Accumulator, Zero PageX, 2 Bytes, 6 Cycles
				case 0x56:
					{
						LsrOperation(AddressingMode.ZeroPageX);
						break;
					}
				//LSR Shift Left 1 Bit Memory or Accumulator, Absolute, 3 Bytes, 6 Cycles
				case 0x4E:
					{
						LsrOperation(AddressingMode.Absolute);
						break;
					}
				//LSR Shift Left 1 Bit Memory or Accumulator, AbsoluteX, 3 Bytes, 7 Cycles
				case 0x5E:
					{
						LsrOperation(AddressingMode.AbsoluteX);
                        IncrementCycleCount();
						break;
					}
				//ROL Rotate Left 1 Bit Memory or Accumulator, Accumulator, 1 Bytes, 2 Cycles
				case 0x2A:
					{
						RolOperation(AddressingMode.Accumulator);
						break;
					}
				//ROL Rotate Left 1 Bit Memory or Accumulator, Zero Page, 2 Bytes, 5 Cycles
				case 0x26:
					{
						RolOperation(AddressingMode.ZeroPage);
						break;
					}
				//ROL Rotate Left 1 Bit Memory or Accumulator, Zero PageX, 2 Bytes, 6 Cycles
				case 0x36:
					{
						RolOperation(AddressingMode.ZeroPageX);
						break;
					}
				//ROL Rotate Left 1 Bit Memory or Accumulator, Absolute, 3 Bytes, 6 Cycles
				case 0x2E:
					{
						RolOperation(AddressingMode.Absolute);
						break;
					}
				//ROL Rotate Left 1 Bit Memory or Accumulator, AbsoluteX, 3 Bytes, 7 Cycles
				case 0x3E:
					{
						RolOperation(AddressingMode.AbsoluteX);
                        IncrementCycleCount();
						break;
					}
				//ROR Rotate Right 1 Bit Memory or Accumulator, Accumulator, 1 Bytes, 2 Cycles
				case 0x6A:
					{
						RorOperation(AddressingMode.Accumulator);
						break;
					}
				//ROR Rotate Right 1 Bit Memory or Accumulator, Zero Page, 2 Bytes, 5 Cycles
				case 0x66:
					{
						RorOperation(AddressingMode.ZeroPage);
						break;
					}
				//ROR Rotate Right 1 Bit Memory or Accumulator, Zero PageX, 2 Bytes, 6 Cycles
				case 0x76:
					{
						RorOperation(AddressingMode.ZeroPageX);
						break;
					}
				//ROR Rotate Right 1 Bit Memory or Accumulator, Absolute, 3 Bytes, 6 Cycles
				case 0x6E:
					{
						RorOperation(AddressingMode.Absolute);
						break;
					}
				//ROR Rotate Right 1 Bit Memory or Accumulator, AbsoluteX, 3 Bytes, 7 Cycles
				case 0x7E:
					{
						RorOperation(AddressingMode.AbsoluteX);
					    IncrementCycleCount();
						break;
					}
				#endregion

				#region Store Value In Memory Operations
				//STA Store Accumulator In Memory, Zero Page, 2 Bytes, 3 Cycles
				case 0x85:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPage), (byte)Accumulator);
						break;
					}
				//STA Store Accumulator In Memory, Zero Page X, 2 Bytes, 4 Cycles
				case 0x95:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPageX), (byte)Accumulator);
						break;
					}
				//STA Store Accumulator In Memory, Absolute, 3 Bytes, 4 Cycles
				case 0x8D:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.Absolute), (byte)Accumulator);
						break;
					}
				//STA Store Accumulator In Memory, Absolute X, 3 Bytes, 5 Cycles
				case 0x9D:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.AbsoluteX), (byte)Accumulator);
					    IncrementCycleCount();
						break;
					}
				//STA Store Accumulator In Memory, Absolute Y, 3 Bytes, 5 Cycles
				case 0x99:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.AbsoluteY), (byte)Accumulator);
					    IncrementCycleCount();
						break;
					}
				//STA Store Accumulator In Memory, Indexed Indirect, 2 Bytes, 6 Cycles
				case 0x81:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.IndirectX), (byte)Accumulator);
						break;
					}
				//STA Store Accumulator In Memory, Indirect Indexed, 2 Bytes, 6 Cycles
				case 0x91:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.IndirectY), (byte)Accumulator);
					    IncrementCycleCount();
						break;
					}
				//STX Store Index X, Zero Page, 2 Bytes, 3 Cycles
				case 0x86:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPage), (byte)XRegister);
						break;
					}
				//STX Store Index X, Zero Page Y, 2 Bytes, 4 Cycles
				case 0x96:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPageY), (byte)XRegister);
						break;
					}
				//STX Store Index X, Absolute, 3 Bytes, 4 Cycles
				case 0x8E:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.Absolute), (byte)XRegister);
						break;
					}
				//STY Store Index Y, Zero Page, 2 Bytes, 3 Cycles
				case 0x84:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPage), (byte)YRegister);
						break;
					}
				//STY Store Index Y, Zero Page X, 2 Bytes, 4 Cycles
				case 0x94:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.ZeroPageX), (byte)YRegister);
						break;
					}
				//STY Store Index Y, Absolute, 2 Bytes, 4 Cycles
				case 0x8C:
					{
						WriteMemoryValue(GetAddressByAddressingMode(AddressingMode.Absolute), (byte)YRegister);
						break;
					}
				#endregion

				#region Transfer Operations
				//TAX Transfer Accumulator to X Register, Implied, 1 Bytes, 2 Cycles
				case 0xAA:
					{
                        IncrementCycleCount();
						XRegister = Accumulator;

						SetNegativeFlag(XRegister);
						SetZeroFlag(XRegister);
						break;
					}
				//TAY Transfer Accumulator to Y Register, 1 Bytes, 2 Cycles
				case 0xA8:
					{
                        IncrementCycleCount();
						YRegister = Accumulator;

						SetNegativeFlag(YRegister);
						SetZeroFlag(YRegister);
						break;
					}
				//TXA Transfer X Register to Accumulator, Implied, 1 Bytes, 2 Cycles
				case 0x8A:
					{
                        IncrementCycleCount();
						Accumulator = XRegister;

						SetNegativeFlag(Accumulator);
						SetZeroFlag(Accumulator);
						break;
					}
				//TYA Transfer Y Register to Accumulator, Implied, 1 Bytes, 2 Cycles
				case 0x98:
					{
                        IncrementCycleCount();
						Accumulator = YRegister;

						SetNegativeFlag(Accumulator);
						SetZeroFlag(Accumulator);
						break;
					}
				#endregion
				
				//NOP Operation, Implied, 1 Byte, 2 Cycles
				case 0xEA:
			    {
			        IncrementCycleCount();
						break;
				}

				default:
					throw new NotSupportedException(string.Format("The OpCode {0} is not supported", CurrentOpCode));
			}
		}

		/// <summary>
		/// Sets the IsSignNegative register
		/// </summary>
		/// <param name="value"></param>
		protected void SetNegativeFlag(int value)
		{
			//on the 6502, any value greater than 127 is negative. 128 = 1000000 in Binary. the 8th bit is set, therefore the number is a negative number.
			NegativeFlag = value > 127;
		}

		/// <summary>
		/// Sets the IsResultZero register
		/// </summary>
		/// <param name="value"></param>
		protected void SetZeroFlag(int value)
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
		protected int GetAddressByAddressingMode(AddressingMode addressingMode)
		{
			int address;
		    int highByte;
			switch (addressingMode)
			{
				case (AddressingMode.Absolute):
			    {
			        return (ReadMemoryValue(ProgramCounter++) | (ReadMemoryValue(ProgramCounter++) << 8));
				}
				case AddressingMode.AbsoluteX:
					{
						//Get the low half of the address
						address =ReadMemoryValue(ProgramCounter++);

                        //Get the high byte
						highByte = ReadMemoryValue(ProgramCounter++);

                        //We crossed a page boundry, so an extra read has occurred.
                        //However, if this is an ASL, LSR, DEC, INC, ROR, ROL or STA operation, we do not decrease it by 1.
					    if (address + XRegister > 0xFF)
					    {
                            switch (CurrentOpCode)
                            {
                                case 0x1E:
                                case 0xDE:
                                case 0xFE:
                                case 0x5E:
                                case 0x3E:
                                case 0x7E:
                                case 0x9D:
                                {
                                    //This is a Read Fetch Write Operation, so we don't make the extra read.
                                    return ((highByte << 8 | address) + XRegister) & 0xFFFF;
                                }
                                default:
                                {
                                    ReadMemoryValue((((highByte << 8 | address) + XRegister) - 0xFF) & 0xFFFF);
                                    break;
                                }
                            }
					    }

					    return ((highByte << 8 | address) + XRegister) & 0xFFFF;
					}
				case AddressingMode.AbsoluteY:
					{
                        //Get the low half of the address
                        address = ReadMemoryValue(ProgramCounter++);

                        //Get the high byte
                        highByte = ReadMemoryValue(ProgramCounter++);

                        //We crossed a page boundry, so decrease the number of cycles by 1 if the operation is not STA
					    if (address + YRegister > 0xFF && CurrentOpCode != 0x99)
					    {
                            ReadMemoryValue((((highByte << 8 | address) + YRegister) - 0xFF) & 0xFFFF);
					    }

                        //Bitshift the high byte into place, AND with FFFF to handle wrapping.
                        return ((highByte << 8 | address) + YRegister) & 0xFFFF;
					}
				case AddressingMode.Immediate:
					{
						return ProgramCounter++;
					}
				case AddressingMode.IndirectX:
					{
						//Get the location of the address to retrieve
						address = ReadMemoryValue(ProgramCounter++);
					    ReadMemoryValue(address);

					    address += XRegister;
						
						//Now get the final Address. The is not a zero page address either.
						var finalAddress = ReadMemoryValue((address & 0xFF)) | (ReadMemoryValue((address + 1) & 0xFF) << 8);
						return finalAddress;
					}
				case AddressingMode.IndirectY:
					{
						address = ReadMemoryValue(ProgramCounter++);

					    var finalAddress = ReadMemoryValue(address) + (ReadMemoryValue((address + 1) & 0xFF) << 8);

                        if ((finalAddress & 0xFF) + YRegister > 0xFF && CurrentOpCode != 0x91)
                        {
                            ReadMemoryValue((finalAddress + YRegister - 0xFF) & 0xFFFF);
                        }
						
						return (finalAddress + YRegister) & 0xFFFF;
					}
				case AddressingMode.Relative:
					{
						return ProgramCounter;
					}
				case (AddressingMode.ZeroPage):
					{
						address =ReadMemoryValue(ProgramCounter++);
						return address;
					}
				case (AddressingMode.ZeroPageX):
					{
						address = ReadMemoryValue(ProgramCounter++);
					    ReadMemoryValue(address);

						address += XRegister;
					    address &= 0xFF;

						//This address wraps if its greater than 0xFF
						if (address > 0xFF)
						{
							address -= 0x100;
							return address;
						}

						return address;
					}
				case (AddressingMode.ZeroPageY):
					{
						address =ReadMemoryValue(ProgramCounter++);
                        ReadMemoryValue(address);

                        address += YRegister;
                        address &= 0xFF;

						return address;
					}
				default:
					throw new InvalidOperationException(string.Format("The Address Mode '{0}' does not require an address", addressingMode));
			}
		}
	
		/// <summary>
		/// Moves the ProgramCounter in a given direction based on the value inputted
		/// 
		/// </summary>
		private void MoveProgramCounterByRelativeValue(byte valueToMove)
		{
			var movement = valueToMove > 127 ? (valueToMove - 255) : valueToMove;

			var newProgramCounter = ProgramCounter + movement;
	
			//This makes sure that we always land on the correct spot for a positive number
			if (movement >= 0)
				newProgramCounter++;

            //We Crossed a Page Boundary. So we increment the cycle counter by one. The +1 is because we always check from the end of the instruction not the beginning
            if (((ProgramCounter + 1 ^ newProgramCounter) & 0xff00) != 0x0000)
            {
                IncrementCycleCount();
            }

            ProgramCounter = newProgramCounter;
		    ReadMemoryValue(ProgramCounter);
		}

		/// <summary>
		/// Returns a the value from the stack without changing the position of the stack pointer
		/// </summary>
		
		/// <returns>The value at the current Stack Pointer</returns>
		private byte PeekStack()
		{
			//The stack lives at 0x100-0x1FF, but the value is only a byte so it needs to be translated
			return Memory[StackPointer + 0x100];
		}

		/// <summary>
		/// Write a value directly to the stack without modifying the Stack Pointer
		/// </summary>
		/// <param name="value">The value to be written to the stack</param>
		private void PokeStack(byte value)
		{
			//The stack lives at 0x100-0x1FF, but the value is only a byte so it needs to be translated
			Memory[StackPointer + 0x100] = value;
		}

		/// <summary>
		/// Coverts the Flags into its byte representation.
		/// </summary>
		/// <param name="setBreak">Determines if the break flag should be set during conversion. IRQ does not set the flag on the stack, but PHP and BRK do</param>
		/// <returns></returns>
		private byte ConvertFlagsToByte(bool setBreak)
		{
			return (byte)((CarryFlag ? 0x01 : 0) + (ZeroFlag ? 0x02 : 0) + (DisableInterruptFlag ? 0x04 : 0) +
						 (DecimalFlag ? 8 : 0) + (setBreak ? 0x10 : 0) + 0x20 + (OverflowFlag ? 0x40 : 0) + (NegativeFlag ? 0x80 : 0));
		}

        [Conditional("DEBUG")]
		private void SetDisassembly()
        {
            if (!_logger.IsDebugEnabled)
                return;

			var addressMode = GetAddressingMode();
			
			var currentProgramCounter = ProgramCounter;

			currentProgramCounter = WrapProgramCounter(++currentProgramCounter);
			int? address1 = Memory[currentProgramCounter];
		
			currentProgramCounter = WrapProgramCounter(++currentProgramCounter);
			int? address2 = Memory[currentProgramCounter];

			string disassembledStep = string.Empty;

			switch (addressMode)
			{
				case AddressingMode.Absolute:
					{
						disassembledStep = string.Format("${0}{1}", address2.Value.ToString("X").PadLeft(2, '0'), address1.Value.ToString("X").PadLeft(2, '0'));
						break;
					}
				case AddressingMode.AbsoluteX:
					{
						disassembledStep = string.Format("${0}{1},X", address2.Value.ToString("X").PadLeft(2, '0'), address1.Value.ToString("X").PadLeft(2, '0'));
						break;
					}
				case AddressingMode.AbsoluteY:
					{
						disassembledStep = string.Format("${0}{1},Y", address2.Value.ToString("X").PadLeft(2, '0'), address1.Value.ToString("X").PadLeft(2, '0'));
						break;
					}
				case AddressingMode.Accumulator:
					{
						address1 = null;
						address2 = null;

						disassembledStep = "A";
						break;
					}
				case AddressingMode.Immediate:
					{
						disassembledStep = string.Format("#${0}", address1.Value.ToString("X").PadLeft(4, '0'));
						address2 = null;
						break;
					}
				case AddressingMode.Implied:
					{
						address1 = null;
						address2 = null;
						break;
					}
				case AddressingMode.Indirect:
					{
						disassembledStep = string.Format("(${0}{1})", address2.Value.ToString("X").PadLeft(2, '0'), address1.Value.ToString("X").PadLeft(2, '0'));
						break;
					}
				case AddressingMode.IndirectX:
					{
						address2 = null;

						disassembledStep = string.Format("(${0},X)", address1.Value.ToString("X").PadLeft(2, '0'));
						break;
					}
				case AddressingMode.IndirectY:
					{
						address2 = null;

						disassembledStep = string.Format("(${0}),Y", address1.Value.ToString("X").PadLeft(2, '0'));
						break;
					}
				case AddressingMode.Relative:
			    {
                    var valueToMove = (byte)address1.Value;

                    var movement = valueToMove > 127 ? (valueToMove - 255) : valueToMove;

                    var newProgramCounter = ProgramCounter + movement;

                    //This makes sure that we always land on the correct spot for a positive number
                    if (movement >= 0)
                        newProgramCounter++;

                    var stringAddress = ProgramCounter.ToString("X").PadLeft(4, '0');

                    address1 = int.Parse(stringAddress.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                    address2 = int.Parse(stringAddress.Substring(2, 2), NumberStyles.AllowHexSpecifier);

                    disassembledStep = string.Format("${0}", newProgramCounter.ToString("X").PadLeft(4, '0'));

                    break;
				}
				case AddressingMode.ZeroPage:
					{
						address2 = null;

						disassembledStep = string.Format("${0}", address1.Value.ToString("X").PadLeft(2, '0'));
						break;
					}
				case AddressingMode.ZeroPageX:
					{
						address2 = null;

						disassembledStep = string.Format("${0},X", address1.Value.ToString("X").PadLeft(2, '0'));
						break;
					}
				case AddressingMode.ZeroPageY:
					{
						address2 = null;

						disassembledStep = string.Format("${0},Y", address1.Value.ToString("X").PadLeft(4, '0'));
						break;
					}
				default:
					throw new InvalidEnumArgumentException("Invalid Addressing Mode");

			}


			CurrentDisassembly = new Disassembly
				                     {
					                     HighAddress = address2.HasValue ? address2.Value.ToString("X").PadLeft(2,'0') : string.Empty,
					                     LowAddress = address1.HasValue ? address1.Value.ToString("X").PadLeft(2,'0') : string.Empty,
					                     OpCodeString = CurrentOpCode.ConvertOpCodeIntoString(),
					                     DisassemblyOutput = disassembledStep
				                     };

			_logger.Debug("{0} : {1}{2}{3} {4} {5} A: {6} X: {7} Y: {8} SP {9} N: {10} V: {11} B: {12} D: {13} I: {14} Z: {15} C: {16}",
							 ProgramCounter.ToString("X").PadLeft(4, '0'),
							 CurrentOpCode.ToString("X").PadLeft(2, '0'),
							 CurrentDisassembly.LowAddress,
							 CurrentDisassembly.HighAddress,
							 
							 CurrentDisassembly.OpCodeString,
							 CurrentDisassembly.DisassemblyOutput.PadRight(10, ' '),
			                 
							 Accumulator.ToString("X").PadLeft(3, '0'),
			                 XRegister.ToString("X").PadLeft(3, '0'),
			                 YRegister.ToString("X").PadLeft(3, '0'),
			                 StackPointer.ToString("X").PadLeft(3, '0'),
			                 Convert.ToInt16(NegativeFlag),
			                 Convert.ToInt16(OverflowFlag),
			                 0,
			                 Convert.ToInt16(DecimalFlag),
			                 Convert.ToInt16(DisableInterruptFlag),
			                 Convert.ToInt16(ZeroFlag),
			                 Convert.ToInt16(CarryFlag));
		}

		private int WrapProgramCounter(int value)
		{
			return value & 0xFFFF;
		}

		private AddressingMode GetAddressingMode()
		{
			switch (CurrentOpCode)
			{
				case 0x0D: //ORA
				case 0x2D: //AND
				case 0x4D: //EOR
				case 0x6D: //ADC
				case 0x8D: //STA
				case 0xAD: //LDA
				case 0xCD: //CMP
				case 0xED: //SBC
				case 0x0E: //ASL
				case 0x2E: //ROL
				case 0x4E: //LSR
				case 0x6E: //ROR
				case 0x8E: //SDX
				case 0xAE: //LDX
				case 0xCE: //DEC
				case 0xEE: //INC
				case 0x2C: //Bit
				case 0x4C: //JMP
				case 0x8C: //STY
				case 0xAC: //LDY
				case 0xCC: //CPY
				case 0xEC: //CPX
				case 0x20: //JSR
					{
						return AddressingMode.Absolute;
					}
				case 0x1D: //ORA
				case 0x3D: //AND
				case 0x5D: //EOR
				case 0x7D: //ADC
				case 0x9D: //STA
				case 0xBD: //LDA
				case 0xDD: //CMP
				case 0xFD: //SBC
				case 0xBC: //LDY
				case 0xFE: //INC
				case 0x1E: //ASL
				case 0x3E: //ROL
				case 0x5E: //LSR
				case 0x7E: //ROR
					{
						return AddressingMode.AbsoluteX;
					}
				case 0x19: //ORA
				case 0x39: //AND
				case 0x59: //EOR
				case 0x79: //ADC
				case 0x99: //STA
				case 0xB9: //LDA
				case 0xD9: //CMP
				case 0xF9: //SBC
				case 0xBE: //LDX
					{
						return AddressingMode.AbsoluteY;
					}
				case 0x0A: //ASL
				case 0x4A: //LSR
				case 0x2A: //ROL
				case 0x6A: //ROR
					{
						return AddressingMode.Accumulator;
					}

				case 0x09: //ORA
				case 0x29: //AND
				case 0x49: //EOR
				case 0x69: //ADC
				case 0xA0: //LDY
				case 0xC0: //CPY
				case 0xE0: //CMP
				case 0xA2: //LDX
				case 0xA9: //LDA
				case 0xC9: //CMP
				case 0xE9: //SBC
					{
						return AddressingMode.Immediate;
					}
				case 0x00: //BRK
				case 0x18: //CLC
				case 0xD8: //CLD
				case 0x58: //CLI
				case 0xB8: //CLV
				case 0xDE: //DEC
				case 0xCA: //DEX
				case 0x88: //DEY
				case 0xE8: //INX
				case 0xC8: //INY
				case 0xEA: //NOP
				case 0x48: //PHA
				case 0x08: //PHP
				case 0x68: //PLA
				case 0x28: //PLP
				case 0x40: //RTI
				case 0x60: //RTS
				case 0x38: //SEC
				case 0xF8: //SED
				case 0x78: //SEI
				case 0xAA: //TAX
				case 0xA8: //TAY
				case 0xBA: //TSX
				case 0x8A: //TXA
				case 0x9A: //TXS
				case 0x98: //TYA
					{
						return AddressingMode.Implied;
					}
				case 0x6C:
					{
						return AddressingMode.Indirect;
					}

				case 0x61: //ADC
				case 0x21: //AND
				case 0xC1: //CMP
				case 0x41: //EOR
				case 0xA1: //LDA
				case 0x01: //ORA
				case 0xE1: //SBC
				case 0x81: //STA
					{
						return AddressingMode.IndirectX;
					}
				case 0x71: //ADC
				case 0x31: //AND
				case 0xD1: //CMP
				case 0x51: //EOR
				case 0xB1: //LDA
				case 0x11: //ORA
				case 0xF1: //SBC
				case 0x91: //STA
					{
						return AddressingMode.IndirectY;
					}
				case 0x90: //BCC
				case 0xB0: //BCS
				case 0xF0: //BEQ
				case 0x30: //BMI
				case 0xD0: //BNE
				case 0x10: //BPL
				case 0x50: //BVC
				case 0x70: //BVS
					{
						return AddressingMode.Relative;
					}
				case 0x65: //ADC
				case 0x25: //AND
				case 0x06: //ASL
				case 0x24: //BIT
				case 0xC5: //CMP
				case 0xE4: //CPX
				case 0xC4: //CPY
				case 0xC6: //DEC
				case 0x45: //EOR
				case 0xE6: //INC
				case 0xA5: //LDA
				case 0xA6: //LDX
				case 0xA4: //LDY
				case 0x46: //LSR
				case 0x05: //ORA
				case 0x26: //ROL
				case 0x66: //ROR
				case 0xE5: //SBC
				case 0x85: //STA
				case 0x86: //STX
				case 0x84: //STY
					{
						return AddressingMode.ZeroPage;
					}
				case 0x75: //ADC
				case 0x35: //AND
				case 0x16: //ASL
				case 0xD5: //CMP
				case 0xD6: //DEC
				case 0x55: //EOR
				case 0xF6: //INC
				case 0xB5: //LDA
				case 0xB6: //LDX
				case 0xB4: //LDY
				case 0x56: //LSR
				case 0x15: //ORA
				case 0x36: //ROL
				case 0x76: //ROR
				case 0xF5: //SBC
				case 0x95: //STA
				case 0x96: //STX
				case 0x94: //STY
					{
						return AddressingMode.ZeroPageX;
					}
				default:
                    throw new NotSupportedException(string.Format("Opcode {0} is not supported", CurrentOpCode));
			}
		}

		#region Op Code Operations
		/// <summary>
		/// The ADC - Add Memory to Accumulator with Carry Operation
		/// </summary>
		/// <param name="addressingMode">The addressing mode used to perform this operation.</param>
		protected virtual void AddWithCarryOperation(AddressingMode addressingMode)
		{
			//Accumulator, Carry = Accumulator + ValueInMemoryLocation + Carry 
			var memoryValue = ReadMemoryValue(GetAddressByAddressingMode(addressingMode));
			var newValue = memoryValue + Accumulator + (CarryFlag ? 1 : 0);

			
			OverflowFlag = (((Accumulator ^ newValue) & 0x80) != 0) && (((Accumulator ^ memoryValue) & 0x80) == 0);

			if (DecimalFlag)
			{
				newValue = int.Parse(memoryValue.ToString("x")) + int.Parse(Accumulator.ToString("x")) + (CarryFlag ? 1 : 0);

				if (newValue > 99)
				{
					CarryFlag = true;
					newValue -= 100;
				}
				else
				{
					CarryFlag = false;
				}

				newValue = (int)Convert.ToInt64(string.Concat("0x", newValue), 16);
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
			Accumulator =ReadMemoryValue(GetAddressByAddressingMode(addressingMode)) & Accumulator;

			SetZeroFlag(Accumulator);
			SetNegativeFlag(Accumulator);
		}

		/// <summary>
		/// The ASL - Shift Left One Bit (Memory or Accumulator)
		/// </summary>
		/// <param name="addressingMode">The addressing Mode being used</param>
		public void AslOperation(AddressingMode addressingMode)
		{
			int value;
			var memoryAddress = 0;
		    if (addressingMode == AddressingMode.Accumulator)
		    {
		        ReadMemoryValue(ProgramCounter + 1);
                value = Accumulator;
		    }
			else
			{
				memoryAddress = GetAddressByAddressingMode(addressingMode);
				value =ReadMemoryValue(memoryAddress);
			}

            //Dummy Write
		    if (addressingMode != AddressingMode.Accumulator)
		    {
                WriteMemoryValue(memoryAddress, (byte)value);
		    }
           
			//If the 7th bit is set, then we have a carry
			CarryFlag = ((value & 0x80) != 0);

			//The And here ensures that if the value is greater than 255 it wraps properly.
			value = (value << 1) & 0xFE;

			SetNegativeFlag(value);
			SetZeroFlag(value);
		   

			if (addressingMode == AddressingMode.Accumulator)
				Accumulator = value;
			else
			{
				WriteMemoryValue(memoryAddress, (byte)value);
			}
		}

		/// <summary>
		/// Performs the different branch operations.
		/// </summary>
		/// <param name="performBranch">Is a branch required</param>
		private void BranchOperation(bool performBranch)
		{
            var value = ReadMemoryValue(GetAddressByAddressingMode(AddressingMode.Relative));

			if (!performBranch)
			{
			    ProgramCounter++;
				return;
			}

			MoveProgramCounterByRelativeValue(value);
		}

		/// <summary>
		/// The bit operation, does an & comparison between a value in memory and the accumulator
		/// </summary>
		/// <param name="addressingMode"></param>
		private void BitOperation(AddressingMode addressingMode)
		{

			var memoryValue =ReadMemoryValue(GetAddressByAddressingMode(addressingMode));
			var valueToCompare = memoryValue & Accumulator;

			OverflowFlag = (memoryValue & 0x40) != 0;

			SetNegativeFlag(memoryValue);
			SetZeroFlag(valueToCompare);
		}

		/// <summary>
		/// The compare operation. This operation compares a value in memory with a value passed into it.
		/// </summary>
		/// <param name="addressingMode">The addressing mode to use</param>
		/// <param name="comparisonValue">The value to compare against memory</param>
		private void CompareOperation(AddressingMode addressingMode, int comparisonValue)
		{
			var memoryValue =ReadMemoryValue(GetAddressByAddressingMode(addressingMode));
			var comparedValue = comparisonValue - memoryValue;

			if (comparedValue < 0)
				comparedValue += 0x10000;

			SetZeroFlag(comparedValue);

			CarryFlag = memoryValue <= comparisonValue;
			SetNegativeFlag(comparedValue);
		}

		/// <summary>
		/// Changes a value in memory by 1
		/// </summary>
		/// <param name="addressingMode">The addressing mode to use</param>
		/// <param name="decrement">If the operation is decrementing or incrementing the vaulue by 1 </param>
		private void ChangeMemoryByOne(AddressingMode addressingMode, bool decrement)
		{
			var memoryLocation = GetAddressByAddressingMode(addressingMode);
			var memory =ReadMemoryValue(memoryLocation);
			
            WriteMemoryValue(memoryLocation, memory);

			if (decrement)
				memory -= 1;
			else
				memory += 1;

			SetZeroFlag(memory);
			SetNegativeFlag(memory);
		   

			WriteMemoryValue(memoryLocation,memory);
		}

		/// <summary>
		/// Changes a value in either the X or Y register by 1
		/// </summary>
		/// <param name="useXRegister">If the operation is using the X or Y register</param>
		/// <param name="decrement">If the operation is decrementing or incrementing the vaulue by 1 </param>
		private void ChangeRegisterByOne(bool useXRegister, bool decrement)
		{
			var value = useXRegister ? XRegister : YRegister;

			if (decrement)
				value -= 1;
			else
				value += 1;

			if (value < 0x00)
				value += 0x100;
			else if (value > 0xFF)
				value -= 0x100;

			SetZeroFlag(value);
			SetNegativeFlag(value);
		    IncrementCycleCount();

			if (useXRegister)
				XRegister = value;
			else
				YRegister = value;
		}

		/// <summary>
		/// The EOR Operation, Performs an Exclusive OR Operation against the Accumulator and a value in memory
		/// </summary>
		/// <param name="addressingMode">The addressing mode to use</param>
		private void EorOperation(AddressingMode addressingMode)
		{
			Accumulator = Accumulator ^ReadMemoryValue(GetAddressByAddressingMode(addressingMode));	

			SetNegativeFlag(Accumulator);
			SetZeroFlag(Accumulator);
		}

		/// <summary>
		/// The LSR Operation. Performs a Left shift operation on a value in memory
		/// </summary>
		/// <param name="addressingMode">The addressing mode to use</param>
		private void LsrOperation(AddressingMode addressingMode)
		{
			int value;
			var memoryAddress = 0;
		    if (addressingMode == AddressingMode.Accumulator)
		    {
		        ReadMemoryValue(ProgramCounter + 1);
                value = Accumulator;
		    }
			else
			{
				memoryAddress = GetAddressByAddressingMode(addressingMode);
				value =ReadMemoryValue(memoryAddress);
			}

            //Dummy Write
		    if (addressingMode != AddressingMode.Accumulator)
		    {
                WriteMemoryValue(memoryAddress, (byte)value);
		    }

			NegativeFlag = false;

			//If the Zero bit is set, we have a carry
			CarryFlag = ( value & 0x01 ) != 0;
			
			value = (value >> 1);

			SetZeroFlag(value);
			if (addressingMode == AddressingMode.Accumulator)
				Accumulator = value;
			else
			{
				WriteMemoryValue(memoryAddress, (byte)value);
			}
		}

		/// <summary>
		/// The Or Operation. Performs an Or Operation with the accumulator and a value in memory
		/// </summary>
		/// <param name="addressingMode">The addressing mode to use</param>
		private void OrOperation(AddressingMode addressingMode)
		{
			Accumulator = Accumulator |ReadMemoryValue(GetAddressByAddressingMode(addressingMode));
			
			SetNegativeFlag(Accumulator);
			SetZeroFlag(Accumulator);
		}

		/// <summary>
		/// The ROL operation. Performs a rotate left operation on a value in memory.
		/// </summary>
		/// <param name="addressingMode">The addressing mode to use</param>
		private void RolOperation(AddressingMode addressingMode)
		{
			int value;
			var memoryAddress = 0;
		    if (addressingMode == AddressingMode.Accumulator)
		    {
                //Dummy Read
		        ReadMemoryValue(ProgramCounter + 1);
                value = Accumulator;
		    }
			else
			{
				memoryAddress = GetAddressByAddressingMode(addressingMode);
				value =ReadMemoryValue(memoryAddress);
			}

            //Dummy Write
		    if (addressingMode != AddressingMode.Accumulator)
		    {
		        WriteMemoryValue(memoryAddress, (byte)value);
		    }

			//Store the carry flag before shifting it
			var newCarry = (0x80 & value) != 0;

			//The And here ensures that if the value is greater than 255 it wraps properly.
			value = (value << 1) & 0xFE;

			if (CarryFlag)
				value = value | 0x01;

			CarryFlag = newCarry;

			SetZeroFlag(value);
			SetNegativeFlag(value);
		    

			if (addressingMode == AddressingMode.Accumulator)
				Accumulator = value;
			else
			{
				WriteMemoryValue(memoryAddress, (byte)value);
			}
		}

		/// <summary>
		/// The ROR operation. Performs a rotate right operation on a value in memory.
		/// </summary>
		/// <param name="addressingMode">The addressing mode to use</param>
		private void RorOperation(AddressingMode addressingMode)
		{
			int value;
			var memoryAddress = 0;
			if (addressingMode == AddressingMode.Accumulator)
            {
                //Dummy Read
                ReadMemoryValue(ProgramCounter + 1);
                value = Accumulator;
            }
			else
			{
				memoryAddress = GetAddressByAddressingMode(addressingMode);
				value =ReadMemoryValue(memoryAddress);
			}
            
            //Dummy Write
            if (addressingMode != AddressingMode.Accumulator)
            {
                WriteMemoryValue(memoryAddress, (byte)value);
            }

			//Store the carry flag before shifting it
			var newCarry = (0x01 & value) != 0;

			value = (value >> 1);

			//If the carry flag is set then 0x
			if (CarryFlag)
				value = value | 0x80;

			CarryFlag = newCarry;

			SetZeroFlag(value);
			SetNegativeFlag(value);

			if (addressingMode == AddressingMode.Accumulator)
				Accumulator = value;
			else
			{
                WriteMemoryValue(memoryAddress, (byte)value);
			}
		}

		/// <summary>
		/// The SBC operation. Performs a subtract with carry operation on the accumulator and a value in memory.
		/// </summary>
		/// <param name="addressingMode">The addressing mode to use</param>
		protected virtual void SubtractWithBorrowOperation(AddressingMode addressingMode)
		{
			var memoryValue =ReadMemoryValue(GetAddressByAddressingMode(addressingMode));
			var newValue = DecimalFlag
				               ? int.Parse(Accumulator.ToString("x")) - int.Parse(memoryValue.ToString("x")) - (CarryFlag ? 0 : 1)
				               : Accumulator - memoryValue - (CarryFlag ? 0 : 1);

			CarryFlag = newValue >= 0;

			if (DecimalFlag)
			{
				if (newValue < 0)
					newValue += 100;

				newValue = (int)Convert.ToInt64(string.Concat("0x", newValue), 16);
			}
			else
			{
				OverflowFlag = (((Accumulator ^ newValue) & 0x80) != 0) && (((Accumulator ^ memoryValue) & 0x80) != 0);

				if (newValue < 0)
					newValue += 256;
			}

			SetNegativeFlag(newValue);
			SetZeroFlag(newValue);

			Accumulator = newValue;
		}

		/// <summary>
		/// The PSP Operation. Pushes the Status Flags to the stack
		/// </summary>
		private void PushFlagsOperation()
		{
			PokeStack(ConvertFlagsToByte(true));
		}

		/// <summary>
		/// The PLP Operation. Pull the status flags off the stack on sets the flags accordingly.
		/// </summary>
		private void PullFlagsOperation()
		{
			var flags = PeekStack();
			CarryFlag = (flags & 0x01) != 0;
			ZeroFlag = (flags & 0x02) != 0;
			DisableInterruptFlag = (flags & 0x04) != 0;
			DecimalFlag = (flags & 0x08) != 0;
			OverflowFlag = (flags & 0x40) != 0;
			NegativeFlag = (flags & 0x80) != 0;
			

		}

		/// <summary>
		/// The JSR routine. Jumps to a subroutine. 
		/// </summary>
		private void JumpToSubRoutineOperation()
		{
		    IncrementCycleCount();

            //Put the high value on the stack, this should be the address after our operation -1
			//The RTS operation increments the PC by 1 which is why we don't move 2
			PokeStack((byte)(((ProgramCounter + 1) >> 8) & 0xFF));
            StackPointer--;
		    IncrementCycleCount();

			PokeStack((byte)((ProgramCounter + 1) & 0xFF));
            StackPointer--;
		    IncrementCycleCount();

			ProgramCounter = GetAddressByAddressingMode(AddressingMode.Absolute);
		}

	    /// <summary>
	    /// The RTS routine. Called when returning from a subroutine.
	    /// </summary>
	    private void ReturnFromSubRoutineOperation()
	    {
	         ReadMemoryValue(++ProgramCounter);
	        StackPointer++;
	        IncrementCycleCount();

	        var lowBit = PeekStack();
	        StackPointer++;
	        IncrementCycleCount();

	        var highBit = PeekStack() << 8;
            IncrementCycleCount();

	        ProgramCounter = (highBit | lowBit) + 1;
            IncrementCycleCount();
        }


	    /// <summary>
		/// The BRK routine. Called when a BRK occurs.
		/// </summary>
		private void BreakOperation(bool isBrk, int vector)
	    {
            ReadMemoryValue(++ProgramCounter);

			//Put the high value on the stack
			//When we RTI the address will be incremented by one, and the address after a break will not be used.
			PokeStack((byte)(((ProgramCounter) >> 8) & 0xFF));
            StackPointer--;
	        IncrementCycleCount();

			//Put the low value on the stack
			PokeStack((byte)((ProgramCounter) & 0xFF));
            StackPointer--;
            IncrementCycleCount();
			
            //We only set the Break Flag is a Break Occurs
			if (isBrk)
				PokeStack((byte)(ConvertFlagsToByte(true) | 0x10));
			else
				PokeStack(ConvertFlagsToByte(false));

			StackPointer--;
	        IncrementCycleCount();

			DisableInterruptFlag = true;

            ProgramCounter = (ReadMemoryValue(vector + 1) << 8) | ReadMemoryValue(vector);

            _previousInterrupt = false;
        }

		/// <summary>
		/// The RTI routine. Called when returning from a BRK opertion.
		/// Note: when called after a BRK operation the Program Counter is not set to the location after the BRK,
		/// it is set +1
		/// </summary>
		private void ReturnFromInterruptOperation()
		{
            ReadMemoryValue(++ProgramCounter);
			StackPointer++;
		    IncrementCycleCount();

			PullFlagsOperation();
            StackPointer++;
            IncrementCycleCount();

			var lowBit = PeekStack();
			StackPointer++;
            IncrementCycleCount();

			var highBit = PeekStack() << 8;
            IncrementCycleCount();

			ProgramCounter = (highBit | lowBit);
		}

        /// <summary>
        /// This is ran anytime an NMI occurrs
        /// </summary>
	    private void ProcessNMI()
	    {
            ProgramCounter--;
            BreakOperation(false, 0xFFFA);
            CurrentOpCode = ReadMemoryValue(ProgramCounter);

            SetDisassembly();
	    }

        /// <summary>
        /// This is ran anytime an IRQ occurrs
        /// </summary>
        private void ProcessIRQ()
        {
            if (DisableInterruptFlag)
                return;

            ProgramCounter--;
            BreakOperation(false, 0xFFFE);
            CurrentOpCode = ReadMemoryValue(ProgramCounter);

            SetDisassembly();
        }
		#endregion

		#endregion
    }
}