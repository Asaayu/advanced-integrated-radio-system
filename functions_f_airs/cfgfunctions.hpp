class cfgfunctions
{
	class functions_f_airs
	{
		tag = "AIRS";
		class airs_log
		{
		        file = "\airs\functions_f_airs\functions\log";
			class log {};
		};
		class airs_extension
		{
		        file = "\airs\functions_f_airs\functions\extension";
			class callback {};
			class callback_server {};
		};
		class airs_callbacks
		{
		        file = "\airs\functions_f_airs\functions\callbacks";
			class set_input_device {};
			class set_output_device {};
		};
		class airs_chat
		{
		        file = "\airs\functions_f_airs\functions\chat";
			class disable_channels {};
			class switch_channel {};
		};
	};
};
