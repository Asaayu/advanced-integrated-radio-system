using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TeamSpeak.Sdk;
using TeamSpeak.Sdk.Client;

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

            Log.Setup();
            Log.Info("Setup successful...");

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

    public static class Functions
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
                // APPLICATION_START: Is run when the Arma 3 application starts
                case "application_start":
                    try
                    {
                        Log.Info("'APPLICATION_START' Starting...");


                        Log.Info("'APPLICATION_START' Ending...");
                    }
                    catch (Exception e)
                    {
                        Log.Info("'APPLICATION_START' Error:");
                        Log.Error(e.ToString());
                    }
                    return "true";

                // PLAYER_POS: Set the players position, velocity, and orentation
                case "player_pos":
                    try
                    {
                        Log.Info("'PLAYER_POS' Starting...");

                        List<float> inputs = new List<float>();
                        foreach (string i in parameters)
                        {
                            inputs.Add(float.Parse(i));
                        };

                        Vector pos = new Vector(inputs[0], inputs[1], inputs[2]);
                        Vector forward = new Vector(inputs[3], inputs[4], inputs[5]);
                        Vector up = new Vector(inputs[6], inputs[7], inputs[8]);

                        TS_SDK.connection.Set3DListenerAttributes(pos, forward, up);
                        Log.Info("'PLAYER_POS' Ending...");
                    }
                    catch (Exception e)
                    {
                        Log.Info("'PLAYER_POS' Error:");
                        Log.Error(e.ToString());
                    }
                    return "true";

                // INFO: Show version information
                case "info":
                    return App.version_info;
            }
            return "";
        }
    };

    public class TS_SDK
    {
        static byte[] WaveHeader = new byte[]
        {
            0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
            0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x80, 0xBB, 0x00, 0x00, 0x00, 0xB8, 0x0B, 0x00,
            0x10, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61, 0x00, 0x00, 0x00, 0x00
        };

        internal static Connection connection;
        static bool abort = false;
        const string identityfile = "identity.txt";
        static FileStream wavefile;

        static void Main()
        {
            Initialize();

            connection.OpenPlayback();
            Console.WriteLine($"Playback: {connection.PlaybackDevice}");
            connection.OpenCapture();
            Console.WriteLine($"Capture: {connection.CaptureDevice}");

            string identity = ReadIdentity() ?? Library.CreateIdentity();
            Task starting = connection.Start(identity, "localhost", 9986, "client", serverPassword: "secret");

            Console.WriteLine("Client lib initialized and running");
            Console.WriteLine($"Client lib version: {Library.Version}({Library.VersionNumber})");
        }

        private static void Initialize()
        {
            LibraryParameters parameters = new LibraryParameters("ts3_sdk_3.0.4/bin/");
            parameters.UsedLogTypes = LogTypes.File | LogTypes.Console | LogTypes.Userlogging;
            Library.Initialize(parameters);

            connection = Library.SpawnNewConnection();
            connection.StatusChanged += Connection_StatusChanged;
            connection.TalkStatusChanged += Connection_TalkStatusChanged;
            connection.ServerError += Connection_ServerError;
            //connection.EditMixedPlaybackVoiceData += Connection_EditMixedPlaybackVoiceData;
            //connection.Custom3dRolloffCalculationClient += Connection_Custom3dRolloffCalculationClient;
        }

        private static void Connection_StatusChanged(Connection connection, ConnectStatus newStatus, Error error)
        {
            Log.Info("Status Changed:");
            Log.Info(newStatus.ToString());
        }

        private static void Connection_ServerError(Connection connection, Error error, string returnCode, string extraMessage)
        {
            switch (error)
            {
                case Error.ConnectionLost:
                    Log.Error("Client lost connection to the server!");
                    break;
                
                case Error.SoundCouldNotOpenCaptureDevice:
                    Log.Error("Could not open capture device!");
                    break;
                
                case Error.SoundCouldNotOpenPlaybackDevice:
                    Log.Error("Could not open playback device!");
                    break;

                default:
                    Log.Error("And error has occured:");
                    Log.Error(extraMessage);
                    break;
            }
        }

        private static void Connection_TalkStatusChanged(Client client, TalkStatus status, bool isReceivedWhisper)
        {
            if (status == TalkStatus.Talking)
            {
                if (client.ID == connection.ID)
                {

                }
                else
                {

                }
            }
            else
            {

            }
        }

        private static string ReadIdentity()
        {
            try
            {
                return File.ReadAllText(identityfile);
            }
            catch
            {
                Console.WriteLine($"Could not read file '{identityfile}'.");
                return null;
            }
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
            log_file = current_directory + @"\logs\" + "AIRS_" + DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".txt";

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
