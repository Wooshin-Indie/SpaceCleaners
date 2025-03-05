using UnityEngine;

public class VacuumableObject : MonoBehaviour
{
    private Collider objectCollider;
    public Collider ObjectCollider { get => objectCollider ; set => objectCollider = value; }
    private Rigidbody objectRigidbody;

    private Transform targetPoint;

    private bool isInitialized = false;

    public void Init(Transform target)
    {
        objectCollider = GetComponent<Collider>();
        objectRigidbody = GetComponent<Rigidbody>();
        targetPoint = target;
        isInitialized = true;

        // ���� �ý��ۿ��� ���� (���� �����ϱ� ����)
        if (objectRigidbody != null)
        {
            objectRigidbody.isKinematic = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isInitialized) return;
        else
        {
            MoveTowardTarget();
        }
    }



    private void MoveTowardTarget()
    {
        if (targetPoint == null) return;
        transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, 0.1f);
    }
}
