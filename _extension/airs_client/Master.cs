using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace airs_client
{
    public class App
    {
        internal static string version = "0.0.1";
        internal static string version_info = "Advanced Integrated Radio System - " + version;

    };

    public class Master
    {
        // Import console
        [DllImport("kernel32")]
        static extern bool AllocConsole();

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

            AllocConsole();
            Console.WriteLine("Hello!");

            Log.Setup();

            Log.Info("DLL loaded by game...");            

            output.Append("AIRS VOIP - " + App.version);
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

    public class Internal
    {
        internal static double[] pos_asl = new double[3];
        internal static double[] vector_dir = new double[3];
        internal static double[] vector_up = new double[3];

        internal static bool Update(double[] pos, double[] dir, double[] up)
        {
            // Update variables
            pos_asl = pos;
            vector_dir = dir;
            vector_up = up;
            return true;
        }
    };

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
                // UPDATE: Update the internal position, direction and up vectors in 3D space.
                case "update":
                    try
                    {
                        double[] pos = new double[3] { double.Parse(parameters[1]), double.Parse(parameters[2]), double.Parse(parameters[3]) };
                        double[] dir = new double[3] { double.Parse(parameters[4]), double.Parse(parameters[5]), double.Parse(parameters[6]) };
                        double[] up = new double[3] { double.Parse(parameters[7]), double.Parse(parameters[8]), double.Parse(parameters[9]) };
                        Internal.Update(pos, dir, up);
                    }
                    catch (Exception e)
                    {
                        Log.Info("Error updating position, direction, and up vectors...");
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
            log_file = current_directory + @"\logs\" + "AIRS_CLIENT_" + DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".txt";

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
                using (StreamWriter sw = File.AppendText(log_file))
                {
                    sw.WriteLine(DateTime.Now.ToString("[dd/MM/yyyy hh:mm:ss tt]") + "[" + prefix + "] " + message);
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
}
