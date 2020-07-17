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
		private readonly Memory     memory;
		private readonly Graphics   graphics;
		private readonly Interrupts interrupts;

		public Cpu()
		{
			//Initialize modules
			interrupts = new Interrupts();
			memory     = new Memory(this, interrupts);
			graphics   = new Graphics(memory, this);
		}

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

		//Flags
		private int disableInterruptsNextOpcode;

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

				//Delayed Interrupt disabling
				switch (disableInterruptsNextOpcode)
				{
					case 1:
						disableInterruptsNextOpcode = 2;
						break;
					case 2:
						disableInterruptsNextOpcode      = 0;
						interrupts.masterInterruptEnable = false;
						break;
				}

				cyclesThisFrame += cycles;
				graphics.Update(cycles);
			}
		}

		private int ExecuteOpcode()
		{
			byte opcode = Load8BitImmediate();

			switch (opcode)
			{
				//NOP
				case 0x00:
					return 4;
				//LD BC,nn
				case 0x01:
					BcRegister = Load16BitImmediate();
					return 12;
				//INC BC
				case 0x03:
					BcRegister = Increment(BcRegister);
					return 8;
				//INC B
				case 0x04:
					bRegister = Increment(bRegister);
					return 4;
				//DEC B
				case 0x05:
					bRegister = Decrement(bRegister);
					return 4;
				//LD B,n
				case 0x06:
					bRegister = Load8BitImmediate();
					return 8;
				//LD A,(BC)
				case 0x0A:
					aRegister = memory.Read(BcRegister);
					return 8;
				//INC C
				case 0x0C:
					cRegister = Increment(cRegister);
					return 4;
				//DEC C
				case 0x0D:
					cRegister = Decrement(cRegister);
					return 4;
				//LD C,n
				case 0x0E:
					cRegister = Load8BitImmediate();
					return 8;
				//LD DE,nn
				case 0x11:
					DeRegister = Load16BitImmediate();
					return 12;
				//LD (DE),A
				case 0x12:
					memory.Write(DeRegister, aRegister);
					return 8;
				//INC DE
				case 0x13:
					DeRegister = Increment(DeRegister);
					return 8;
				//INC D
				case 0x14:
					dRegister = Increment(dRegister);
					return 4;
				//DEC D
				case 0x15:
					dRegister = Decrement(dRegister);
					return 4;
				//LD D,n
				case 0x16:
					dRegister = Load8BitImmediate();
					return 8;
				//RLA
				case 0x17:
					aRegister = RotateLeftCarry(aRegister);
					return 4;
				//JR n
				case 0x18:
					return JumpRelative(JumpConditions.NoCondition);
				//LD A,(DE)
				case 0x1A:
					aRegister = memory.Read(DeRegister);
					return 8;
				//INC E
				case 0x1C:
					eRegister = Increment(eRegister);
					return 4;
				//DEC E
				case 0x1D:
					eRegister = Decrement(eRegister);
					return 4;
				//LD E,n
				case 0x1E:
					eRegister = Load8BitImmediate();
					return 8;
				//JR NZ,n
				case 0x20:
					return JumpRelative(JumpConditions.Nz);
				//LD HL,nn
				case 0x21:
					HlRegister = Load16BitImmediate();
					return 12;
				//LD (HL+),A
				case 0x22:
					return WriteHlDecrement(aRegister, true);
				//INC HL
				case 0x23:
					HlRegister = Increment(HlRegister);
					return 8;
				//INC H
				case 0x24:
					hRegister = Increment(hRegister);
					return 4;
				//JR Z,n
				case 0x28:
					return JumpRelative(JumpConditions.Z);
				//LD A,(HL+)
				case 0x2A:
					aRegister = ReadHlDecrement(true);
					return 8;
				//LD L,n
				case 0x2E:
					lRegister = Load8BitImmediate();
					return 8;
				//LD SP,nn
				case 0x31:
					stackPointer = Load16BitImmediate();
					return 12;
				//LD (HL-),A
				case 0x32:
					return WriteHlDecrement(aRegister, false);
				//DEC A
				case 0x3D:
					aRegister = Decrement(aRegister);
					return 4;
				//LD A,n
				case 0x3E:
					aRegister = Load8BitImmediate();
					return 8;
				//LD B,A
				case 0x47:
					bRegister = aRegister;
					return 4;
				//LD C,A
				case 0x4F:
					cRegister = aRegister;
					return 4;
				//LD D,A
				case 0x57:
					dRegister = aRegister;
					return 4;
				//LD H,C
				case 0x61:
					hRegister = cRegister;
					return 4;
				//LD H,E
				case 0x63:
					hRegister = eRegister;
					return 4;
				//LD H,L
				case 0x65:
					hRegister = lRegister;
					return 4;
				//LD H,A
				case 0x67:
					hRegister = aRegister;
					return 4;
				//LD L,C
				case 0x69:
					lRegister = cRegister;
					return 4;
				//LD L,H
				case 0x6C:
					lRegister = hRegister;
					return 4;
				//LD (HL),B
				case 0x70:
					memory.Write(HlRegister, bRegister);
					return 8;
				//LD (HL),A
				case 0x77:
					memory.Write(HlRegister, aRegister);
					return 8;
				//LD A,B
				case 0x78:
					aRegister = bRegister;
					return 4;
				//LD A,E
				case 0x7B:
					aRegister = eRegister;
					return 4;
				//LD A,H
				case 0x7C:
					aRegister = hRegister;
					return 4;
				//LD A,L
				case 0x7D:
					aRegister = lRegister;
					return 4;
				//ADD A,(HL)
				case 0x86:
					AddByteToAReg(memory.Read(HlRegister));
					return 8;
				//SUB B
				case 0x90:
					SubtractByteFromAReg(bRegister, false);
					return 4;
				//XOR A
				case 0xAF:
					XorIntoA(aRegister);
					return 4;
				//OR C
				case 0xB1:
					OrIntoA(cRegister);
					return 4;
				//CP (HL)
				case 0xBE:
					SubtractByteFromAReg(memory.Read(HlRegister), true);
					return 8;
				//POP BC
				case 0xC1:
					BcRegister = PopStack();
					return 12;
				//JP NZ,nn
				case 0xC2:
					return JumpImmediate(JumpConditions.Nz);
				//JP nn
				case 0xC3:
					return JumpImmediate(JumpConditions.NoCondition);
				//CALL NZ,nn
				case 0xC4:
					return CallSubroutine(JumpConditions.Nz);
				//PUSH BC
				case 0xC5:
					return PushStack(BcRegister);
				//RET
				case 0xC9:
					return ReturnSubroutine(JumpConditions.NoCondition);
				//Extended Opcode
				case 0xCB:
					return ExecuteExtendedOpcode();
				//CALL nn
				case 0xCD:
					return CallSubroutine(JumpConditions.NoCondition);
				//SUB n
				case 0xD6:
					SubtractByteFromAReg(Load8BitImmediate(), false);
					return 8;
				//RET C
				case 0xD8:
					return ReturnSubroutine(JumpConditions.C);
				//LD (0xFF00+n),A
				case 0xE0:
					return WriteIoPortsImmediateOffset(aRegister);
				//POP HL
				case 0xE1:
					HlRegister = PopStack();
					return 12;
				//LD (0xFF00+C),A
				case 0xE2:
					return WriteIoPortsCRegisterOffset(aRegister);
				//PUSH HL
				case 0xE5:
					return PushStack(HlRegister);
				//AND n
				case 0xE6:
					AndIntoA(Load8BitImmediate());
					return 8;
				//LD (nn),A
				case 0xEA:
					memory.Write(Load16BitImmediate(), aRegister);
					return 16;
				//LD A,(0xFF00+n)
				case 0xF0:
					aRegister = memory.Read((ushort)(0xFF00 + Load8BitImmediate()));
					return 12;
				//POP AF
				case 0xF1:
					//TODO - Upper nibble of flag register should not be affected
					AfRegister = PopStack();
					return 12;
				//DI
				case 0xF3:
					disableInterruptsNextOpcode = 1;
					return 4;
				//PUSH AF
				case 0xF5:
					return PushStack(AfRegister);
				//LD A,(nn)
				case 0xFA:
					aRegister = memory.Read(Load16BitImmediate());
					return 16;
				//CP n
				case 0xFE:
					SubtractByteFromAReg(Load8BitImmediate(), true);
					return 8;

				//Invalid Opcode
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
				//RL C
				case 0x11:
					cRegister = RotateLeftCarry(cRegister);
					return 8;
				//BIT 7,H
				case 0x7C:
					GameboyBit(hRegister, 7);
					return 8;

				//Invalid Opcode
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
		public static bool GetBit(byte data, int bit)
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

		private void OrIntoA(byte data)
		{
			aRegister |= data;

			SetFlags(aRegister == 0, 0, 0, 0);
		}

		private void AndIntoA(byte data)
		{
			aRegister &= data;

			SetFlags(aRegister == 0, 0, 1, 0);
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

		public void InitializeRegisters()
		{
			AfRegister     = 0x01B0;
			BcRegister     = 0x0013;
			DeRegister     = 0x00D8;
			HlRegister     = 0x014D;
			stackPointer   = 0xFFFE;
			programCounter = 0x100;
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

		private byte ReadHlDecrement(bool increment)
		{
			byte data = memory.Read(HlRegister);

			HlRegister += (ushort)(increment ? 1 : -1);

			return data;
		}

		//Jump functions
		private int JumpRelative(JumpConditions condition)
		{
			//Signed relative Jump amount
			sbyte relativeJumpAmount = (sbyte)Load8BitImmediate();

			if (!ShouldJump(condition)) return 8;

			programCounter = AddSignedToUnsigned(programCounter, relativeJumpAmount);
			return 12;
		}

		private int JumpImmediate(JumpConditions condition)
		{
			ushort jumpAddress = Load16BitImmediate();

			if (!ShouldJump(condition)) return 12;

			programCounter = jumpAddress;
			return 16;
		}

		private bool ShouldJump(JumpConditions condition)
		{
			return condition switch
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
		}

		private int CallSubroutine(JumpConditions condition)
		{
			ushort jumpAddress = Load16BitImmediate();

			if (!ShouldJump(condition)) return 12;

			PushStack((ushort)(programCounter + 2));
			programCounter = jumpAddress;
			return 24;
		}

		private int ReturnSubroutine(JumpConditions condition)
		{
			if (condition == JumpConditions.NoCondition)
			{
				//Special Case for unconditional return
				programCounter = PopStack();
				return 16;
			}

			if (!ShouldJump(condition)) return 8;

			programCounter = PopStack();
			return 20;
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
		private int PushStack(ushort data)
		{
			byte lo = GetLoByte(data);
			byte hi = GetHiByte(data);

			memory.Write(stackPointer--, lo);
			memory.Write(stackPointer--, hi);

			return 16;
		}

		private ushort PopStack()
		{
			byte hi = memory.Read(++stackPointer);
			byte lo = memory.Read(++stackPointer);

			return MakeWord(hi, lo);
		}
	}
}