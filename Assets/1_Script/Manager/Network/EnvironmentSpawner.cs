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

		private GameObject currentSpaceship = null;

		[Header("LobbyScene")]

		[SerializeField]
		private GameObject spaceshipPrefab;


		[Header("GameScene")]

		[SerializeField] 
		private List<GameObject> environments = new List<GameObject>();
        [SerializeField] 
		private GameObject spaceship;

        [SerializeField] 
		private GameObject planetPrefab;
		[SerializeField]
		private GameObject sunPrefab;


		private List<GameObject> lobbySceneObjects = new List<GameObject>();
		private List<GameObject> gameSceneObjects = new List<GameObject>();

		public void SpawnLobbyScene()
		{
			DespawnGameSceneObjects();
			SpawnSpaceship();
			SpawnLobbyPlanet();
			NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>().FindPlanets();
		}

		private void SpawnSpaceship()
		{
			currentSpaceship = Instantiate(spaceshipPrefab);
			NetworkObject no = currentSpaceship.GetComponent<NetworkObject>();
			no?.Spawn();

			currentSpaceship.GetComponent<Rigidbody>().isKinematic = true;
			currentSpaceship.transform.position = new Vector3(-12.25f, -0.21f, -10.59f);
			currentSpaceship.transform.rotation = Quaternion.Euler(0, -90, 0);
		}

		public void SpawnGameScene()
		{
			DespawnLobbySceneObjects();
			MoveSpaceship();
			SpawnEnvironments();
			SpawnGalaxy();
		}

		private void MoveSpaceship()
		{
			if (currentSpaceship == null) return;

			currentSpaceship.GetComponent<Rigidbody>().isKinematic = false;
			currentSpaceship.GetComponent<Rigidbody>().MovePosition(new Vector3(300f, 300f, 300f));
		}

		public void SpawnEnvironments()
		{
			for (int i = 0; i < environments.Count; i++)
			{
				GameObject go = Instantiate(environments[i]);
				NetworkObject no = go.GetComponent<NetworkObject>();
				no?.Spawn();
				gameSceneObjects.Add(go);
			}
		}

		private void SpawnLobbyPlanet()
		{
			GameObject go = Instantiate(planetPrefab);
			NetworkObject no = go.GetComponent<NetworkObject>();
			no?.Spawn();
			go.GetComponent<PlanetBody>().SetStation(true);
			go.GetComponent<PlanetBody>().SetPlanetSize(1000f);
			go.GetComponent<Rigidbody>().position = new Vector3(0f, -500f, 0f);

			lobbySceneObjects.Add(go);
		}

		public GameObject SpaceshipOb { get => currentSpaceship; }

        public void SpawnGalaxy()
		{
			GameObject go;
			NetworkObject no;
			// HACK - 3개만 임시로 설치함
			for (int i = 0; i < 3; i++)
			{
				go = Instantiate(planetPrefab);
				go.GetComponent<PlanetBody>().SetPlanetSize(Random.Range(300, 500), 1000 * (i + 1), Random.Range(0f, 30f));
				no = go.GetComponent<NetworkObject>();
				no?.Spawn();
				gameSceneObjects.Add(go);
			}

			go = Instantiate(sunPrefab);
			go.transform.position = new Vector3(0f, 0f, 0f);
			no = go.GetComponent<NetworkObject>();
			no?.Spawn();
			gameSceneObjects.Add(go);

			NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>().FindPlanets();
		}

		public void DespawnLobbySceneObjects()
		{
			for (int i = 0; i < lobbySceneObjects.Count; i++)
			{
				lobbySceneObjects[i].GetComponent<NetworkObject>().Despawn();
				GameObject.Destroy(lobbySceneObjects[i]);
			}
			lobbySceneObjects.Clear();
		}

		public void DespawnGameSceneObjects()
		{
			for(int i=0; i<gameSceneObjects.Count; i++)
			{
				gameSceneObjects[i].GetComponent<NetworkObject>().Despawn();
				GameObject.Destroy(gameSceneObjects[i]);
			}
			gameSceneObjects.Clear();
		}

	}
}