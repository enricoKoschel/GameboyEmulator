using System.IO;

namespace GameboyEmulator
{
	public class Debug
	{
		private static readonly StreamWriter writer = new StreamWriter("../../../debug.txt");

		public static void DumpOpcodes(int programCounter, int opcode)
		{
			writer.WriteLine($"PC: 0x{programCounter:X}		Opcode: 0x{opcode:X}");
		}
	}
}