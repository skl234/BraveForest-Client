public class CharacterListInfo
{
	public readonly ulong characterIndex;
	public readonly string name;
	public readonly ulong jobClass;
	public readonly ulong level;

	public CharacterListInfo(ulong _characterIndex, string _name, ulong _jobClass, ulong _level)
	{
		characterIndex = _characterIndex;
		name = _name;
		jobClass = _jobClass;
		level = _level;
	}
}
