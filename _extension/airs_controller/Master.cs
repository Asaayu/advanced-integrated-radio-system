using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace airs_controller
{
    public class App
    {
        internal static string version = "0.0.1";
        internal static string version_info = "Advanced Integrated Radio System - " + version;
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
        internal static int[] pos_asl = new int[3];
        internal static int[] vector_dir = new int[3];
        internal static int[] vector_up = new int[3];
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
                // UPDATE: Update the internal position, direction and up vectors for 3D space.
                case "update":
                    // Set position variable
                    Internal.pos_asl.SetValue(int.Parse(parameters[1]), 0);
                    Internal.pos_asl.SetValue(int.Parse(parameters[2]), 1);
                    Internal.pos_asl.SetValue(int.Parse(parameters[3]), 2);

                    // Set direction vector
                    Internal.vector_dir.SetValue(int.Parse(parameters[4]), 0);
                    Internal.vector_dir.SetValue(int.Parse(parameters[5]), 1);
                    Internal.vector_dir.SetValue(int.Parse(parameters[6]), 2);

                    // Set up vector
                    Internal.vector_dir.SetValue(int.Parse(parameters[7]), 0);
                    Internal.vector_dir.SetValue(int.Parse(parameters[8]), 1);
                    Internal.vector_dir.SetValue(int.Parse(parameters[9]), 2);
                    return "";

                // INFO: Show version information
                case "info":
                    return App.version_info;
            }
            return "";
        }
    };
}
