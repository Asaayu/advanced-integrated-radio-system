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
using System.Runtime.Serialization.Formatters.Binary;

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
                // SET_PTT: Enables/Disables Push-To-Talk capturing
                case "set_ptt":                    
                    return VOIP.PTT(parameters[1] == "1", 0).ToString();

                // SET_PTT_RADIO: Enables/Disables radio Push-To-Talk capturing
                case "set_ptt_radio":
                    return VOIP.PTT(parameters[1] == "1", 1).ToString();

                // SET_PTT_GLOBAL: Enables/Disables global admin Push-To-Talk capturing
                case "set_ptt_global":
                    return VOIP.PTT(parameters[1] == "1", 2).ToString();

                // SET_AUDIO_CLICK: Enables/Disables the audio mic click to allow the user to know when they are transmitting
                case "set_audio_click":
                    VOIP.audio_click = parameters[1] == "1";
                    return "true";

                // SET_VOICE_MODE: Change between Push-To-Talk and VAD capture modes
                case "set_voice_mode":
                    VOIP.SetVoiceMode(int.Parse(parameters[1]));
                    return "true";
                    
                // SET_VOLUME_GATE: Set the amplitude for the volume gate
                case "set_volume_gate":
                    VOIP.volume_gate = int.Parse(parameters[1]);
                    return "true";

                // SET_LOCAL_PLAYBACK: Enables/Disables local playback of the 
                case "set_local_playback":
                    VOIP.local_playback = parameters[1] == "1";
                    return "true";
                    
                // SET_REMOTE_PLAYBACK: 
                case "set_remote_playback":
                    VOIP.remote_playback = parameters[1] == "1";
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

                    // User is in a mission
                    VOIP.in_mission = true;

                    // Connect to server
                    ConnectToServer(parameters[1]);
                    return "true";
                    
                // CONNECTED: Called when the server responds to the players connection request
                case "connected":
                    // Save connected variable
                    UDP.connected = true;

                    // Create UDP connection
                    if (UDP.client != null)
                        UDP.Dispose();

                    // Create UDP connection
                    UDP.Create();
                    UDP.Enable();

                    // Create output device
                    VOIP.CreateOutputEvent();

                    // Play notification
                    Audio.PlayNotification("ui_connected");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"*** CONNECTED TO SERVER ***");
                    Console.ResetColor();
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
        
        private static string ConnectToServer(string server_ip)
        {
            UDP.server_ip = server_ip;

            // Call back to server to connect client
            Master.callback.Invoke("AIRS_VOIP", "airs_server_connect", $"['{VOIP.player_id}','{UDP.client_ip}']");
            return "true";
        }

        private static string DisconnectFromServer()
        {
            UDP.connected = false;

            // Dispose of the UDP connection to the server 
            UDP.Dispose();

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
        internal static string player_id = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process)["STEAMID"].ToString();

        internal static int voice_mode;
        internal static bool local_playback;
        internal static bool remote_playback;
        internal static int volume_gate;
        internal static float mic_gain;
        internal static float output_volume = 1.0f;

        internal static int transmission_type;

        internal static bool in_mission;

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

        internal static string Setup()
        {
            encoder = new OpusEncoder(Application.VoIP, 16000, 2);
            decoder = new OpusDecoder(16000, 2);
            return "true";
        }

        internal static bool CheckStatus()
        {
            return (in_mission && UDP.connected);
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

        private static void OnAudioCaptured(object sender, WaveInEventArgs e)
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

                    try
                    {
                        // Stopping the recording in the middle causes not enough frames to be saved
                        if (e.BytesRecorded < 3840)
                            return;

                        // Encode the bytes in the Opus codec
                        byte[] encoded_bytes = new byte[e.BytesRecorded];
                        encoder.Encode(e.Buffer, e.BytesRecorded, encoded_bytes, 60);

                        AIRS_DATA packet = new AIRS_DATA();
                        packet.Remote_playback = remote_playback;
                        packet.Object_id = player_id;
                        packet.Transmission_type = transmission_type;
                        packet.Voice_data = encoded_bytes;

                        BinaryFormatter bf = new BinaryFormatter();
                        using (var ms = new MemoryStream())
                        {
                            bf.Serialize(ms, packet);
                            UDP.Send(ms.ToArray());
                        }
                    }
                    catch (Exception exc)
                    {
                        Log.Info("An exception occured during opus encoding");
                        Log.Error(exc.ToString());
                    }
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

    class UDP
    {
        internal static string client_ip = new WebClient().DownloadString("http://icanhazip.com").Replace("\n", "").Replace(" ", "");
        internal static string server_ip;
        internal const int server_port = 9986;

        internal static bool connected;

        internal static IPEndPoint server_end_point = new IPEndPoint(IPAddress.Any, 0);

        internal static UdpClient client;

        private static Thread listen_thread;

        internal static bool Create()
        {
            try
            {
                // Create UDP client on the defined port
                client = new UdpClient(server_port - 1);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Log for user
                Console.ForegroundColor = ConsoleColor.Green;
                Log.Info("Created UDP client for client...");
                Console.ResetColor();
                return true;
            }
            catch (Exception e)
            {
                Log.Info("An error occured creating UDP client for client...");
                Log.Error(e.ToString());
                return false;
            }
        }

        internal static bool Enable()
        {
            try
            {
                // Create listen thread
                listen_thread = new Thread(Listen);
                listen_thread.Start();

                Console.ForegroundColor = ConsoleColor.Green;
                Log.Info("Client is now listening for data from server...");
                Console.ResetColor();
                return true;
            }
            catch (Exception e)
            {
                Log.Info("An error occured enabling UDP client for client...");
                Log.Error(e.ToString());
                return false;
            }
        }

        internal static bool Disable()
        {
            try
            {
                listen_thread.Abort();

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Log.Info($"Client is no longer listening for data from server...");
                Console.ResetColor();
                return true;
            }
            catch (Exception e)
            {
                Log.Info("An error occured disabling UDP client for client...");
                Log.Error(e.ToString());
                return false;
            }
        }

        internal static bool Dispose()
        {
            try
            {
                Disable();
                client.Dispose();
                return true;
            }
            catch (Exception e)
            {
                Log.Info("An error occured disposing UDP client for client...");
                Log.Error(e.ToString());
                return false;
            }
        }

        private static void Listen()
        {
            try
            {
                while (true)
                {
                    // Receive data
                    byte[] data = client.Receive(ref server_end_point);
                    Log.Debug("Got Data!");
                }
            }
            catch (ThreadAbortException)
            {
                Log.Info("Listening thread execution aborted!");
                return;
            }
            catch (Exception e)
            {
                Log.Info("An error occured when listening to UDP client on server...");
                Log.Error(e.ToString());
                return;
            }
        }

        internal static bool Send(byte[] data)
        {
            try
            {
                // Send data to server
                Log.Debug(client.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, server_port)).ToString());
                return true;
            }
            catch (Exception e)
            {
                Log.Info("An error occured sending data to UDP client on server...");
                Log.Error(e.ToString());
                return false;
            }
        }
    }

    [Serializable]
    public class AIRS_DATA
    {
        public int Transmission_type { get; set; }
        public bool Remote_playback { get; set; }
        public string Object_id { get; set; }
        public byte[] Voice_data { get; set; }
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