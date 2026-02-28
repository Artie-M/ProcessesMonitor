using System;

namespace ProcessesMonitor.Services;

public class AffinityHelper
{
    public static bool IsCoreEnabled(IntPtr mask, int coreIndex)
    {
        long maskValue = mask.ToInt64();
        return (maskValue & (1L << coreIndex)) != 0;
    }

    public static IntPtr SetCoreMask(bool[] cores)
    {
        long mask = 0;
        for (int i = 0; i < cores.Length; i++)
        {
            if (cores[i]) mask |= (1L << i);
        }

        return new IntPtr(mask);
    }
}