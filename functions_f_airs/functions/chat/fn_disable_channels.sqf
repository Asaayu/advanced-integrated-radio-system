for "_id" from 0 to 15 do
{
	// Disable voice in all channels, no not change text permission.
	(channelEnabled _id) params ["_chat", "_voice"];
	_id enableChannel [_chat, false];
};
true;
