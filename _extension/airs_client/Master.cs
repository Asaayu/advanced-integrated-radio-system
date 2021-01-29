using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NAudio.Wave;
using NetworkChat;
using NAudio.CoreAudioApi;
using NAudio.Codecs;
using OpusDotNet;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace airs_client
{
    public class App
    {
        internal static string version = "0.0.1";
        internal static string version_info = "Advanced Integrated Radio System - " + version;
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

            Log.Info("AIRS VOIP - " + App.version);
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

            string return_string = "";

            // Send input to switch function
            try
            {
                return_string = Functions.Main(function);
                Log.Debug($"Function call: '{function}', returned '{return_string}'");
            }
            catch (Exception e)
            {
                Log.Info("Exception caught in main function call!");
                Log.Error(e.ToString());
            };

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
                // SET_PTT: Called when the user presses their Push-To-Talk key
                case "set_ptt":
                    VOIP.PTT(parameters[1] == "1", 0);
                    return "true";
                    
                // SET_PTT_RADIO: Called when the user presses their Radio Push-To-Talk key
                case "set_ptt_radio":
                    VOIP.PTT(parameters[1] == "1", 1);
                    return "true";
                    
                // SET_PTT_GLOBAL: Called when the user presses their Global Push-To-Talk key
                case "set_ptt_global":
                    VOIP.PTT(parameters[1] == "1", 2);
                    return "true";
                    
                // SET_PTT_RELEASE: Called when the user changes the release time before ending PTT
                case "set_ptt_release":
                    VOIP.ptt_release = float.Parse(parameters[1]);
                    return "true";

                // SET_VOICE_MODE: Called when the user switches from PTT to VAD or vice versa
                case "set_voice_mode":
                    VOIP.SetVoiceMode(int.Parse(parameters[1]));
                    return "true";
                    
                // SET_VOLUME_GATE: Called when the user chnages the volume gate setting
                case "set_volume_gate":
                    VOIP.volume_gate = int.Parse(parameters[1]);
                    return "true";

                // SET_LOCAL_PLAYBACK: Called when the user enables/disables local playback
                case "set_local_playback":
                    VOIP.local_playback = parameters[1] == "1";
                    return "true";
                    
                // SET_MIC_GAIN: Called when the user changes their mic gain setting
                case "set_mic_gain":
                    VOIP.mic_gain = float.Parse(parameters[1]);
                    return "true";

                // PREINIT: Called when a mission starts
                case "preinit":
                    UpdateDevice();
                    return "true";

                // SETUP: Called when the game starts
                case "setup":
                    return Setup();

                // DEBUG: Called when the game wants to log debug information to the debug log
                case "debug":
                    return Log.Debug(parameters[1]).ToString();

                // LOG: Called when the game wants to log to the debug log
                case "log":
                    return Log.Info(parameters[1]).ToString();

                // INFO: Show version information
                case "info":
                    return App.version_info;
            }
            return "";
        }

        private static string Setup()
        {
            // Setup the VOIP stuff
            VOIP.Setup();
            return "";
        }
        
        private static string UpdateDevice()
        {
            // Put the name of the device used for audio in the options menu
            var enumerator = new MMDeviceEnumerator();
            string device_name = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).FriendlyName;
            Master.callback.Invoke("AIRS_VOIP", "airs_fnc_set_device", device_name);
            return "";
        }
    };

    class VOIP
    {
        private static int voice_mode;
        internal static bool local_playback;
        internal static int volume_gate;
        internal static float mic_gain;

        internal static float ptt_release;

        internal static bool microphone_muted;
        internal static bool speakers_muted;

        private static OpusEncoder encoder;
        private static OpusDecoder decoder;

        private static WaveInEvent audio_input;
        private static WaveOutEvent audio_output;

        private static BufferedWaveProvider provider = new BufferedWaveProvider(new WaveFormat(16000, 2));

        private static UdpAudioSender sender;
        private static UdpAudioReceiver receiver;

        internal static string Setup()
        {
            sender = new UdpAudioSender(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9986));


            encoder = new OpusEncoder(Application.VoIP, 48000, 2);
            decoder = new OpusDecoder();

            audio_input = new WaveInEvent
            {
                BufferMilliseconds = 60,
                NumberOfBuffers = 2,
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 2)
        };
            audio_input.DataAvailable += OnAudioCaptured;

            audio_output = new WaveOutEvent
            {
                DesiredLatency = 60,
                NumberOfBuffers = 2
            };
            provider.DiscardOnBufferOverflow = true;
            audio_output.Init(provider);
            audio_output.Play();

            return "true";
        }

        internal static string SetVoiceMode(int mode)
        {
            Log.Debug($"Setting voice mode to '{mode}'");

            // Stop any recording that maybe already running
            audio_input.StopRecording();
            PTT(false, 0);

            // Start new system
            switch (mode)
            {
                // Push-To-Talk
                case 0:
                    voice_mode = 0;
                    return "true";

                // Voice Activation Detection
                case 1:
                    voice_mode = 1;
                    audio_input.StartRecording();
                    return "true";

                // Continuous Transmission
                case 2:
                    voice_mode = 2;
                    audio_input.StartRecording();
                    return "true";
            }
            Log.Error($"Voice mode can only be Push-To-Talk (0) Voice Activation Detection (1), or Continuous Transmission (2). Incorrect value: '{mode}'");
            return "false";
        }
        internal static string PTT(bool active, int type)
        {
            Log.Debug($"PTT: {active}");

            if (microphone_muted || speakers_muted)
                return "false";

            if (active)
            {
                // Start recording audio
                audio_input.StartRecording();
            }
            else
            {
                // Stop recording audio
                Task.Run(() =>
                {
                    // Delay the end of the message by the PTT release delay
                    Task.Delay(TimeSpan.FromSeconds(ptt_release));
                    audio_input.StopRecording();
                });
            };
            return "true";
        }

        private static async void OnAudioCaptured(object sender, WaveInEventArgs e)
        {
            // Do not send data if microphone or speakers are muted
            if (!microphone_muted && !speakers_muted)
            {
                bool gate_passed = true;

                switch (voice_mode)
                {
                        // Push-To-Talk does not need to be checked as audio is only recorded when button is pressed
                    case 0:
                        // Continuous Transmission always records audio
                    case 2:
                        if (mic_gain != 1.0f)
                            ApplyMicrophoneGain(e, 0, mic_gain);
                        break;

                        // Voice Activation Detection will need to make sure the volume is above the volume gate
                    case 1:
                        gate_passed = ApplyMicrophoneGain(e, 0, mic_gain) > volume_gate;
                        break;
                };


                if (gate_passed)
                {
                    if (local_playback)
                        provider.AddSamples(e.Buffer, 0, e.BytesRecorded);

                    await Task.Run(() =>
                    {
                        try
                        {
                            // Stopping the recording in the middle causes not enough frames to be saved
                            if (e.BytesRecorded < 3840)
                            {
                                List<byte> holder = new List<byte>();
                                holder.CopyTo(e.Buffer, 0);
                                while (e.BytesRecorded < 3840)
                                {
                                    holder.Add(0);
                                }
                            }
                            
                            // Encode the bytes in the Opus codec
                            byte[] encoded_bytes = new byte[e.BytesRecorded];
                            encoder.Encode(e.Buffer, e.BytesRecorded, encoded_bytes, 60);
                        }
                        catch (Exception exc)
                        {
                            Log.Info("An exception occured during opus encoding");
                            Log.Error(exc.ToString());
                        }
                    });
                }
            }
        }
    
        private static int ApplyMicrophoneGain(WaveInEventArgs e, int offset, float gain)
        {
            int max = 0;
            var buffer = new WaveBuffer(e.Buffer).ByteBuffer;


            if (gain == 0.0f)
            {
                for (int n = 0; n < e.BytesRecorded; n++)
                {
                    buffer[offset++] = 0;
                }
            }
            else
            {
                for (int n = 0; n < e.BytesRecorded; n += 2)
                {
                    short sample = (short)((buffer[offset + 1] << 8) | buffer[offset]);
                    var newSample = sample * gain;
                    sample = (short)newSample;

                    if (gain > 1.0f)
                    {
                        if (newSample > Int16.MaxValue) sample = Int16.MaxValue;
                        else if (newSample < Int16.MinValue) sample = Int16.MinValue;
                    }
                    if (sample >= max) max = sample;

                    buffer[offset++] = (byte)(sample & 0xFF);
                    buffer[offset++] = (byte)(sample >> 8);
                }
            }
            
            return max;
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
                if (App.airs_debug)
                    Console.WriteLine(DateTime.Now.ToString("[dd/MM/yyyy hh:mm:ss tt]") + "[CLIENT]" + "[" + prefix + "] " + message);

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
        [DllImport("kernel32")]
        static extern bool AllocConsole();

        internal static bool Setup(bool debug_console)
        {
            App.airs_debug = debug_console;
            if (debug_console)
            {
                AllocConsole();
                Console.Title = "AIRS VOIP - " + App.version + " | DO NOT CLOSE THIS WINDOW!!!";

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!! DO NOT CLOSE THIS WINDOW !!!");
                Console.ResetColor();

                Log.Info("'-airs_debug' parameter found, opening live log console");
            }
            return true;
        }

    }    
}

