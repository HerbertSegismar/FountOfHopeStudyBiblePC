// App.xaml.cs
using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace FountOfHopeStudyBiblePC
{
    public partial class App : Application
    {
        public static readonly string LogPath =
            Path.Combine(AppContext.BaseDirectory, "startup_log.txt");

        public static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"{DateTime.Now:HH:mm:ss.fff}  {msg}{Environment.NewLine}");
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try { File.WriteAllText(LogPath, ""); } catch { }

            Log("=== OnStartup entered ===");

            // Force app to stay alive even if no window is open.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // ═══════════════════════════════════════════════════════════
            //  Dispatcher exceptions — log AND swallow so the app
            //  doesn't crash on non-fatal UI exceptions.
            // ═══════════════════════════════════════════════════════════
            DispatcherUnhandledException += (_, args) =>
            {
                Log("!!! DispatcherUnhandledException: " + args.Exception);
                // Critical: without this, WPF terminates the app.
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                Log("!!! AppDomain.UnhandledException: " + args.ExceptionObject);
            };

            // ═══════════════════════════════════════════════════════════
            //  First-chance logging — captures even caught exceptions
            //  to help diagnose silent failures.
            // ═══════════════════════════════════════════════════════════
            AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
            {
                var ex = args.Exception;
                if (ex is NullReferenceException ||
                    ex is InvalidOperationException ||
                    ex is TypeInitializationException ||
                    ex is XamlParseException ||
                    ex is FileNotFoundException ||
                    ex is DllNotFoundException ||
                    ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is NotSupportedException ||
                    ex is System.Data.Common.DbException ||
                    ex is Microsoft.Data.Sqlite.SqliteException)
                {
                    Log("FirstChanceException: " + ex.GetType().Name + " — " + ex.Message);
                }
            };

            Log("=== calling base.OnStartup ===");
            base.OnStartup(e);
            Log("=== base.OnStartup returned ===");
        }
    }
}