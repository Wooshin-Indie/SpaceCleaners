using MPGame.Utils;

namespace MPGame.Structs
{
	public class LobbyScene : SceneBase
	{
		protected override void Init()
		{
			base.Init();
			SceneEnum = SceneEnum.Lobby;
		}

		public override void Clear()
		{

		}
	}
}