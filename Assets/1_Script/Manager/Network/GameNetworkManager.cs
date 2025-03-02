using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;
using System.Threading.Tasks;

namespace MPGame.Manager
{
	public class GameNetworkManager : MonoBehaviour
	{
		public static GameNetworkManager Instance { get => instance; }
		private static GameNetworkManager instance = null;

		private FacepunchTransport transport = null;

		public Lobby? currentLobby { get; private set; } = null;

		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else
			{
				Destroy(gameObject);
				return;
			}
		}


		private void Start()
		{
			transport = GetComponent<FacepunchTransport>();

			SteamMatchmaking.OnLobbyCreated += SteamMatchmaking_OnLobbyCreated;
			SteamMatchmaking.OnLobbyEntered += SteamMatchmaking_OnLobbyEntered;
			SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyJoined;
			SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyLeaved;
			SteamMatchmaking.OnLobbyInvite += SteamMatchMaking_OnLobbyInvite;
			SteamMatchmaking.OnLobbyGameCreated += SteamMatchmaking_OnLobbyGameCreated;
			SteamFriends.OnGameLobbyJoinRequested += SteamFriends_OnGameLobbyJoinRequested;

		}

		private void OnDestroy()
		{
			SteamMatchmaking.OnLobbyCreated -= SteamMatchmaking_OnLobbyCreated;
			SteamMatchmaking.OnLobbyEntered -= SteamMatchmaking_OnLobbyEntered;
			SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyJoined;
			SteamMatchmaking.OnLobbyMemberLeave -= SteamMatchmaking_OnLobbyLeaved;
			SteamMatchmaking.OnLobbyInvite -= SteamMatchMaking_OnLobbyInvite;
			SteamMatchmaking.OnLobbyGameCreated -= SteamMatchmaking_OnLobbyGameCreated;
			SteamFriends.OnGameLobbyJoinRequested -= SteamFriends_OnGameLobbyJoinRequested;

			if (NetworkManager.Singleton == null) return;

			NetworkManager.Singleton.OnServerStarted -= Singleton_OnServerStarted;
			NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
			NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectedCallback;
		}

		private void OnApplicationQuit()
		{
			Disconnected();
		}

		private void SteamMatchmaking_OnLobbyCreated(Result result, Lobby lobby)
		{
			if (result != Result.OK)
			{
				Debug.Log("Lobby was not created");
				return;
			}
			lobby.SetPublic();
			lobby.SetJoinable(true);
			lobby.SetGameServer(lobby.Owner.Id);
			Debug.Log($"Lobby created : {lobby.Owner.Name}");

			NetworkTransmission.instance.AddMeToDictionayServerRPC(SteamClient.SteamId, SteamClient.Name, NetworkManager.Singleton.LocalClientId);
			PlayerSpawner.Instance.SpawnPlayerServerRPC(NetworkManager.Singleton.LocalClientId);
		}

		private void SteamMatchmaking_OnLobbyEntered(Lobby lobby)
		{
			Debug.Log("LobbyEntered!");
			if (NetworkManager.Singleton.IsHost) return;
			StartClient(currentLobby.Value.Owner.Id);
		}
		private void SteamMatchmaking_OnLobbyJoined(Lobby lobby, Friend friend)
		{
			Debug.Log("member join");
		}
		private void SteamMatchmaking_OnLobbyLeaved(Lobby lobby, Friend friend)
		{
			Debug.Log("member leave");
			if(friend.Id == lobby.Owner.Id)
			{
				Debug.Log("HOST LEAVED");
			}
			GameManagerEx.Instance.SendMessageToChat($"{friend.Name} has left", friend.Id, true);
			NetworkTransmission.instance.RemoveMeFromDictionaryServerRPC(friend.Id);
		}

		private void SteamMatchMaking_OnLobbyInvite(Friend friend, Lobby lobby)
		{
			Debug.Log($"Invite from {friend.Name}");
		}
		private void SteamMatchmaking_OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId steamId)
		{
			Debug.Log("Lobby was created");
			GameManagerEx.Instance.SendMessageToChat($"Lobby was created : ", NetworkManager.Singleton.LocalClientId, true);
		}

		// Accept the invice or join on a friend
		private async void SteamFriends_OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
		{
			RoomEnter joinedLobby = await lobby.Join();

			if (joinedLobby != RoomEnter.Success)
			{
				Debug.Log("Failed to create lobby");
			}
			else
			{
				currentLobby = lobby;
				GameManagerEx.Instance.ConnectedAsClient();
				Debug.Log("Joined Lobby");
			}
		}

		public async void StartHost(int maxMembers)
		{
			NetworkManager.Singleton.OnServerStarted += Singleton_OnServerStarted;
			NetworkManager.Singleton.StartHost();
			GameManagerEx.Instance.MyClientId = NetworkManager.Singleton.LocalClientId;
			currentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
		}

		public void StartClient(SteamId steamId)
		{
			NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
			NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectedCallback;
			transport.targetSteamId = steamId;
			GameManagerEx.Instance.MyClientId = NetworkManager.Singleton.LocalClientId;
			if (NetworkManager.Singleton.StartClient())
			{
				Debug.Log("Client has started");
			}
		}

		public async void Disconnected()
		{
			PlayerSpawner.Instance.DespawnPlayerServerRPC(NetworkManager.Singleton.LocalClientId);
			if (NetworkManager.Singleton.IsHost)
			{
				NetworkTransmission.instance.DisconnectAllClientRPC();
				await Task.Delay(500);
			}

			currentLobby?.Leave();
			if (NetworkManager.Singleton == null) return;

			if (NetworkManager.Singleton.IsHost)
			{
				NetworkManager.Singleton.OnServerStarted -= Singleton_OnServerStarted;
			}
			else
			{
				NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
			}
			NetworkManager.Singleton.Shutdown(true);
			GameManagerEx.Instance.Disconnected();
		}


		private void Singleton_OnClientDisconnectedCallback(ulong clientId)
		{
			NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectedCallback;
			if (clientId == 0)
			{
				Disconnected();
			}
		}
		private void Singleton_OnClientConnectedCallback(ulong clientId)
		{
			NetworkTransmission.instance.AddMeToDictionayServerRPC(SteamClient.SteamId, SteamClient.Name, clientId); 
			PlayerSpawner.Instance.SpawnPlayerServerRPC(clientId);

			GameManagerEx.Instance.MyClientId = clientId;
			NetworkTransmission.instance.IsTheClientReadyServerRPC(false, clientId);
			Debug.Log($"Client has connected : {clientId}");
		}

		private void Singleton_OnServerStarted()
		{
			Debug.Log("Host started");
			GameManagerEx.Instance.HostCreated();
		}


	}
}