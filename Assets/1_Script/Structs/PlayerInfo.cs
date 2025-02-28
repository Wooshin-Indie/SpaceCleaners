
namespace MPGame.Structs
{
	public class PlayerInfo
	{
		public PlayerInfo(string name, ulong id)
		{
			steamName = name;
			steamId = id;
			isReady = false;
		}

		public string steamName;
		public ulong steamId;
		public bool isReady;
	}
}