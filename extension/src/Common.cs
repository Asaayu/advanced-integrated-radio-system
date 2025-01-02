using System;
using System.Globalization;
using System.Security.Principal;
using TeamSpeak.Sdk.Client;

namespace AdvancedIntegratedRadioSystem
{
    internal static class Common
    {
        internal static NumberFormatInfo numberFormatInfo;

        internal static void Setup()
        {
            // Setup the logging system
            Logger.Setup();

            // Setup the version manager
            VersionManager.Setup();

            // Setup the number format info for internal conversions to avoid culture issues
            numberFormatInfo = new NumberFormatInfo
            {
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = ""
            };
        }

        internal static async void ConnectToTeamSpeak()
        {
            try
            {
                Library.Initialize();

                //using (Connection connection = new Connection())
                //{
                //    await connection.Start("rQTSXAha4hGsogV6xn4MKJqrd/4=", "127.0.0.1", 9987, "client");
                //    await connection.SendTextMessage("Hello, World!");
                //    await connection.Stop("And good bye!");
                //}
            }
            catch (DllNotFoundException dllNotFoundException)
            {
                // handle missing native library
                Logger.Error("The TeamSpeak SDK library could not be found.", dllNotFoundException);
            }
            catch (Exception exception)
            {
                // handle any other exception
                Logger.Error("An error occurred while attempting to initialize the TeamSpeak SDK library.", exception);
            }
        }
    }
}