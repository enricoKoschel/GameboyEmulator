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

		private enum InterruptStatus
		{
			ThisCycle,
			NextCycle,
			False
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
				aRegister = GetHiByte(value);
				//Lower nibble of Flag Register should always be zero
				flagRegister = (byte)(GetLoByte(value) & 0xF0);
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
		private InterruptStatus disableInterrupts;
		private InterruptStatus enableInterrupts;

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
				switch (disableInterrupts)
				{
					case InterruptStatus.NextCycle:
						disableInterrupts = InterruptStatus.ThisCycle;
						break;
					case InterruptStatus.ThisCycle:
						disableInterrupts      = InterruptStatus.False;
						interrupts.masterInterruptEnable = false;
						break;
				}
				//Delayed Interrupt enabling
				switch (enableInterrupts)
				{
					case InterruptStatus.NextCycle:
						enableInterrupts = InterruptStatus.ThisCycle;
						break;
					case InterruptStatus.ThisCycle:
						enableInterrupts       = InterruptStatus.False;
						interrupts.masterInterruptEnable = true;
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
				//RLCA
				case 0x07:
					aRegister = RotateLeftIntoCarry(aRegister);
					return 4;
				//LD (nn),SP
				case 0x08:
					return LoadStackPointerIntoMemory();
				//ADD HL,BC
				case 0x09:
					HlRegister = Add16BitRegisters(HlRegister, BcRegister);
					return 8;
				//LD A,(BC)
				case 0x0A:
					aRegister = memory.Read(BcRegister);
					return 8;
				//DEC BC
				case 0x0B:
					BcRegister = Decrement(BcRegister);
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
					aRegister = RotateLeftThroughCarry(aRegister);
					return 4;
				//JR n
				case 0x18:
					return JumpRelative(JumpConditions.NoCondition);
				//ADD HL,DE
				case 0x19:
					HlRegister = Add16BitRegisters(HlRegister, DeRegister);
					return 8;
				//LD A,(DE)
				case 0x1A:
					aRegister = memory.Read(DeRegister);
					return 8;
				//DEC DE
				case 0x1B:
					DeRegister = Decrement(DeRegister);
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
				//RRA
				case 0x1F:
					aRegister = RotateRightThroughCarry(aRegister);
					return 4;
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
				//DEC H
				case 0x25:
					hRegister = Decrement(hRegister);
					return 4;
				//LD H,n
				case 0x26:
					hRegister = Load8BitImmediate();
					return 8;
				//DAA
				case 0x27:
					return DecimalAdjustARegister();
				//JR Z,n
				case 0x28:
					return JumpRelative(JumpConditions.Z);
				//ADD HL,HL
				case 0x29:
					HlRegister = Add16BitRegisters(HlRegister, HlRegister);
					return 8;
				//LD A,(HL+)
				case 0x2A:
					aRegister = ReadHlDecrement(true);
					return 8;
				//DEC HL
				case 0x2B:
					HlRegister = Decrement(HlRegister);
					return 8;
				//INC L
				case 0x2C:
					lRegister = Increment(lRegister);
					return 4;
				//DEC L
				case 0x2D:
					lRegister = Decrement(lRegister);
					return 4;
				//LD L,n
				case 0x2E:
					lRegister = Load8BitImmediate();
					return 8;
				//CPL
				case 0x2F:
					return ComplementARegister();
				//JR NC,n
				case 0x30:
					return JumpRelative(JumpConditions.Nc);
				//LD SP,nn
				case 0x31:
					stackPointer = Load16BitImmediate();
					return 12;
				//LD (HL-),A
				case 0x32:
					return WriteHlDecrement(aRegister, false);
				//INC SP
				case 0x33:
					stackPointer = Increment(stackPointer);
					return 8;
				//DEC (HL)
				case 0x35:
					memory.Write(HlRegister, Decrement(memory.Read(HlRegister)));
					return 12;
				//LD (HL),n
				case 0x36:
					memory.Write(HlRegister, Load8BitImmediate());
					return 12;
				//JR C,n
				case 0x38:
					return JumpRelative(JumpConditions.C);
				//ADD HL,SP
				case 0x39:
					HlRegister = Add16BitRegisters(HlRegister, stackPointer);
					return 8;
				//DEC SP
				case 0x3B:
					stackPointer = Decrement(stackPointer);
					return 8;
				//INC A
				case 0x3C:
					aRegister = Increment(aRegister);
					return 4;
				//DEC A
				case 0x3D:
					aRegister = Decrement(aRegister);
					return 4;
				//LD A,n
				case 0x3E:
					aRegister = Load8BitImmediate();
					return 8;
				//LD B,B
				case 0x40:
					//Useless Opcode
					return 4;
				//LD B,C
				case 0x41:
					bRegister = cRegister;
					return 4;
				//LD B,D
				case 0x42:
					bRegister = dRegister;
					return 4;
				//LD B,E
				case 0x43:
					bRegister = eRegister;
					return 4;
				//LD B,H
				case 0x44:
					bRegister = hRegister;
					return 4;
				//LD B,L
				case 0x45:
					bRegister = lRegister;
					return 4;
				//LD B,(HL)
				case 0x46:
					bRegister = memory.Read(HlRegister);
					return 8;
				//LD B,A
				case 0x47:
					bRegister = aRegister;
					return 4;
				//LD C,B
				case 0x48:
					cRegister = bRegister;
					return 4;
				//LD C,C
				case 0x49:
					//Useless Opcode
					return 4;
				//LD C,D
				case 0x4A:
					cRegister = dRegister;
					return 4;
				//LD C,E
				case 0x4B:
					cRegister = eRegister;
					return 4;
				//LD C,H
				case 0x4C:
					cRegister = hRegister;
					return 4;
				//LD C,L
				case 0x4D:
					cRegister = lRegister;
					return 4;
				//LD C,(HL)
				case 0x4E:
					cRegister = memory.Read(HlRegister);
					return 8;
				//LD C,A
				case 0x4F:
					cRegister = aRegister;
					return 4;
				//LD D,B
				case 0x50:
					dRegister = bRegister;
					return 4;
				//LD D,C
				case 0x51:
					dRegister = cRegister;
					return 4;
				//LD D,D
				case 0x52:
					//Useless Opcode
					return 4;
				//LD D,E
				case 0x53:
					dRegister = eRegister;
					return 4;
				//LD D,H
				case 0x54:
					dRegister = hRegister;
					return 4;
				//LD D,L
				case 0x55:
					dRegister = lRegister;
					return 4;
				//LD D,(HL)
				case 0x56:
					dRegister = memory.Read(HlRegister);
					return 8;
				//LD D,A
				case 0x57:
					dRegister = aRegister;
					return 4;
				//LD E,B
				case 0x58:
					eRegister = bRegister;
					return 4;
				//LD E,C
				case 0x59:
					eRegister = cRegister;
					return 4;
				//LD E,D
				case 0x5A:
					eRegister = dRegister;
					return 4;
				//LD E,E
				case 0x5B:
					//Useless Opcode
					return 4;
				//LD E,H
				case 0x5C:
					eRegister = hRegister;
					return 4;
				//LD E,L
				case 0x5D:
					eRegister = lRegister;
					return 4;
				//LD E,(HL)
				case 0x5E:
					eRegister = memory.Read(HlRegister);
					return 8;
				//LD E,A
				case 0x5F:
					eRegister = aRegister;
					return 4;
				//LD H,B
				case 0x60:
					hRegister = bRegister;
					return 4;
				//LD H,C
				case 0x61:
					hRegister = cRegister;
					return 4;
				//LD H,D
				case 0x62:
					hRegister = dRegister;
					return 4;
				//LD H,E
				case 0x63:
					hRegister = eRegister;
					return 4;
				//LD H,H
				case 0x64:
					//Useless Opcode
					return 4;
				//LD H,L
				case 0x65:
					hRegister = lRegister;
					return 4;
				//LD H,(HL)
				case 0x66:
					hRegister = memory.Read(HlRegister);
					return 8;
				//LD H,A
				case 0x67:
					hRegister = aRegister;
					return 4;
				//LD L,B
				case 0x68:
					lRegister = bRegister;
					return 4;
				//LD L,C
				case 0x69:
					lRegister = cRegister;
					return 4;
				//LD L,D
				case 0x6A:
					lRegister = dRegister;
					return 4;
				//LD L,E
				case 0x6B:
					lRegister = eRegister;
					return 4;
				//LD L,H
				case 0x6C:
					lRegister = hRegister;
					return 4;
				//LD L,L
				case 0x6D:
					//Useless Opcode
					return 4;
				//LD L,(HL)
				case 0x6E:
					lRegister = memory.Read(HlRegister);
					return 8;
				//LD L,A
				case 0x6F:
					lRegister = aRegister;
					return 4;
				//LD (HL),B
				case 0x70:
					memory.Write(HlRegister, bRegister);
					return 8;
				//LD (HL),C
				case 0x71:
					memory.Write(HlRegister, cRegister);
					return 8;
				//LD (HL),D
				case 0x72:
					memory.Write(HlRegister, dRegister);
					return 8;
				//LD (HL),E
				case 0x73:
					memory.Write(HlRegister, eRegister);
					return 8;
				//LD (HL),H
				case 0x74:
					memory.Write(HlRegister, hRegister);
					return 8;
				//LD (HL),L
				case 0x75:
					memory.Write(HlRegister, lRegister);
					return 8;
				//LD (HL),A
				case 0x77:
					memory.Write(HlRegister, aRegister);
					return 8;
				//LD A,B
				case 0x78:
					aRegister = bRegister;
					return 4;
				//LD A,C
				case 0x79:
					aRegister = cRegister;
					return 4;
				//LD A,D
				case 0x7A:
					aRegister = dRegister;
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
				//LD A,(HL)
				case 0x7E:
					aRegister = memory.Read(HlRegister);
					return 8;
				//LD A,A
				case 0x7F:
					//Useless Opcode
					return 4;
				//ADD A,L
				case 0x85:
					AddByteToAReg(lRegister);
					return 4;
				//ADD A,(HL)
				case 0x86:
					AddByteToAReg(memory.Read(HlRegister));
					return 8;
				//SUB B
				case 0x90:
					SubtractByteFromAReg(bRegister);
					return 4;
				//AND A
				case 0xA7:
					AndIntoA(aRegister);
					return 4;
				//XOR C
				case 0xA9:
					XorIntoA(cRegister);
					return 4;
				//XOR L
				case 0xAD:
					XorIntoA(lRegister);
					return 4;
				//XOR (HL)
				case 0xAE:
					XorIntoA(memory.Read(HlRegister));
					return 8;
				//XOR A
				case 0xAF:
					XorIntoA(aRegister);
					return 4;
				//OR B
				case 0xB0:
					OrIntoA(bRegister);
					return 4;
				//OR C
				case 0xB1:
					OrIntoA(cRegister);
					return 4;
				//OR (HL)
				case 0xB6:
					OrIntoA(memory.Read(HlRegister));
					return 8;
				//OR A
				case 0xB7:
					OrIntoA(aRegister);
					return 4;
				//CP B
				case 0xB8:
					SubtractByteFromAReg(bRegister, true);
					return 4;
				//CP C
				case 0xB9:
					SubtractByteFromAReg(cRegister, true);
					return 4;
				//CP D
				case 0xBA:
					SubtractByteFromAReg(dRegister, true);
					return 4;
				//CP E
				case 0xBB:
					SubtractByteFromAReg(eRegister, true);
					return 4;
				//CP (HL)
				case 0xBE:
					SubtractByteFromAReg(memory.Read(HlRegister), true);
					return 8;
				//RET NZ
				case 0xC0:
					return ReturnSubroutine(JumpConditions.Nz);
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
				//ADD A,n
				case 0xC6:
					AddByteToAReg(Load8BitImmediate());
					return 8;
				//RST 00H
				case 0xC7:
					return ResetOpcode(0x00);
				//RET Z
				case 0xC8:
					return ReturnSubroutine(JumpConditions.Z);
				//RET
				case 0xC9:
					return ReturnSubroutine(JumpConditions.NoCondition);
				//JP Z,nn
				case 0xCA:
					return JumpImmediate(JumpConditions.Z);
				//Extended Opcode
				case 0xCB:
					return ExecuteExtendedOpcode();
				//CALL Z,nn
				case 0xCC:
					return CallSubroutine(JumpConditions.Z);
				//CALL nn
				case 0xCD:
					return CallSubroutine(JumpConditions.NoCondition);
				//ADC A,n
				case 0xCE:
					AddByteToAReg(Load8BitImmediate(), CarryFlag);
					return 8;
				//RST 08H
				case 0xCF:
					return ResetOpcode(0x08);
				//RET NC
				case 0xD0:
					return ReturnSubroutine(JumpConditions.Nc);
				//POP DE
				case 0xD1:
					DeRegister = PopStack();
					return 12;
				//JP NC,nn
				case 0xD2:
					return JumpImmediate(JumpConditions.Nc);
				//CALL NC,nn
				case 0xD4:
					return CallSubroutine(JumpConditions.Nc);
				//PUSH DE
				case 0xD5:
					return PushStack(DeRegister);
				//SUB n
				case 0xD6:
					SubtractByteFromAReg(Load8BitImmediate());
					return 8;
				//RST 10H
				case 0xD7:
					return ResetOpcode(0x10);
				//RET C
				case 0xD8:
					return ReturnSubroutine(JumpConditions.C);
				//RETI
				case 0xD9:
					enableInterrupts = InterruptStatus.ThisCycle;
					return ReturnSubroutine(JumpConditions.NoCondition);
				//JP C,nn
				case 0xDA:
					return JumpImmediate(JumpConditions.C);
				//CALL C,nn
				case 0xDC:
					return CallSubroutine(JumpConditions.C);
				//SBC A,n
				case 0xDE:
					SubtractByteFromAReg(Load8BitImmediate(), false, CarryFlag);
					return 8;
				//RST 18H
				case 0xDF:
					return ResetOpcode(0x18);
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
				//RST 20H
				case 0xE7:
					return ResetOpcode(0x20);
				//ADD SP,n
				case 0xE8:
					stackPointer = AddUnsignedImmediateToStackPointer();
					return 16;
				//JP (HL)
				case 0xE9:
					programCounter = HlRegister;
					return 4;
				//LD (nn),A
				case 0xEA:
					memory.Write(Load16BitImmediate(), aRegister);
					return 16;
				//XOR n
				case 0xEE:
					XorIntoA(Load8BitImmediate());
					return 8;
				//RST 28H
				case 0xEF:
					return ResetOpcode(0x28);
				//LD A,(0xFF00+n)
				case 0xF0:
					aRegister = memory.Read((ushort)(0xFF00 + Load8BitImmediate()));
					return 12;
				//POP AF
				case 0xF1:
					AfRegister = PopStack();
					return 12;
				//DI
				case 0xF3:
					disableInterrupts = InterruptStatus.NextCycle;
					return 4;
				//PUSH AF
				case 0xF5:
					return PushStack(AfRegister);
				//OR n
				case 0xF6:
					OrIntoA(Load8BitImmediate());
					return 8;
				//RST 30H
				case 0xF7:
					return ResetOpcode(0x30);
				//LD HL,SP+n
				case 0xF8:
					HlRegister = AddUnsignedImmediateToStackPointer();
					return 12;
				//LD SP,HL
				case 0xF9:
					stackPointer = HlRegister;
					return 8;
				//LD A,(nn)
				case 0xFA:
					aRegister = memory.Read(Load16BitImmediate());
					return 16;
				//CP n
				case 0xFE:
					SubtractByteFromAReg(Load8BitImmediate(), true);
					return 8;
				//RST 38H
				case 0xFF:
					return ResetOpcode(0x38);

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
					cRegister = RotateLeftThroughCarry(cRegister);
					return 8;
				//RR C
				case 0x19:
					cRegister = RotateRightThroughCarry(cRegister);
					return 8;
				//RR D
				case 0x1A:
					dRegister = RotateRightThroughCarry(dRegister);
					return 8;
				//RR E
				case 0x1B:
					eRegister = RotateRightThroughCarry(eRegister);
					return 8;
				//SWAP A
				case 0x37:
					aRegister = SwapNibbles(aRegister);
					return 8;
				//SRL B
				case 0x38:
					bRegister = ShiftRightIntoCarryMsb0(bRegister);
					return 8;
				//BIT 7,H
				case 0x7C:
					BitOpcode(hRegister, 7);
					return 8;
				//BIT 7,(HL)
				case 0x7E:
					BitOpcode(memory.Read(HlRegister), 7);
					return 16;
				//RES 7,(HL)
				case 0xBE:
					memory.Write(HlRegister, SetBit(memory.Read(HlRegister), 7, false));
					return 16;
				//SET 7,(HL)
				case 0xFE:
					memory.Write(HlRegister, SetBit(memory.Read(HlRegister), 7, true));
					return 16;

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

		private void BitOpcode(byte data, int bit)
		{
			SetFlags(!GetBit(data, bit), 0, 1, "");
		}

		private int ComplementARegister()
		{
			aRegister = (byte)~aRegister;

			SetFlags("", 1, 1, "");

			return 4;
		}

		private byte SwapNibbles(byte data)
		{
			byte lowerNibble = (byte)(data & 0xF);
			data >>= 4;
			data |=  (byte)(lowerNibble << 4);

			SetFlags(data == 0, 0, 0, 0);

			return data;
		}

		private byte RotateLeftThroughCarry(byte data)
		{
			bool bit7 = GetBit(data, 7);

			data <<= 1;
			data =   SetBit(data, 0, CarryFlag);

			SetFlags(data == 0, 0, 0, bit7);

			return data;
		}

		private byte RotateLeftIntoCarry(byte data)
		{
			bool bit7 = GetBit(data, 7);

			data <<= 1;
			data =   SetBit(data, 0, bit7);

			SetFlags(data == 0, 0, 0, bit7);

			return data;
		}

		private byte RotateRightThroughCarry(byte data)
		{
			bool bit0 = GetBit(data, 0);

			data >>= 1;
			data =   SetBit(data, 7, CarryFlag);

			SetFlags(data == 0, 0, 0, bit0);

			return data;
		}

		private byte RotateRightIntoCarry(byte data)
		{
			bool bit0 = GetBit(data, 0);

			data >>= 1;
			data =   SetBit(data, 7, bit0);

			SetFlags(data == 0, 0, 0, bit0);

			return data;
		}

		private byte ShiftRightIntoCarryMsb0(byte data)
		{
			bool bit0 = GetBit(data, 0);

			data >>= 1;
			data =   SetBit(data, 7, false);

			SetFlags(data == 0, 0, 0, bit0);

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

		private int LoadStackPointerIntoMemory()
		{
			ushort address = Load16BitImmediate();

			memory.Write(address++, GetLoByte(stackPointer));
			memory.Write(address, GetHiByte(stackPointer));

			return 20;
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

			PushStack(programCounter);
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

		private int ResetOpcode(byte address)
		{
			PushStack(programCounter);

			programCounter = address;

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

		private void SubtractByteFromAReg(byte data, bool compare = false, bool withCarry = false)
		{
			int result = aRegister - data - (withCarry ? 1 : 0);
			
			bool halfCarry = ToBool(((aRegister & 0xF) - (data & 0xF) - (withCarry ? 1 : 0)) & 0x10);
			bool carryFlag = result < 0;

			SetFlags((byte)result == 0, 1, halfCarry, carryFlag);

			if (!compare) aRegister = (byte)result;
		}

		private void AddByteToAReg(byte data, bool withCarry = false)
		{
			bool halfCarry = ToBool(((aRegister & 0xF) + (data & 0xF) + (withCarry ? 1 : 0)) & 0x10);
			bool carryFlag = (aRegister + data + (withCarry ? 1 : 0)) > 255;

			aRegister += (byte)(data + (withCarry ? 1 : 0));

			SetFlags(aRegister == 0, 0, halfCarry, carryFlag);
		}

		private ushort Add16BitRegisters(ushort data1, ushort data2)
		{
			bool halfCarry = ToBool(((data1 & 0xFFF) + (data2 & 0xFFF)) & 0x1000);
			bool carry     = (data1 + data2) > 0xFFFF;

			SetFlags("", 0, halfCarry, carry);

			return (ushort)(data1 + data2);
		}

		private int DecimalAdjustARegister()
		{
			int correction = 0;

			if (HalfCarryFlag || (!SubtractFlag && ((aRegister & 0xF) > 9))) correction |= 6;

			if (CarryFlag || (!SubtractFlag && aRegister > 0x99))
			{
				correction |= 0x60;
				CarryFlag  =  true;
			}

			aRegister += (byte)(SubtractFlag ? -correction : correction);

			ZeroFlag = aRegister == 0;

			HalfCarryFlag = false;

			return 4;
		}

		private ushort AddUnsignedImmediateToStackPointer()
		{
			byte  dataU = Load8BitImmediate();
			sbyte dataS = (sbyte)dataU;

			bool halfCarry = ToBool(((stackPointer & 0xF) + (dataU & 0xF)) & 0x10);
			bool carry     = ((stackPointer & 0xFF) + dataU) > 0xFF;

			SetFlags(0, 0, halfCarry, carry);

			return (ushort)(stackPointer + dataS);
		}

		//Stack functions
		private int PushStack(ushort data)
		{
			byte lo = GetLoByte(data);
			byte hi = GetHiByte(data);

			memory.Write(--stackPointer, hi);
			memory.Write(--stackPointer, lo);

			return 16;
		}

		private ushort PopStack()
		{
			byte lo = memory.Read(stackPointer++);
			byte hi = memory.Read(stackPointer++);

			return MakeWord(hi, lo);
		}
	}
}