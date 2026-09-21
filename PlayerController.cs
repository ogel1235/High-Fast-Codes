using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private GameController gameController;

    private PlayerStats playerStats;
    private PlayerAnimator playerAnimator;
    private CameraController cameraController;
    private PlayerIKController playerIKController;

    [SerializeField]
    private float movePower;
    [SerializeField]
    private float airMovePower;
    [SerializeField]
    private float jumpPower;
    [SerializeField]
    private float airJumpPower;
    [SerializeField]
    private int airJumpCount;
    public bool airJumpEnable = false;
    public int AirJumpCount => airJumpCount;
    private int remainingAirJumpCount;
    public int RemainingAirJumpCount { get => remainingAirJumpCount; set => remainingAirJumpCount = value; }
    public bool isGrounded;
    [SerializeField]
    private float jumpBufferDuration = 0.2f;
    private float jumpBufferTimer;

    [SerializeField]
    private float dashPower;
    [SerializeField]
    private float dashCooldown = 1f;
    private float dashCooldownTime;
    [SerializeField]
    private float dashAirborneTime;
    public bool dashEnable = false;

    private float inputX;
    private float inputZ;
    private Vector3 inputDir;
    private Vector3 horizontalInputDir;
    public Vector3 HorizontalInputDir => horizontalInputDir;
    public Vector3 InputDir => inputDir;
    private Vector3 moveVector;
    private Vector3 worldMoveVector;

    private new Rigidbody rigidbody;
    private Collider[] playerColliders;

    public Vector3 currentSpeedVector;
    
    public float horizontalSpeed = 0;
    Vector3 worldVelocity;
    public Vector3 localVelocity { get; private set; }

    Quaternion targetRotation;
    Quaternion smoothedRotation;

    private bool devMode;
    public bool DevMode => devMode;
    [SerializeField]
    private float devModeMoveSpeed = 20;
    private int inputY;

    [HideInInspector]
    public bool isMoveAble;
    [HideInInspector]
    public Vector3 targetPosition = Vector3.zero;

    [SerializeField]
    private Transform cameraRig;

    [HideInInspector]
    public bool isClimbing = false;
    RaycastHit hitInfoFront;
    RaycastHit hitInfoDown;

    [Header("Raycast 설정")]
    public LayerMask groundLayer;
    public float frontCheckDistance = 1f;
    public float downCheckDistance = 1f;
    public float yOffsetLow = 0.2f;
    public float yOffsetHigh = 1.2f;
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        devMode = false;
        rigidbody = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>();
        playerAnimator = GetComponent<PlayerAnimator>();
        cameraController = GetComponent<CameraController>();
        isGrounded = true;
        isMoveAble = true;
    }

    void Update()
    {
        if (gameController.isPaused) return;

        if (dashCooldownTime > 0) dashCooldownTime -= Time.deltaTime;
        else dashCooldownTime = 0;

        inputX = Input.GetAxis("Horizontal");
        inputZ = Input.GetAxis("Vertical");

        inputDir = new Vector3(inputX, inputY, inputZ).normalized;
        horizontalInputDir = Vector3.zero;

        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (devMode)
        {
            if (Input.GetKey(KeyCode.Space)) inputY = 1;
            else if (Input.GetKey(KeyCode.LeftControl)) inputY = -1;
            else inputY = 0;

            if (inputDir != Vector3.zero)
            {
                transform.Translate(inputDir * devModeMoveSpeed * Time.deltaTime);
            }

            transform.rotation = Quaternion.Euler(0, cameraRig.eulerAngles.y, 0);
        }
        else if (isMoveAble)
        {
            horizontalInputDir = new Vector3(inputX, 0, inputZ).normalized;

            if(!isGrounded && rigidbody.linearVelocity.y < 0)
            {
                if(!Physics.Raycast(transform.position + Vector3.up * yOffsetHigh, transform.forward, out hitInfoFront, frontCheckDistance, groundLayer))
                {
                    if (Physics.Raycast(transform.position + Vector3.up * yOffsetHigh + transform.forward * frontCheckDistance, Vector3.down, out hitInfoDown, downCheckDistance, groundLayer))
                    {
                        StartCoroutine(ClimbHigh(hitInfoDown.point));
                    }
                }
                if (!Physics.Raycast(transform.position + Vector3.up * yOffsetLow, transform.forward, out hitInfoFront, frontCheckDistance, groundLayer))
                {
                    if (Physics.Raycast(transform.position + Vector3.up * yOffsetLow + transform.forward * frontCheckDistance, Vector3.down, out hitInfoDown, downCheckDistance, groundLayer))
                    {
                        StartCoroutine(ClimbLow(hitInfoDown.point));
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                jumpBufferTimer = jumpBufferDuration;
            }
            if (Input.GetKeyDown(KeyCode.V) && horizontalInputDir != Vector3.zero && dashEnable && dashCooldownTime <= 0)
            {
                Dash();
                dashCooldownTime = dashCooldown;
            }

            if (jumpBufferTimer > 0 && isGrounded)
            {
                GroundJump();
                jumpBufferTimer = 0f;
            }
            else if (jumpBufferTimer > 0 && !isGrounded && remainingAirJumpCount > 0 && airJumpEnable)
            {
                AirJump();
                jumpBufferTimer = 0f;
            }

            //transform.rotation = Quaternion.Euler(0, cameraRig.eulerAngles.y, 0);
            //rigidbody.MoveRotation(Quaternion.Euler(0, cameraRig.eulerAngles.y, 0));
            targetRotation = Quaternion.Euler(0, cameraRig.eulerAngles.y, 0);
            smoothedRotation = Quaternion.Slerp(rigidbody.rotation, targetRotation, 20f * Time.deltaTime);
            rigidbody.MoveRotation(smoothedRotation);

            worldVelocity = rigidbody.linearVelocity;
            localVelocity = Quaternion.Inverse(Quaternion.Euler(0, cameraRig.eulerAngles.y, 0)) * worldVelocity;

            if ((horizontalInputDir.x > 0 && localVelocity.x < horizontalInputDir.x * playerStats.currentSpeed) || (horizontalInputDir.x < 0 && localVelocity.x > horizontalInputDir.x * playerStats.currentSpeed))
            {
                moveVector.x = horizontalInputDir.x;
            }
            else
            {
                moveVector.x = 0;
            }

            if ((horizontalInputDir.z > 0 && localVelocity.z < horizontalInputDir.z * playerStats.currentSpeed) || (horizontalInputDir.z < 0 && localVelocity.z > horizontalInputDir.z * playerStats.currentSpeed))
            {
                moveVector.z = horizontalInputDir.z;
            }
            else
            {
                moveVector.z = 0;
            }

            worldMoveVector = Quaternion.Euler(0, cameraRig.eulerAngles.y, 0) * moveVector;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            rigidbody.isKinematic = !rigidbody.isKinematic;
            foreach (Collider c in playerColliders) c.isTrigger = !c.isTrigger;
            devMode = !devMode;
        }
    }

    private void FixedUpdate()
    {
        if (gameController.isPaused) return;
        //rigidbody.MoveRotation(Quaternion.Euler(0, cameraRig.eulerAngles.y, 0));
        MoveCharacter();

        currentSpeedVector = rigidbody.linearVelocity;
        horizontalSpeed = new Vector3(rigidbody.linearVelocity.x, 0, rigidbody.linearVelocity.z).magnitude;
    }

    public void DisableMovement()
    {
        isMoveAble = false;
        cameraController.ExitScopeMode();
    }

    private void GroundJump()
    {
        rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, 0, rigidbody.linearVelocity.z);
        rigidbody.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
    }

    private void AirJump()
    {
        rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, 0, rigidbody.linearVelocity.z);
        rigidbody.AddForce(Vector3.up * airJumpPower, ForceMode.Impulse);
        remainingAirJumpCount--;
    }


    private void MoveCharacter()
    {
        if (!isMoveAble) return;

        if (devMode) return;

        rigidbody.AddForce(worldMoveVector * (isGrounded ? movePower : airMovePower), ForceMode.Acceleration);
    }

    private void Dash()
    {
        rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, 0, rigidbody.linearVelocity.z);
        StartCoroutine(removeGravity(dashAirborneTime));
        rigidbody.AddRelativeForce(horizontalInputDir * dashPower, ForceMode.Impulse);
    }

    private IEnumerator removeGravity(float time)
    {
        rigidbody.useGravity = false;
        yield return new WaitForSeconds(time);
        rigidbody.useGravity = true;
    }

    public void SetVelocity(Vector3 velocity)
    {
        rigidbody.linearVelocity = velocity;
    }

    private IEnumerator ClimbHigh(Vector3 groundPos)
    {
        RaycastHit hit;

        foreach(Collider c in playerColliders)
        {
            c.enabled = false;
        }
        yield return new WaitForEndOfFrame();
        groundPos.y = 0;
        moveVector = groundPos - new Vector3(transform.position.x, 0, transform.position.z);
        if (Physics.Raycast(transform.position + Vector3.up * (yOffsetHigh - downCheckDistance * 1.1f), transform.forward * frontCheckDistance, out hit, frontCheckDistance, groundLayer))
        {
            Vector3 hitPos = hit.point;
            hitPos.y = 0;
            moveVector = (hitPos - new Vector3(transform.position.x, 0, transform.position.z));
        }
        moveVector *= 0.75f;
        rigidbody.MovePosition(transform.position + moveVector);
        DisableMovement();
        rigidbody.useGravity = false;
        rigidbody.linearVelocity = Vector3.zero;
        playerAnimator.ClimbHigh();
        yield return new WaitForSeconds(3f);
        rigidbody.useGravity = true;
        foreach (Collider c in playerColliders)
        {
            c.enabled = true;
        }
        
    }

    private IEnumerator ClimbLow(Vector3 groundPos)
    {
        foreach (Collider c in playerColliders)
        {
            c.enabled = false;
        }
        DisableMovement();
        rigidbody.isKinematic = true;
        rigidbody.linearVelocity = Vector3.zero;
        playerAnimator.ClimbLow();
        targetPosition = groundPos + Vector3.up;
        yield return new WaitForSeconds(1.067f);
        foreach (Collider c in playerColliders)
        {
            c.enabled = true;
        }
    }

    private void OnDrawGizmos()
    {
        // 스크립트가 비활성화되어도 Gizmos는 표시될 수 있으므로 null 체크를 추가합니다.
        if (Application.isPlaying)
        {
            DrawHighFrontRaycast();
            DrawHighDownRaycast();
            DrawLowFrontRaycast();
            DrawLowDownRaycast();
        }
    }

    // 1. 전방 Raycast 시각화
    private void DrawHighFrontRaycast()
    {
        Vector3 origin = transform.position + Vector3.up * yOffsetHigh;
        Vector3 direction = transform.forward;

        bool hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, frontCheckDistance, groundLayer);
        Color color = hit ? successColor : failColor;

        // Gizmos.color 설정
        Gizmos.color = color;
        // Raycast 선 그리기
        Gizmos.DrawLine(origin, origin + direction * frontCheckDistance);

        // 충돌 지점에 구체 그리기
        if (hit)
        {
            Gizmos.DrawSphere(hitInfo.point, 0.05f);
        }

        // 디버깅 메시지 출력
        Debug.DrawRay(origin, direction * frontCheckDistance, color);
    }

    // 2. 전방 이동 후 아래 Raycast 시각화 (두 번째 조건)
    private void DrawHighDownRaycast()
    {
        // 전방 Raycast가 실패했을 때만 두 번째 Raycast가 실행되므로, 이 시각화는 항상 그립니다.
        Vector3 downRayOrigin = transform.position + Vector3.up * yOffsetHigh + transform.forward * frontCheckDistance;
        Vector3 downDirection = Vector3.down;

        // 실제 조건 확인: 첫 번째 Raycast가 실패했는지 확인
        bool frontRayFailed = !Physics.Raycast(transform.position + Vector3.up * yOffsetHigh, transform.forward, frontCheckDistance, groundLayer);

        // 두 번째 Raycast 실행 및 시각화
        bool downHit = Physics.Raycast(downRayOrigin, downDirection, out RaycastHit downHitInfo, downCheckDistance, groundLayer);

        Color color;

        if (frontRayFailed && downHit)
        {
            // 두 조건 모두 성공 (낭떠러지 감지)
            color = successColor;
        }
        else if (frontRayFailed && !downHit)
        {
            // 전방은 뚫렸으나, 앞 아래도 없음 (아주 큰 구멍)
            color = Color.yellow;
        }
        else
        {
            // 첫 번째 Raycast가 성공했거나 (전방에 벽), 조건에 해당하지 않음
            color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 회색 (비활성)
        }

        // Gizmos.color 설정
        Gizmos.color = color;
        // Raycast 선 그리기
        Gizmos.DrawLine(downRayOrigin, downRayOrigin + downDirection * downCheckDistance);

        // 충돌 지점에 구체 그리기
        if (downHit)
        {
            Gizmos.DrawSphere(downHitInfo.point, 0.05f);
        }

        // Debug.DrawRay로도 표시
        Debug.DrawRay(downRayOrigin, downDirection * downCheckDistance, color);
    }

    private void DrawLowFrontRaycast()
    {
        Vector3 origin = transform.position + Vector3.up * yOffsetHigh;
        Vector3 direction = transform.forward;

        bool hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, frontCheckDistance, groundLayer);
        Color color = hit ? successColor : failColor;

        // Gizmos.color 설정
        Gizmos.color = color;
        // Raycast 선 그리기
        Gizmos.DrawLine(origin, origin + direction * frontCheckDistance);

        // 충돌 지점에 구체 그리기
        if (hit)
        {
            Gizmos.DrawSphere(hitInfo.point, 0.05f);
        }

        // 디버깅 메시지 출력
        Debug.DrawRay(origin, direction * frontCheckDistance, color);
    }

    // 2. 전방 이동 후 아래 Raycast 시각화 (두 번째 조건)
    private void DrawLowDownRaycast()
    {
        // 전방 Raycast가 실패했을 때만 두 번째 Raycast가 실행되므로, 이 시각화는 항상 그립니다.
        Vector3 downRayOrigin = transform.position + Vector3.up * yOffsetHigh + transform.forward * frontCheckDistance;
        Vector3 downDirection = Vector3.down;

        // 실제 조건 확인: 첫 번째 Raycast가 실패했는지 확인
        bool frontRayFailed = !Physics.Raycast(transform.position + Vector3.up * yOffsetHigh, transform.forward, frontCheckDistance, groundLayer);

        // 두 번째 Raycast 실행 및 시각화
        bool downHit = Physics.Raycast(downRayOrigin, downDirection, out RaycastHit downHitInfo, downCheckDistance, groundLayer);

        Color color;

        if (frontRayFailed && downHit)
        {
            // 두 조건 모두 성공 (낭떠러지 감지)
            color = successColor;
        }
        else if (frontRayFailed && !downHit)
        {
            // 전방은 뚫렸으나, 앞 아래도 없음 (아주 큰 구멍)
            color = Color.yellow;
        }
        else
        {
            // 첫 번째 Raycast가 성공했거나 (전방에 벽), 조건에 해당하지 않음
            color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 회색 (비활성)
        }

        // Gizmos.color 설정
        Gizmos.color = color;
        // Raycast 선 그리기
        Gizmos.DrawLine(downRayOrigin, downRayOrigin + downDirection * downCheckDistance);

        // 충돌 지점에 구체 그리기
        if (downHit)
        {
            Gizmos.DrawSphere(downHitInfo.point, 0.05f);
        }

        // Debug.DrawRay로도 표시
        Debug.DrawRay(downRayOrigin, downDirection * downCheckDistance, color);
    }

    
}