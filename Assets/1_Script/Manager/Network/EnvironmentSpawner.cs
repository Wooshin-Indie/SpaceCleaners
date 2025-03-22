using MPGame.Controller;
using MPGame.Physics;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MPGame.Manager
{
    public class EnvironmentSpawner : NetworkBehaviour
	{
		#region Singleton
		private static EnvironmentSpawner instance;
		public static EnvironmentSpawner Instance { get => instance; }

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
		#endregion

		[SerializeField] 
		private List<GameObject> environments = new List<GameObject>();
		
		[SerializeField] 
		private GameObject planetPrefab;

		public void SpawnEnvironments()
		{
			for (int i = 0; i < environments.Count; i++)
			{
				GameObject go = Instantiate(environments[i]);
				NetworkObject no = go.GetComponent<NetworkObject>();
				no?.Spawn();
			}
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

	}
}