using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BouncingBlaster : MonoBehaviour
{
    public float speed = 100f;
    public int maxBounces = 1000;
    public float maxLifetime = 20f;
    public GameObject owner;
    private bool canHitOwner = false;
    private Rigidbody rb;
    private int bounceCount = 0;
    private Vector3 lastVelocity;
    private bool hasBounced = false;

    // (★★★★★ 1. 쿨다운 변수 복구 ★★★★★)
    private bool isBouncing = false;
    private float bounceCooldown = 0.1f; // 0.1초간 연속 반사 금지

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // (적용된 수정 1) 중력 비활성화
        rb.useGravity = false;

        Vector3 initialVelocity = transform.forward * speed;
        rb.velocity = new Vector3(initialVelocity.x, 0f, initialVelocity.z);
        Destroy(gameObject, maxLifetime);
        StartCoroutine(EnableOwnerHit());
    }

    void FixedUpdate()
    {
        lastVelocity = rb.velocity;
    }

    void Update() { }

    private IEnumerator EnableOwnerHit()
    {
        yield return new WaitForSeconds(0.1f);
        canHitOwner = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // (★★★★★ 2. 쿨다운 체크 복구 ★★★★★)
        // 현재 튕기는 중(isBouncing)이라면, 어떤 충돌도 무시하고 즉시 함수 종료
        if (isBouncing) return;

        if (collision.gameObject == owner && canHitOwner == false && hasBounced == false)
        {
            return;
        }

        if (bounceCount >= maxBounces)
        {
            Destroy(gameObject);
            return;
        }

        // (적용된 수정 2: "Player" 태그만 먼저 확인)
        if (collision.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.PlayerTookDamage(1);
            Destroy(gameObject);
            return;
        }

        // (적용된 수정 3: "Block" 태그 처리)
        if (collision.gameObject.CompareTag("Block"))
        {
            DestructibleBlock block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (block != null)
            {
                block.TakeDamage(1);
            }
        }

        // (적용된 수정 4: "Player"가 아닌 '모든 것'에 튕기기)

        hasBounced = true;
        bounceCount++;
        ContactPoint contact = collision.contacts[0];
        Vector3 normal = contact.normal;

        Vector3 reflectDir = Vector3.Reflect(lastVelocity.normalized, normal);

        // (적용된 수정 5: Y축 고정)
        reflectDir.y = 0f;

        if (reflectDir == Vector3.zero)
        {
            reflectDir = -lastVelocity.normalized;
            reflectDir.y = 0f;
        }

        rb.velocity = reflectDir.normalized * speed;
        transform.rotation = Quaternion.LookRotation(reflectDir.normalized);

        // (★★★★★ 3. 쿨다운 코루틴 호출 복구 ★★★★★)
        StartCoroutine(StartBounceCooldown());
    }

    // (★★★★★ 4. 쿨다운 코루틴 함수 복구 ★★★★★)
    private IEnumerator StartBounceCooldown()
    {
        isBouncing = true; // 튕기는 중 ON
        yield return new WaitForSeconds(bounceCooldown); // 0.1초 대기
        isBouncing = false; // 튕기는 중 OFF
    }
}