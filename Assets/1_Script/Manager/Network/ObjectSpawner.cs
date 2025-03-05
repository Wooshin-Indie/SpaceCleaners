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

        private List<VacuumableObject> vacuumableObjects = new List<VacuumableObject>();
        [SerializeField] private GameObject tempObject;

        [ServerRpc(RequireOwnership = false)]
        public void SpawnVacuumableObjectServerRPC(Vector3 pos)
        {
            GameObject ob = Instantiate(tempObject, pos, Quaternion.identity);
            NetworkObject no = ob.GetComponent<NetworkObject>();

            no.Spawn();

        }
    }
}