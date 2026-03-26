using System.Windows.Forms;

namespace FFBoost.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using (var splash = new SplashForm())
        {
            splash.ShowDialog();
        }
        Application.Run(AppBootstrapper.CreateMainForm());
    }
}
