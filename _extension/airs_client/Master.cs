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
using NetworkChat;
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
                Log.Debug($"Calling: '{function}'...");
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
                    return VOIP.PTT(parameters[1] == "1", 0).ToString();

                // SET_PTT_RADIO: Called when the user presses their Radio Push-To-Talk key
                case "set_ptt_radio":
                    return VOIP.PTT(parameters[1] == "1", 1).ToString();

                // SET_PTT_GLOBAL: Called when the user presses their Global Push-To-Talk key
                case "set_ptt_global":
                    return VOIP.PTT(parameters[1] == "1", 2).ToString();

                // SET_AUDIO_CLICK: Called when the user changes the aucio click setting
                case "set_audio_click":
                    VOIP.audio_click = parameters[1] == "1";
                    return "true";

                // SET_VOICE_MODE: Called when the user switches from PTT to VAD or CT
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
                    
                // SET_PLAYBACK_VOLUME: Called when the user changes their playback volume setting
                case "set_output_volume":
                    VOIP.output_volume = float.Parse(parameters[1]);
                    if (VOIP.audio_output != null)
                        VOIP.audio_output.Volume = VOIP.output_volume;
                    return "true";
                    
                // SET_NOTIFICATION_VOLUME: Called when the user changes their notification volume setting
                case "set_notification_volume":
                    Audio.notification_volume = float.Parse(parameters[1]);
                    return "true";

                // SET_AUDIO_POSITION: Set where the audio is located for the notification audio
                case "set_audio_position":
                    Audio.SetAudioPosition(parameters[1], parameters[2]);
                    return "true";

                // TOGGLE_SPEAKERS: Called when the user mutes/unmutes the speaker output
                case "toggle_speakers":
                    return VOIP.ToggleSpeakers().ToString();

                // TOGGLE_MICROPHONE: Called when the user mutes/unmutes the microphone input
                case "toggle_microphone":
                    return VOIP.ToggleMicrophone().ToString();

                // UPDATE_DEVICES: Called when a mission starts
                case "update_devices":
                    // List default device in options menu
                    UpdateDevices();
                    return "true";
                    
                // CONNECT: Called when a mission starts
                case "connect":
                    // List default device in options menu
                    UpdateDevices();

                    VOIP.in_mission = true;

                    // Connect to server
                    ConnectToServer();
                    return "true";

                // DISCONNECT: Called when leaving a mission
                case "disconnect":
                    // List default device in options menu
                    UpdateDevices();

                    VOIP.in_mission = false;

                    // Disconnect from server
                    DisconnectFromServer();
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
        
        private static string ConnectToServer()
        {
            VOIP.connected = true;

            // Create output device
            VOIP.CreateOutputEvent();

            
            // Play notification
            Audio.PlayNotification("ui_connected");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"*** CONNECTED TO SERVER ***");
            Console.ResetColor();

            return "true";
        }
        
        private static string DisconnectFromServer()
        {
            VOIP.connected = false;

            // Delete any recording/output device
            VOIP.DeleteInputEvent();
            VOIP.DeleteOutputEvent();

            // Play notification
            Audio.PlayNotification("ui_disconnected");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"*** DISCONNECTED FROM SERVER ***");
            Console.ResetColor();
            return "true";
        }
        
        private static string UpdateDevices()
        {
            // Put the name of the devices used for audio in the options menu
            var enumerator = new MMDeviceEnumerator();
            string input_device_name = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).FriendlyName;
            string ouput_device_name = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console).FriendlyName;
            Master.callback.Invoke("AIRS_VOIP", "airs_fnc_set_input_device", input_device_name);
            Master.callback.Invoke("AIRS_VOIP", "airs_fnc_set_output_device", ouput_device_name);
            return "true";
        }
    };

    class Audio
    {
        internal static float notification_volume = 1.0f;
        private static Dictionary<string, string> audio_positions = new Dictionary<string, string>();
        internal static bool SetAudioPosition(string name, string filepath)
        {
            if (!filepath.StartsWith(@"\"))
                filepath = @"\" + filepath;

            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            path = $"{path.Remove(path.Length - 6)}{filepath}";
            if (!File.Exists($"{path}"))
            {
                Log.Error($"'{path}' could not be found make sure the file exists and has the correct extension!");
                return false;
            }
            audio_positions.Add(name, $"{path}");
            Log.Info($"Added {name} to audio positions dictionary at '{path}'");
            return true;
        }

        internal static bool PlayNotification(string name)
        {
            try
            {
                Log.Debug($"Playing notification: '{name}'");
                string filepath = audio_positions[name];

                WaveOutEvent notification_player = new WaveOutEvent();
                notification_player.PlaybackStopped += (object sender, StoppedEventArgs e) => { notification_player.Dispose();  };
                notification_player.Volume = notification_volume;
                notification_player.Init(new VorbisWaveReader(filepath));
                notification_player.Play();
                return true;

            }
            catch (Exception e)
            {
                Log.Info($"'{name}' file could not be found in audio position dictionary!");
                foreach (string i in audio_positions.Keys)
                {
                    Log.Debug(i);
                }
                Log.Error(e.ToString());
                return false;
            }
        }
    }

    class VOIP
    {
        private static string player_id = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process)["STEAMID"].ToString();

        private static int voice_mode;
        internal static bool local_playback;
        internal static int volume_gate;
        internal static float mic_gain;
        internal static float output_volume = 1.0f;

        internal static int transmission_type;

        internal static bool in_mission;
        internal static bool connected;

        internal static bool audio_click = false;
        internal static bool audio_click_done = false;
        internal static bool ct_click_done = false;

        internal static bool microphone_muted;
        internal static bool speakers_muted;

        private static OpusEncoder encoder;
        private static OpusDecoder decoder;

        private static WaveInEvent audio_input;
        internal static WaveOutEvent audio_output;

        private static BufferedWaveProvider provider = new BufferedWaveProvider(new WaveFormat(16000, 2));

        private static UdpAudioSender sender;
        private static UdpAudioReceiver receiver;

        internal static string Setup()
        {
            sender = new UdpAudioSender(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9986));

            encoder = new OpusEncoder(Application.VoIP, 16000, 2);
            decoder = new OpusDecoder(16000, 2);
            return "true";
        }

        internal static bool CheckStatus()
        {
            return (in_mission && connected);
        }

        internal static bool ToggleSpeakers()
        {
            if (!CheckStatus())
                return false;

            if (speakers_muted)
            {
                // Speakers unmuted
                CreateOutputEvent();
                speakers_muted = false;

                // Play notification
                Audio.PlayNotification("ui_speaker_unmute");
            }
            else
            {
                // Speakers muted
                DeleteOutputEvent();
                speakers_muted = true;

                // Play notification
                Audio.PlayNotification("ui_speaker_mute");
            };
            Log.Debug($"Speakers muted: {speakers_muted}");

            return speakers_muted;
        }

        internal static bool ToggleMicrophone()
        {
            if (!CheckStatus())
                return false;

            if (microphone_muted)
            {
                // Play notification
                Audio.PlayNotification("ui_mic_unmute");

                // Microphone unmuted
                switch (voice_mode)
                {
                    // Voice Activation Detection 
                    case 1:
                    // Continuous Transmission
                    case 2:
                        CreateInputEvent().StartRecording();
                        if (!ct_click_done)
                        {
                            if (audio_click)
                            {
                                // Play notification
                                Audio.PlayNotification("ui_ptt_start");
                            }

                            // Call eventhandler
                            Master.callback.Invoke("AIRS_VOIP", "airs_player_talking", $"['{player_id}',{transmission_type},{true}]");
                            ct_click_done = true;
                        }
                        break;
                }
                microphone_muted = false;
            }
            else
            {
                // Play notification
                Audio.PlayNotification("ui_mic_mute");

                if (audio_click_done)
                {
                    if (audio_click)
                    {
                        // Play notification
                        Audio.PlayNotification("ui_ptt_end");
                    }

                    // Call eventhandler
                    Master.callback.Invoke("AIRS_VOIP", "airs_player_talking", $"['{player_id}',{transmission_type},{false}]");
                    audio_click_done = false;
                }

                // Microphone muted
                DeleteInputEvent();
                microphone_muted = true;
            };
            Log.Debug($"Microphone muted: {microphone_muted}");

            return microphone_muted;
        }

        internal static bool SetVoiceMode(int mode)
        {
            Log.Debug($"Setting voice mode to '{mode}'");

            // Stop any audio input
            DeleteInputEvent();

            // Reset transmission type
            transmission_type = 0;

            // Start new system
            switch (mode)
            {
                // Push-To-Talk
                case 0:
                    voice_mode = 0;
                    return true;

                // Voice Activation Detection
                case 1:
                    voice_mode = 1;
                    CreateInputEvent().StartRecording();
                    return true;
            }

            Log.Error($"Voice mode can only be Push-To-Talk (0) or Voice Activation Detection (1). Incorrect value: '{mode}'");
            return false;
        }
        
        internal static bool PTT(bool active, int type)
        {
            if (!CheckStatus())
                return false;

            if (voice_mode != 0)
                return false;

            // Stop any audio recording
            DeleteInputEvent();

            if (microphone_muted)
                return false;

            if (active)
            {
                // Set transmission type for udp packet
                transmission_type = type;

                if (audio_click)
                {
                    // Play notification
                    Audio.PlayNotification("ui_ptt_start");
                }

                // Call eventhandler
                Master.callback.Invoke("AIRS_VOIP", "airs_player_talking", $"['{player_id}',{transmission_type},{true}]");
                audio_click_done = true;

                // Start recording audio
                CreateInputEvent().StartRecording();                
            }
            else if (audio_click_done)
            {
                if (audio_click)
                {
                    // Play notification
                    Audio.PlayNotification("ui_ptt_end");
                }

                // Call eventhandler
                Master.callback.Invoke("AIRS_VOIP", "airs_player_talking", $"['{player_id}',{transmission_type},{false}]");
                audio_click_done = false;
            }

            Log.Debug($"PTT: {active}");
            return active;
        }

        private static async void OnAudioCaptured(object sender, WaveInEventArgs e)
        {
            if (!CheckStatus())
                return;

            // Do not send data if microphone or speakers are muted
            if (!microphone_muted && !speakers_muted)
            {
                bool gate_passed = true;

                switch (voice_mode)
                {
                        // Push-To-Talk does not need to be checked as audio is only recorded when button is pressed
                    case 0:
                        if (mic_gain != 1.0f)
                            ApplyMicrophoneGain(e, 0, mic_gain);
                        break;

                    // Voice Activation Detection will need to make sure the volume is above the volume gate
                    case 1:
                        gate_passed = ApplyMicrophoneGain(e, 0, mic_gain) > volume_gate;

                        if (gate_passed && !audio_click_done)
                        {
                            if (audio_click)
                            {
                                // Play notification
                                Audio.PlayNotification("ui_ptt_start");
                            }

                            // Call eventhandler
                            Master.callback.Invoke("AIRS_VOIP", "airs_player_talking", $"['{player_id}',{transmission_type},{true}]");
                            audio_click_done = true;
                        }
                        else if (!gate_passed && audio_click_done)
                        {
                            if (audio_click)
                            {
                                // Play notification
                                Audio.PlayNotification("ui_ptt_end");
                            }

                            // Call eventhandler
                            Master.callback.Invoke("AIRS_VOIP", "airs_player_talking", $"['{player_id}',{transmission_type},{false}]");
                            audio_click_done = false;
                        }
                        break;
                };

                if (gate_passed)
                {
                    // Playback to player if enabled
                    if (local_playback)
                        provider.AddSamples(e.Buffer, 0, e.BytesRecorded);

                    

                    await Task.Run(() =>
                    {
                        try
                        {

                            // Stopping the recording in the middle causes not enough frames to be saved
                            if (e.BytesRecorded < 3840)
                                return;
                            
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

        internal static WaveInEvent CreateInputEvent()
        {
            if (!CheckStatus())
                return null;

            // Create new input event
            audio_input = new WaveInEvent
            {
                BufferMilliseconds = 60,
                NumberOfBuffers = 2,
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 2)
            };
            audio_input.DataAvailable += OnAudioCaptured;
            return audio_input;
        }   

        internal static WaveOutEvent CreateOutputEvent()
        {
            if (!CheckStatus())
                return null;

            // Create new output event
            audio_output = new WaveOutEvent
            {
                DesiredLatency = 60,
                NumberOfBuffers = 2
            };
            audio_output.Volume = output_volume;
            provider.DiscardOnBufferOverflow = true;
            audio_output.Init(provider);
            audio_output.Play();
            return audio_output;
        }

        internal static bool DeleteInputEvent()
        {
            if (audio_input != null)
            {
                audio_input.StopRecording();
                audio_input.Dispose();
            }
            return true;
        }

        internal static bool DeleteOutputEvent()
        {
            if (audio_output != null)
            {
                audio_output.Stop();
                audio_output.Dispose();
            }
            return true;
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
                Console.Title = "AIRS VOIP - " + App.version + " | DO NOT CLOSE THIS WINDOW!!!";
                Console.TreatControlCAsInput = true;

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