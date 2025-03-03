using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
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

		[SerializeField] private List<GameObject> environments =new List<GameObject>();

		public void SpawnEnvironments()
		{
			for (int i = 0; i < environments.Count; i++)
			{
				GameObject go = Instantiate(environments[i]);
				NetworkObject no = go.GetComponent<NetworkObject>();
				no?.Spawn();
			}
		}

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