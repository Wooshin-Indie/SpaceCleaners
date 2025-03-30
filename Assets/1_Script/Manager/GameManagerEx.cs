using MPGame.Structs;
using MPGame.Utils;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MPGame.Manager
{
    public class GameManagerEx : MonoBehaviour
    {
		#region Singleton
		private static GameManagerEx instance;
		public static GameManagerEx Instance { get => instance; }

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
		}
		#endregion

		private bool isConnected;
		private bool isGame;
		private bool isHost;
		private ulong myClientId;

		public ulong MyClientId { get => myClientId; set => myClientId = value;}

		public Dictionary<ulong, PlayerInfo> playerInfo = new Dictionary<ulong, PlayerInfo>();


		public void SendMessageToChat(string text, ulong fromwho, bool server)
		{
			string name = Constants.NAME_SERVER;

			if (!server && playerInfo.ContainsKey(fromwho))
			{
				name = playerInfo[fromwho].steamName;
			}

			UIManager.Lobby.SendMessageToUI(name, text);
		}

		public void GameStarted()
		{
			Managers.Scene.ChangeScene(Utils.SceneEnum.Game);

			if (NetworkManager.Singleton.IsHost)
			{
				EnvironmentSpawner.Instance.SpawnGameScene();
			}
		}

		public void GameEnded()
		{
			Managers.Scene.ChangeScene(Utils.SceneEnum.Lobby);
			
			if (NetworkManager.Singleton.IsHost)
			{
				EnvironmentSpawner.Instance.SpawnLobbyScene();
			}
		}

		public void HostCreated()
		{
			Managers.Scene.ChangeScene(SceneEnum.Lobby);
			EnvironmentSpawner.Instance.SpawnLobbyScene();
			isHost = true;
			isConnected = true;
		}

		public void ConnectedAsClient()
		{
			Managers.Scene.UnloadCurrentScene();

			isHost = false;
			isConnected = true;
		}

		public void Disconnected()
		{
			playerInfo.Clear();
			GameObject[] playercards = GameObject.FindGameObjectsWithTag(Constants.TAG_PCARD);
			foreach(GameObject card in playercards)
			{
				Destroy(card);
			}

			Managers.Scene.ChangeScene(SceneEnum.Main);
			isHost = false;
			isConnected = false;
		}

		public void AddPlayerToDictionary(ulong clientId, string steamName, ulong steamId)
		{
			if (!playerInfo.ContainsKey(clientId))
			{
				PlayerInfo pi = new PlayerInfo(steamName, steamId);
				playerInfo.Add(clientId, pi);
				UIManager.Lobby.OnAddPlayerToDictionary(pi);
			}
		}

		public void UpdateClients()
		{
			foreach(KeyValuePair<ulong, PlayerInfo> player in playerInfo)
			{
				ulong steamId = player.Value.steamId;
				string steamName = player.Value.steamName;
				ulong clientId = player.Key;

				NetworkTransmission.instance.UpdateClientsPlayerInfoClientRPC(steamId, steamName, clientId);
			}
		}

		public void RemovePlayerFromDictionary(ulong steamId)
		{
			PlayerInfo value = null;
			ulong key = 100;
			foreach(KeyValuePair<ulong, PlayerInfo> player in playerInfo)
			{
				if (player.Value.steamId == steamId)
				{
					value = player.Value;
					key = player.Key;
				}
			}
			if (key != 100)
			{
				playerInfo.Remove(key);
			}
			UIManager.Lobby.OnRemovePlayerFromDictionary(value);
		}

		public void UpdatePlayerIsReady(bool isReady, ulong clientId)
		{
			foreach (KeyValuePair<ulong, PlayerInfo> player in playerInfo)
			{
				if (player.Key == clientId)
				{
					player.Value.isReady = isReady;
					UIManager.Lobby.OnUpdatePlayerReady(isReady, player.Value.steamId);
				}
			}
		}

		public bool IsAllPlayerReady()
		{
			foreach (KeyValuePair<ulong, PlayerInfo> player in playerInfo)
			{
				if (!player.Value.isReady)
				{
					return false;
				}
			}

			return true;
		}

		public void Quit()
		{
			Application.Quit();
		}
	}
}