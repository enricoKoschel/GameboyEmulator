using System;

namespace GameboyEmulator
{
	class Cpu
	{
		private enum JumpConditions
		{
			Nz,
			Z,
			Nc,
			C,
			NoCondition
		}

		//Modules
		private readonly Memory   memory;
		private readonly Graphics graphics;

		//Registers
		private ushort programCounter;
		private ushort stackPointer;

		private byte aRegister;
		private byte bRegister;
		private byte cRegister;
		private byte dRegister;
		private byte eRegister;

		private byte hRegister;
		private byte lRegister;

		//Register pairs
		private ushort AfRegister
		{
			get => MakeWord(aRegister, flagRegister);
			set
			{
				aRegister    = GetHiByte(value);
				flagRegister = GetLoByte(value);
			}
		}

		private ushort BcRegister
		{
			get => MakeWord(bRegister, cRegister);
			set
			{
				bRegister = GetHiByte(value);
				cRegister = GetLoByte(value);
			}
		}

		private ushort DeRegister
		{
			get => MakeWord(dRegister, eRegister);
			set
			{
				dRegister = GetHiByte(value);
				eRegister = GetLoByte(value);
			}
		}

		private ushort HlRegister
		{
			get => MakeWord(hRegister, lRegister);
			set
			{
				hRegister = GetHiByte(value);
				lRegister = GetLoByte(value);
			}
		}

		//Flag register
		private byte flagRegister;

		private bool ZeroFlag
		{
			get => GetBit(flagRegister, 7);
			set => flagRegister = SetBit(flagRegister, 7, value);
		}

		private bool SubtractFlag
		{
			get => GetBit(flagRegister, 6);
			set => flagRegister = SetBit(flagRegister, 6, value);
		}

		private bool HalfCarryFlag
		{
			get => GetBit(flagRegister, 5);
			set => flagRegister = SetBit(flagRegister, 5, value);
		}

		private bool CarryFlag
		{
			get => GetBit(flagRegister, 4);
			set => flagRegister = SetBit(flagRegister, 4, value);
		}

		public bool IsRunning => graphics.IsScreenOpen;

		//Constants
		private const int MAX_CPU_CYCLES_PER_FRAME = 70224;

		public Cpu()
		{
			//Initialize modules
			memory   = new Memory();
			graphics = new Graphics(memory, this);
		}

		public void Start()
		{
			memory.LoadGame();
		}

		public void Update()
		{
			int cyclesThisFrame = 0;

			while (cyclesThisFrame < MAX_CPU_CYCLES_PER_FRAME)
			{
				int cycles = ExecuteOpcode();

				cyclesThisFrame += cycles;
				graphics.Update(cycles);
			}
		}

