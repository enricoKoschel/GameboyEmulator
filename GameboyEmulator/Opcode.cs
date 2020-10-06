using System;
using System.Reflection;

namespace GameboyEmulator
{
	public class Opcode
	{
		public string Mnemonic                  { get; }
		public int    NumberOfParameters        { get; }
		public int    NumberOfClockCyclesExec   { get; }
		public int    NumberOfClockCyclesNoExec { get; }

		public bool ExecutedLast { get; set; }

		private Action function;

		public Opcode(
			string mnemonic, int numberOfParameters, int numberOfClockCyclesExec, int numberOfClockCyclesNoExec
		)
		{
			Mnemonic                  = mnemonic;
			NumberOfParameters        = numberOfParameters;
			NumberOfClockCyclesExec   = numberOfClockCyclesExec;
			NumberOfClockCyclesNoExec = numberOfClockCyclesNoExec;
		}

		public void SetFunction(Action function)
		{
			this.function = function;
		}

		public void Execute()
		{
			function?.Invoke();
		}
	}
}