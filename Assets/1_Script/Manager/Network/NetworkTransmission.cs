using Unity.Netcode;
using UnityEngine;

namespace MPGame.Manager
{
	public class NetworkTransmission : NetworkBehaviour
	{
		public static NetworkTransmission instance;

		private void Awake()
		{
			if (instance != null)
			{
				Destroy(gameObject);
			}
			else
			{
				instance = this;
				DontDestroyOnLoad(gameObject);
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void IWishToSendAChatServerRPC(string message, ulong fromwho)
		{
			ChatFromServerClientRPC(message, fromwho);
		}

		[ClientRpc]
		private void ChatFromServerClientRPC(string message, ulong fromwho)
		{
			GameManagerEx.Instance.SendMessageToChat(message, fromwho, false);
		}

		[ServerRpc(RequireOwnership = false)]
		public void AddMeToDictionayServerRPC(ulong steamId, string steamName, ulong clientId)
		{
			GameManagerEx.Instance.SendMessageToChat($"{steamName} has joined", clientId, true);
			GameManagerEx.Instance.AddPlayerToDictionary(clientId, steamName, steamId);
			GameManagerEx.Instance.UpdateClients();
		}

		[ClientRpc]
		public void UpdateClientsPlayerInfoClientRPC(ulong steamId, string steamName, ulong clientId)
		{
			GameManagerEx.Instance.AddPlayerToDictionary(clientId, steamName, steamId);
		}

		[ServerRpc(RequireOwnership = false)]
		public void RemoveMeFromDictionaryServerRPC(ulong steamId)
		{
			RemovePlayerFromDictionaryClientRPC(steamId);
		}

		[ClientRpc]
		public void RemovePlayerFromDictionaryClientRPC(ulong steamId)
		{
			Debug.Log("removing client");
			GameManagerEx.Instance.RemovePlayerFromDictionary(steamId);
		}

		[ServerRpc(RequireOwnership = false)]
		public void IsTheClientReadyServerRPC(bool ready, ulong clientId)
		{
			AClientMightBeReadyClientRPC(ready, clientId);
		}

		[ClientRpc]
		private void AClientMightBeReadyClientRPC(bool ready, ulong clientId)
		{
			GameManagerEx.Instance.UpdatePlayerIsReady(ready, clientId);
		}

		[ClientRpc]
		public void DisconnectAllClientRPC()
		{
			if (IsHost)
			{
				return;
			}
			GameNetworkManager.Instance.Disconnected();
		}



		public bool isInGame = false;

		[ServerRpc(RequireOwnership = false)]
		public void StartGameServerRPC()
		{
			if (!isInGame && GameManagerEx.Instance.IsAllPlayerReady())
			{
				isInGame = true;
				GameNetworkManager.Instance.LockLobby();
				StartGameClientRPC();
			}
		}

		[ClientRpc]
		public void StartGameClientRPC()
		{
			isInGame = true;
			GameManagerEx.Instance.GameStarted();
		}

		[ServerRpc(RequireOwnership = false)]
		public void EndGameServerRPC()
		{
			if (isInGame)
			{
				GameNetworkManager.Instance.UnlockLobby();
				EndGameClientRPC();
			}
		}

		[ClientRpc]
		public void EndGameClientRPC()
		{
			isInGame = false;
			GameManagerEx.Instance.GameEnded();
		}

	}
}