		private int ExecuteOpcode()
		{
			byte opcode = Load8BitImmediate();

			switch (opcode)
			{
				case 0x04:
				{
					//INC B
					bRegister = Increment(bRegister);
					return 4;
				}
				case 0x05:
				{
					//DEC B
					bRegister = Decrement(bRegister);
					return 4;
				}
				case 0x06:
				{
					//LD B,n
					bRegister = Load8BitImmediate();
					return 8;
				}
				case 0x0C:
				{
					//INC C
					cRegister = Increment(cRegister);
					return 4;
				}
				case 0x0D:
				{
					//DEC C
					cRegister = Decrement(cRegister);
					return 4;
				}
				case 0x0E:
				{
					//LD C,n
					cRegister = Load8BitImmediate();
					return 8;
				}
				case 0x11:
				{
					//LD DE,nn
					DeRegister = Load16BitImmediate();
					return 12;
				}
				case 0x13:
				{
					//INC DE
					DeRegister = Increment(DeRegister);
					return 8;
				}
				case 0x15:
				{
					//DEC D
					dRegister = Decrement(dRegister);
					return 4;
				}
				case 0x16:
				{
					//LD D,n
					dRegister = Load8BitImmediate();
					return 8;
				}
				case 0x17:
				{
					//RLA
					aRegister = RotateLeftCarry(aRegister);
					return 4;
				}
				case 0x18:
				{
					//JR n
					return JumpRelative(JumpConditions.NoCondition);
				}
				case 0x1A:
				{
					//LD A,(DE)
					aRegister = memory.Read(DeRegister);
					return 8;
				}
				case 0x1D:
				{
					//DEC E
					eRegister = Decrement(eRegister);
					return 4;
				}
				case 0x1E:
				{
					//LD E,n
					eRegister = Load8BitImmediate();
					return 8;
				}
				case 0x20:
				{
					//JR NZ,n
					return JumpRelative(JumpConditions.Nz);
				}
				case 0x21:
				{
					//LD HL,nn
					HlRegister = Load16BitImmediate();
					return 12;
				}
				case 0x22:
				{
					//LD (HL+),A                       
					return WriteHlDecrement(aRegister, true);
				}
				case 0x23:
				{
					//INC HL
					HlRegister = Increment(HlRegister);
					return 8;
				}
				case 0x24:
				{
					//INC H
					hRegister = Increment(hRegister);
					return 4;
				}
				case 0x28:
				{
					//JR Z,n
					return JumpRelative(JumpConditions.Z);
				}
				case 0x2E:
				{
					//LD L,n
					lRegister = Load8BitImmediate();
					return 8;
				}
				case 0x31:
				{
					//LD SP,nn
					stackPointer = Load16BitImmediate();
					return 12;
				}
				case 0x32:
				{
					//LD (HL-),A
					return WriteHlDecrement(aRegister, false);
				}
				case 0x3D:
				{
					//DEC A
					aRegister = Decrement(aRegister);
					return 4;
				}
				case 0x3E:
				{
					//LD A,n
					aRegister = Load8BitImmediate();
					return 8;
				}
				case 0x4F:
				{
					//LD C,A
					cRegister = aRegister;
					return 4;
				}
				case 0x57:
				{
					//LD D,A
					dRegister = aRegister;
					return 4;
				}
				case 0x67:
				{
					//LD H,A
					hRegister = aRegister;
					return 4;
				}
				case 0x77:
				{
					//LD (HL),A
					memory.Write(HlRegister, aRegister);
					return 8;
				}
				case 0x78:
				{
					//LD A,B
					aRegister = bRegister;
					return 4;
				}
				case 0x7B:
				{
					//LD A,E
					aRegister = eRegister;
					return 4;
				}
				case 0x7C:
				{
					//LD A,H
					aRegister = hRegister;
					return 4;
				}
				case 0x7D:
				{
					//LD A,L
					aRegister = lRegister;
					return 4;
				}
				case 0x86:
				{
					//ADD A,(HL)
					AddByteToAReg(memory.Read(HlRegister));
					return 8;
				}
				case 0x90:
				{
					//SUB B
					SubtractByteFromAReg(bRegister, false);
					return 4;
				}
				case 0xAF:
				{
					//XOR A
					XorIntoA(aRegister);
					return 4;
				}
				case 0xBE:
				{
					//CP (HL)
					SubtractByteFromAReg(memory.Read(HlRegister), true);
					return 8;
				}
				case 0xC1:
				{
					//POP BC
					BcRegister = PopStack();
					return 12;
				}
				case 0xC5:
				{
					//PUSH BC
					PushStack(BcRegister);
					return 16;
				}
				case 0xC9:
				{
					//RET
					return ReturnSubroutine();
				}
				case 0xCB:
				{
					//Extended Opcode
					return ExecuteExtendedOpcode();
				}
				case 0xCD:
				{
					//CALL nn
					return CallSubroutine();
				}
				case 0xE0:
				{
					//LD (0xFF00+n),A
					return WriteIoPortsImmediateOffset(aRegister);
				}
				case 0xE2:
				{
					//LD (0xFF00+C),A
					return WriteIoPortsCRegisterOffset(aRegister);
				}
				case 0xEA:
				{
					//LD (nn),A
					memory.Write(Load16BitImmediate(), aRegister);
					return 16;
				}
				case 0xF0:
				{
					//LD A,(0xFF00+n)
					aRegister = memory.Read((ushort)(0xFF00 + Load8BitImmediate()));
					return 12;
				}
				case 0xFE:
				{
					//CP n
					SubtractByteFromAReg(Load8BitImmediate(), true);
					return 8;
				}
				default:
					throw new NotImplementedException(
						$"Opcode 0x{opcode:X} not implemented yet!"
					); //TODO - implement opcodes
			}
		}

