using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace MPGame.Manager
{
    public class PlayerSpawner : NetworkBehaviour
	{
		private static PlayerSpawner instance;
		public static PlayerSpawner Instance { get => instance; }

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

		[SerializeField] private GameObject playerPrefab;
		private Dictionary<ulong, GameObject> players = new Dictionary<ulong, GameObject>();

		[ServerRpc(RequireOwnership = false)]
		public void SpawnPlayerServerRPC(ulong clientId)
		{
			GameObject go = Instantiate(playerPrefab, new Vector3(0, 1f, 0), Quaternion.identity);
			NetworkObject no = go.GetComponent<NetworkObject>();
			
			if (no != null)
			{
				no.SpawnAsPlayerObject(clientId);
			}

			players.Add(clientId, go);
		}

		[ServerRpc(RequireOwnership = false)]
		public void DespawnPlayerServerRPC(ulong clientId)
		{
			ulong key = 100;
			foreach (var entry in players)
			{
				if(entry.Key == clientId)
				{
					key = entry.Key;
					break;
				}
			}

			GameObject go = players[key];
			players.Remove(key);
			go.GetComponent<NetworkObject>().Despawn();
			Destroy(go);
		}

	}
}