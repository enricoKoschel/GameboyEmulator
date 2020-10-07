using System;

namespace GameboyEmulator
{
	public class Opcode
	{
		public string Mnemonic                      { get; }
		public int    LengthInBytes                 { get; }
		public int    NrOfClockCyclesConditionTrue  { get; }
		public int    NrOfClockCyclesConditionFalse { get; }
		public int    ClockCyclesOfLastExec         { get; private set; }

		private readonly Func<bool> function;

		public Opcode(
			string     mnemonic, int lengthInBytes, int nrOfClockCyclesConditionTrue, int nrOfClockCyclesConditionFalse,
			Func<bool> function
		)
		{
			Mnemonic                      = mnemonic;
			LengthInBytes                 = lengthInBytes;
			NrOfClockCyclesConditionTrue  = nrOfClockCyclesConditionTrue;
			NrOfClockCyclesConditionFalse = nrOfClockCyclesConditionFalse;

			this.function = function;
		}

		public void Execute()
		{
			ClockCyclesOfLastExec = function.Invoke() ? NrOfClockCyclesConditionTrue : NrOfClockCyclesConditionFalse;
		}
	}
}