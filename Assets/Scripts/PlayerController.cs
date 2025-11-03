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
        moveInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

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
            // (★★★★★ 1. 핵심 수정: 데드존(Deadzone) 추가 ★★★★★)
            // 입력 벡터의 크기가 0.1f (임의의 작은 값)보다 작으면
            // 드리프트로 간주하고 속도를 0으로 고정합니다.
            if (moveInput.magnitude < 0.1f)
            {
                rb.velocity = new Vector3(0f, 0f, 0f);
            }
            else // 0.1f 이상일 때만 정상적으로 움직입니다.
            {
                Vector3 camForward = Vector3.Scale(mainCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
                Vector3 camRight = Vector3.Scale(mainCamera.transform.right, new Vector3(1, 0, 1)).normalized;
                Vector3 moveDirection = (camForward * moveInput.z + camRight * moveInput.x).normalized;
                rb.velocity = new Vector3(moveDirection.x * moveSpeed, 0f, moveDirection.z * moveSpeed);
            }
        }

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