using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;

namespace FMT
{
    public static class GCHelpers
    {
        [DllImport("psapi.dll", EntryPoint = "EmptyWorkingSet")]
        private static extern bool EmptyWorkingSetCall(IntPtr hProcess);

        public static void EmptyWorkingSet()
        {
            EmptyWorkingSetCall(Process.GetCurrentProcess().Handle);
        }

        public static bool Emptying = false;

        public static void ClearGarbage(bool emptyTheSet = false)
        {
            Collect(force: true);
            if (Emptying)
                return;

            if (emptyTheSet)
            {
                Emptying = true;
                RunHeapPreAllocation();
                Collect(force: true);
                EmptyWorkingSet();
            }
            Emptying = false;
        }

        public static void RunHeapPreAllocation()
        {
            int num = 512;
            if (num > 0)
            {
                object[] array = new object[1024 * num];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = new byte[1024];
                }
                array = null;
            }
        }

        public static void Collect(bool force = false)
        {
            Collect(1, GCCollectionMode.Optimized, isBlocking: true, compacting: false, force);
            Collect(2, GCCollectionMode.Optimized, isBlocking: true, compacting: false, force);
            Collect(3, GCCollectionMode.Optimized, isBlocking: true, compacting: false, force);
        }

        public static void Collect(int generation, GCCollectionMode gcMode, bool isBlocking, bool compacting, bool force)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(generation, gcMode, isBlocking, compacting);
        }
    }
}
