using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterRagdollController : MonoBehaviour
{
    [Header("Main Components")]
    [SerializeField] private Rigidbody mainRig;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private Collider footCollider;
    [SerializeField] private Animator animator;
    private PlayerController playerController;
    private CameraController cameraController;
    private PlayerIKController playerIKController;

    [Header("Ragdoll Bones")]
    public Transform hipsBone;

    private float blendDuration = 0.5f;

    private Rigidbody[] ragdollRigs;
    private Rigidbody hipsRig;

    public bool isRagdollEnabled = false;
    private Quaternion targetRotation;

    private class BoneSnapshot
    {
        public Transform transform;
        public Vector3 position;
        public Quaternion rotation;
    }
    private List<BoneSnapshot> boneSnapshots = new List<BoneSnapshot>();

    private bool isBlending = false;
    private float blendTime = 0f;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        cameraController = GetComponent<CameraController>();
        playerIKController = GetComponentInChildren<PlayerIKController>();

        if (animator == null) return;

        ragdollRigs = animator.GetComponentsInChildren<Rigidbody>();

        if (hipsBone == null && animator.avatar.isHuman)
            hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

        if (hipsBone != null) hipsRig = hipsBone.GetComponent<Rigidbody>();

        foreach (var rig in ragdollRigs)
        {
            rig.isKinematic = true;
            rig.detectCollisions = false;
            rig.interpolation = RigidbodyInterpolation.None;
            if (rig.transform.GetComponent<CheckCollision>() != null || rig.transform == hipsBone)
            {
                boneSnapshots.Add(new BoneSnapshot { transform = rig.transform });
            }
        }

        if (mainRig != null) mainRig.isKinematic = false;
        if (mainCollider != null) mainCollider.enabled = true;
        if (footCollider != null) footCollider.enabled = true;
        if (animator != null) animator.enabled = true;
    }

    // 래그돌 → 기상 애니메이션: Animator가 계산한 자세에 저장된 물리 자세를 섞는다.
    // 시작 자세는 고정하고 도착 자세는 매 프레임 갱신해, 움직이는 기상 동작으로 이어준다.
    private void LateUpdate()
    {
        if (isBlending)
        {
            float t = blendTime / blendDuration;

            if (t >= 1.0f)
            {
                isBlending = false;
                return;
            }

            foreach (var snapshot in boneSnapshots)
            {
                // t=0은 저장한 래그돌 자세, t=1은 현재 애니메이션 자세다.
                snapshot.transform.position = Vector3.Lerp(
                    snapshot.position, snapshot.transform.position, t);
                snapshot.transform.rotation = Quaternion.Slerp(
                    snapshot.rotation, snapshot.transform.rotation, t);
            }

            blendTime += Time.deltaTime;
        }
    }

    // 사망 시 캐릭터 이동·IK·Animator의 제어를 멈추고, 본의 Rigidbody로 제어권을 넘긴다.
    public void EnableRagdoll()
    {
        StopAllCoroutines();
        isBlending = false;

        if (playerIKController != null) playerIKController.enabled = false;
        if (playerController != null) playerController.DisableMovement();

        isRagdollEnabled = true;

        // 전환 직전의 이동 속도를 보관해, 쓰러지는 순간에도 이동 방향과 속도를 이어간다.
        Vector3 inheritedVelocity = Vector3.zero;
        if (mainRig != null) inheritedVelocity = mainRig.linearVelocity;

        if (animator != null) animator.enabled = false;

        if (mainRig != null)
        {
            mainRig.linearVelocity = Vector3.zero;
            mainRig.angularVelocity = Vector3.zero;
            mainRig.isKinematic = true;
        }

        if (mainCollider != null) mainCollider.enabled = false;
        if (footCollider != null) footCollider.enabled = false;

        // 메인 물리 몸체를 비활성화한 뒤 각 본에 같은 선속도를 전달한다.
        foreach (var rig in ragdollRigs)
        {
            rig.isKinematic = false;
            rig.detectCollisions = true;
            rig.interpolation = RigidbodyInterpolation.Interpolate;
            rig.linearVelocity = inheritedVelocity;
        }
    }

    public void DisableRagdoll()
    {
        if (!isRagdollEnabled) return;
        isRagdollEnabled = false;
        StartCoroutine(_DisableRagdoll());
    }

    // 복귀 순서: 물리 자세 저장 → 기상 방향 선택 → 루트 위치 보정 → 자세 블렌딩.
    private IEnumerator _DisableRagdoll()
    {
        // 기상 애니메이션 시작 전에 현제 래그돌 상태의 포즈를 저장. 이후 기상 애니메이션으로 블랜딩.
        foreach (var snapshot in boneSnapshots)
        {
            snapshot.position = snapshot.transform.position;
            snapshot.rotation = snapshot.transform.rotation;
        }

        BoneSnapshot hipsSnapshot = null;
        foreach (var snapshot in boneSnapshots)
        {
            if (snapshot.transform == hipsBone)
            {
                hipsSnapshot = snapshot;
                break;
            }
        }

        Vector3 ragdollVelocity = Vector3.zero;
        if (hipsRig != null) ragdollVelocity = hipsRig.linearVelocity;

        foreach (var rig in ragdollRigs)
        {
            rig.isKinematic = true;
            rig.detectCollisions = false;
            rig.interpolation = RigidbodyInterpolation.None;
        }

        int lyingDirection = GetLyingDirection();

        bool onGround = false;
        foreach (var rig in ragdollRigs)
        {
            CheckCollision cc = rig.GetComponent<CheckCollision>();
            if (cc != null && cc.hasCollided)
            {
                onGround = true;
                break;
            }
        }

        if (animator != null)
        {
            animator.enabled = true;

            if (onGround)
            {
                string stateName = "StandUp_Front";

                // 골반 방향으로 분류한 등·배·좌·우 자세에 맞춰 기상 클립과 보간 시간을 선택.
                switch (lyingDirection)
                {
                    case 1:
                        stateName = "StandUp_Back";
                        blendDuration = 0.9f;
                        break;
                    case 2:
                        stateName = "StandUp_Front";
                        blendDuration = 0.7f;
                        break;
                    case 3:
                        stateName = "StandUp_Left";
                        blendDuration = 0.5f;
                        break;
                    case 4:
                        stateName = "StandUp_Right";
                        blendDuration = 0.5f;
                        break;
                    default:
                        stateName = "StandUp_Front";
                        blendDuration = 0.7f;
                        break;
                }

                // 골반 기준 위치 보정: 선택한 기상 애니메이션의 시작 자세를 즉시 평가한다.
                animator.Play(stateName, 0, 0f);
                animator.Update(0f);

                Vector3 targetPosition;
                if (hipsSnapshot != null)
                {
                    // 물리로 쓰러진 골반 위치와 애니메이션 적용 후 골반 위치를 비교한다.
                    Vector3 ragdollHipsPos = hipsSnapshot.position;
                    Vector3 animatedHipsPos = hipsBone.position;

                    // 애니메이션 때문에 골반이 이동한 만큼 루트를 반대로 옮겨 위치 차이를 보정한다.
                    Vector3 positionOffset = animatedHipsPos - ragdollHipsPos;
                    targetPosition = transform.position - positionOffset;
                }
                else
                {
                    targetPosition = hipsBone.position + Vector3.up * 0.8f;
                }

                transform.position = targetPosition;
                if (onGround) transform.rotation = targetRotation;

                if (mainRig != null)
                {
                    mainRig.isKinematic = false;
                    mainRig.position = targetPosition;
                    if (onGround) mainRig.rotation = targetRotation;

                    if (onGround)
                    {
                        mainRig.linearVelocity = Vector3.zero;
                        mainRig.angularVelocity = Vector3.zero;
                    }
                    else
                    {
                        float verticalVel = Mathf.Clamp(ragdollVelocity.y, -20f, 0f);
                        Vector3 horizontalVel = new Vector3(ragdollVelocity.x, 0, ragdollVelocity.z);

                        mainRig.linearVelocity = horizontalVel + (Vector3.up * verticalVel);
                        mainRig.angularVelocity = Vector3.zero;
                    }
                }

                if (mainCollider != null) mainCollider.enabled = true;
                if (footCollider != null) footCollider.enabled = true;

                // 위치 보정 후 LateUpdate에서 저장된 래그돌 자세를 기상 자세로 블렌딩한다.
                // 이동·IK의 재활성화는 Idle에 연결된 ResetPositionOnEnter가 담당한다.
                blendTime = 0f;
                isBlending = true;

                while (blendTime < blendDuration)
                {
                    yield return null;
                }
                isBlending = false;
            }
            else
            {
                // 공중 복귀는 골반 위치에서 애니메이션 없이 시작하며 수평 속도를 유지하고 낙하 속도를 제한한다.
                Vector3 targetPosition = hipsSnapshot.position;

                if (mainRig != null)
                {
                    mainRig.isKinematic = false;
                    mainRig.position = targetPosition;

                    float verticalVel = Mathf.Clamp(ragdollVelocity.y, -20f, 0f);
                    Vector3 horizontalVel = new Vector3(ragdollVelocity.x, 0, ragdollVelocity.z);

                    mainRig.linearVelocity = horizontalVel + (Vector3.up * verticalVel);
                    mainRig.angularVelocity = Vector3.zero;
                }

                if (mainCollider != null) mainCollider.enabled = true;
                if (footCollider != null) footCollider.enabled = true;

                isBlending = false;
                if (playerController != null) playerController.isMoveAble = true;
                if (playerIKController != null) playerIKController.enabled = true;

                animator.Update(0f);
            }
        }
    }

    // 골반의 로컬 축과 월드 위·아래 방향의 내적으로 쓰러진 자세를 분류한다.
    // 내적이 클수록 같은 방향에 가깝다. 반환값: 1=등, 2=배, 3=왼쪽, 4=오른쪽.
    public int GetLyingDirection()
    {
        float dotLyingBack = Vector3.Dot(hipsBone.forward, Vector3.up);
        float dotLyingFront = Vector3.Dot(hipsBone.forward, Vector3.down);
        float dotLyingLeft = Vector3.Dot(hipsBone.right, Vector3.up);
        float dotLyingRight = Vector3.Dot(hipsBone.right, Vector3.down);

        float maxDot = Mathf.Max(dotLyingBack, dotLyingFront, dotLyingLeft, dotLyingRight);

        Vector3 frontVector = Vector3.forward;
        int direction = 1;

        if (maxDot == dotLyingBack) { frontVector = -hipsBone.up; direction = 1; }
        else if (maxDot == dotLyingFront) { frontVector = hipsBone.up; direction = 2; }
        else if (maxDot == dotLyingLeft) { frontVector = hipsBone.forward; direction = 3; }
        else { frontVector = hipsBone.forward; direction = 4; }

        // 기상할 때 몸이 기울지 않도록 전방 벡터를 수평면에 투영해 루트 회전을 결정한다.
        frontVector.y = 0;
        if (frontVector.sqrMagnitude > 0.001f)
            targetRotation = Quaternion.LookRotation(frontVector.normalized, Vector3.up);
        else targetRotation = transform.rotation;

        return direction;
    }
}
