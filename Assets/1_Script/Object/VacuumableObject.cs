using System.Runtime.ConstrainedExecution;
using UnityEngine;

public class VacuumableObject : MonoBehaviour
{
    private Collider objectCollider;
    [SerializeField] private float vacuumingForce;
    [SerializeField] private float vacuumingForceToCenter;
    public Collider ObjectCollider { get => objectCollider ; set => objectCollider = value; }
    private Rigidbody objectRigidbody;

    private Vector3 targetPoint;
    private Vector3 cameraDirection;
    private bool isVacuumed = false;

    private void Awake()
    {
        objectCollider = GetComponent<Collider>();
        objectRigidbody = GetComponent<Rigidbody>();
    }

    public void Init(Vector3 target, Vector3 camDirection)
    {
        targetPoint = target;
        cameraDirection = camDirection;
        isVacuumed = true;
        GetComponent<Renderer>().material.color = Color.green;
    }

    public void VacuumEnd()
    {
        isVacuumed = false;
        targetPoint = Vector3.zero;
        cameraDirection = Vector3.zero;
        GetComponent<Renderer>().material.color = Color.red;
    }

    private void Update()
    {
        if (!isVacuumed) return;
        else
        {
            MoveTowardTarget();
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
}
