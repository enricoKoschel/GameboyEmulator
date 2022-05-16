using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace GameboyEmulator;

public static class WinApi
{
	[SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible")]
	[SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
	[SuppressUnmanagedCodeSecurity]
	[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
	public static extern uint TimeBeginPeriod(uint uMilliseconds);

	[SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible")]
	[SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
	[SuppressUnmanagedCodeSecurity]
	[DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
	public static extern uint TimeEndPeriod(uint uMilliseconds);
}