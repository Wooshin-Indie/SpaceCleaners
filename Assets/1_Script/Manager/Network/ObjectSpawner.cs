using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using MPGame.Props;

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

            foreach (var ob in vacuumableObjects)
            {
                ob.OnUpdate();
            }
        }

        private List<VacuumableObject> vacuumableObjects = new List<VacuumableObject>();
        [SerializeField] private GameObject tempObject;

        [ServerRpc(RequireOwnership = false)]
        public void SpawnVacuumableObjectServerRPC(Vector3 pos)
        {
            GameObject ob = Instantiate(tempObject, pos, Quaternion.identity);
            ob.GetComponent<NetworkObject>().Spawn();
            vacuumableObjects.Add(ob.GetComponent<VacuumableObject>());
        }

        [ServerRpc(RequireOwnership = false)]
        public void DespawnVacuumableObjectServerRPC()
        {
            vacuumableObjects.Remove(tmpOb);
            tmpOb.GetComponent<NetworkObject>().Despawn();
            Destroy(tmpOb.gameObject);
            Debug.Log("Despawned!!");
        }

        private VacuumableObject tmpOb;
        public void RequestDespawnVacuumableObjectToServer(VacuumableObject ob)
        {
            tmpOb = ob;
            DespawnVacuumableObjectServerRPC();
        }
    }
}