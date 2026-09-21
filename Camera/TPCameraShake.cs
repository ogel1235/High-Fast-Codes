using UnityEngine;

public class TPCameraShake : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public PlayerController playerController;
    public PlayerAnimator playerAnimator;

    [Header("Rhythm Settings")]
    public float fullStrideDuration = 0.5275f; // 기본 속도일 때의 한 사이클 시간
    [Range(0f, 1f)] public float impactPercent = 0.33f; // 첫 발 닿는 타이밍

    [Header("Shake Intensity")]
    public float bobHeight = 0.15f;
    public float swayAngle = 2.0f;
    public float blendSpeed = 5f; // 부드러운 On/Off (10 -> 5 추천)
    public bool invertShake = false;

    [Header("Smoothing & Damping (New)")]
    [Tooltip("최종 카메라 위치를 부드럽게 갱신 (박자는 유지하되 움직임만 둥글게, 높을수록 빠릿함)")]
    public float positionSoftness = 20f;

    [Tooltip("속도가 빨라질 때 흔들림 폭을 줄이는 정도 (0.3 ~ 0.5 추천)")]
    [Range(0f, 1f)] public float highSpeedDamp = 0.3f;

    // 내부 변수
    private Vector3 startPos;
    private Quaternion startRot;

    // 진행된 위상(Phase) 저장 변수
    private float currentPhase = 0f;

    private float shakeIntensity = 0f;
    private float baseBobFrequency;
    private float startOffset;
    private bool wasShaking = false;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = transform;
        if (playerAnimator == null) playerAnimator = transform.root.GetComponent<PlayerAnimator>();

        startPos = cameraTransform.localPosition;
        startRot = cameraTransform.localRotation;

        // 주파수 계산 (기본 속도 기준)
        float oneStepTime = fullStrideDuration / 2f;
        baseBobFrequency = (2 * Mathf.PI) / oneStepTime;

        // 오프셋 계산 (33% 타이밍 맞추기)
        float targetPhase = 0.75f;
        float offsetPercent = targetPhase - impactPercent;
        startOffset = offsetPercent * (2 * Mathf.PI);
    }

    void LateUpdate()
    {
        if (playerController == null) return;

        bool isMoving = playerController.HorizontalInputDir.sqrMagnitude > 0.01f;
        bool isGrounded = playerController.isGrounded;
        bool shouldShake = isMoving && isGrounded;

        // 리셋 로직
        if (shouldShake && !wasShaking)
        {
            currentPhase = 0f; // 위상 초기화
        }
        wasShaking = shouldShake;

        // 강도 조절
        float targetIntensity = shouldShake ? 1f : 0f;
        shakeIntensity = Mathf.Lerp(shakeIntensity, targetIntensity, Time.deltaTime * blendSpeed);

        Vector3 targetPos = startPos;
        Quaternion targetRot = startRot;

        if (shakeIntensity > 0.01f)
        {
            // 1. PlayerAnimator에서 현재 속도 배율 가져오기
            float speedMult = 1f;
            if (playerAnimator != null)
            {
                // 프로퍼티 직접 참조 (가장 빠름)
                speedMult = playerAnimator.speedMultiplier;

                // 너무 느려져서 멈추는 것 방지 (최소 0.5배속 유지)
                speedMult = Mathf.Max(speedMult, 0.5f);
            }

            // 2. 위상 누적 (Phase Accumulation) - 박자 동기화
            // 즉시 반영된 배율을 사용하여 박자가 밀리지 않음
            currentPhase += Time.deltaTime * baseBobFrequency * speedMult;

            // 3. 고속 주행 시 진폭 감소 (High Speed Damping)
            // 배율이 1보다 클 때(빨리 달릴 때), 흔들림의 '폭'을 줄여줌
            // 1.0 / (1.0 + (초과배율 * 감쇠력)) 공식을 사용하여 부드럽게 줄임
            float dampFactor = 1f / (1f + (Mathf.Max(0, speedMult - 1f) * highSpeedDamp));

            // 감쇠된 높이와 각도 계산
            float finalBobHeight = bobHeight * dampFactor;
            float finalSwayAngle = swayAngle * dampFactor;

            // 4. 최종 계산
            float finalCycle = currentPhase + startOffset;

            float bobY = Mathf.Sin(finalCycle) * finalBobHeight * shakeIntensity;

            float swayVal = Mathf.Sin(finalCycle * 0.5f);
            if (invertShake) swayVal *= -1f;
            float swayZ = swayVal * finalSwayAngle * shakeIntensity;

            targetPos = startPos + new Vector3(0, bobY, 0);
            targetRot = startRot * Quaternion.Euler(0, 0, swayZ);
        }

        // 5. 최종 위치 보간 (Softness) - 움직임 부드럽게 만들기
        // 바로 대입(=)하지 않고 Lerp를 사용하여 기계적인 덜덜거림을 없앰
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetPos, Time.deltaTime * positionSoftness);
        cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetRot, Time.deltaTime * positionSoftness);
    }
}