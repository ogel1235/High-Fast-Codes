using System.Collections;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    private PlayerController playerController;
    [SerializeField]
    private Animator animator;
    private CapsuleCollider capsule;
    private Collider[] selfColliders; // 같은 GameObject의 콜라이더만

    [Header("Layer / Filter")]
    [SerializeField]
    private LayerMask groundLayer = ~0; // 인스펙터에서 Ground 레이어만 선택

    [Header("SphereCast Settings")]
    [SerializeField]
    private float groundCheckDistance = 0.35f; // 캡슐 바닥 아래 추가 검사 거리
    [SerializeField]
    private float originOffset = 0.05f;        // 캡슐 바닥 구 중심에서 살짝 위로 시작
    [SerializeField, Range(0.1f, 1f)]
    private float sphereRadiusScale = 0.9f;    // 구 반지름 = 캡슐 반지름 * 스케일
    [SerializeField, Range(0f, 89f)]
    private float maxGroundAngle = 60f;        // 허용 경사 최대 각도

    [Header("Debug")]
    [SerializeField]
    private bool drawGizmos = true;

    // 현재 접지 표면의 법선(없으면 Vector3.up)
    public Vector3 GroundNormal { get; private set; } = Vector3.up;

    // GC 방지를 위한 버퍼
    private readonly RaycastHit[] sphereHits = new RaycastHit[8];

    //기타 재활용 변수
    private Vector3 zeroZVelocity;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        capsule = GetComponent<CapsuleCollider>();
        selfColliders = GetComponents<Collider>();
    }

    private void FixedUpdate()
    {
        bool grounded = CheckGrounded(out RaycastHit hit);

        GroundNormal = grounded ? hit.normal : Vector3.up;

        if (grounded && !playerController.isGrounded)
        {
            playerController.isGrounded = true;
            playerController.RemainingAirJumpCount = playerController.AirJumpCount;
            zeroZVelocity = new Vector3(playerController.currentSpeedVector.x, 0, playerController.currentSpeedVector.z);
            playerController.SetVelocity(zeroZVelocity);
        }
        else if (!grounded && playerController.isGrounded)
        {
            playerController.isGrounded = false;
            animator.SetTrigger("Jump");
        }
    }

    private bool CheckGrounded(out RaycastHit bestHit)
    {
        bestHit = default;

        if (capsule == null)
        {
            // 캡슐이 없으면 단순 Raycast (예외적 상황)
            Vector3 origin = transform.position + Vector3.up * originOffset;
            float rayLen = originOffset + groundCheckDistance;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLen, groundLayer, QueryTriggerInteraction.Ignore))
            {
                if (!IsSelf(hit.collider) && IsWalkable(hit.normal))
                {
                    bestHit = hit;
                    return true;
                }
            }
            return false;
        }

        // 캡슐 하단 구 중심과 반지름 계산(스케일 반영)
        Vector3 up = transform.up;
        Vector3 down = -up;

        Vector3 worldCenter = transform.TransformPoint(capsule.center);
        float radius = capsule.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        float halfHeight = Mathf.Max((capsule.height * Mathf.Abs(transform.lossyScale.y)) * 0.5f - radius, 0f);
        Vector3 bottomSphereCenter = worldCenter + down * halfHeight;

        float castRadius = Mathf.Max(radius * sphereRadiusScale - 0.001f, 0.001f);
        Vector3 castOrigin = bottomSphereCenter + up * originOffset;
        float castDistance = originOffset + groundCheckDistance;

        int hitCount = Physics.SphereCastNonAlloc(
            castOrigin, castRadius, down,
            sphereHits, castDistance, groundLayer, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            var h = sphereHits[i];
            if (h.collider == null) continue;
            if (IsSelf(h.collider)) continue;
            if (!IsWalkable(h.normal)) continue;

            if (h.distance < best)
            {
                best = h.distance;
                bestHit = h;
                found = true;
            }
        }

        return found;
    }

    private bool IsSelf(Collider col)
    {
        for (int i = 0; i < selfColliders.Length; i++)
        {
            if (col == selfColliders[i]) return true;
        }
        return false;
    }

    private bool IsWalkable(Vector3 normal)
    {
        float angle = Vector3.Angle(normal, Vector3.up);
        return angle <= maxGroundAngle;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        CapsuleCollider cap = capsule != null ? capsule : GetComponent<CapsuleCollider>();
        if (cap == null) return;

        Vector3 up = transform.up;
        Vector3 down = -up;

        Vector3 worldCenter = transform.TransformPoint(cap.center);
        float radius = cap.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        float halfHeight = Mathf.Max((cap.height * Mathf.Abs(transform.lossyScale.y)) * 0.5f - radius, 0f);
        Vector3 bottomSphereCenter = worldCenter + down * halfHeight;

        float castRadius = Mathf.Max(radius * sphereRadiusScale - 0.001f, 0.001f);
        Vector3 castOrigin = bottomSphereCenter + up * originOffset;
        float castDistance = originOffset + groundCheckDistance;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(castOrigin, castRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(castOrigin, castOrigin + down * castDistance);

        if (Physics.SphereCast(castOrigin, castRadius, down, out RaycastHit hit, castDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Gizmos.color = IsWalkable(hit.normal) ? Color.green : Color.red;
            Gizmos.DrawSphere(hit.point, 0.05f);
        }
    }
}