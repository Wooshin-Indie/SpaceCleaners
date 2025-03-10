using MPGame.Manager;
using System.Runtime.ConstrainedExecution;
using Unity.Netcode;
using UnityEngine;

public class VacuumableObject : MonoBehaviour
{
    private Collider objectCollider;
    [SerializeField] private float vacuumingForce;
    [SerializeField] private float vacuumingForceToCenter;
    public Collider ObjectCollider { get => objectCollider ; set => objectCollider = value; }
    private Rigidbody objectRigidbody;

    private ulong? objectOwner = null;
    private Vector3 targetPoint;
    private Vector3 cameraDirection;
    private bool isBeingVacuumed = false;

    private void Awake()
    {
        objectCollider = GetComponent<Collider>();
        objectRigidbody = GetComponent<Rigidbody>();
    }

    public void Init(ulong playerID, Vector3 target, Vector3 camDirection)
    {
        if (objectOwner == null)
            objectOwner = playerID;
        targetPoint = target;
        cameraDirection = camDirection;
        isBeingVacuumed = true;
        GetComponent<Renderer>().material.color = Color.green;
    }

    public void VacuumEnd()
    {
        objectOwner = null;
        isBeingVacuumed = false;
        targetPoint = Vector3.zero;
        cameraDirection = Vector3.zero;
        GetComponent<Renderer>().material.color = Color.red;
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
            ObjectSpawner.Instance.DespawnVacuumableObjectServerRPC(GetComponent<NetworkObject>());
        }
    }
}