		private int ExecuteExtendedOpcode()
		{
			byte opcode = Load8BitImmediate();

			switch (opcode)
			{
				case 0x11:
				{
					//RL C
					cRegister = RotateLeftCarry(cRegister);
					return 8;
				}
				case 0x7C:
				{
					//BIT 7,H
					GameboyBit(hRegister, 7);
					return 8;
				}
				default:
					throw new NotImplementedException(
						$"Extended Opcode 0xCB{opcode:X} not implemented yet!"
					); //TODO - implement extended opcodes
			}
		}

		//Word functions
		private static ushort MakeWord(byte hi, byte lo)
		{
			return (ushort)((hi << 8) | lo);
		}

		private static byte GetLoByte(ushort word)
		{
			return (byte)(word & 0xFF);
		}

		private static byte GetHiByte(ushort word)
		{
			return (byte)(word >> 8);
		}

		//Bit-wise functions
		// ReSharper disable once MemberCanBeMadeStatic.Global
		public bool GetBit(byte data, int bit)
		{
			if (bit > 7 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Byte!");

			return ToBool(((data >> bit) & 1));
		}

		private static bool GetBit(ushort data, int bit)
		{
			if (bit > 15 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Word!");

			return ToBool(((data >> bit) & 1));
		}

		private static byte SetBit(byte data, int bit, bool state)
		{
			if (bit > 7 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Byte!");

			byte mask = (byte)(1 << bit);

			if (state)
				data |= mask;
			else
				data &= (byte)~mask;


			return data;
		}

		private static ushort SetBit(ushort data, int bit, bool state)
		{
			if (bit > 15 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Word!");

			byte mask = (byte)(1 << bit);

			if (state)
				data |= mask;
			else
				data &= (byte)~mask;

			return data;
		}

		private void XorIntoA(byte data)
		{
			aRegister ^= data;
			SetFlags(aRegister == 0, 0, 0, 0);
		}

		private void GameboyBit(byte data, int bit)
		{
			SetFlags(!GetBit(data, bit), 0, 1, "");
		}

		private byte RotateLeftCarry(byte data)
		{
			bool bit7 = GetBit(data, 7);

			data <<= 1;
			data =   SetBit(data, 0, CarryFlag);

			SetFlags(data == 0, 0, 0, bit7);

			return data;
		}

		private byte RotateLeft(byte data)
		{
			bool bit7 = GetBit(data, 7);

			data <<= 1;
			data =   SetBit(data, 0, bit7);

			SetFlags(data == 0, 0, 0, bit7);

			return data;
		}

		//Bool functions
		private static bool ToBool<T>(T data)
		{
			return Convert.ToBoolean(data);
		}

		//Register functions
		private void SetFlags<T1, T2, T3, T4>(T1 zeroFlag, T2 subtractFlag, T3 halfCarryFlag, T4 carryFlag)
		{
			//Argument of type String means "Leave Flag unchanged"

			if (zeroFlag.GetType().Name != "String") ZeroFlag           = ToBool(zeroFlag);
			if (subtractFlag.GetType().Name != "String") SubtractFlag   = ToBool(subtractFlag);
			if (halfCarryFlag.GetType().Name != "String") HalfCarryFlag = ToBool(halfCarryFlag);
			if (carryFlag.GetType().Name != "String") CarryFlag         = ToBool(carryFlag);
		}

		//Load functions
		private ushort Load16BitImmediate()
		{
			byte lo = Load8BitImmediate();
			byte hi = Load8BitImmediate();
			return MakeWord(hi, lo);
		}

		private byte Load8BitImmediate()
		{
			return memory.Read(programCounter++);
		}

		private int WriteIoPortsCRegisterOffset(byte data)
		{
			memory.Write((ushort)(0xFF00 + cRegister), data);
			return 8;
		}

		private int WriteIoPortsImmediateOffset(byte data)
		{
			memory.Write((ushort)(0xFF00 + Load8BitImmediate()), data);
			return 12;
		}

		private int WriteHlDecrement(byte data, bool increment)
		{
			memory.Write(HlRegister, data);

			HlRegister += (ushort)(increment ? 1 : -1);

			return 8;
		}

		//Jump functions
		private int JumpRelative(JumpConditions condition)
		{
			//Signed relative Jump amount
			sbyte relativeJumpAmount = (sbyte)Load8BitImmediate();

			bool shouldJump = condition switch
			{
				//Non-Zero
				JumpConditions.Nz =>
				!ZeroFlag,

				//Zero
				JumpConditions.Z =>
				ZeroFlag,

				//No Carry
				JumpConditions.Nc =>
				!CarryFlag,

				//Carry
				JumpConditions.C =>
				CarryFlag,

				//Always Jump
				JumpConditions.NoCondition =>
				true,

				//Default
				_ => false
			};

			if (!shouldJump) return 8;

			programCounter = AddSignedToUnsigned(programCounter, relativeJumpAmount);
			return 12;
		}

		private int CallSubroutine()
		{
			PushStack((ushort)(programCounter + 2));
			programCounter = Load16BitImmediate();
			return 24;
		}

		private int ReturnSubroutine()
		{
			programCounter = PopStack();
			return 16;
		}

		//Math functions
		private static ushort AddSignedToUnsigned(ushort unsignedWord, sbyte signedByte)
		{
			if (signedByte > 0) return (ushort)(unsignedWord + (ushort)signedByte);

			return (ushort)(unsignedWord - (ushort)(signedByte * -1));
		}

		private byte Increment(byte data)
		{
			bool halfCarry = ToBool(((data & 0xF) + 1) & 0x10);

			data++;
			SetFlags(data == 0, 0, halfCarry, "");
			return data;
		}

		private static ushort Increment(ushort data)
		{
			return (ushort)(data + 1);
		}

		private byte Decrement(byte data)
		{
			bool halfCarry = ToBool(((data & 0xF) - 1) & 0x10);

			data--;
			SetFlags(data == 0, 1, halfCarry, "");
			return data;
		}

		private static ushort Decrement(ushort data)
		{
			return (ushort)(data - 1);
		}

		private void SubtractByteFromAReg(byte data, bool compare)
		{
			bool halfCarry = ToBool(((aRegister & 0xF) - (data & 0xF)) & 0x10);
			bool carry     = ToBool((aRegister - data) < 0);

			SetFlags((aRegister - data) == 0, 1, halfCarry, carry);

			if (!compare) aRegister -= data;
		}

		private void AddByteToAReg(byte data)
		{
			bool halfCarry = ToBool(((aRegister & 0xF) + (data & 0xF)) & 0x10);
			bool carry     = ToBool((aRegister + data) > 255);

			aRegister += data;

			SetFlags(aRegister == 0, 0, halfCarry, carry);
		}

		//Stack functions
		private void PushStack(ushort data)
		{
			byte lo = GetLoByte(data);
			byte hi = GetHiByte(data);

			memory.Write(stackPointer--, lo);
			memory.Write(stackPointer--, hi);
		}

		private ushort PopStack()
		{
			byte hi = memory.Read(++stackPointer);
			byte lo = memory.Read(++stackPointer);

			return MakeWord(hi, lo);
		}
	}
}