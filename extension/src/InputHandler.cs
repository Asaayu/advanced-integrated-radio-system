
using TeamSpeak.Sdk.Client;

namespace AdvancedIntegratedRadioSystem
{
    internal static class InputHandler
    {
        internal static string ProcessInput(string argument)
        {
            switch (argument)
            {
                // INFO: Return information about the extension
                case "info":
                    return $"Advanced Integrated Radio System [{VersionManager.GetModVersion()}]";

                // CONNECT: Connect to the TeamSpeak server
                case "connect":
                    Common.ConnectToTeamSpeak();
                    return "true";

                // DEFAULT: Return false as the command is not recognized
                default:
                    Logger.Warn($"The command '{argument}' is not recognized.");
                    return "false";
            }
        }

        internal static (int, string) ProcessArrayInput(string argument, string[] parameters)
        {
            switch (argument)
            {
                default:
                    Logger.Warn($"The array command '{argument}' is not recognized.");
                    return (-1, null);
            }
        }
    }
}