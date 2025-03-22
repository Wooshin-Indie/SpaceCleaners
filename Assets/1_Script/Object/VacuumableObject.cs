using MPGame.Controller;
using MPGame.Manager;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MPGame.Props
{
    public class VacuumableObject : OwnableProp
    {
        private Collider objectCollider;

        [SerializeField, Tooltip("Force to Vaccumable Object")]
        private float vacuumingForce = 1f;

        [SerializeField, Tooltip("Force Vaccumable Object to Center of OverlapCapsule")]
        private float vacuumingForceToCenter = 4f;
        public Collider ObjectCollider { get => objectCollider; set => objectCollider = value; }
        private Rigidbody objectRigidbody;

        private Vector3 targetPoint;
        private Vector3 cameraDirection;
        private bool isBeingVacuumed = false;

        protected override bool Interaction(ulong newOwnerClientId)
        {
            if (!base.Interaction(newOwnerClientId)) return false;
            return true;
        }

        private void Awake()
        {
            objectCollider = GetComponent<Collider>();
            objectRigidbody = GetComponent<Rigidbody>();
        }

		private void Update()
		{
            OnUpdate();
		}

		public void OnUpdate() // 서버에서만 실행됨 (싱크, AddForce, 오브젝트 가까워졌는지 감지)
        {
            if (!IsHost) return;
            if (!isBeingVacuumed) return;

            AddForceToTarget();
            if(DetectIsClosedToTarget())
            {
                // 여기에 vacuumend를 넣어줘야 오류가 안날라나?

                RemoveVacuumingObjectsFromHashsetsClientRPC(NetworkObjectId);
				// ownerClient의 PlayerController에서 Hashset에서 이 오브젝트를 삭제하라고 요청


				ObjectSpawner.Instance.AddVacuumableObjectToDespawnListServerRPC(NetworkObject.NetworkObjectId);
				GetComponent<NetworkObject>().Despawn(); //NetworkObjectId로 디스폰
				Destroy(gameObject);
            }
        }

        public void Init(ulong playerID, Vector3 target, Vector3 camDirection)
        {
            //OwnerClientId가 설정이 안돼있으면 서버에 권한 요청
            if (OwnerClientId.Value == ulong.MaxValue)
                TryInteract(); //serverRPC로 오브젝트 권한 요청 후 clientRPC로 바뀐 권한 뿌림

            if (Interaction(OwnerClientId.Value)) //ownerClientID가 '나'면 접근
            {
                Debug.Log("Interaction...");
                SetTargetServerRPC(target, camDirection);
                GetComponent<Renderer>().material.color = Color.green;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetTargetServerRPC(Vector3 target, Vector3 camDirection)
        {
            targetPoint = target;
            cameraDirection = camDirection;
            isBeingVacuumed = true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnsetTargetServerRPC()
        {
            isBeingVacuumed = false;
            targetPoint = Vector3.zero;
            cameraDirection = Vector3.zero;
        }

        [ClientRpc]
        private void RemoveVacuumingObjectsFromHashsetsClientRPC(ulong NetworkObjectId)
        {
            if (OwnerClientId.Value == NetworkManager.Singleton.LocalClientId)
            {
                NetworkManager.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>().RemoveVacuumingObjectsFromHashsets(this);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateObjectPositionServerRPC(Vector3 objectPosition)
        {
            UpdateObjectPositionClientRPC(objectPosition);
        }

        [ClientRpc]
        private void UpdateObjectPositionClientRPC(Vector3 objectPosition, bool fromServer = false)
        {
            // 일단 넣어놓긴 함, 아마 동작은 안할 듯
            if (!fromServer && IsOwner) return;

            transform.position = objectPosition;
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateObjectRotateServerRPC(Quaternion objectQuat)
        {
            UpdateObjectRotateClientRPC(objectQuat);
        }

        [ClientRpc]
        private void UpdateObjectRotateClientRPC(Quaternion objectQuat, bool fromServer = false)
        {
            if (!fromServer && IsOwner) return;
            transform.rotation = objectQuat;
        }

        public void VacuumEnd()
        {
            EndInteraction(); // 이 때 serverRPC로 ownerClientId를 초기화해서 clientRPC로 다시 뿌림
            UnsetTargetServerRPC();
            GetComponent<Renderer>().material.color = Color.red;
            Debug.Log("Interaction End");
        }

        private void AddForceToTarget()
        {
            Vector3 toObject = transform.position - targetPoint;
            Vector3 proj = Vector3.Project(toObject, cameraDirection);
            if (targetPoint == Vector3.zero) return;
            objectRigidbody.AddForce(-proj.normalized * vacuumingForce, ForceMode.Acceleration);
            objectRigidbody.AddForce((proj - toObject).normalized * vacuumingForceToCenter, ForceMode.Acceleration);

        }

        [SerializeField] private float removeDistance = 1f;
        private bool DetectIsClosedToTarget() //플레이어와 물체가 가까워졌는지 감지
        {
            Debug.Log("OwnerClientId: " + OwnerClientId.Value);
            if (Vector3.Distance(transform.position, targetPoint) < removeDistance) return true;
            else return false;
        }
    }
}
