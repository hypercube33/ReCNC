using System;
using Eluant;

class Probe
{
    static int Main()
    {
        try
        {
            using var rt = new LuaRuntime();
            using var r = rt.DoString("return 6*7");
            Console.WriteLine("Eluant OK, result=" + r[0]);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Eluant FAIL: " + ex.GetType().Name + ": " + ex.Message);
            return 1;
        }
    }
}
