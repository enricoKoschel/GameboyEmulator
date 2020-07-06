using System;
using GameboyEmulatorGraphics;
using GameboyEmulatorMemory;

namespace GameboyEmulatorCPU
{
    class CPU
    {
        enum JumpConditions
        {
            NZ,
            Z,
            NC,
            C,
            NoCondition
        }

        //Modules
        readonly Memory memory;
        readonly Graphics graphics;

        //Registers
        private ushort programCounter;
        private ushort stackPointer;

        private byte ARegister;
        private byte BRegister;
        private byte CRegister;
        private byte DRegister;
        private byte ERegister;
        
        private byte HRegister;
        private byte LRegister;              

        //Register pairs
        private ushort AFRegister
        {
            get
            {
                return MakeWord(ARegister, FlagRegister);
            }
            set
            {
                ARegister = GetHiByte(value);
                FlagRegister = GetLoByte(value);
            }
        }
        private ushort BCRegister
        {
            get
            {
                return MakeWord(BRegister, CRegister);
            }
            set
            {
                BRegister = GetHiByte(value);
                CRegister = GetLoByte(value);
            }
        }
        private ushort DERegister
        {
            get
            {
                return MakeWord(DRegister, ERegister);
            }
            set
            {
                DRegister = GetHiByte(value);
                ERegister = GetLoByte(value);
            }
        }
        private ushort HLRegister
        {
            get
            {
                return MakeWord(HRegister, LRegister);
            }
            set
            {
                HRegister = GetHiByte(value);
                LRegister = GetLoByte(value);
            }
        }

        //Flag register
        private byte FlagRegister;

        private bool ZeroFlag
        {
            get
            {
                return GetBit(FlagRegister, 7);
            }
            set
            {
                FlagRegister = SetBit(FlagRegister, 7, value);
            }
        }
        private bool SubtractFlag
        {
            get
            {
                return GetBit(FlagRegister, 6);
            }
            set
            {
                FlagRegister = SetBit(FlagRegister, 6, value);
            }
        }
        private bool HalfCarryFlag
        {
            get
            {
                return GetBit(FlagRegister, 5);
            }
            set
            {
                FlagRegister = SetBit(FlagRegister, 5, value);
            }
        }
        private bool CarryFlag
        {
            get
            {
                return GetBit(FlagRegister, 4);
            }
            set
            {
                FlagRegister = SetBit(FlagRegister, 4, value);
            }
        }

        //Constants
        private const int MAX_CPU_CYCLES_PER_FRAME = 70224;

