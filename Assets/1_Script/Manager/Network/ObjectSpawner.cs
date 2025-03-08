using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

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
    }
}