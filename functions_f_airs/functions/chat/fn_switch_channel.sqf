params ["_reset"];

if (isNil "airs_current_channel") then
{
	airs_current_channel = 3;
};

if _reset then
{
	// Reset the selected channel back to the old channel
	setCurrentChannel airs_current_channel;
}
else
{
	// Save the current channel, then switch to group to stop accidental double up on audio
	airs_current_channel = currentChannel;
	setCurrentChannel 3;
};