        public CPU()
        {
            //Initialize modules
            memory = new Memory();
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
            byte opcode = memory.Read(programCounter++);
            
            switch (opcode)
            {
                case 0x04:
                    {
                        //INC B
                        BRegister = Increment(BRegister);
                        return 4;
                    }
                case 0x05:
                    {
                        //DEC B
                        BRegister = Decrement(BRegister);
                        return 4;
                    }
                case 0x06:
                    {
                        //LD B,n
                        BRegister = Load8BitImmediate();
                        return 8;
                    }
                case 0x0C:
                    {
                        //INC C
                        CRegister = Increment(CRegister);
                        return 4;
                    }
                case 0x0D:
                    {
                        //DEC C
                        CRegister = Decrement(CRegister);
                        return 4;
                    }
                case 0x0E:
                    {
                        //LD C,n
                        CRegister = Load8BitImmediate();
                        return 8;
                    }
                case 0x11:
                    {
                        //LD DE,nn
                        DERegister = Load16BitImmediate();
                        return 12;
                    }
                case 0x13:
                    {
                        //INC DE
                        DERegister = Increment(DERegister);
                        return 8;
                    }
                case 0x15:
                    {
                        //DEC D
                        DRegister = Decrement(DRegister);
                        return 4;
                    }
                case 0x16:
                    {
                        //LD D,n
                        DRegister = Load8BitImmediate();
                        return 8;
                    }
                case 0x17:
                    {
                        //RLA
                        ARegister = RotateLeftCarry(ARegister);
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
                        ARegister = memory.Read(DERegister);
                        return 8;
                    }
                case 0x1D:
                    {
                        //DEC E
                        ERegister = Decrement(ERegister);
                        return 4;
                    }
                case 0x1E:
                    {
                        //LD E,n
                        ERegister = Load8BitImmediate();
                        return 8;
                    }
                case 0x20:
                    {
                        //JR NZ,n
                        return JumpRelative(JumpConditions.NZ);
                    }
                case 0x21:
                    {
                        //LD HL,nn
                        HLRegister = Load16BitImmediate();
                        return 12;
                    }
                case 0x22:
                    {
                        //LD (HL+),A                       
                        return WriteHLDecrement(ARegister, true);
                    }
                case 0x23:
                    {
                        //INC HL
                        HLRegister = Increment(HLRegister);
                        return 8;
                    }
                case 0x24:
                    {
                        //INC H
                        HRegister = Increment(HRegister);
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
                        LRegister = Load8BitImmediate();
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
                        return WriteHLDecrement(ARegister, false);
                    }
                case 0x3D:
                    {
                        //DEC A
                        ARegister = Decrement(ARegister);
                        return 4;
                    }
                case 0x3E:
                    {
                        //LD A,n
                        ARegister = Load8BitImmediate();
                        return 8;
                    }
                case 0x4F:
                    {
                        //LD C,A
                        CRegister = ARegister;
                        return 4;
                    }
                case 0x57:
                    {
                        //LD D,A
                        DRegister = ARegister;
                        return 4;
                    }
                case 0x67:
                    {
                        //LD H,A
                        HRegister = ARegister;
                        return 4;
                    }
                case 0x77:
                    {
                        //LD (HL),A
                        memory.Write(HLRegister, ARegister);
                        return 8;
                    }
                case 0x78:
                    {
                        //LD A,B
                        ARegister = BRegister;
                        return 4;
                    }
                case 0x7B:
                    {
                        //LD A,E
                        ARegister = ERegister;
                        return 4;
                    }
                case 0x7C:
                    {
                        //LD A,H
                        ARegister = HRegister;
                        return 4;
                    }
                case 0x7D:
                    {
                        //LD A,L
                        ARegister = LRegister;
                        return 4;
                    }
                case 0x86:
                    {
                        //ADD A,(HL)
                        AddByteToAReg(memory.Read(HLRegister));
                        return 8;
                    }
                case 0x90:
                    {
                        //SUB B
                        SubtractByteFromAReg(BRegister, false);
                        return 4;
                    }
                case 0xAF:
                    {
                        //XOR A
                        XORintoA(ARegister);
                        return 4;
                    }
                case 0xBE:
                    {
                        //CP (HL)
                        SubtractByteFromAReg(memory.Read(HLRegister), true);
                        return 8;
                    }
                case 0xC1:
                    {
                        //POP BC
                        BCRegister = PopStack();
                        return 12;
                    }
                case 0xC5:
                    {
                        //PUSH BC
                        PushStack(BCRegister);
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
                        return WriteIOPortsImmediateOffset(ARegister);
                    }
                case 0xE2:
                    {
                        //LD (0xFF00+C),A
                        return WriteIOPortsCRegisterOffset(ARegister);
                    }
                case 0xEA:
                    {
                        //LD (nn),A
                        memory.Write(Load16BitImmediate(), ARegister);
                        return 16;
                    }
                case 0xF0:
                    {
                        //LD A,(0xFF00+n)
                        ARegister = memory.Read((ushort)(0xFF00 + Load8BitImmediate()));
                        return 12;
                    }
                case 0xFE:
                    {
                        //CP n
                        SubtractByteFromAReg(Load8BitImmediate(), true);
                        return 8;
                    }
                default:
                    throw new NotImplementedException($"Opcode 0x{opcode:X} not implemented yet!"); //TODO - implement opcodes
            }             
        }
        
        private int ExecuteExtendedOpcode()
        {
            byte opcode = memory.Read(programCounter++);

            switch (opcode)
            {
                case 0x11:
                    {
                        //RL C
                        CRegister = RotateLeftCarry(CRegister);
                        return 8;
                    }
                case 0x7C:
                    {
                        //BIT 7,H
                        GameboyBit(HRegister, 7);
                        return 8;
                    }
                default:
                    throw new NotImplementedException($"Extended Opcode 0xCB{opcode:X} not implemented yet!"); //TODO - implement extended opcodes
            }
        }

        //Utility functions

        //Word functions
        private ushort MakeWord(byte hi, byte lo)
        {
            return (ushort)((hi << 8) | lo);
        }

        private byte GetLoByte(ushort word)
        {
            return (byte)(word & 0xFF);
        }
        private byte GetHiByte(ushort word)
        {
            return (byte)(word >> 8);
        }
        
