using System.Collections.Generic;
using Unity.Netcode;
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


            if (!NetworkManager.IsHost) return; //OnUpdate는 서버에서만 실행
            //foreach (var obID in vacuumableObjects.Keys)
            //{
             //   vacuumableObjects[obID].OnUpdate();
            //}
            //DespawnVacuumableObjects();
        }

        // 스폰된 VacuumableObject들 관리하는 딕셔너리
        private Dictionary<ulong, VacuumableObject> vacuumableObjects = new Dictionary<ulong, VacuumableObject>();

        [SerializeField] private GameObject tempObject; // 임시로 큐브모양 오브젝트 넣음

        [ServerRpc(RequireOwnership = false)]
        public void SpawnVacuumableObjectServerRPC(Vector3 pos)
        {
            GameObject go = Instantiate(trashPrefab, pos, Random.rotation);
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



        [SerializeField] private GameObject trashPrefab;
        [SerializeField] private int numberOfTrash;	// 한 그룹 당 생성될 쓰레기 개수 가중치
        [SerializeField] private int numberOfGroup;	// 생성될 쓰레기 그룹 개수
        [SerializeField] private Vector3 minGroupArea;	// 한 그룹 영역 최소 크기
        [SerializeField] private Vector3 maxGroupArea;	// 한 그룹 영역 최대 크기
        private Vector3 aGroupArea;	// 한 그룹 영역 크기
        [SerializeField] private Vector3 spawnRange;	// 우주에서 쓰레기무리가 생성될 수 있는 직육면체 범위의 x,y,z 각각 길이 값
        [SerializeField] private Vector3 centerPoint;	// "쓰레기무리들"의 생성 중심 (지금은 태양)
        private Vector3 spawnPoint;	// "쓰레기무리하나"가 생성되는 포인트의 중심
        [SerializeField] private float objectRadius;	// 오브젝트의 충돌 검사에 사용할 반지름
        [SerializeField] private int maxAttempts;	// 각 오브젝트마다 시도할 최대 횟수

        public void SpawnTrashArea() // 우주에 떠다니는 쓰레기 더미 생성하는 함수
        {
            int groupCount = 0;
            while (groupCount < numberOfGroup)
            {
                aGroupArea = new Vector3(
                    Random.Range(minGroupArea.x, maxGroupArea.x),
                    Random.Range(minGroupArea.y, maxGroupArea.y),
                    Random.Range(minGroupArea.z, maxGroupArea.z));

                spawnPoint = centerPoint + new Vector3(
                    Random.Range(-spawnRange.x / 2, spawnRange.x / 2),
                    Random.Range(-spawnRange.y / 2, spawnRange.y / 2),
                    Random.Range(-spawnRange.z / 2, spawnRange.z / 2)); // 스폰예정 위치

                if (IsValidArea(spawnPoint, aGroupArea)) // 스폰예정 범위 안에 오브젝트가 존재하는지 검사
                {
                    groupCount++;
                    int adjustedNumberOfTrash = numberOfTrash *
                        (int)(aGroupArea.x / minGroupArea.x * aGroupArea.y / minGroupArea.y * aGroupArea.z / minGroupArea.z);
                    // 범위에 따라 쓰레기 개수 조정

                    int count = 0;
                    int attempts = 0;
                    while (count < adjustedNumberOfTrash && attempts < numberOfTrash * maxAttempts)
                    {
                        //Vector3 randomPos = new Vector3(
                        //    Random.Range(-aGroupArea.x / 2, aGroupArea.x / 2),
                        //    Random.Range(-aGroupArea.y / 2, aGroupArea.y / 2),
                        //    Random.Range(-aGroupArea.z / 2, aGroupArea.z / 2)
                        //);

                        Vector3 randSphere = Random.insideUnitSphere; // (1,1,1)구 내에서 랜덤지점 반환

                        Vector3 randomPos = new Vector3(
                            randSphere.x * aGroupArea.x,
                            randSphere.y * aGroupArea.y,
                            randSphere.z * aGroupArea.z
                            ); // 타원체 안 지점으로 보정

                        randomPos += spawnPoint; // 스폰중심 포인트 기준으로 생성위치 조정

                        // 해당 위치가 겹치지 않는지 확인
                        if (IsValidPosition(randomPos))
                        {
                            SpawnVacuumableObject(randomPos);
                            count++;
                        }
                        attempts++;
                    }
                    count = 0;
                    attempts = 0;
                }
            }

        }

        private bool IsValidArea(Vector3 point, Vector3 area)
        {
            Collider[] hitColliders = UnityEngine.Physics.OverlapBox(point, area / 2);
            return hitColliders.Length == 0;
        }

        private bool IsValidPosition(Vector3 point)
        {
            Collider[] hitColliders = UnityEngine.Physics.OverlapBox(point, objectRadius * Vector3.one);
            return hitColliders.Length == 0;
        }

        public void SpawnVacuumableObject(Vector3 pos)
        {
            GameObject go = Instantiate(trashPrefab, pos, Random.rotation);
            VacuumableObject vacuumOb = go.GetComponent<VacuumableObject>();
            NetworkObject networkOb = go.GetComponent<NetworkObject>();
            networkOb.Spawn();

            ulong tmpKey = networkOb.NetworkObjectId;
            vacuumableObjects.Add(tmpKey, vacuumOb);
        }
    }
}