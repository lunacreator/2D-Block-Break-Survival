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

    private bool isBouncing = false;
    private float bounceCooldown = 0.1f;

    private Coroutine ownerHitCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        Vector3 initialVelocity = transform.forward * speed;
        rb.velocity = new Vector3(initialVelocity.x, 0f, initialVelocity.z);
        Destroy(gameObject, maxLifetime);

        ownerHitCoroutine = StartCoroutine(EnableOwnerHit());
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
        // (플레이어 충돌 최우선 검사)
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject == owner && canHitOwner == false)
            {
                return; // 0.1초간 주인 무시
            }
            GameManager.Instance.PlayerTookDamage(1);
            Destroy(gameObject);
            return;
        }

        // (★★★★★ 1. 핵심 수정: 무적 현상 즉시 해제 ★★★★★)
        // 만약 총알이 "플레이어가 아닌" 무언가(벽, 블록 등)에 부딪혔고,
        // 아직 0.1초 무적 시간(canHitOwner == false)이 끝나지 않았다면,
        if (canHitOwner == false)
        {
            // 0.1초 타이머(코루틴)를 '강제 중지'시킵니다.
            StopCoroutine(ownerHitCoroutine);

            // '즉시' 플레이어를 때릴 수 있도록 true로 변경합니다.
            canHitOwner = true;
        }

        // (★★★★★ 2. 이하는 이전과 동일한 벽/블록 쿨다운 로직 ★★★★★)

        // (isBouncing은 '방금 벽을 쳤다'는 뜻)
        if (isBouncing)
        {
            if (collision.gameObject.CompareTag("Block"))
            {
                isBouncing = false; // 블록 만나면 쿨다운 강제 해제
            }
            else if (collision.gameObject.CompareTag("Wall"))
            {
                return; // 지그재그 방지
            }
            else
            {
                isBouncing = false; // 기타 물체 만나도 일단 해제
            }
        }

        // (★★★★★ 3. 괄호 오류 수정 ★★★★★)
        // (발사 직후 '벽' 튕기기 전 주인 충돌 무시)
        if (collision.gameObject == owner && canHitOwner == false && hasBounced == false)
        {
            return; // (여기에 있던 잘못된 '}' 괄호를 삭제했습니다)
        }

        // (최대 바운스 횟수)
        if (bounceCount >= maxBounces)
        {
            Destroy(gameObject);
            return;
        }

        // (블록 데미지)
        if (collision.gameObject.CompareTag("Block"))
        {
            DestructibleBlock block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (block != null)
            {
                block.TakeDamage(1);
            }
        }

        // (공통 반사 로직)
        hasBounced = true;
        bounceCount++;
        ContactPoint contact = collision.contacts[0];
        Vector3 normal = contact.normal;

        Vector3 reflectDir = Vector3.Reflect(lastVelocity.normalized, normal);
        reflectDir.y = 0f;

        if (reflectDir == Vector3.zero)
        {
            reflectDir = -lastVelocity.normalized;
            reflectDir.y = 0f;
        }

        rb.velocity = reflectDir.normalized * speed;
        transform.rotation = Quaternion.LookRotation(reflectDir.normalized);

        // (오직 "Wall"에 부딪혔을 때만 쿨다운을 시작함)
        if (collision.gameObject.CompareTag("Wall"))
        {
            StartCoroutine(StartBounceCooldown());
        }
    }

    private IEnumerator StartBounceCooldown()
    {
        isBouncing = true;
        yield return new WaitForSeconds(bounceCooldown);
        isBouncing = false;
    }
}