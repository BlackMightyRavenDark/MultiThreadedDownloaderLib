using System;

namespace MultiThreadedDownloaderLib.GuiTest
{
    public static class MemoryWatcher
    {
        public static ulong RamTotal { get; private set; } = 0U;
        public static ulong RamUsed { get; private set; } = 0U;
        public static ulong RamFree { get; private set; } = 0U;
        private static Microsoft.VisualBasic.Devices.ComputerInfo computerInfo = null;

        public static bool Update()
        {
            try
            {
                if (computerInfo == null)
                {
                    computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                }
                RamTotal = computerInfo.TotalPhysicalMemory;
                RamFree = computerInfo.AvailablePhysicalMemory;
                RamUsed = RamTotal - RamFree;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                computerInfo = null;
                return false;
            }
        }
    }
}
