#include "\airs\_common.hpp"
class cfgpatches
{
	class functions_f_airs
	{
		AUTHORS
		NAME(Functions)
		URL
		VERSION
		units[] = {};
		weapons[] = {};
		requiredAddons[] = { "A3_Functions_F", "main_f_airs"};
	};
};
class extended_preStart_eventhandlers
{
	class airs_preStart_event
	{
		init = "call compile preprocessFileLineNumbers '\airs\functions_f_airs\XEH\XEH_prestart.sqf'";
	};
};
class extended_preinit_eventhandlers
{
	class airs_preinit_event
	{
		init = "call compile preprocessFileLineNumbers '\airs\functions_f_airs\XEH\XEH_preinit.sqf'";
	};
};

#include "cfgfunctions.hpp"