namespace NetworkChat
{
    interface IAudioSender : IDisposable
    {
        void Send(byte[] payload);
    }

    interface IAudioReceiver : IDisposable
    {
        void OnReceived(Action<byte[]> handler);
    }

    class UdpAudioSender : IAudioSender
    {
        private readonly UdpClient udpSender;
        public UdpAudioSender(IPEndPoint endPoint)
        {
            udpSender = new UdpClient();
            udpSender.Connect(endPoint);
        }

        public void Send(byte[] payload)
        {
            udpSender.Send(payload, payload.Length);
        }

        public void Dispose()
        {
            udpSender?.Close();
        }
    }

    class UdpAudioReceiver : IAudioReceiver
    {
        private Action<byte[]> handler;
        private readonly UdpClient udpListener;
        private bool listening;

        public UdpAudioReceiver(int portNumber)
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, portNumber);

            udpListener = new UdpClient();

            // To allow us to talk to ourselves for test purposes:
            // http://stackoverflow.com/questions/687868/sending-and-receiving-udp-packets-between-two-programs-on-the-same-computer
            udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpListener.Client.Bind(endPoint);

            ThreadPool.QueueUserWorkItem(ListenerThread, endPoint);
            listening = true;
        }

        private void ListenerThread(object state)
        {
            var endPoint = (IPEndPoint)state;
            try
            {
                while (listening)
                {
                    byte[] b = udpListener.Receive(ref endPoint);
                    handler?.Invoke(b);
                }
            }
            catch (SocketException)
            {
                // usually not a problem - just means we have disconnected
            }
        }

        public void Dispose()
        {
            listening = false;
            udpListener?.Close();
        }

        public void OnReceived(Action<byte[]> onAudioReceivedAction)
        {
            handler = onAudioReceivedAction;
        }
    }
}