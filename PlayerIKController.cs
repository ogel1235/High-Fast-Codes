using UnityEngine;
using System.Collections;

public class PlayerIKController : MonoBehaviour
{
    private Animator animator;
    private CameraController cameraController;

    [SerializeField]
    private float ikActiveTime;
    private float ikActiveTimer = 0;
    private bool ikActive;

    [SerializeField]
    private Transform desiredHandPosition;
    [SerializeField]
    private Transform handPosition;
    private Transform rightUpperArmTransform;
    private Vector3 rightUpperArmPosition;
    [SerializeField]
    private Transform rightElbowHint;
    [SerializeField]
    private float distanceFromArmToHand = 0.5f;
    private Vector3 aimPoint;
    [SerializeField]
    private float recoilDistance = 0.2f;
    [SerializeField]
    private float recoilRecoveryTime = 1f;
    [SerializeField]
    private float handDownTime = 1f;
    private bool isFired;
    private Vector3 smoothDampVelocity = Vector3.zero;
    [SerializeField, Range(0, 1)]
    private float randomRecoilVertical = 0.1f;
    [SerializeField, Range(0, 1)]
    private float randomRecoilHorizontal = 0.1f;

    private float currentValue;
    private Coroutine changeValueOverTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        cameraController = GetComponentInParent<CameraController>();
        rightUpperArmTransform = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        isFired = false;
    }

    // 조준점 기반 손 IK: 반동 없는 기준 목표와 반동·복구가 적용되는 실제 IK 목표를 분리한다.
    // desiredHandPosition=기준 목표, handPosition=실제 목표. 관절 계산은 Animator IK에 맡긴다.
    public void OnAnimatorIK(int layerIndex)
    {
        aimPoint = cameraController.GetAimPoint();

        animator.SetLookAtWeight(currentValue);
        animator.SetLookAtPosition(aimPoint);

        rightUpperArmPosition = rightUpperArmTransform.position;

        // 어깨에서 조준점을 향해 팔 길이만큼 이동한 위치를 기준 목표로 매번 갱신한다.
        desiredHandPosition.position = rightUpperArmPosition;
        desiredHandPosition.LookAt(aimPoint);
        desiredHandPosition.Translate(Vector3.forward * distanceFromArmToHand, Space.Self);
        desiredHandPosition.Rotate(0f, 0f, -90f, Space.Self);

        if (isFired)
        {
            // 반동을 목표의 로컬 축으로 계산해, 어느 방향을 겨누어도 조준 방향을 따라 적용한다.
            float recoilX = Random.Range(-randomRecoilHorizontal, randomRecoilHorizontal);
            float recoilY = Random.Range(0, randomRecoilVertical);

            Vector3 right = desiredHandPosition.right;
            Vector3 up = desiredHandPosition.up;
            Vector3 forward = desiredHandPosition.forward;

            Vector3 recoilOffset =
                (right * recoilX) + (up * recoilY) - (forward * recoilDistance);

            handPosition.position = desiredHandPosition.position + recoilOffset;
            handPosition.rotation = desiredHandPosition.rotation;

            isFired = false;
        }
        else
        {
            // 발사 후에는 갱신되는 기준 목표를 따라가며 반동의 위치·회전을 부드럽게 복구한다.
            handPosition.position = Vector3.SmoothDamp(
                handPosition.position, desiredHandPosition.position,
                ref smoothDampVelocity, recoilRecoveryTime);
            handPosition.rotation = Quaternion.Slerp(
                handPosition.rotation, desiredHandPosition.rotation,
                Time.deltaTime / recoilRecoveryTime);

            if (smoothDampVelocity.magnitude < 0.01f)
            {
                smoothDampVelocity = Vector3.zero;
            }
        }

        // 사격 중에는 IK 가중치를 1로 유지한다. 재사격 시 진행 중인 가중치 감소를 중단한다.
        if (ikActiveTimer > 0)
        {
            if (!ikActive)
            {
                ikActive = true;
                if (changeValueOverTime != null)
                {
                    StopCoroutine(changeValueOverTime);
                    changeValueOverTime = null;
                }
            }
            currentValue = 1;
        }
        else
        {
            if (ikActive)
            {
                ikActive = false;
                if (changeValueOverTime == null)
                {
                    // 사격이 끊기면 가중치를 1→0으로 내려 기본 애니메이션 자세로 돌아간다.
                    changeValueOverTime = StartCoroutine(ChangeValueOverTime(1, 0, handDownTime));
                }
            }
        }

        // 계산한 손 목표와 팔꿈치 힌트를 같은 가중치로 적용한다.
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentValue);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, currentValue);
        animator.SetIKPosition(AvatarIKGoal.RightHand, handPosition.position);
        animator.SetIKRotation(AvatarIKGoal.RightHand, handPosition.rotation);

        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, currentValue);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow, rightElbowHint.position);

        if (ikActiveTimer > 0) ikActiveTimer -= Time.deltaTime;
        else ikActiveTimer = 0;
    }

    public void OnShoot()
    {
        ikActiveTimer = ikActiveTime;
        isFired = true;
    }

    private IEnumerator ChangeValueOverTime(float start, float end, float time)
    {
        float elapsedTime = 0f;
        currentValue = start;

        while (elapsedTime < time)
        {
            float t = elapsedTime / time;
            currentValue = Mathf.Lerp(start, end, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentValue = end;
        changeValueOverTime = null;
    }
}
