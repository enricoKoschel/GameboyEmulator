using System.IO;

namespace GameboyEmulator
{
	public static class Debug
	{
		private static StreamWriter writer;
		public static  int          totalExecutedOpcodes;

		public static void Initialize()
		{
			writer = new StreamWriter("../../../debug.txt");
		}

		public static void DumpOpcode(int programCounter, int opcode)
		{
			writer.WriteLine($"PC: 0x{programCounter:X}		Opcode: 0x{opcode:X}");
		}

		public static void DumpByteArray(byte[] array)
		{
			File.WriteAllBytes("../../../arrayDump.bin", array);
		}
	}
}