
namespace MPGame.Utils
{
	public static class Constants
	{
		public static readonly string NAME_LOBBY = "lobby_name";
		public static readonly string NAME_SERVER = "Server";

		public static readonly string TAG_CHAT = "ChatMessage";
		public static readonly string TAG_PCARD = "PlayerCard";
		public static readonly string TAG_SCENE = "Scene";

		public static readonly int INT_VACUUMABLE = 12;

		public static readonly int LAYER_INTERACTABLE = 1 << 10;
		public static readonly int LAYER_GROUND = 1 << 11;
		public static readonly int LAYER_VACUUMABLE = 1 << INT_VACUUMABLE;

		public static readonly int MAX_PLAYERS = 4;

		public static readonly float CONST_GRAV = 100f;	// Gravitational Constant
	}
}