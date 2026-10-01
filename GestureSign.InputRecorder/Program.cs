using GestureSign.InputRecorder.Native;
using GestureSign.InputRecorder.Recording;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GestureSign.InputRecorder
{
    internal static class Program
    {
        private const string Usage =
            "Usage:\n" +
            "  GestureSign.InputRecorder.exe                      interactive recorder\n" +
            "  GestureSign.InputRecorder.exe --out <file> --seconds <n> [--description <text>] [--no-mouse-move]\n" +
            "                                                     record headlessly for n seconds and write <file>";

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                return 0;
            }

            RecorderNativeMethods.AttachConsole(RecorderNativeMethods.ATTACH_PARENT_PROCESS);

            CliOptions options;
            string error = CliOptions.TryParse(args, out options);
            if (error != null)
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine(Usage);
                return 2;
            }
            if (options.Help)
            {
                Console.WriteLine(Usage);
                return 0;
            }
            return RunHeadless(options);
        }

        private static int RunHeadless(CliOptions options)
        {
            RecordingSession session;
            using (var capture = new InputCapture())
            {
                session = capture.Start(options.Description, options.RecordMouseMoves);
                var timer = new Timer { Interval = Math.Max(1, (int)(options.Seconds * 1000)) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    Application.ExitThread();
                };
                timer.Start();
                Application.Run();
                capture.Stop();
                timer.Dispose();
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(options.Output));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            session.Recording.Save(options.Output);

            foreach (string warning in session.Warnings)
                Console.Error.WriteLine("warning: " + warning);
            Console.WriteLine("Wrote {0}: {1} device(s), {2} hid event(s), {3} mouse event(s), {4} device change(s), {5} error(s)",
                options.Output,
                session.Recording.Devices.Count,
                session.Recording.Devices.Sum(d => session.GetHidCount(d.Id)),
                session.MouseEventCount,
                session.DeviceChangeCount,
                session.ErrorCount);
            return 0;
        }

        private sealed class CliOptions
        {
            public string Output;
            public double Seconds = -1;
            public string Description = "Command-line recording";
            public bool RecordMouseMoves = true;
            public bool Help;

            public static string TryParse(string[] args, out CliOptions options)
            {
                options = new CliOptions();
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    switch (arg)
                    {
                        case "--help":
                        case "-h":
                        case "/?":
                            options.Help = true;
                            return null;
                        case "--no-mouse-move":
                            options.RecordMouseMoves = false;
                            break;
                        case "--out":
                        case "--seconds":
                        case "--description":
                            if (i + 1 >= args.Length)
                                return "Missing value for " + arg;
                            string value = args[++i];
                            if (arg == "--out")
                                options.Output = value;
                            else if (arg == "--description")
                                options.Description = value;
                            else if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out options.Seconds) || options.Seconds <= 0)
                                return "--seconds must be a positive number";
                            break;
                        default:
                            return "Unknown argument " + arg;
                    }
                }
                if (string.IsNullOrEmpty(options.Output))
                    return "--out is required";
                if (options.Seconds <= 0)
                    return "--seconds is required";
                return null;
            }
        }
    }
}
