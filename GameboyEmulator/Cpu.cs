using System;

namespace GameboyEmulator;

public class Cpu
{
	private enum JumpCondition
	{
		Nz,
		Z,
		Nc,
		C,
		Always
	}

	private enum HaltMode
	{
		Halted,
		HaltBug,
		NotHalted
	}

	private readonly Emulator emulator;

	public Cpu(Emulator emulator)
	{
		this.emulator = emulator;
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
		get => MakeWord(aRegister, FlagRegister);
		set
		{
			aRegister    = GetHiByte(value);
			FlagRegister = GetLoByte(value);
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
	private byte internalFlagRegister;

	private byte FlagRegister
	{
		//Lower nibble of Flag Register should always be zero
		get => (byte)(internalFlagRegister & 0b11110000);
		set => internalFlagRegister = (byte)(value & 0b11110000);
	}

	private bool ZeroFlag
	{
		get => GetBit(FlagRegister, 7);
		set => FlagRegister = SetBit(FlagRegister, 7, value);
	}

	private bool SubtractFlag
	{
		get => GetBit(FlagRegister, 6);
		set => FlagRegister = SetBit(FlagRegister, 6, value);
	}

	private bool HalfCarryFlag
	{
		get => GetBit(FlagRegister, 5);
		set => FlagRegister = SetBit(FlagRegister, 5, value);
	}

	private bool CarryFlag
	{
		get => GetBit(FlagRegister, 4);
		set => FlagRegister = SetBit(FlagRegister, 4, value);
	}

	//Constants
	public const int MAX_CYCLES_PER_FRAME = 70224;

	//Flags
	private HaltMode haltMode = HaltMode.NotHalted;
	private int      waitNopAmount;

	public int ExecuteOpcode()
	{
		//Emulates length it takes to service interrupts
		if (waitNopAmount > 0)
		{
			waitNopAmount--;
			return 4;
		}

		//If cpu is halted, return without executing an opcode
		if (haltMode == HaltMode.Halted) return 4;

		HandleHaltBug();

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
			//LD (BC),A
			case 0x02:
				emulator.memory.Write(BcRegister, aRegister);
				return 8;
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
				aRegister = RotateLeftIntoCarry(aRegister, true);
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
				aRegister = emulator.memory.Read(BcRegister);
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
			//RRCA
			case 0x0F:
				aRegister = RotateRightIntoCarry(aRegister, true);
				return 4;
			//STOP
			case 0x10:
				Logger.LogMessage("STOP Opcode not implemented yet", Logger.LogLevel.Error, true);

				Environment.Exit(1);
				return 0; //Useless return but the program does not compile without it
			//LD DE,nn
			case 0x11:
				DeRegister = Load16BitImmediate();
				return 12;
			//LD (DE),A
			case 0x12:
				emulator.memory.Write(DeRegister, aRegister);
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
				aRegister = RotateLeftThroughCarry(aRegister, true);
				return 4;
			//JR n
			case 0x18:
				return JumpRelative(JumpCondition.Always);
			//ADD HL,DE
			case 0x19:
				HlRegister = Add16BitRegisters(HlRegister, DeRegister);
				return 8;
			//LD A,(DE)
			case 0x1A:
				aRegister = emulator.memory.Read(DeRegister);
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
				aRegister = RotateRightThroughCarry(aRegister, true);
				return 4;
			//JR NZ,n
			case 0x20:
				return JumpRelative(JumpCondition.Nz);
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
				return JumpRelative(JumpCondition.Z);
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
				return JumpRelative(JumpCondition.Nc);
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
			//INC (HL)
			case 0x34:
				emulator.memory.Write(HlRegister, Increment(emulator.memory.Read(HlRegister)));
				return 12;
			//DEC (HL)
			case 0x35:
				emulator.memory.Write(HlRegister, Decrement(emulator.memory.Read(HlRegister)));
				return 12;
			//LD (HL),n
			case 0x36:
				emulator.memory.Write(HlRegister, Load8BitImmediate());
				return 12;
			//SCF
			case 0x37:
				return SetCarryFlagOpcode();
			//JR C,n
			case 0x38:
				return JumpRelative(JumpCondition.C);
			//ADD HL,SP
			case 0x39:
				HlRegister = Add16BitRegisters(HlRegister, stackPointer);
				return 8;
			//LD A,(HL-)
			case 0x3A:
				aRegister = ReadHlDecrement(false);
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
			//CCF
			case 0x3F:
				return ComplementCarryFlagOpcode();
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
				bRegister = emulator.memory.Read(HlRegister);
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
				cRegister = emulator.memory.Read(HlRegister);
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
				dRegister = emulator.memory.Read(HlRegister);
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
				eRegister = emulator.memory.Read(HlRegister);
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
				hRegister = emulator.memory.Read(HlRegister);
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
				lRegister = emulator.memory.Read(HlRegister);
				return 8;
			//LD L,A
			case 0x6F:
				lRegister = aRegister;
				return 4;
			//LD (HL),B
			case 0x70:
				emulator.memory.Write(HlRegister, bRegister);
				return 8;
			//LD (HL),C
			case 0x71:
				emulator.memory.Write(HlRegister, cRegister);
				return 8;
			//LD (HL),D
			case 0x72:
				emulator.memory.Write(HlRegister, dRegister);
				return 8;
			//LD (HL),E
			case 0x73:
				emulator.memory.Write(HlRegister, eRegister);
				return 8;
			//LD (HL),H
			case 0x74:
				emulator.memory.Write(HlRegister, hRegister);
				return 8;
			//LD (HL),L
			case 0x75:
				emulator.memory.Write(HlRegister, lRegister);
				return 8;
			//HALT
			case 0x76:
				return HaltCpu();
			//LD (HL),A
			case 0x77:
				emulator.memory.Write(HlRegister, aRegister);
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
				aRegister = emulator.memory.Read(HlRegister);
				return 8;
			//LD A,A
			case 0x7F:
				//Useless Opcode
				return 4;
			//ADD A,B
			case 0x80:
				AddByteToAReg(bRegister);
				return 4;
			//ADD A,C
			case 0x81:
				AddByteToAReg(cRegister);
				return 4;
			//ADD A,D
			case 0x82:
				AddByteToAReg(dRegister);
				return 4;
			//ADD A,E
			case 0x83:
				AddByteToAReg(eRegister);
				return 4;
			//ADD A,H
			case 0x84:
				AddByteToAReg(hRegister);
				return 4;
			//ADD A,L
			case 0x85:
				AddByteToAReg(lRegister);
				return 4;
			//ADD A,(HL)
			case 0x86:
				AddByteToAReg(emulator.memory.Read(HlRegister));
				return 8;
			//ADD A,A
			case 0x87:
				AddByteToAReg(aRegister);
				return 4;
			//ADC A,B
			case 0x88:
				AddByteToAReg(bRegister, CarryFlag);
				return 4;
			//ADC A,C
			case 0x89:
				AddByteToAReg(cRegister, CarryFlag);
				return 4;
			//ADC A,D
			case 0x8A:
				AddByteToAReg(dRegister, CarryFlag);
				return 4;
			//ADC A,E
			case 0x8B:
				AddByteToAReg(eRegister, CarryFlag);
				return 4;
			//ADC A,H
			case 0x8C:
				AddByteToAReg(hRegister, CarryFlag);
				return 4;
			//ADC A,L
			case 0x8D:
				AddByteToAReg(lRegister, CarryFlag);
				return 4;
			//ADC A,(HL)
			case 0x8E:
				AddByteToAReg(emulator.memory.Read(HlRegister), CarryFlag);
				return 8;
			//ADC A,A
			case 0x8F:
				AddByteToAReg(aRegister, CarryFlag);
				return 4;
			//SUB B
			case 0x90:
				SubtractByteFromAReg(bRegister);
				return 4;
			//SUB C
			case 0x91:
				SubtractByteFromAReg(cRegister);
				return 4;
			//SUB D
			case 0x92:
				SubtractByteFromAReg(dRegister);
				return 4;
			//SUB E
			case 0x93:
				SubtractByteFromAReg(eRegister);
				return 4;
			//SUB H
			case 0x94:
				SubtractByteFromAReg(hRegister);
				return 4;
			//SUB L
			case 0x95:
				SubtractByteFromAReg(lRegister);
				return 4;
			//SUB (HL)
			case 0x96:
				SubtractByteFromAReg(emulator.memory.Read(HlRegister));
				return 8;
			//SUB A
			case 0x97:
				SubtractByteFromAReg(aRegister);
				return 4;
			//SBC A,B
			case 0x98:
				SubtractByteFromAReg(bRegister, false, CarryFlag);
				return 4;
			//SBC A,C
			case 0x99:
				SubtractByteFromAReg(cRegister, false, CarryFlag);
				return 4;
			//SBC A,D
			case 0x9A:
				SubtractByteFromAReg(dRegister, false, CarryFlag);
				return 4;
			//SBC A,E
			case 0x9B:
				SubtractByteFromAReg(eRegister, false, CarryFlag);
				return 4;
			//SBC A,H
			case 0x9C:
				SubtractByteFromAReg(hRegister, false, CarryFlag);
				return 4;
			//SBC A,L
			case 0x9D:
				SubtractByteFromAReg(lRegister, false, CarryFlag);
				return 4;
			//SBC A,(HL)
			case 0x9E:
				SubtractByteFromAReg(emulator.memory.Read(HlRegister), false, CarryFlag);
				return 8;
			//SBC A,A
			case 0x9F:
				SubtractByteFromAReg(aRegister, false, CarryFlag);
				return 4;
			//AND B
			case 0xA0:
				AndIntoA(bRegister);
				return 4;
			//AND C
			case 0xA1:
				AndIntoA(cRegister);
				return 4;
			//AND D
			case 0xA2:
				AndIntoA(dRegister);
				return 4;
			//AND E
			case 0xA3:
				AndIntoA(eRegister);
				return 4;
			//AND H
			case 0xA4:
				AndIntoA(hRegister);
				return 4;
			//AND L
			case 0xA5:
				AndIntoA(lRegister);
				return 4;
			//AND (HL)
			case 0xA6:
				AndIntoA(emulator.memory.Read(HlRegister));
				return 8;
			//AND A
			case 0xA7:
				AndIntoA(aRegister);
				return 4;
			//XOR B
			case 0xA8:
				XorIntoA(bRegister);
				return 4;
			//XOR C
			case 0xA9:
				XorIntoA(cRegister);
				return 4;
			//XOR D
			case 0xAA:
				XorIntoA(dRegister);
				return 4;
			//XOR E
			case 0xAB:
				XorIntoA(eRegister);
				return 4;
			//XOR H
			case 0xAC:
				XorIntoA(hRegister);
				return 4;
			//XOR L
			case 0xAD:
				XorIntoA(lRegister);
				return 4;
			//XOR (HL)
			case 0xAE:
				XorIntoA(emulator.memory.Read(HlRegister));
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
			//OR D
			case 0xB2:
				OrIntoA(dRegister);
				return 4;
			//OR E
			case 0xB3:
				OrIntoA(eRegister);
				return 4;
			//OR H
			case 0xB4:
				OrIntoA(hRegister);
				return 4;
			//OR L
			case 0xB5:
				OrIntoA(lRegister);
				return 4;
			//OR (HL)
			case 0xB6:
				OrIntoA(emulator.memory.Read(HlRegister));
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
			//CP H
			case 0xBC:
				SubtractByteFromAReg(hRegister, true);
				return 4;
			//CP L
			case 0xBD:
				SubtractByteFromAReg(lRegister, true);
				return 4;
			//CP (HL)
			case 0xBE:
				SubtractByteFromAReg(emulator.memory.Read(HlRegister), true);
				return 8;
			//CP A
			case 0xBF:
				SubtractByteFromAReg(aRegister, true);
				return 4;
			//RET NZ
			case 0xC0:
				return ReturnSubroutine(JumpCondition.Nz);
			//POP BC
			case 0xC1:
				BcRegister = PopStack();
				return 12;
			//JP NZ,nn
			case 0xC2:
				return JumpImmediate(JumpCondition.Nz);
			//JP nn
			case 0xC3:
				return JumpImmediate(JumpCondition.Always);
			//CALL NZ,nn
			case 0xC4:
				return CallSubroutine(JumpCondition.Nz);
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
				return ReturnSubroutine(JumpCondition.Z);
			//RET
			case 0xC9:
				return ReturnSubroutine(JumpCondition.Always);
			//JP Z,nn
			case 0xCA:
				return JumpImmediate(JumpCondition.Z);
			//Extended Opcode
			case 0xCB:
				return ExecuteExtendedOpcode();
			//CALL Z,nn
			case 0xCC:
				return CallSubroutine(JumpCondition.Z);
			//CALL nn
			case 0xCD:
				return CallSubroutine(JumpCondition.Always);
			//ADC A,n
			case 0xCE:
				AddByteToAReg(Load8BitImmediate(), CarryFlag);
				return 8;
			//RST 08H
			case 0xCF:
				return ResetOpcode(0x08);
			//RET NC
			case 0xD0:
				return ReturnSubroutine(JumpCondition.Nc);
			//POP DE
			case 0xD1:
				DeRegister = PopStack();
				return 12;
			//JP NC,nn
			case 0xD2:
				return JumpImmediate(JumpCondition.Nc);
			//Invalid Opcode
			//0xD3
			//CALL NC,nn
			case 0xD4:
				return CallSubroutine(JumpCondition.Nc);
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
				return ReturnSubroutine(JumpCondition.C);
			//RETI
			case 0xD9:
				emulator.interrupts.enableInterruptsStatus = Interrupts.EnableInterruptsStatus.ThisCycle;
				return ReturnSubroutine(JumpCondition.Always);
			//JP C,nn
			case 0xDA:
				return JumpImmediate(JumpCondition.C);
			//Invalid Opcode
			//0xDB
			//CALL C,nn
			case 0xDC:
				return CallSubroutine(JumpCondition.C);
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
			//Invalid Opcode
			//0xE3
			////Invalid Opcode
			//0xE4
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
			//JP HL
			case 0xE9:
				programCounter = HlRegister;
				return 4;
			//LD (nn),A
			case 0xEA:
				emulator.memory.Write(Load16BitImmediate(), aRegister);
				return 16;
			//Invalid Opcode
			//0xEB
			//Invalid Opcode
			//0xEC
			//Invalid Opcode
			//0xED
			//XOR n
			case 0xEE:
				XorIntoA(Load8BitImmediate());
				return 8;
			//RST 28H
			case 0xEF:
				return ResetOpcode(0x28);
			//LD A,(0xFF00+n)
			case 0xF0:
				aRegister = emulator.memory.Read((ushort)(0xFF00 + Load8BitImmediate()));
				return 12;
			//POP AF
			case 0xF1:
				AfRegister = PopStack();
				return 12;
			//LD A,(0xFF00+C)
			case 0xF2:
				aRegister = emulator.memory.Read((ushort)(0xFF00 + cRegister));
				return 8;
			//DI
			case 0xF3:
				emulator.interrupts.InterruptMasterEnable = false;

				//Make sure interrupts are not enabled next cycle
				emulator.interrupts.enableInterruptsStatus = Interrupts.EnableInterruptsStatus.None;
				return 4;
			//Invalid Opcode
			//0xF4
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
				aRegister = emulator.memory.Read(Load16BitImmediate());
				return 16;
			//EI
			case 0xFB:
				//Interrupts are actually enabled one cycle after this instruction is executed
				emulator.interrupts.enableInterruptsStatus = Interrupts.EnableInterruptsStatus.NextCycle;
				return 4;
			//Invalid Opcode
			//0xFC
			//Invalid Opcode
			//0xFD
			//CP n
			case 0xFE:
				SubtractByteFromAReg(Load8BitImmediate(), true);
				return 8;
			//RST 38H
			case 0xFF:
				return ResetOpcode(0x38);

			//Invalid Opcode
			default:
				Logger.LogMessage(
					$"Invalid Opcode 0x{opcode:X} encountered at 0x{programCounter - 1:X}!", Logger.LogLevel.Error,
					true
				);

				Environment.Exit(1);
				return 0; //Useless return but the program does not compile without it
		}
	}

	private void HandleHaltBug()
	{
		if (haltMode != HaltMode.HaltBug) return;

		//If Halt Bug occurs, the Program Counter does not get increased after fetching Opcode 
		programCounter -= 1;
		haltMode       =  HaltMode.NotHalted;
	}

	private int ExecuteExtendedOpcode()
	{
		byte opcode = Load8BitImmediate();

		switch (opcode)
		{
			//RLC B
			case 0x00:
				bRegister = RotateLeftIntoCarry(bRegister);
				return 8;
			//RLC C
			case 0x01:
				cRegister = RotateLeftIntoCarry(cRegister);
				return 8;
			//RLC D
			case 0x02:
				dRegister = RotateLeftIntoCarry(dRegister);
				return 8;
			//RLC E
			case 0x03:
				eRegister = RotateLeftIntoCarry(eRegister);
				return 8;
			//RLC H
			case 0x04:
				hRegister = RotateLeftIntoCarry(hRegister);
				return 8;
			//RLC L
			case 0x05:
				lRegister = RotateLeftIntoCarry(lRegister);
				return 8;
			//RLC (HL)
			case 0x06:
				emulator.memory.Write(HlRegister, RotateLeftIntoCarry(emulator.memory.Read(HlRegister)));
				return 16;
			//RLC A
			case 0x07:
				aRegister = RotateLeftIntoCarry(aRegister);
				return 8;
			//RRC B
			case 0x08:
				bRegister = RotateRightIntoCarry(bRegister);
				return 8;
			//RRC C
			case 0x09:
				cRegister = RotateRightIntoCarry(cRegister);
				return 8;
			//RRC D
			case 0x0A:
				dRegister = RotateRightIntoCarry(dRegister);
				return 8;
			//RRC E
			case 0x0B:
				eRegister = RotateRightIntoCarry(eRegister);
				return 8;
			//RRC H
			case 0x0C:
				hRegister = RotateRightIntoCarry(hRegister);
				return 8;
			//RRC L
			case 0x0D:
				lRegister = RotateRightIntoCarry(lRegister);
				return 8;
			//RRC (HL)
			case 0x0E:
				emulator.memory.Write(HlRegister, RotateRightIntoCarry(emulator.memory.Read(HlRegister)));
				return 16;
			//RRC A
			case 0x0F:
				aRegister = RotateRightIntoCarry(aRegister);
				return 8;
			//RL B
			case 0x10:
				bRegister = RotateLeftThroughCarry(bRegister);
				return 8;
			//RL C
			case 0x11:
				cRegister = RotateLeftThroughCarry(cRegister);
				return 8;
			//RL D
			case 0x12:
				dRegister = RotateLeftThroughCarry(dRegister);
				return 8;
			//RL E
			case 0x13:
				eRegister = RotateLeftThroughCarry(eRegister);
				return 8;
			//RL H
			case 0x14:
				hRegister = RotateLeftThroughCarry(hRegister);
				return 8;
			//RL L
			case 0x15:
				lRegister = RotateLeftThroughCarry(lRegister);
				return 8;
			//RL (HL)
			case 0x16:
				emulator.memory.Write(HlRegister, RotateLeftThroughCarry(emulator.memory.Read(HlRegister)));
				return 16;
			//RL A
			case 0x17:
				aRegister = RotateLeftThroughCarry(aRegister);
				return 8;
			//RR B
			case 0x18:
				bRegister = RotateRightThroughCarry(bRegister);
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
			//RR H
			case 0x1C:
				hRegister = RotateRightThroughCarry(hRegister);
				return 8;
			//RR L
			case 0x1D:
				lRegister = RotateRightThroughCarry(lRegister);
				return 8;
			//RR (HL)
			case 0x1E:
				emulator.memory.Write(HlRegister, RotateRightThroughCarry(emulator.memory.Read(HlRegister)));
				return 16;
			//RR A
			case 0x1F:
				aRegister = RotateRightThroughCarry(aRegister);
				return 8;
			//SLA B
			case 0x20:
				bRegister = ShiftLeftIntoCarryLsb0(bRegister);
				return 8;
			//SLA C
			case 0x21:
				cRegister = ShiftLeftIntoCarryLsb0(cRegister);
				return 8;
			//SLA D
			case 0x22:
				dRegister = ShiftLeftIntoCarryLsb0(dRegister);
				return 8;
			//SLA E
			case 0x23:
				eRegister = ShiftLeftIntoCarryLsb0(eRegister);
				return 8;
			//SLA H
			case 0x24:
				hRegister = ShiftLeftIntoCarryLsb0(hRegister);
				return 8;
			//SLA L
			case 0x25:
				lRegister = ShiftLeftIntoCarryLsb0(lRegister);
				return 8;
			//SLA (HL)
			case 0x26:
				emulator.memory.Write(HlRegister, ShiftLeftIntoCarryLsb0(emulator.memory.Read(HlRegister)));
				return 16;
			//SLA A
			case 0x27:
				aRegister = ShiftLeftIntoCarryLsb0(aRegister);
				return 8;
			//SRA B
			case 0x28:
				bRegister = ShiftRightIntoCarryKeepMsb(bRegister);
				return 8;
			//SRA C
			case 0x29:
				cRegister = ShiftRightIntoCarryKeepMsb(cRegister);
				return 8;
			//SRA D
			case 0x2A:
				dRegister = ShiftRightIntoCarryKeepMsb(dRegister);
				return 8;
			//SRA E
			case 0x2B:
				eRegister = ShiftRightIntoCarryKeepMsb(eRegister);
				return 8;
			//SRA H
			case 0x2C:
				hRegister = ShiftRightIntoCarryKeepMsb(hRegister);
				return 8;
			//SRA L
			case 0x2D:
				lRegister = ShiftRightIntoCarryKeepMsb(lRegister);
				return 8;
			//SRA (HL)
			case 0x2E:
				emulator.memory.Write(HlRegister, ShiftRightIntoCarryKeepMsb(emulator.memory.Read(HlRegister)));
				return 16;
			//SRA A
			case 0x2F:
				aRegister = ShiftRightIntoCarryKeepMsb(aRegister);
				return 8;
			//SWAP B
			case 0x30:
				bRegister = SwapNibbles(bRegister);
				return 8;
			//SWAP C
			case 0x31:
				cRegister = SwapNibbles(cRegister);
				return 8;
			//SWAP D
			case 0x32:
				dRegister = SwapNibbles(dRegister);
				return 8;
			//SWAP E
			case 0x33:
				eRegister = SwapNibbles(eRegister);
				return 8;
			//SWAP H
			case 0x34:
				hRegister = SwapNibbles(hRegister);
				return 8;
			//SWAP L
			case 0x35:
				lRegister = SwapNibbles(lRegister);
				return 8;
			//SWAP (HL)
			case 0x36:
				emulator.memory.Write(HlRegister, SwapNibbles(emulator.memory.Read(HlRegister)));
				return 16;
			//SWAP A
			case 0x37:
				aRegister = SwapNibbles(aRegister);
				return 8;
			//SRL B
			case 0x38:
				bRegister = ShiftRightIntoCarryMsb0(bRegister);
				return 8;
			//SRL C
			case 0x39:
				cRegister = ShiftRightIntoCarryMsb0(cRegister);
				return 8;
			//SRL D
			case 0x3A:
				dRegister = ShiftRightIntoCarryMsb0(dRegister);
				return 8;
			//SRL E
			case 0x3B:
				eRegister = ShiftRightIntoCarryMsb0(eRegister);
				return 8;
			//SRL H
			case 0x3C:
				hRegister = ShiftRightIntoCarryMsb0(hRegister);
				return 8;
			//SRL L
			case 0x3D:
				lRegister = ShiftRightIntoCarryMsb0(lRegister);
				return 8;
			//SRL (HL)
			case 0x3E:
				emulator.memory.Write(HlRegister, ShiftRightIntoCarryMsb0(emulator.memory.Read(HlRegister)));
				return 16;
			//SRL A
			case 0x3F:
				aRegister = ShiftRightIntoCarryMsb0(aRegister);
				return 8;
			//BIT 0,B
			case 0x40:
				BitOpcode(bRegister, 0);
				return 8;
			//BIT 0,C
			case 0x41:
				BitOpcode(cRegister, 0);
				return 8;
			//BIT 0,D
			case 0x42:
				BitOpcode(dRegister, 0);
				return 8;
			//BIT 0,E
			case 0x43:
				BitOpcode(eRegister, 0);
				return 8;
			//BIT 0,H
			case 0x44:
				BitOpcode(hRegister, 0);
				return 8;
			//BIT 0,L
			case 0x45:
				BitOpcode(lRegister, 0);
				return 8;
			//BIT 0,(HL)
			case 0x46:
				BitOpcode(emulator.memory.Read(HlRegister), 0);
				return 12;
			//BIT 0,A
			case 0x47:
				BitOpcode(aRegister, 0);
				return 8;
			//BIT 1,B
			case 0x48:
				BitOpcode(bRegister, 1);
				return 8;
			//BIT 1,C
			case 0x49:
				BitOpcode(cRegister, 1);
				return 8;
			//BIT 1,D
			case 0x4A:
				BitOpcode(dRegister, 1);
				return 8;
			//BIT 1,E
			case 0x4B:
				BitOpcode(eRegister, 1);
				return 8;
			//BIT 1,H
			case 0x4C:
				BitOpcode(hRegister, 1);
				return 8;
			//BIT 1,L
			case 0x4D:
				BitOpcode(lRegister, 1);
				return 8;
			//BIT 1,(HL)
			case 0x4E:
				BitOpcode(emulator.memory.Read(HlRegister), 1);
				return 12;
			//BIT 1,A
			case 0x4F:
				BitOpcode(aRegister, 1);
				return 8;
			//BIT 2,B
			case 0x50:
				BitOpcode(bRegister, 2);
				return 8;
			//BIT 2,C
			case 0x51:
				BitOpcode(cRegister, 2);
				return 8;
			//BIT 2,D
			case 0x52:
				BitOpcode(dRegister, 2);
				return 8;
			//BIT 2,E
			case 0x53:
				BitOpcode(eRegister, 2);
				return 8;
			//BIT 2,H
			case 0x54:
				BitOpcode(hRegister, 2);
				return 8;
			//BIT 2,L
			case 0x55:
				BitOpcode(lRegister, 2);
				return 8;
			//BIT 2,(HL)
			case 0x56:
				BitOpcode(emulator.memory.Read(HlRegister), 2);
				return 12;
			//BIT 2,A
			case 0x57:
				BitOpcode(aRegister, 2);
				return 8;
			//BIT 3,B
			case 0x58:
				BitOpcode(bRegister, 3);
				return 8;
			//BIT 3,C
			case 0x59:
				BitOpcode(cRegister, 3);
				return 8;
			//BIT 3,D
			case 0x5A:
				BitOpcode(dRegister, 3);
				return 8;
			//BIT 3,E
			case 0x5B:
				BitOpcode(eRegister, 3);
				return 8;
			//BIT 3,H
			case 0x5C:
				BitOpcode(hRegister, 3);
				return 8;
			//BIT 3,L
			case 0x5D:
				BitOpcode(lRegister, 3);
				return 8;
			//BIT 3,(HL)
			case 0x5E:
				BitOpcode(emulator.memory.Read(HlRegister), 3);
				return 12;
			//BIT 3,A
			case 0x5F:
				BitOpcode(aRegister, 3);
				return 8;
			//BIT 4,B
			case 0x60:
				BitOpcode(bRegister, 4);
				return 8;
			//BIT 4,C
			case 0x61:
				BitOpcode(cRegister, 4);
				return 8;
			//BIT 4,D
			case 0x62:
				BitOpcode(dRegister, 4);
				return 8;
			//BIT 4,E
			case 0x63:
				BitOpcode(eRegister, 4);
				return 8;
			//BIT 4,H
			case 0x64:
				BitOpcode(hRegister, 4);
				return 8;
			//BIT 4,L
			case 0x65:
				BitOpcode(lRegister, 4);
				return 8;
			//BIT 4,(HL)
			case 0x66:
				BitOpcode(emulator.memory.Read(HlRegister), 4);
				return 12;
			//BIT 4,A
			case 0x67:
				BitOpcode(aRegister, 4);
				return 8;
			//BIT 5,B
			case 0x68:
				BitOpcode(bRegister, 5);
				return 8;
			//BIT 5,C
			case 0x69:
				BitOpcode(cRegister, 5);
				return 8;
			//BIT 5,D
			case 0x6A:
				BitOpcode(dRegister, 5);
				return 8;
			//BIT 5,E
			case 0x6B:
				BitOpcode(eRegister, 5);
				return 8;
			//BIT 5,H
			case 0x6C:
				BitOpcode(hRegister, 5);
				return 8;
			//BIT 5,L
			case 0x6D:
				BitOpcode(lRegister, 5);
				return 8;
			//BIT 5,(HL)
			case 0x6E:
				BitOpcode(emulator.memory.Read(HlRegister), 5);
				return 12;
			//BIT 5,A
			case 0x6F:
				BitOpcode(aRegister, 5);
				return 8;
			//BIT 6,B
			case 0x70:
				BitOpcode(bRegister, 6);
				return 8;
			//BIT 6,C
			case 0x71:
				BitOpcode(cRegister, 6);
				return 8;
			//BIT 6,D
			case 0x72:
				BitOpcode(dRegister, 6);
				return 8;
			//BIT 6,E
			case 0x73:
				BitOpcode(eRegister, 6);
				return 8;
			//BIT 6,H
			case 0x74:
				BitOpcode(hRegister, 6);
				return 8;
			//BIT 6,L
			case 0x75:
				BitOpcode(lRegister, 6);
				return 8;
			//BIT 6,(HL)
			case 0x76:
				BitOpcode(emulator.memory.Read(HlRegister), 6);
				return 12;
			//BIT 6,A
			case 0x77:
				BitOpcode(aRegister, 6);
				return 8;
			//BIT 7,B
			case 0x78:
				BitOpcode(bRegister, 7);
				return 8;
			//BIT 7,C
			case 0x79:
				BitOpcode(cRegister, 7);
				return 8;
			//BIT 7,D
			case 0x7A:
				BitOpcode(dRegister, 7);
				return 8;
			//BIT 7,E
			case 0x7B:
				BitOpcode(eRegister, 7);
				return 8;
			//BIT 7,H
			case 0x7C:
				BitOpcode(hRegister, 7);
				return 8;
			//BIT 7,L
			case 0x7D:
				BitOpcode(lRegister, 7);
				return 8;
			//BIT 7,(HL)
			case 0x7E:
				BitOpcode(emulator.memory.Read(HlRegister), 7);
				return 12;
			//BIT 7,A
			case 0x7F:
				BitOpcode(aRegister, 7);
				return 8;
			//RES 0,B
			case 0x80:
				bRegister = SetBit(bRegister, 0, false);
				return 8;
			//RES 0,C
			case 0x81:
				cRegister = SetBit(cRegister, 0, false);
				return 8;
			//RES 0,D
			case 0x82:
				dRegister = SetBit(dRegister, 0, false);
				return 8;
			//RES 0,E
			case 0x83:
				eRegister = SetBit(eRegister, 0, false);
				return 8;
			//RES 0,H
			case 0x84:
				hRegister = SetBit(hRegister, 0, false);
				return 8;
			//RES 0,L
			case 0x85:
				lRegister = SetBit(lRegister, 0, false);
				return 8;
			//RES 0,(HL)
			case 0x86:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 0, false));
				return 16;
			//RES 0,A
			case 0x87:
				aRegister = SetBit(aRegister, 0, false);
				return 8;
			//RES 1,B
			case 0x88:
				bRegister = SetBit(bRegister, 1, false);
				return 8;
			//RES 1,C
			case 0x89:
				cRegister = SetBit(cRegister, 1, false);
				return 8;
			//RES 1,D
			case 0x8A:
				dRegister = SetBit(dRegister, 1, false);
				return 8;
			//RES 1,E
			case 0x8B:
				eRegister = SetBit(eRegister, 1, false);
				return 8;
			//RES 1,H
			case 0x8C:
				hRegister = SetBit(hRegister, 1, false);
				return 8;
			//RES 1,L
			case 0x8D:
				lRegister = SetBit(lRegister, 1, false);
				return 8;
			//RES 1,(HL)
			case 0x8E:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 1, false));
				return 16;
			//RES 1,A
			case 0x8F:
				aRegister = SetBit(aRegister, 1, false);
				return 8;
			//RES 2,B
			case 0x90:
				bRegister = SetBit(bRegister, 2, false);
				return 8;
			//RES 2,C
			case 0x91:
				cRegister = SetBit(cRegister, 2, false);
				return 8;
			//RES 2,D
			case 0x92:
				dRegister = SetBit(dRegister, 2, false);
				return 8;
			//RES 2,E
			case 0x93:
				eRegister = SetBit(eRegister, 2, false);
				return 8;
			//RES 2,H
			case 0x94:
				hRegister = SetBit(hRegister, 2, false);
				return 8;
			//RES 2,L
			case 0x95:
				lRegister = SetBit(lRegister, 2, false);
				return 8;
			//RES 2,(HL)
			case 0x96:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 2, false));
				return 16;
			//RES 2,A
			case 0x97:
				aRegister = SetBit(aRegister, 2, false);
				return 8;
			//RES 3,B
			case 0x98:
				bRegister = SetBit(bRegister, 3, false);
				return 8;
			//RES 3,C
			case 0x99:
				cRegister = SetBit(cRegister, 3, false);
				return 8;
			//RES 3,D
			case 0x9A:
				dRegister = SetBit(dRegister, 3, false);
				return 8;
			//RES 3,E
			case 0x9B:
				eRegister = SetBit(eRegister, 3, false);
				return 8;
			//RES 3,H
			case 0x9C:
				hRegister = SetBit(hRegister, 3, false);
				return 8;
			//RES 3,L
			case 0x9D:
				lRegister = SetBit(lRegister, 3, false);
				return 8;
			//RES 3,(HL)
			case 0x9E:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 3, false));
				return 16;
			//RES 3,A
			case 0x9F:
				aRegister = SetBit(aRegister, 3, false);
				return 8;
			//RES 4,B
			case 0xA0:
				bRegister = SetBit(bRegister, 4, false);
				return 8;
			//RES 4,C
			case 0xA1:
				cRegister = SetBit(cRegister, 4, false);
				return 8;
			//RES 4,D
			case 0xA2:
				dRegister = SetBit(dRegister, 4, false);
				return 8;
			//RES 4,E
			case 0xA3:
				eRegister = SetBit(eRegister, 4, false);
				return 8;
			//RES 4,H
			case 0xA4:
				hRegister = SetBit(hRegister, 4, false);
				return 8;
			//RES 4,L
			case 0xA5:
				lRegister = SetBit(lRegister, 4, false);
				return 8;
			//RES 4,(HL)
			case 0xA6:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 4, false));
				return 16;
			//RES 4,A
			case 0xA7:
				aRegister = SetBit(aRegister, 4, false);
				return 8;
			//RES 5,B
			case 0xA8:
				bRegister = SetBit(bRegister, 5, false);
				return 8;
			//RES 5,C
			case 0xA9:
				cRegister = SetBit(cRegister, 5, false);
				return 8;
			//RES 5,D
			case 0xAA:
				dRegister = SetBit(dRegister, 5, false);
				return 8;
			//RES 5,E
			case 0xAB:
				eRegister = SetBit(eRegister, 5, false);
				return 8;
			//RES 5,H
			case 0xAC:
				hRegister = SetBit(hRegister, 5, false);
				return 8;
			//RES 5,L
			case 0xAD:
				lRegister = SetBit(lRegister, 5, false);
				return 8;
			//RES 5,(HL)
			case 0xAE:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 5, false));
				return 16;
			//RES 5,A
			case 0xAF:
				aRegister = SetBit(aRegister, 5, false);
				return 8;
			//RES 6,B
			case 0xB0:
				bRegister = SetBit(bRegister, 6, false);
				return 8;
			//RES 6,C
			case 0xB1:
				cRegister = SetBit(cRegister, 6, false);
				return 8;
			//RES 6,D
			case 0xB2:
				dRegister = SetBit(dRegister, 6, false);
				return 8;
			//RES 6,E
			case 0xB3:
				eRegister = SetBit(eRegister, 6, false);
				return 8;
			//RES 6,H
			case 0xB4:
				hRegister = SetBit(hRegister, 6, false);
				return 8;
			//RES 6,L
			case 0xB5:
				lRegister = SetBit(lRegister, 6, false);
				return 8;
			//RES 6,(HL)
			case 0xB6:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 6, false));
				return 16;
			//RES 6,A
			case 0xB7:
				aRegister = SetBit(aRegister, 6, false);
				return 8;
			//RES 7,B
			case 0xB8:
				bRegister = SetBit(bRegister, 7, false);
				return 8;
			//RES 7,C
			case 0xB9:
				cRegister = SetBit(cRegister, 7, false);
				return 8;
			//RES 7,D
			case 0xBA:
				dRegister = SetBit(dRegister, 7, false);
				return 8;
			//RES 7,E
			case 0xBB:
				eRegister = SetBit(eRegister, 7, false);
				return 8;
			//RES 7,H
			case 0xBC:
				hRegister = SetBit(hRegister, 7, false);
				return 8;
			//RES 7,L
			case 0xBD:
				lRegister = SetBit(lRegister, 7, false);
				return 8;
			//RES 7,(HL)
			case 0xBE:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 7, false));
				return 16;
			//RES 7,A
			case 0xBF:
				aRegister = SetBit(aRegister, 7, false);
				return 8;
			//SET 0,B
			case 0xC0:
				bRegister = SetBit(bRegister, 0, true);
				return 8;
			//SET 0,C
			case 0xC1:
				cRegister = SetBit(cRegister, 0, true);
				return 8;
			//SET 0,D
			case 0xC2:
				dRegister = SetBit(dRegister, 0, true);
				return 8;
			//SET 0,E
			case 0xC3:
				eRegister = SetBit(eRegister, 0, true);
				return 8;
			//SET 0,H
			case 0xC4:
				hRegister = SetBit(hRegister, 0, true);
				return 8;
			//SET 0,L
			case 0xC5:
				lRegister = SetBit(lRegister, 0, true);
				return 8;
			//SET 0,(HL)
			case 0xC6:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 0, true));
				return 16;
			//SET 0,A
			case 0xC7:
				aRegister = SetBit(aRegister, 0, true);
				return 8;
			//SET 1,B
			case 0xC8:
				bRegister = SetBit(bRegister, 1, true);
				return 8;
			//SET 1,C
			case 0xC9:
				cRegister = SetBit(cRegister, 1, true);
				return 8;
			//SET 1,D
			case 0xCA:
				dRegister = SetBit(dRegister, 1, true);
				return 8;
			//SET 1,E
			case 0xCB:
				eRegister = SetBit(eRegister, 1, true);
				return 8;
			//SET 1,H
			case 0xCC:
				hRegister = SetBit(hRegister, 1, true);
				return 8;
			//SET 1,L
			case 0xCD:
				lRegister = SetBit(lRegister, 1, true);
				return 8;
			//SET 1,(HL)
			case 0xCE:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 1, true));
				return 16;
			//SET 1,A
			case 0xCF:
				aRegister = SetBit(aRegister, 1, true);
				return 8;
			//SET 2,B
			case 0xD0:
				bRegister = SetBit(bRegister, 2, true);
				return 8;
			//SET 2,C
			case 0xD1:
				cRegister = SetBit(cRegister, 2, true);
				return 8;
			//SET 2,D
			case 0xD2:
				dRegister = SetBit(dRegister, 2, true);
				return 8;
			//SET 2,E
			case 0xD3:
				eRegister = SetBit(eRegister, 2, true);
				return 8;
			//SET 2,H
			case 0xD4:
				hRegister = SetBit(hRegister, 2, true);
				return 8;
			//SET 2,L
			case 0xD5:
				lRegister = SetBit(lRegister, 2, true);
				return 8;
			//SET 2,(HL)
			case 0xD6:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 2, true));
				return 16;
			//SET 2,A
			case 0xD7:
				aRegister = SetBit(aRegister, 2, true);
				return 8;
			//SET 3,B
			case 0xD8:
				bRegister = SetBit(bRegister, 3, true);
				return 8;
			//SET 3,C
			case 0xD9:
				cRegister = SetBit(cRegister, 3, true);
				return 8;
			//SET 3,D
			case 0xDA:
				dRegister = SetBit(dRegister, 3, true);
				return 8;
			//SET 3,E
			case 0xDB:
				eRegister = SetBit(eRegister, 3, true);
				return 8;
			//SET 3,H
			case 0xDC:
				hRegister = SetBit(hRegister, 3, true);
				return 8;
			//SET 3,L
			case 0xDD:
				lRegister = SetBit(lRegister, 3, true);
				return 8;
			//SET 3,(HL)
			case 0xDE:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 3, true));
				return 16;
			//SET 3,A
			case 0xDF:
				aRegister = SetBit(aRegister, 3, true);
				return 8;
			//SET 4,B
			case 0xE0:
				bRegister = SetBit(bRegister, 4, true);
				return 8;
			//SET 4,C
			case 0xE1:
				cRegister = SetBit(cRegister, 4, true);
				return 8;
			//SET 4,D
			case 0xE2:
				dRegister = SetBit(dRegister, 4, true);
				return 8;
			//SET 4,E
			case 0xE3:
				eRegister = SetBit(eRegister, 4, true);
				return 8;
			//SET 4,H
			case 0xE4:
				hRegister = SetBit(hRegister, 4, true);
				return 8;
			//SET 4,L
			case 0xE5:
				lRegister = SetBit(lRegister, 4, true);
				return 8;
			//SET 4,(HL)
			case 0xE6:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 4, true));
				return 16;
			//SET 4,A
			case 0xE7:
				aRegister = SetBit(aRegister, 4, true);
				return 8;
			//SET 5,B
			case 0xE8:
				bRegister = SetBit(bRegister, 5, true);
				return 8;
			//SET 5,C
			case 0xE9:
				cRegister = SetBit(cRegister, 5, true);
				return 8;
			//SET 5,D
			case 0xEA:
				dRegister = SetBit(dRegister, 5, true);
				return 8;
			//SET 5,E
			case 0xEB:
				eRegister = SetBit(eRegister, 5, true);
				return 8;
			//SET 5,H
			case 0xEC:
				hRegister = SetBit(hRegister, 5, true);
				return 8;
			//SET 5,L
			case 0xED:
				lRegister = SetBit(lRegister, 5, true);
				return 8;
			//SET 5,(HL)
			case 0xEE:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 5, true));
				return 16;
			//SET 5,A
			case 0xEF:
				aRegister = SetBit(aRegister, 5, true);
				return 8;
			//SET 6,B
			case 0xF0:
				bRegister = SetBit(bRegister, 6, true);
				return 8;
			//SET 6,C
			case 0xF1:
				cRegister = SetBit(cRegister, 6, true);
				return 8;
			//SET 6,D
			case 0xF2:
				dRegister = SetBit(dRegister, 6, true);
				return 8;
			//SET 6,E
			case 0xF3:
				eRegister = SetBit(eRegister, 6, true);
				return 8;
			//SET 6,H
			case 0xF4:
				hRegister = SetBit(hRegister, 6, true);
				return 8;
			//SET 6,L
			case 0xF5:
				lRegister = SetBit(lRegister, 6, true);
				return 8;
			//SET 6,(HL)
			case 0xF6:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 6, true));
				return 16;
			//SET 6,A
			case 0xF7:
				aRegister = SetBit(aRegister, 6, true);
				return 8;
			//SET 7,B
			case 0xF8:
				bRegister = SetBit(bRegister, 7, true);
				return 8;
			//SET 7,C
			case 0xF9:
				cRegister = SetBit(cRegister, 7, true);
				return 8;
			//SET 7,D
			case 0xFA:
				dRegister = SetBit(dRegister, 7, true);
				return 8;
			//SET 7,E
			case 0xFB:
				eRegister = SetBit(eRegister, 7, true);
				return 8;
			//SET 7,H
			case 0xFC:
				hRegister = SetBit(hRegister, 7, true);
				return 8;
			//SET 7,L
			case 0xFD:
				lRegister = SetBit(lRegister, 7, true);
				return 8;
			//SET 7,(HL)
			case 0xFE:
				emulator.memory.Write(HlRegister, SetBit(emulator.memory.Read(HlRegister), 7, true));
				return 16;
			//SET 7,A
			case 0xFF:
				aRegister = SetBit(aRegister, 7, true);
				return 8;
		}
	}

	//Word functions
	public static ushort MakeWord(byte hi, byte lo)
	{
		return (ushort)((hi << 8) | lo);
	}

	public static byte MakeByte(bool bit7, bool bit6, bool bit5, bool bit4, bool bit3, bool bit2, bool bit1, bool bit0)
	{
		int bit7B = (bit7 ? 1 : 0) << 7;
		int bit6B = (bit6 ? 1 : 0) << 6;
		int bit5B = (bit5 ? 1 : 0) << 5;
		int bit4B = (bit4 ? 1 : 0) << 4;
		int bit3B = (bit3 ? 1 : 0) << 3;
		int bit2B = (bit2 ? 1 : 0) << 2;
		int bit1B = (bit1 ? 1 : 0) << 1;
		int bit0B = bit0 ? 1 : 0;

		return (byte)(bit7B | bit6B | bit5B | bit4B | bit3B | bit2B | bit1B | bit0B);
	}

	public static byte GetLoByte(ushort word)
	{
		return (byte)(word & 0xFF);
	}

	public static byte GetHiByte(ushort word)
	{
		return (byte)(word >> 8);
	}

	//Bit-wise functions
	public static bool GetBit(byte data, int bit)
	{
		if (bit is >= 0 and <= 7) return ToBool((data >> bit) & 1);

		Logger.LogMessage($"Cannot access Bit {bit} of a Byte!", Logger.LogLevel.Error);
		throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Byte!");
	}

	public static byte SetBit(byte data, int bit, bool state)
	{
		if (bit is > 7 or < 0)
		{
			Logger.LogMessage($"Cannot access Bit {bit} of a Byte!", Logger.LogLevel.Error);
			throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Byte!");
		}

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

		SetFlags(aRegister == 0, false, false, false);
	}

	private void OrIntoA(byte data)
	{
		aRegister |= data;

		SetFlags(aRegister == 0, false, false, false);
	}

	private void AndIntoA(byte data)
	{
		aRegister &= data;

		SetFlags(aRegister == 0, false, true, false);
	}

	private void BitOpcode(byte data, int bit)
	{
		SetFlags(!GetBit(data, bit), false, true, null);
	}

	private int ComplementARegister()
	{
		aRegister = (byte)~aRegister;

		SetFlags(null, true, true, null);

		return 4;
	}

	private byte SwapNibbles(byte data)
	{
		byte lowerNibble = (byte)(data & 0xF);
		data >>= 4;
		data |=  (byte)(lowerNibble << 4);

		SetFlags(data == 0, false, false, false);

		return data;
	}

	private byte RotateLeftThroughCarry(byte data, bool resetZeroFlag = false)
	{
		bool bit7 = GetBit(data, 7);

		data <<= 1;
		data =   SetBit(data, 0, CarryFlag);

		SetFlags(!resetZeroFlag && data == 0, false, false, bit7);

		return data;
	}

	private byte RotateLeftIntoCarry(byte data, bool resetZeroFlag = false)
	{
		bool bit7 = GetBit(data, 7);

		data <<= 1;
		data =   SetBit(data, 0, bit7);

		SetFlags(!resetZeroFlag && data == 0, false, false, bit7);

		return data;
	}

	private byte RotateRightThroughCarry(byte data, bool resetZeroFlag = false)
	{
		bool bit0 = GetBit(data, 0);

		data >>= 1;
		data =   SetBit(data, 7, CarryFlag);

		SetFlags(!resetZeroFlag && data == 0, false, false, bit0);

		return data;
	}

	private byte RotateRightIntoCarry(byte data, bool resetZeroFlag = false)
	{
		bool bit0 = GetBit(data, 0);

		data >>= 1;
		data =   SetBit(data, 7, bit0);

		SetFlags(!resetZeroFlag && data == 0, false, false, bit0);

		return data;
	}

	private byte ShiftRightIntoCarryMsb0(byte data)
	{
		bool bit0 = GetBit(data, 0);

		data >>= 1;
		data =   SetBit(data, 7, false);

		SetFlags(data == 0, false, false, bit0);

		return data;
	}

	private byte ShiftRightIntoCarryKeepMsb(byte data)
	{
		bool bit0 = GetBit(data, 0);

		data >>= 1;
		data =   SetBit(data, 7, GetBit(data, 6));

		SetFlags(data == 0, false, false, bit0);

		return data;
	}

	private byte ShiftLeftIntoCarryLsb0(byte data)
	{
		bool bit7 = GetBit(data, 7);

		data <<= 1;
		data =   SetBit(data, 0, false);

		SetFlags(data == 0, false, false, bit7);

		return data;
	}

	public static bool ToBool(int data)
	{
		return data != 0;
	}

	//Register functions
	/// <summary>
	/// Sets CPU Flags. Argument of type Null leaves Flag unchanged
	/// </summary>
	private void SetFlags(bool? zeroFlag, bool? subtractFlag, bool? halfCarryFlag, bool? carryFlag)
	{
		if (zeroFlag != null) ZeroFlag           = (bool)zeroFlag;
		if (subtractFlag != null) SubtractFlag   = (bool)subtractFlag;
		if (halfCarryFlag != null) HalfCarryFlag = (bool)halfCarryFlag;
		if (carryFlag != null) CarryFlag         = (bool)carryFlag;
	}

	public void InitialiseRegisters()
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
		return emulator.memory.Read(programCounter++);
	}

	private int WriteIoPortsCRegisterOffset(byte data)
	{
		emulator.memory.Write((ushort)(0xFF00 + cRegister), data);
		return 8;
	}

	private int WriteIoPortsImmediateOffset(byte data)
	{
		emulator.memory.Write((ushort)(0xFF00 + Load8BitImmediate()), data);
		return 12;
	}

	private int WriteHlDecrement(byte data, bool increment)
	{
		emulator.memory.Write(HlRegister, data);

		HlRegister += (ushort)(increment ? 1 : -1);

		return 8;
	}

	private byte ReadHlDecrement(bool increment)
	{
		byte data = emulator.memory.Read(HlRegister);

		HlRegister += (ushort)(increment ? 1 : -1);

		return data;
	}

	private int LoadStackPointerIntoMemory()
	{
		ushort address = Load16BitImmediate();

		emulator.memory.Write(address++, GetLoByte(stackPointer));
		emulator.memory.Write(address, GetHiByte(stackPointer));

		return 20;
	}

	private int SetCarryFlagOpcode()
	{
		SetFlags(null, false, false, true);
		return 4;
	}

	private int ComplementCarryFlagOpcode()
	{
		SetFlags(null, false, false, !CarryFlag);
		return 4;
	}

	//Jump functions
	private int JumpRelative(JumpCondition condition)
	{
		//Signed relative Jump amount
		sbyte relativeJumpAmount = (sbyte)Load8BitImmediate();

		if (!ShouldJump(condition)) return 8;

		programCounter = AddSignedToUnsigned(programCounter, relativeJumpAmount);
		return 12;
	}

	private int JumpImmediate(JumpCondition condition)
	{
		ushort jumpAddress = Load16BitImmediate();

		if (!ShouldJump(condition)) return 12;

		programCounter = jumpAddress;
		return 16;
	}

	private bool ShouldJump(JumpCondition condition)
	{
		return condition switch
		{
			//Non-Zero
			JumpCondition.Nz =>
				!ZeroFlag,

			//Zero
			JumpCondition.Z =>
				ZeroFlag,

			//No Carry
			JumpCondition.Nc =>
				!CarryFlag,

			//Carry
			JumpCondition.C =>
				CarryFlag,

			//Always Jump
			JumpCondition.Always =>
				true,

			//Default
			_ => false
		};
	}

	private int CallSubroutine(JumpCondition condition)
	{
		ushort jumpAddress = Load16BitImmediate();

		if (!ShouldJump(condition)) return 12;

		PushStack(programCounter);
		programCounter = jumpAddress;
		return 24;
	}

	private int ReturnSubroutine(JumpCondition condition)
	{
		if (condition == JumpCondition.Always)
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
		if (haltMode != HaltMode.NotHalted && haltMode != HaltMode.HaltBug) haltMode = HaltMode.NotHalted;

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
		SetFlags(data == 0, false, halfCarry, null);
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
		SetFlags(data == 0, true, halfCarry, null);
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

		SetFlags((byte)result == 0, true, halfCarry, carryFlag);

		if (!compare) aRegister = (byte)result;
	}

	private void AddByteToAReg(byte data, bool withCarry = false)
	{
		bool halfCarry = ToBool(((aRegister & 0xF) + (data & 0xF) + (withCarry ? 1 : 0)) & 0x10);
		bool carryFlag = aRegister + data + (withCarry ? 1 : 0) > 255;

		aRegister += (byte)(data + (withCarry ? 1 : 0));

		SetFlags(aRegister == 0, false, halfCarry, carryFlag);
	}

	private ushort Add16BitRegisters(ushort data1, ushort data2)
	{
		bool halfCarry = ToBool(((data1 & 0xFFF) + (data2 & 0xFFF)) & 0x1000);
		bool carry     = data1 + data2 > 0xFFFF;

		SetFlags(null, false, halfCarry, carry);

		return (ushort)(data1 + data2);
	}

	private int DecimalAdjustARegister()
	{
		int correction = 0;

		if (HalfCarryFlag || !SubtractFlag && (aRegister & 0xF) > 9) correction |= 6;

		if (CarryFlag || !SubtractFlag && aRegister > 0x99)
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
		bool carry     = (stackPointer & 0xFF) + dataU > 0xFF;

		SetFlags(false, false, halfCarry, carry);

		return (ushort)(stackPointer + dataS);
	}

	//Stack functions
	private int PushStack(ushort data)
	{
		byte lo = GetLoByte(data);
		byte hi = GetHiByte(data);

		emulator.memory.Write(--stackPointer, hi);
		emulator.memory.Write(--stackPointer, lo);

		return 16;
	}

	private ushort PopStack()
	{
		byte lo = emulator.memory.Read(stackPointer++);
		byte hi = emulator.memory.Read(stackPointer++);

		return MakeWord(hi, lo);
	}

	//Interrupt functions
	public void ServiceInterrupt(ushort address)
	{
		PushStack(programCounter);
		programCounter = address;
		waitNopAmount  = 5;
	}

	//Halt/Stop functions
	private int HaltCpu()
	{
		if (emulator.interrupts.InterruptMasterEnable || !emulator.interrupts.HasPendingInterrupts)
			haltMode = HaltMode.Halted;
		else
			haltMode = HaltMode.HaltBug;

		return 4;
	}

	public void ExitHaltMode()
	{
		//Exit halt mode except if halt bug occured, halt mode is exited automatically after halt bug occured
		if (haltMode != HaltMode.HaltBug) haltMode = HaltMode.NotHalted;
	}
}