        //Bit-wise functions
        public bool GetBit(byte data, int bit)
        {
            if (bit > 7 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Byte!");
            return ToBool(((data >> bit) & 1));
        }
        private bool GetBit(ushort data, int bit)
        {
            if (bit > 15 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Word!");
            return ToBool(((data >> bit) & 1));
        }
        public byte SetBit(byte data, int bit, bool state)
        {
            if (bit > 7 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Byte!");
            
            byte mask = (byte)(1 << bit);

            if (state)
            {                
                data |= mask;
            }
            else
            {
                data &= (byte)~mask;
            }

            return data;
        }
        private ushort SetBit(ushort data, int bit, bool state)
        {
            if (bit > 15 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Word!");

            byte mask = (byte)(1 << bit);

            if (state)
            {
                data |= mask;
            }
            else
            {
                data &= (byte)~mask;
            }

            return data;
        }
        private void XORintoA(byte data)
        {
            ARegister ^= data;
            SetFlags(ARegister == 0, 0, 0, 0);
        }
        private void GameboyBit(byte data, int bit)
        {
            SetFlags(!GetBit(data, bit), 0, 1, "");
        }
        private byte RotateLeftCarry(byte data)
        {
            bool oldCarryFlag = CarryFlag;
            CarryFlag = GetBit(data, 7);

            data <<= 1;
            data = SetBit(data, 0, oldCarryFlag);

            SetFlags(data == 0, 0, 0, "");

            return data;
        }
        private byte RotateLeft(byte data)
        {
            bool bit7 = GetBit(data, 7);

            data <<= 1;
            data = SetBit(data, 0, bit7);

            SetFlags(data == 0, 0, 0, bit7);

            return data;
        }

        //Bool functions
        private bool ToBool<T>(T data)
        {
            return Convert.ToBoolean(data);
        }

        //Register functions
        private void SetFlags<T1, T2, T3, T4>(T1 zeroFlag, T2 subtractFlag, T3 halfCarryFlag, T4 carryFlag)
        {
            //Argument of type String means "Leave Flag unchanged"

            if(zeroFlag.GetType().Name != "String")
            {
                ZeroFlag = ToBool(zeroFlag);
            }
            if (subtractFlag.GetType().Name != "String")
            {
                SubtractFlag = ToBool(subtractFlag);
            }
            if (halfCarryFlag.GetType().Name != "String")
            {
                HalfCarryFlag = ToBool(halfCarryFlag);
            }
            if (carryFlag.GetType().Name != "String")
            {
                CarryFlag = ToBool(carryFlag);
            }   
        }

        //Load functions
        private ushort Load16BitImmediate()
        {
            byte lo = memory.Read(programCounter++);
            byte hi = memory.Read(programCounter++);
            return MakeWord(hi, lo);
        }
        private byte Load8BitImmediate()
        {
            return memory.Read(programCounter++);
        }
        private int WriteIOPortsCRegisterOffset(byte data)
        {
            memory.Write((ushort)(0xFF00 + CRegister), data);
            return 8;
        }
        private int WriteIOPortsImmediateOffset(byte data)
        {
            memory.Write((ushort)(0xFF00 + memory.Read(programCounter++)), data);
            return 12;
        }
        private int WriteHLDecrement(byte data, bool increment)
        {
            memory.Write(HLRegister, data);

            HLRegister += (ushort)(increment ? 1 : -1);

            return 8;
        }

        //Jump funtions
        private int JumpRelative(JumpConditions condition)
        {
            //Signed relative Jump amount
            sbyte relativeJumpAmount = (sbyte)memory.Read(programCounter++);
            bool shouldJump = false;

            switch (condition)
            {
                case JumpConditions.NZ:
                    {
                        //Non-Zero
                        shouldJump = !ZeroFlag;
                        break;
                    }
                case JumpConditions.Z:
                    {
                        //Zero
                        shouldJump = ZeroFlag;
                        break;
                    }
                case JumpConditions.NC:
                    {
                        //No Carry
                        shouldJump = !CarryFlag;
                        break;
                    }
                case JumpConditions.C:
                    {
                        //Carry
                        shouldJump = CarryFlag;
                        break;
                    }
                case JumpConditions.NoCondition:
                    {
                        //Always Jump
                        shouldJump = true;
                        break;
                    }
            }

            if (shouldJump)
            {
                programCounter = AddSignedToUnsigned(programCounter, relativeJumpAmount);
                return 12;
            }
            else
            {
                return 8;
            }
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
        private ushort AddSignedToUnsigned(ushort unsingedWord, sbyte signedByte)
        {
            if(signedByte > 0)
            {
                return (ushort)(unsingedWord + (ushort)signedByte);
            }
            else
            {
                return (ushort)(unsingedWord - (ushort)(signedByte * -1));
            }
        }
        private byte Increment(byte data)
        {
            bool halfCarry = ToBool(((data & 0xF) + 1) & 0x10);

            data++;
            SetFlags(data == 0, 0, halfCarry, "");
            return data;
        }
        private ushort Increment(ushort data)
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
        private ushort Decrement(ushort data)
        {
            return (ushort)(data - 1);
        }
        private void SubtractByteFromAReg(byte data, bool compare)
        {
            bool halfCarry = ToBool(((ARegister & 0xF) - (data & 0xF)) & 0x10);
            bool carry = ToBool((ARegister - data) < 0);
                   
            SetFlags((ARegister - data) == 0, 1, halfCarry, carry);

            if (!compare)
            {
                ARegister -= data;
            }
        }
        private void AddByteToAReg(byte data)
        {
            bool halfCarry = ToBool(((ARegister & 0xF) + (data & 0xF)) & 0x10);
            bool carry = ToBool((ARegister + data) > 255);

            ARegister += data;

            SetFlags(ARegister == 0, 0, halfCarry, carry);
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
