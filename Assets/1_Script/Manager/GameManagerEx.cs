using MPGame.Structs;
using MPGame.Utils;
using System;
using System.Collections.Generic;
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


		public Action<string, string> OnSendMessageAction { get; set; }
		public Action <PlayerInfo> OnAddPlayerAction { get; set; }
		public Action <PlayerInfo> OnRemovePlayerAction { get; set; }
		public Action<bool, ulong> OnUpdatePlayerReadyAction { get; set; }

		public void SendMessageToChat(string text, ulong fromwho, bool server)
		{
			string name = Constants.NAME_SERVER;

			if (!server && playerInfo.ContainsKey(fromwho))
			{
				name = playerInfo[fromwho].steamName;
			}

			OnSendMessageAction(name, text);
		}

		public void HostCreated()
		{
			Managers.Scene.ChangeScene(SceneEnum.Lobby);
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
				OnAddPlayerAction?.Invoke(pi);

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
			
			OnRemovePlayerAction?.Invoke(value);
		}

		public void UpdatePlayerIsReady(bool isReady, ulong clientId)
		{
			foreach (KeyValuePair<ulong, PlayerInfo> player in playerInfo)
			{
				if (player.Key == clientId)
				{
					player.Value.isReady = isReady;
					OnUpdatePlayerReadyAction?.Invoke(isReady, player.Value.steamId);
				}
			}
		}

		public void Quit()
		{
			Application.Quit();
		}
	}
}