using MPGame.Controller;
using MPGame.Physics;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEditor.PlayerSettings;

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
		public Dictionary<ulong, GameObject> Players { get => players; }

        [SerializeField] private List<GameObject> environments =new List<GameObject>();
        [SerializeField] private GameObject spaceship;

        [SerializeField] private GameObject planetPrefab;

		public void SpawnEnvironments()
		{
			for (int i = 0; i < environments.Count; i++)
			{
				GameObject go = Instantiate(environments[i]);
				NetworkObject no = go.GetComponent<NetworkObject>();
				no?.Spawn();
			}
		}

        private GameObject spaceshipOb;
        public GameObject SpaceshipOb { get => spaceshipOb; }
        public void SpawnSpaceship()
        {
            spaceshipOb = Instantiate(spaceship);
            NetworkObject no = spaceshipOb.GetComponent<NetworkObject>();
            no?.Spawn();
        }

        public void SpawnGalaxy()
		{
			// HACK - 3개만 임시로 설치함
			for (int i = 0; i < 3; i++)
			{
				GameObject go = Instantiate(planetPrefab);
				go.GetComponent<PlanetBody>().SetPlanetSize(Random.Range(300, 500), 1000 * (i + 1), 30 * i);
				NetworkObject no = go.GetComponent<NetworkObject>();
				no?.Spawn();
			}

			NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>().FindPlanets();
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