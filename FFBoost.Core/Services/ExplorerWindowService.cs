using System.Runtime.InteropServices;
using System.Text;

namespace FFBoost.Core.Services;

public class ExplorerWindowService
{
    private const uint WmClose = 0x0010;

    public int CloseExplorerWindows()
    {
        var closed = 0;

        EnumWindows((hWnd, _) =>
        {
            try
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                var className = GetClassName(hWnd);
                if (!className.Equals("CabinetWClass", StringComparison.OrdinalIgnoreCase) &&
                    !className.Equals("ExploreWClass", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                PostMessage(hWnd, WmClose, IntPtr.Zero, IntPtr.Zero);
                closed++;
            }
            catch
            {
            }

            return true;
        }, IntPtr.Zero);

        return closed;
    }

    private static string GetClassName(IntPtr hWnd)
    {
        var buffer = new StringBuilder(256);
        _ = GetClassName(hWnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
