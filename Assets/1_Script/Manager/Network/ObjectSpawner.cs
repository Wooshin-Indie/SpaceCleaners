using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using MPGame.Props;
using System.Linq;
using UnityEngine.InputSystem;

namespace MPGame.Manager
{
    public class ObjectSpawner : NetworkBehaviour
    {
        private static ObjectSpawner instance;
        public static ObjectSpawner Instance { get => instance; }

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                Vector3 pos = new Vector3(Random.Range(5, 7), 0.5f, -6);
                SpawnVacuumableObjectServerRPC(pos);
            }


            if (!NetworkManager.IsHost) return; //OnUpdate는 서버에서만 실행
            foreach (var obID in vacuumableObjects.Keys)
            {
                vacuumableObjects[obID].OnUpdate();
            }
            DespawnVacuumableObjects();
        }

        // 스폰된 VacuumableObject들 관리하는 딕셔너리
        private Dictionary<ulong, VacuumableObject> vacuumableObjects = new Dictionary<ulong, VacuumableObject>();

        [SerializeField] private GameObject tempObject; // 임시로 큐브모양 오브젝트 넣음

        [ServerRpc(RequireOwnership = false)]
        public void SpawnVacuumableObjectServerRPC(Vector3 pos)
        {
            GameObject go = Instantiate(tempObject, pos, Quaternion.identity);
            VacuumableObject vacuumOb = go.GetComponent<VacuumableObject>();
            NetworkObject networkOb = go.GetComponent<NetworkObject>();
            networkOb.Spawn();

            ulong tmpKey = networkOb.NetworkObjectId;
            vacuumableObjects.Add(tmpKey, vacuumOb);
        }

        public void DespawnVacuumableObjects() //key는 NetworkObjectId
        {
            foreach (var obKey in vacuumObjectDespawn)
            {
                vacuumableObjects[obKey].VacuumEnd();
                vacuumableObjects.Remove(obKey);
                NetworkObject no = NetworkObject.NetworkManager.SpawnManager.SpawnedObjects[obKey];
                no.Despawn(); //NetworkObjectId로 디스폰
                Destroy(no.gameObject);
                Debug.Log("Despawned!!");
            }
            vacuumObjectDespawn.Clear();
        }

        private List<ulong> vacuumObjectDespawn = new List<ulong>();
        [ServerRpc(RequireOwnership = false)]
        public void AddVacuumableObjectToDespawnListServerRPC(ulong obKey)
        {
            vacuumObjectDespawn.Add(obKey);
        }
    }
}