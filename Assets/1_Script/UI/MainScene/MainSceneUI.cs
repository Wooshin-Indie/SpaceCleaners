using MPGame.Manager;
using MPGame.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MPGame.UI.MainScene
{
    public class MainSceneUI : MonoBehaviour
    {
        [SerializeField] private Button hostButton;

		private void Start()
		{
			hostButton.onClick.AddListener(() =>
			{
				GameNetworkManager.Instance.StartHost(Constants.MAX_PLAYERS);
			});
		}

		private void OnDestroy()
		{
			hostButton.onClick.RemoveAllListeners();
		}
	}
}