using MPGame.Utils;

namespace MPGame.Structs
{
	public class GameScene : SceneBase
	{
		protected override void Init()
		{
			base.Init();
			SceneEnum = SceneEnum.Game;
		}

		public override void Clear()
		{

		}
	}
}