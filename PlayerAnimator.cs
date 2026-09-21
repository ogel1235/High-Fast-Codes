using Unity.Mathematics;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    private PlayerController playerController;
    private PlayerStats playerStats;

    [Header("Smoothing Settings")]
    [Tooltip("값이 튀는 것을 방지하는 부드러운 보간 시간 (0.1 ~ 0.2 추천)")]
    [SerializeField] private float smoothTime = 0.15f;

    // 내부 변수
    private Vector3 horizontalInputDir;
    private Vector3 currentSmoothedVelocity; // 부드럽게 보정된 속도 (X, Z)
    private Vector3 velocitySmoothingRef;    // SmoothDamp용 참조 변수
    private bool isGrounded;
    private bool isMoving;
    public float speedMultiplier { get; private set; }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerStats = GetComponent<PlayerStats>();

        // 만약 animator가 인스펙터에서 할당 안 되었을 경우 자동 찾기
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        //HandleMovementAnimation();
        HandleStateAnimation();
    }

    private void HandleMovementAnimation()
    {
        // 1. 목표 속도 가져오기 (물리 엔진이라 값이 미세하게 튀는 상태)
        //Vector3 targetVelocity = playerController.isMoveAble ? playerController.localVelocity : Vector3.zero;
        horizontalInputDir = playerController.isMoveAble ? playerController.HorizontalInputDir : Vector3.zero;

        // 2. ★ 핵심: SmoothDamp로 노이즈 제거 ★
        // 물리 엔진의 '튀는 값'을 부드러운 곡선으로 만들어줍니다.
        // X와 Z를 동시에 보정합니다.
        //currentSmoothedVelocity = Vector3.SmoothDamp(
        //    currentSmoothedVelocity,
        //    targetVelocity,
        //    ref velocitySmoothingRef,
        //    smoothTime
        //);

        //// 정지 시 미세한 떨림 방지 (0에 가까우면 0으로 고정)
        //if (currentSmoothedVelocity.magnitude < 0.01f) currentSmoothedVelocity = Vector3.zero;

        // 3. 애니메이터에 전달 (이미 보정되었으므로 dampTime 없이 바로 넣어도 부드러움)
        

        // 디버깅용 (필요 없으면 주석 처리)
        // Debug.Log($"Raw: {targetVelocity.z:F2} / Smoothed: {currentSmoothedVelocity.z:F2}");
    }

    private void HandleStateAnimation()
    {
        horizontalInputDir = playerController.isMoveAble ? playerController.HorizontalInputDir : Vector3.zero;
        isGrounded = playerController.isGrounded;

        // 입력이 있거나 속도가 어느 정도 있으면 움직이는 것으로 간주
        isMoving = horizontalInputDir != Vector3.zero;

        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsMoveable", playerController.isMoveAble);

        // 이동 속도 배율 (기본 속도 대비 현재 속도 비율)
        // 여기서는 물리 속도가 아닌 설정된 스탯 기준으로 하는 것이 애니메이션 재생 속도 조절에 안정적일 수 있음
        // 만약 발이 밀린다면 currentSmoothedVelocity.magnitude를 사용해도 됨
        speedMultiplier = playerStats.currentSpeed / playerStats.baseSpeed;
        animator.SetFloat("MoveSpeedMultiplier", speedMultiplier);
        animator.SetFloat("MoveX", horizontalInputDir.x);
        animator.SetFloat("MoveZ", horizontalInputDir.z);
    }

    // ★ 카메라 흔들림 스크립트에서 호출할 함수 ★
    // 카메라 스크립트에서: playerAnimator.GetSmoothedSpeed()를 호출해서 쓰면
    // 애니메이션과 카메라가 완벽하게 동기화됨.
    public float GetSmoothedSpeed()
    {
        // 2D 평면(X, Z)에서의 속력 반환
        return new Vector3(currentSmoothedVelocity.x, 0, currentSmoothedVelocity.z).magnitude;
    }

    public void OnShoot()
    {
        animator.Play("Fire", 1, 0);
    }

    public void OnDamaged()
    {
        animator.CrossFade("Damage", 0.15f, 2);
    }

    public void ClimbHigh()
    {
        animator.CrossFade("Climb_High", 0.15f, 0);
    }

    public void ClimbLow()
    {
        animator.CrossFade("Climb_Low", 0.15f, 0);
    }
}