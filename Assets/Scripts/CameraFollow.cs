using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("Collision Handling")]
    public LayerMask wallLayer;
    public float collisionBuffer = 0.2f;

    void Start()
    {
        // Start에서는 초기화하지 않고 GameManager의 명령을 기다림
    }

    // GameManager가 호출할 초기화 함수
    public void InitializeTarget(Transform newTarget)
    {
        target = newTarget;
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 rayStartPoint = target.position + Vector3.up * 0.5f;
        Vector3 rayDirection = (desiredPosition - rayStartPoint).normalized;
        float rayDistance = Vector3.Distance(rayStartPoint, desiredPosition);
        Vector3 finalPosition;
        RaycastHit hit;

        if (Physics.Raycast(rayStartPoint, rayDirection, out hit, rayDistance, wallLayer))
        {
            finalPosition = hit.point - rayDirection * collisionBuffer;
        }
        else
        {
            finalPosition = desiredPosition;
        }

        transform.position = Vector3.Lerp(transform.position, finalPosition, smoothSpeed);
    }
}