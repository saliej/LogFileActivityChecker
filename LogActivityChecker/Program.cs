using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace LogActivityChecker
{

    class Program
    {
        [DllImport("Kernel32.Dll", EntryPoint = "Wow64EnableWow64FsRedirection")]
        public static extern bool EnableWow64FSRedirection(bool enable);

        
        private static System.Timers.Timer _timer;
        private static DateTime _lastLineReadTime;
        static void Main(string[] args)
        {
            _timer = new System.Timers.Timer { Interval = Properties.Settings.Default.CheckInterval };
            
            _timer.Elapsed += (sender, eventArgs) =>
            {
                if ((DateTime.Now - _lastLineReadTime).TotalMilliseconds >= Properties.Settings.Default.InactivityLimit)
                {
                    Console.WriteLine("Logging activity appears to have stopped.");

                    var todaysLog = $"{DateTime.Today:yyyy-MM-dd}.log";
                    var errorCount = 0;
                    if (File.Exists(todaysLog))
                    {
                        errorCount = Convert.ToInt16(File.ReadAllText(todaysLog).Trim());
                    }

                    if (errorCount < Properties.Settings.Default.DailyErrorLimit)
                    {
                        try
                        {
                            File.WriteAllText(todaysLog, $"{++errorCount}");
                            EnableWow64FSRedirection(false);
                            Process.Start(Properties.Settings.Default.OnErrorProcess,
                                Properties.Settings.Default.OnErrorProcessArguments).WaitForExit();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }

                    _timer.Stop();

                    Environment.Exit(0);
                }
            };

            // Have the timer fire repeated events (true is the default)
            _timer.AutoReset = true;

            // Start the timer
            _timer.Enabled = true;
            _timer.Start();

            string logMessage;
            while ((logMessage = Console.ReadLine()) != null)
            {
                Console.WriteLine(logMessage);
                _lastLineReadTime = DateTime.Now;
            }

            _timer.Stop();

        }
    }
}
