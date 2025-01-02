using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AdvancedIntegratedRadioSystem.Types;

namespace AdvancedIntegratedRadioSystem
{
    internal class Logger
    {
        // Will return true if Arma 3 has been launched in debug mode
        private static readonly bool DEBUG = Environment.CommandLine.Contains("-debug");

        [DllImport("kernel32")]
        private static extern bool AllocConsole();

        private static void OutputConsoleLine(string message, string prefix = "INFO")
        {
            if (string.IsNullOrEmpty(message)) return;

            // Always output errors to the RPT log
            Main.callback.Invoke("AIRS", "log", $"[{prefix}] {message}");
            if (!DEBUG) return;

            switch (prefix)
            {
                case "ERROR":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "SETUP":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "DEBUG":
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case "INPUT":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine($"{DateTime.Now.ToString("[dd/MM/yyyy hh:mm:ss tt]")}[{prefix}] {message}");
        }

        internal static void Setup()
        {
            if (!DEBUG) return;

            AllocConsole();
            Console.Title = "Advanced Integrated Radio System Debug Console";
        }

        internal static void DisplayMessageBox(string[] messages, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None, MessageBoxHelpLink messageBoxHelpLink = MessageBoxHelpLink.None)
        {
            // Display all message boxes in a new thread to prevent blocking the main thread
            Task.Run(() => {
                if (MessageBox.Show(string.Join("\n", messages), title, buttons, icon) == DialogResult.Yes)
                {
                    switch (messageBoxHelpLink)
                    {
                        default:
                            Logger.Warn("The message box help link is not recognized.");
                            break;
                    };
                };
            });
        }

        internal static void Error(string message, Exception exception, bool skipMessageBox = false)
        {
            OutputConsoleLine($"{message}\n{exception.Message}\n{exception.StackTrace}", "ERROR");
            OutputConsoleLine($"{message}\n{exception.Message}\n{exception.StackTrace}", "ERROR");

            if (!skipMessageBox)
            {
                string[] messages =
                {
                    "The Advanced Integrated Radio System has encountered an error.",
                    "Please inform a developer of the following error message:",
                    message,
                    exception.Message,
                    exception.StackTrace
                };
                DisplayMessageBox(messages, "Advanced Integrated Radio System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        internal static void Warn(string message)
        {
            OutputConsoleLine(message, "WARN");
        }

        internal static void Info(string message)
        {
            OutputConsoleLine(message, "INFO");
        }

        internal static void Debug(string message)
        {
            OutputConsoleLine(message, "DEBUG");
        }

        internal static void Input(string message)
        {
            OutputConsoleLine(message, "INPUT");
        }
    }
}