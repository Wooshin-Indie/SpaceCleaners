using MPGame.Controller;
using MPGame.Manager;
using System.Runtime.ConstrainedExecution;
using Unity.Netcode;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace MPGame.Props
{
    public class VacuumableObject : PropsBase
    {
        private Collider objectCollider;
        [SerializeField] private float vacuumingForce;
        [SerializeField] private float vacuumingForceToCenter;
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

        public void OnUpdate() // ���������� �����
        {
            UpdateObjectPositionServerRPC(transform.position);
            UpdateObjectRotateServerRPC(transform.rotation);

            if (!isBeingVacuumed) return;
            
            AddForceToTarget();
            if(DetectIsClosedToTarget())
            {
                // ���⿡ vacuumend�� �־���� ������ �ȳ���?

                RemoveVacuumingObjectsFromHashsetsClientRPC(NetworkObjectId);
                // ownerClient�� PlayerController���� Hashset���� �� ������Ʈ�� �����϶�� ��û

                ObjectSpawner.Instance.DespawnVacuumableObjectServerRPC(NetworkObject.NetworkObjectId); // TODO - NetworkObject ġ�� GetComponentó�� �Ǵ°� �³�?
                //Ȥ�� ���� serverRPC�� �س���
                Debug.Log("Test1!!");
            }
        }

        public void Init(ulong playerID, Vector3 target, Vector3 camDirection)
        {
            //OwnerClientId�� ������ �ȵ������� ������ ���� ��û
            if (OwnerClientId.Value == ulong.MaxValue)
                TryInteract(); //serverRPC�� ������Ʈ ���� ��û �� clientRPC�� �ٲ� ���� �Ѹ�

            if (Interaction(OwnerClientId.Value)) //ownerClientID�� '��'�� ����
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
            // �ϴ� �־���� ��, �Ƹ� ������ ���� ��
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
            EndInteraction(); // �� �� serverRPC�� ownerClientId�� �ʱ�ȭ�ؼ� clientRPC�� �ٽ� �Ѹ�
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

        [SerializeField] private float removeDistance;
        private bool DetectIsClosedToTarget()
        {
            Debug.Log("OwnerClientId: " + OwnerClientId.Value);
            if (Vector3.Distance(transform.position, targetPoint) < removeDistance) return true;
            else return false;
        }
    }
}
