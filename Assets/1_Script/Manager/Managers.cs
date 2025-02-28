using UnityEngine;

namespace MPGame.Manager
{
	public class Managers : MonoBehaviour
	{
		private static Managers instance;
		public static Managers Instance { get => instance; }

		void Awake()
		{
			Init();
		}

		private void Init()
		{
			if (null == instance)
			{
				instance = this;
				DontDestroyOnLoad(this.gameObject);
			}
			else
			{
				Destroy(this.gameObject);
			}

			_scene.Init();
		}

		private static  ResourceManager _resource = new ResourceManager();
		private static  SceneManagerEx _scene = new SceneManagerEx();

		public static ResourceManager Resource { get => _resource; }
		public static SceneManagerEx Scene { get => _scene; }
	}
}