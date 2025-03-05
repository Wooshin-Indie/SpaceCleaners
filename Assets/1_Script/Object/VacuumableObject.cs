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

        // 물리 시스템에서 제외 (직접 제어하기 위해)
        if (objectRigidbody != null)
        {
            objectRigidbody.isKinematic = true;
        }
    }

    public void VacuumEnd()
    {
        objectRigidbody.isKinematic = false; // 물리법칙 영향 다시 받도록 설정
        // 원래 가고있던 벡터로 이동하는 코드 추가해야함
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
