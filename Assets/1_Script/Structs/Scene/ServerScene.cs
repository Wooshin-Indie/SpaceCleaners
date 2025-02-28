using MPGame.Utils;

namespace MPGame.Structs
{
	public class ServerScene : SceneBase
	{
		protected override void Init()
		{
			base.Init();
			SceneEnum = SceneEnum.None;
		}

		public override void Clear()
		{

		}
	}
}