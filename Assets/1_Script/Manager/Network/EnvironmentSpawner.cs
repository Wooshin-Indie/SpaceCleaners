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
		private GameObject planetPrefab;

		// LobbyScene 에서 필요한 NetworkObejct들 스폰하는 함수
		// ex. 우주선
		public void SpawnLobbyScene()
		{
			SpawnSpaceship();
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
			MoveSpaceship();
			SpawnEnvironments();
			SpawnGalaxy();
		}

		private void MoveSpaceship()
		{
			if (currentSpaceship == null) return;

			currentSpaceship.GetComponent<Rigidbody>().isKinematic = false;
			// TODO - 스폰 포인트 설정해야됨
			currentSpaceship.transform.position = new Vector3(-12.25f, -0.21f, -10.59f);
		}

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

		public void DespawnAll()
		{

		}

	}
}