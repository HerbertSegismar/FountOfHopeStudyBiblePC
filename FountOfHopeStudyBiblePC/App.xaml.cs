using System.Windows;
using FountOfHopeStudyBiblePC.Helpers;

namespace FountOfHopeStudyBiblePC
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            DispatcherUnhandledException += (_, args) => args.Handled = true;

            ThemeResources.Install();
            ThemeManager.Changed += ThemeResources.Refresh;

            base.OnStartup(e);
        }
    }
}