using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

		[ServerRpc]
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

		[ServerRpc]
		public void DespawnPlayerServerRPC(ulong clientId)
		{
			foreach (var entry in players)
			{
				if(entry.Key == clientId)
				{
					GameObject go = entry.Value;
					players.Remove(clientId);
					go.GetComponent<NetworkObject>().Despawn();
					Destroy(go);
				}
			}
		}

	}
}