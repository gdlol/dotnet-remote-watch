using System.Diagnostics;

namespace HotReload.Common;

public static class ProcessExtensions
{
    // !
    private static bool CheckHasExitedOrUnassociated(this Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    public static void KillTree(this Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            if (!CheckHasExitedOrUnassociated(process))
            {
                throw;
            }
        }
    }
}
