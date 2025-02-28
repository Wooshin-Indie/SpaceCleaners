using MPGame.Structs;
using TMPro;
using UnityEngine;

namespace MPGame.UI.LobbyScene.Items
{
    public class PlayerCard : MonoBehaviour
	{
		[SerializeField] private TMP_Text playerName;

		public GameObject readyImage;
		public ulong steamId;
		public ulong clientId;


		public void SetPlayerCard(PlayerInfo playerInfo)
		{
			playerName.text = playerInfo.steamName;
			steamId = playerInfo.steamId;
		}

		private void Start()
		{
			readyImage.SetActive(false);
		}
	}
}