using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace airs_server
{
    public class App
    {
        internal static string version = "0.0.1";
        internal static string version_info = "[SERVER] Advanced Integrated Radio System Server - " + version;
    };

    public class Master
    {
        // Function call back stuff
        public static ExtensionCallback callback;
        public delegate int ExtensionCallback([MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string function, [MarshalAs(UnmanagedType.LPStr)] string data);

        // Do not remove these six lines
        #if WIN64
                [DllExport("RVExtensionRegisterCallback", CallingConvention = CallingConvention.Winapi)]
        #else
                [DllExport("_RVExtensionRegisterCallback@4", CallingConvention = CallingConvention.Winapi)]
        #endif
        public static void RVExtensionRegisterCallback([MarshalAs(UnmanagedType.FunctionPtr)] ExtensionCallback func)
        {
            callback = func;
        }

        // Do not remove these six lines
        #if WIN64
                [DllExport("RVExtensionVersion", CallingConvention = CallingConvention.Winapi)]
        #else
                [DllExport("_RVExtensionVersion@8", CallingConvention = CallingConvention.Winapi)]
        #endif
        public static void RvExtensionVersion(StringBuilder output, int outputSize)
        {
            // Reduce output by 1 to avoid accidental overflow
            outputSize--;

            Log.Setup();

            Log.Info("DLL loaded by game...");
            Log.Info("[SERVER] AIRS VOIP - " + App.version);
        }

        // Do not remove these six lines
        #if WIN64
                [DllExport("RVExtension", CallingConvention = CallingConvention.Winapi)]
        #else
                [DllExport("_RVExtension@12", CallingConvention = CallingConvention.Winapi)]
        #endif
        public static void RvExtension(StringBuilder output, int outputSize, [MarshalAs(UnmanagedType.LPStr)] string function)
        {
            // Reduce output by 1 to avoid accidental overflow
            outputSize--;

            // Send input to switch function
            output.Append(Functions.Main(function));
        }
    }


    public class Functions
    {
        internal static string Main(string input)
        {
            // Split on the spacers, in this case ":"
            String[] parameters = input.Split(':');

            // Make sure there is at least one parameter
            if (parameters.Length <= 0)
                return "";

            switch (parameters[0])
            {
                // INFO: Show version information
                case "preinit":
                    try
                    {
                        AIRS_Console.Setup(!bool.Parse(parameters[1]));
                    }
                    catch (Exception e)
                    {
                        Log.Info("Error occurred during preinit...");
                        Log.Error(e.ToString());
                    }
                    return "";
                    
                // INFO: Show version information
                case "info":
                    return App.version_info;
            }
            return "";
        }
    };

    public class Log
    {
        private static String log_file;

        internal static void Setup()
        {
            // Get current directory
            String current_directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // Save locations and files
            log_file = current_directory + @"\logs\" + "AIRS_SERVER" + DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".txt";

            try
            {
                // Create directories
                Directory.CreateDirectory(current_directory + @"\logs\");
            }
            catch (Exception e)
            {
                Info("An error has occured when attempting to create required directories...");
                Error(e.ToString());
            };
        }

        internal static bool Info(string message, string prefix = "INFO")
        {
            try
            {
                string final_message = DateTime.Now.ToString("[dd/MM/yyyy hh:mm:ss tt]") + "[" + prefix + "] " + message;
                if (AIRS_Console.console_open)
                    Console.WriteLine(final_message);

                using (StreamWriter sw = File.AppendText(log_file))
                {
                    sw.WriteLine(final_message);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool Error(string message)
        {
            return Info(message, "ERROR");
        }

    }

    public class AIRS_Console
    {
        internal static bool console_open;

        // Import console
        [DllImport("kernel32")]
        static extern bool AllocConsole();

        internal static bool Setup(bool dedicated_server)
        {
            if (dedicated_server)
            {
                console_open = true;

                AllocConsole();
                Console.Title = "[SERVER] AIRS VOIP - " + App.version + " | DO NOT CLOSE THIS WINDOW!!!";

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!! DO NOT CLOSE THIS WINDOW !!!");
                Console.ResetColor();

                Log.Info("Console opened...");
            }
            else
            {
                console_open = false;
                Log.Info("Console stopped from opening due to local server...");
            }
            return true;
        }

    }
}
