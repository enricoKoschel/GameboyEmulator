using System;
using System.Collections.Generic;
using GameboyEmulatorMemory;

namespace GameboyEmulatorCPU
{
    class CPU
    {
        //Modules
        Memory memory;

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
                Tuple<byte, byte> word = BreakWord(value);
                ARegister = word.Item1;
                FlagRegister = word.Item2;
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
                Tuple<byte, byte> word = BreakWord(value);
                BRegister = word.Item1;
                CRegister = word.Item2;
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
                Tuple<byte, byte> word = BreakWord(value);
                DRegister = word.Item1;
                ERegister = word.Item2;
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
                Tuple<byte, byte> word = BreakWord(value);
                HRegister = word.Item1;
                LRegister = word.Item2;
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
                SetBit(ref FlagRegister, 7, value);
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
                SetBit(ref FlagRegister, 6, value);
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
                SetBit(ref FlagRegister, 5, value);
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
                SetBit(ref FlagRegister, 4, value);
            }
        }

        //Constants
        private const int MAX_CPU_CYCLES_PER_FRAME = 70224;

        public CPU()
        {
            //Initialize modules
            memory = new Memory();
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
                if (cycles == 0)
                {
                    //Boot rom finished, dont update with 0 cycles
                    //TODO - maybe remove this
                    continue;
                }

                cyclesThisFrame += cycles;
            }
        }

        private int ExecuteOpcode()
        {
            byte opcode;
            try
            {
                 opcode = memory.Read(programCounter++);
            }
            catch (IndexOutOfRangeException)
            {
                //Boot rom finished, disable Boot rom and retry last execution
                memory.DisableBootRom();
                programCounter--;
                return 0;
            }
                 
            switch (opcode)
            {
                case 0x21:
                    {
                        //LD HL,nn
                        HLRegister = Load16BitImmediate();
                        return 12;
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
                        memory.Write(HLRegister, ARegister);
                        HLRegister--;
                        return 8;
                    }
                case 0xAF:
                    {
                        //XOR A
                        XORintoA(ARegister);
                        return 4;
                    }
                case 0xCB:
                    {
                        //Extended Opcode
                        return ExecuteExtendedOpcode();
                    }                    
                default:
                    throw new NotImplementedException($"Opcode: 0x{opcode:X} not implemented yet!"); //TODO - implement opcodes
            }             
        }
        
        private int ExecuteExtendedOpcode()
        {
            byte opcode = memory.Read(programCounter++);

            switch (opcode)
            {
                case 0x7C:
                    {
                        //BIT 7,H
                        Bit(HRegister, 7);
                        return 8;
                    }
                default:
                    throw new NotImplementedException($"Extended Opcode: 0xCB{opcode:X} not implemented yet!"); //TODO - implement extended opcodes
            }
        }

        //Utility functions

        //Word functions
        private ushort MakeWord(byte hi, byte lo)
        {
            return (ushort)((hi << 8) | lo);
        }
        private Tuple<byte, byte> BreakWord(ushort word)
        {
            byte hi = (byte)(word >> 8);
            byte lo = (byte)(word & 0xFF);

            return new Tuple<byte, byte>(hi, lo);
        }
        
        //Bit functions
        private bool GetBit(byte data, int bit)
        {
            if (bit > 7 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Byte!");
            return ToBool(((data >> bit) & 1));
        }
        private bool GetBit(ushort data, int bit)
        {
            if (bit > 15 || bit < 0) throw new IndexOutOfRangeException($"Cannot access Bit {bit} of a Word!");
            return ToBool(((data >> bit) & 1));
        }
        private void SetBit(ref byte data, int bit, bool state)
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
        }
        private void SetBit(ref ushort data, int bit, bool state)
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
        }
        
        //Gameboy Bit functions
        private void Bit(byte data, int bit)
        {
            SetFlags(!GetBit(data, bit), 0, 1, "");
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

        //Bit-wise functions
        private void XORintoA(byte data)
        {
            ARegister ^= data;
            SetFlags(ARegister == 0, 0, 0, 0);
        }
    }
}
