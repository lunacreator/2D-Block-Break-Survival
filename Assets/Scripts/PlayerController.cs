using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 12f;
    private Rigidbody rb;
    private Vector3 moveInput;
    private Camera mainCamera;

    [Header("Dash")]
    public float dashSpeed = 10f;
    public float dashDuration = 1f;
    public float dashCooldown = 4f;
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;

    [Header("Audio")]
    public AudioSource playerAudioSource;
    public AudioClip dashSoundClip;

    [Header("VFX")]
    public ParticleSystem dashParticleSystem;

    // (★추가★) public 함수: 현재 대시 중인지 반환
    public bool IsDashing()
    {
        return isDashing;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (Camera.main != null) { mainCamera = Camera.main; }
        if (playerAudioSource == null) { playerAudioSource = GetComponent<AudioSource>(); }

        if (dashParticleSystem != null)
        {
            dashParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void Update()
    {
        // (★★★★★ 1. 핵심 수정: 'GetAxis' -> 'GetAxisRaw'로 변경 ★★★★★)
        // GetAxisRaw는 -1, 0, 1만 반환하며 중간값이 없어 쏠림(Drift)이 원천 차단됩니다.
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // (★★★★★ 2. 데드존 로직 '삭제' ★★★★★)
        // GetAxisRaw를 쓰면 데드존 로직(0.1f 체크)이 더 이상 필요 없습니다.
        /*
        float deadzone = 0.1f;
        if (Mathf.Abs(horizontalInput) < deadzone)
        {
            horizontalInput = 0f;
        }
        if (Mathf.Abs(verticalInput) < deadzone)
        {
            verticalInput = 0f;
        }
        */

        // 3. '깨끗해진' 입력값으로 moveInput을 설정합니다.
        moveInput = new Vector3(horizontalInput, 0f, verticalInput);

        // (이하는 동일한 대시 로직)
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && dashCooldownTimer <= 0f)
        {
            StartCoroutine(Dash());
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (mainCamera == null || rb == null) return;

        if (!isDashing)
        {
            // --- (★★★★★ 1. 이 부분이 통째로 수정되었습니다 ★★★★★) ---

            // 1. 카메라 방향을 기준으로 목표 '방향'을 계산합니다.
            Vector3 camForward = Vector3.Scale(mainCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(mainCamera.transform.right, new Vector3(1, 0, 1)).normalized;
            Vector3 moveDirection = (camForward * moveInput.z + camRight * moveInput.x).normalized;

            // 2. '목표 속도' (targetVelocity)를 계산합니다.
            Vector3 targetVelocity = new Vector3(moveDirection.x * moveSpeed, 0f, moveDirection.z * moveSpeed);

            // 3. 현재 속도(rb.velocity)와 목표 속도(targetVelocity)의 '차이'를 계산합니다.
            Vector3 velocityChange = (targetVelocity - rb.velocity);

            // 4. Y축 속도는 변경하지 않습니다. (중력 등에 영향 주지 않기 위함)
            velocityChange.y = 0f;

            // 5. 계산된 '속도 차이'만큼 '힘'을 가해 목표 속도로 만듭니다.
            // (ForceMode.VelocityChange는 질량을 무시하고 즉각적인 속도 변화를 줍니다)
            rb.AddForce(velocityChange, ForceMode.VelocityChange);

            // --- (rb.velocity = ... 코드는 삭제되었습니다) ---
        }

        // 마우스 바라보기는 그대로 둡니다.
        LookAtMouse();
    }

    void LookAtMouse()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            Vector3 lookDirection = point - transform.position;
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                rb.MoveRotation(targetRotation);
            }
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        rb.velocity = Vector3.zero;

        if (playerAudioSource != null && dashSoundClip != null) { playerAudioSource.PlayOneShot(dashSoundClip); }
        if (dashParticleSystem != null)
        {
            dashParticleSystem.Play();
        }

        Vector3 dashDirection = transform.forward;
        if (mainCamera != null)
        {
            Vector3 camForward = Vector3.Scale(mainCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(mainCamera.transform.right, new Vector3(1, 0, 1)).normalized;
            dashDirection = (camForward * moveInput.z + camRight * moveInput.x).normalized;
            if (dashDirection == Vector3.zero) dashDirection = transform.forward;
        }

        float distance = (dashSpeed * dashDuration) + 10f;
        RaycastHit hit;

        // (★되돌림★) 대시 중 "Wall" 또는 "Block"에 박으면 즉사
        if (rb.SweepTest(dashDirection, out hit, distance))
        {
            if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Block"))
            {
                GameManager.Instance.PlayerInstantDeath();
                isDashing = false;
                yield break;
            }
            distance = hit.distance - 0.1f;
        }

        Vector3 targetPosition = rb.position + (dashDirection * distance);
        rb.MovePosition(targetPosition);

        yield return new WaitForFixedUpdate();

        isDashing = false;
    }

    // (★되돌림★) 일반 이동 중 "Wall" 또는 "Block"에 닿으면 즉사
    private void OnCollisionEnter(Collision collision)
    {
        // 대시 중에는 모든 물리 충돌을 무시
        if (isDashing) return;

        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Block"))
        {
            GameManager.Instance.PlayerInstantDeath();
        }
    }
}