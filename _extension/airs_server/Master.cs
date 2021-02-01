using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NVorbis;
using NAudio.Vorbis;
using OpusDotNet;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace airs_server
{
    public class App
    {
        internal static string version = "0.0.1";
        internal static string version_info = "[SERVER] Advanced Integrated Radio System - " + version;
        internal static bool airs_debug;
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

            AIRS_Console.Setup(Environment.CommandLine.Contains("-airs_debug"));

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
            string return_string = Functions.Main(function);
            Log.Debug($"Function call: '{function}', returned '{return_string}'");

            // Return output to Arma 3
            output.Append(return_string);
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
                // MISSION: Called when the server starts/ends a mission
                case "mission":
                    // Setup server if it hasn't been setup
                    if (UDPListener.listener == null)
                        VOIP.SetupServer();

                    VOIP.in_mission = parameters[1] == "1";
                    if (VOIP.in_mission)
                    {
                        // Allow client connections
                        VOIP.EnableServer();
                    }
                    else
                    {
                        // Disable client connections
                        VOIP.DisableServer();
                    }                    
                    return VOIP.in_mission.ToString();

                // INFO: Show version information
                case "info":
                    return App.version_info;
            }
            return "";
        }
    };

    class VOIP
    {
        internal static bool in_mission;
        internal static bool allow_connection;

        internal static string server_ip = new WebClient().DownloadString("http://icanhazip.com").Replace("\n", "").Replace(" ", "");
        internal static int port = 9987;

        internal static string SetupServer()
        {
            try
            {
                new Thread(() =>
                {
                    UDPListener.StartListener();
                }).Start();

                Master.callback.Invoke("AIRS_VOIP_SERVER", "airs_set_server_address", $"{server_ip}|{port}");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Log.Info($"Server on '{server_ip}' has started on port {port}");
                Log.Info($"Waiting for a mission to start before accepting data...");
                Console.ResetColor();
                return "true";

            }
            catch (Exception e)
            {
                Log.Info("An error occured starting server for client connections...");
                Log.Error(e.ToString());
                return "false";
            }
        }

        internal static string EnableServer()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Log.Info($"Server is now allowing connections from clients...");
            Console.ResetColor();

            allow_connection = true;
            return "true";
        }
        
        internal static string DisableServer()
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Log.Info($"Server is now blocking connections from clients...");
            Console.ResetColor();

            allow_connection = false;
            return "true";
        }
    }

    public class Log
    {
        private static String log_file;

        internal static void Setup()
        {
            // Get current directory
            String current_directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // Save locations and files
            log_file = current_directory + @"\logs\" + "AIRS_SERVER_" + DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".txt";

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
                if (App.airs_debug)
                    Console.WriteLine(DateTime.Now.ToString("[dd/MM/yyyy hh:mm:ss tt]") + "[SERVER]" + "[" + prefix + "] " + message);

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
        
        internal static bool Debug(string message)
        {
            if (App.airs_debug)
            {
                return Info(message, "DEBUG");
            };
            return false;
        }
    }

    public class AIRS_Console
    {
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32")]
        static extern bool AllocConsole();

        internal static bool Setup(bool debug_console)
        {
            App.airs_debug = debug_console;
            if (debug_console)
            {
                AllocConsole();
                DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);
                Console.Title = "[SERVER] AIRS VOIP - " + App.version + " | DO NOT CLOSE THIS WINDOW!!!";
                Console.TreatControlCAsInput = true;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!! DO NOT CLOSE THIS WINDOW !!!");
                Console.ResetColor();

                Log.Info("'-airs_debug' parameter found, opening live log console");
            }
            return true;
        }
    }

    public class UDPListener
    {
        internal static UdpClient listener;
        private static IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, VOIP.port);

        internal static void StartListener()
        {
            listener = new UdpClient(VOIP.port);

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for broadcast");
                    byte[] bytes = listener.Receive(ref remoteEP);

                    Console.WriteLine($"Received broadcast from {remoteEP} :");
                    Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }
        }

        internal static void StopListener()
        {
            Log.Info($"Closing UDP listner on port '{VOIP.port}'");

            listener.Close();
        }
    }
}
