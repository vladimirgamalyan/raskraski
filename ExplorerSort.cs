using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace raskraski
{
    public static class ExplorerSort
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);

        public static int Compare(string? a, string? b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return StrCmpLogicalW(a, b);
        }

        public static IComparer<string> Comparer => Comparer<string>.Create(Compare);
    }
}
