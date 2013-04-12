using System;
using System.ComponentModel;

namespace Processor
{
	public static class Utility
	{
		public static string ConvertOpCodeIntoString(this int i)
		{
			switch (i)
			{
				case 0x69:  // ADC Immediate
				case 0x65:  // ADC Zero Page
				case 0x75:  // ADC Zero Page X
				case 0x6D:  // ADC Absolute
				case 0x7D:  // ADC Absolute X
				case 0x79:  // ADC Absolute Y
				case 0x61:  // ADC Indrect X
				case 0x71:  // ADC Indirect Y
					{
						return "ADC";
					}
				case 0x29:  // AND Immediate
				case 0x25:  // AND Zero Page
				case 0x35:  // AND Zero Page X
				case 0x2D:  // AND Absolute
				case 0x3D:  // AND Absolute X
				case 0x39:  // AND Absolute Y
				case 0x21:  // AND Indirect X
				case 0x31:  // AND Indirect Y
					{
						return "AND";
					}
				case 0x0A:  // ASL Accumulator
				case 0x06:  // ASL Zero Page
				case 0x16:  // ASL Zero Page X
				case 0x0E:  // ASL Absolute
				case 0x1E:  // ASL Absolute X
					{
						return "ASL";
					}
				case 0x90:  // BCC Relative
					{
						return "BCC";
					}
				case 0xB0:  // BCS Relative
					{
						return "BCS";
					}
				case 0xF0:  // BEQ Relative
					{
						return "BEQ";
					}
				case 0x24:  // BIT Zero Page	
				case 0x2C:  // BIT Absolute
					{
						return "BIT";
					}
				case 0x30:  // BMI Relative
					{
						return "BMI";
					}
				case 0xD0:  // BNE Relative
					{
						return "BNE";
					}
				case 0x10:  // BPL Relative
					{
						return "BPL";
					}
				case 0x00:  // BRK Implied
					{
						return "BRK";
					}
				case 0x18:  // CLC Implied
					{
						return "CLC";
					}
				case 0xD8:  // CLD Implied
					{
						return "CLD";
					}
				case 0x58:  // CLI Implied
					{
						return "CLI";
					}
				case 0xB8:  // CLV Implied
					{
						return "CLV";
					}
				case 0xC9:  // CMP Immediate
				case 0xC5:  // CMP ZeroPage
				case 0xD5:  // CMP Zero Page X
				case 0xCD:  // CMP Absolute
				case 0xDD:  // CMP Absolute X
				case 0xD9:  // CMP Absolute Y
				case 0xC1:  // CMP Indirect X
				case 0xD1:  // CMP Indirect Y
					{
						return "CMP";
					}
				case 0xE0:  // CPX Immediate
				case 0xE4:  // CPX ZeroPage
				case 0xEC:  // CPX Absolute
					{
						return "CPX";
					}
				case 0xC0:  // CPY Immediate
				case 0xC4:  // CPY ZeroPage
				case 0xCC:  // CPY Absolute
					{
						return "CPY";
					}
				case 0xC6:  // DEC Zero Page
				case 0xD6:  // DEC Zero Page X
				case 0xCE:  // DEC Absolute
				case 0xDE:  // DEC Absolute X
					{
						return "DEC";
					}
				case 0xCA:  // DEX Implied
					{
						return "DEX";
					}
				case 0x88:  // DEY Implied
					{
						return "DEY";
					}
				case 0x49:  // EOR Immediate
				case 0x45:  // EOR Zero Page
				case 0x55:  // EOR Zero Page X
				case 0x4D:  // EOR Absolute
				case 0x5D:  // EOR Absolute X
				case 0x59:  // EOR Absolute Y
				case 0x41:  // EOR Indrect X
				case 0x51:  // EOR Indirect Y
					{
						return "EOR";
					}
				case 0xE6:  // INC Zero Page
				case 0xF6:  // INC Zero Page X
					{
						return "INC";
					}
				case 0xE8:  // INX Implied
					{
						return "INX";
					}
				case 0xC8:  // INY Implied
					{
						return "INY";
					}
				case 0xEE:  // INC Absolute
				case 0xFE:  // INC Absolute X
					{
						return "INC";
					}
				case 0x4C:  // JMP Absolute
				case 0x6C:  // JMP Indirect
					{
						return "JMP";
					}
				case 0x20:  // JSR Absolute
					{
						return "JSR";
					}
				case 0xA9:  // LDA Immediate
				case 0xA5:  // LDA Zero Page
				case 0xB5:  // LDA Zero Page X
				case 0xAD:  // LDA Absolute
				case 0xBD:  // LDA Absolute X
				case 0xB9:  // LDA Absolute Y
				case 0xA1:  // LDA Indirect X
				case 0xB1:  // LDA Indirect Y
					{
						return "LDA";
					}
				case 0xA2:  // LDX Immediate
				case 0xA6:  // LDX Zero Page
				case 0xB6:  // LDX Zero Page Y
				case 0xAE:  // LDX Absolute
				case 0xBE:  // LDX Absolute Y
					{
						return "LDX";
					}
				case 0xA0:  // LDY Immediate
				case 0xA4:  // LDY Zero Page
				case 0xB4:  // LDY Zero Page Y
				case 0xAC:  // LDY Absolute
				case 0xBC:  // LDY Absolute Y
					{
						return "LDY";
					}
				case 0x4A:  // LSR Accumulator
				case 0x46:  // LSR Zero Page
				case 0x56:  // LSR Zero Page X
				case 0x4E:  // LSR Absolute
				case 0x5E:  // LSR Absolute X
					{
						return "LSR";
					}
				case 0xEA:  // NOP Implied
					{
						return "NOP";
					}
				case 0x09:  // ORA Immediate
				case 0x05:  // ORA Zero Page
				case 0x15:  // ORA Zero Page X
				case 0x0D:  // ORA Absolute
				case 0x1D:  // ORA Absolute X
				case 0x19:  // ORA Absolute Y
				case 0x01:  // ORA Indirect X
				case 0x11:  // ORA Indirect Y
					{
						return "ORA";
					}
				case 0x48:  // PHA Implied
					{
						return "PHA";
					}
				case 0x08:  // PHP Implied
					{
						return "PHP";
					}
				case 0x68:  // PLA Implied
					{
						return "PLA";
					}
				case 0x28:  // PLP Implied
					{
						return "PLP";
					}
				case 0x2A:  // ROL Accumulator
				case 0x26:  // ROL Zero Page
				case 0x36:  // ROL Zero Page X
				case 0x2E:  // ROL Absolute
				case 0x3E:  // ROL Absolute X
					{
						return "ROL";
					}
				case 0x6A:  // ROR Accumulator
				case 0x66:  // ROR Zero Page
				case 0x76:  // ROR Zero Page X
				case 0x6E:  // ROR Absolute
				case 0x7E:  // ROR Absolute X
					{
						return "ROR";
					}
				case 0x40:  // RTI Implied
					{
						return "RTI";
					}
				case 0x60:  // RTS Implied
					{
						return "RTS";
					}
				case 0xE9:  // SBC Immediate
				case 0xE5:  // SBC Zero Page
				case 0xF5:  // SBC Zero Page X
				case 0xED:  // SBC Absolute
				case 0xFD:  // SBC Absolute X
				case 0xF9:  // SBC Absolute Y
				case 0xE1:  // SBC Indrect X
				case 0xF1:  // SBC Indirect Y
					{
						return "SBC";
					}
				case 0x38:  // SEC Implied
					{
						return "SEC";
					}
				case 0xF8:  // SED Implied
					{
						return "SED";
					}
				case 0x78:  // SEI Implied
					{
						return "SEI";
					}
				case 0x85:  // STA ZeroPage
				case 0x95:  // STA Zero Page X
				case 0x8D:  // STA Absolute
				case 0x9D:  // STA Absolute X
				case 0x99:  // STA Absolute Y
				case 0x81:  // STA Indirect X
				case 0x91:  // STA Indirect Y
					{
						return "STA";
					}
				case 0x86:  // STX Zero Page
				case 0x96:  // STX Zero Page Y
				case 0x8E:  // STX Absolute
					{
						return "STX";
					}
				case 0x84:  // STY Zero Page
				case 0x94:  // STY Zero Page X
				case 0x8C:  // STY Absolute
					{
						return "STY";
					}
				case 0xAA:  // TAX Implied
					{
						return "TAX";
					}
				case 0xA8:  // TAY Implied
					{
						return "TAY";
					}
				case 0xBA:  // TSX Implied
					{
						return "TSX";
					}
				case 0x8A:  // TXA Implied
					{
						return "TXA";
					}
				case 0x9A:  // TXS Implied
					{
						return "TXS";
					}
				case 0x98:  // TYA Implied
					{
						return "TYA";
					}
				default:
					throw new InvalidEnumArgumentException(string.Format("A Valid Conversion does not exist for OpCode {0}", i.ToString("X")));

			}
		}
	}
}
