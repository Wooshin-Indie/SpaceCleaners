using MPGame.Controller;
using MPGame.Manager;
using System.Runtime.ConstrainedExecution;
using Unity.Netcode;
using UnityEngine;

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

        private void OnUpdate()
        {
            if (!objectRigidbody.isKinematic)
                UpdateObjectPositionServerRPC(transform.position);
            UpdateObjectRotateServerRPC(transform.rotation);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateObjectPositionServerRPC(Vector3 objectPosition)
        {
            UpdateObjectPositionClientRPC(objectPosition);
        }

        [ClientRpc]
        private void UpdateObjectPositionClientRPC(Vector3 objectPosition, bool fromServer = false)
        {
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

        public void Init(ulong playerID, Vector3 target, Vector3 camDirection)
        {
            if (OwnerClientId.Value == ulong.MaxValue)
                TryInteract();

            if (Interaction(OwnerClientId.Value))
            {
                Debug.Log("Interaction...");
                targetPoint = target;
                cameraDirection = camDirection;
                isBeingVacuumed = true;
                GetComponent<Renderer>().material.color = Color.green;
            }
        }

        public void VacuumEnd()
        {
            isBeingVacuumed = false;
            targetPoint = Vector3.zero;
            cameraDirection = Vector3.zero;
            GetComponent<Renderer>().material.color = Color.red;
            EndInteraction();
            Debug.Log("Interaction End");
        }

        private void Update()
        {
            if (!isBeingVacuumed) return;
            else
            {
                MoveTowardTarget();
                DestroyWhenClosedToPlayer();
            }
        }

        private void MoveTowardTarget()
        {
            Vector3 toObject = transform.position - targetPoint;
            Vector3 proj = Vector3.Project(toObject, cameraDirection);
            if (targetPoint == null) return;
            objectRigidbody.AddForce(-proj.normalized * vacuumingForce, ForceMode.Acceleration);
            objectRigidbody.AddForce((proj - toObject).normalized * vacuumingForceToCenter, ForceMode.Acceleration);

        }

        [SerializeField] private float removeDistance;
        private void DestroyWhenClosedToPlayer()
        {
            if (Vector3.Distance(transform.position, targetPoint) < removeDistance)
            {
                PlayerSpawner.Instance.Players[OwnerClientId.Value].
                    GetComponent<PlayerController>().RemoveVacuumingObjectsFromHashsets(this);
                ObjectSpawner.Instance.RequestDespawnVacuumableObjectToServer(GetComponent<VacuumableObject>());
                Debug.Log("Test1!!");
            }
        }
    }
}
