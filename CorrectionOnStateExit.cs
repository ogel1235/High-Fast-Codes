using UnityEngine;
using System.Collections;

public class CorrectionOnStateExit : StateMachineBehaviour
{
    CharacterRagdollController ragdollController;
    PlayerController playerController;
    PlayerIKController playerIKController;
    Rigidbody mainRig;

    private bool triggered = false;

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);

        if (animator.IsInTransition(layerIndex) && !triggered)
        {
            Debug.Log("OnStateUpdate: 위치 보정 트리거됨");
            triggered = true;
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // --- 캐싱 처리 --- (OnStateExit에서 수행하면 OnStateEnter에서 중복되므로, Start/Awake에서 처리하는 것이 좋지만, 이 컨텍스트에서는 그대로 둡니다.)
        if (ragdollController == null)
        {
            ragdollController = animator.transform.root.GetComponent<CharacterRagdollController>();
        }
        if (playerController == null)
        {
            playerController = animator.transform.root.GetComponent<PlayerController>();
        }
        if (playerIKController == null)
        {
            playerIKController = animator.GetComponent<PlayerIKController>();
        }
        if (mainRig == null)
        {
            mainRig = animator.transform.root.GetComponent<Rigidbody>();
        }

        Debug.Log("OnStateExit: 위치 보정 트리거됨");

        AnimatorTransitionInfo transitionInfo = animator.GetAnimatorTransitionInfo(layerIndex);
        float transitionDuration = transitionInfo.duration;

        mainRig.isKinematic = false; // Rigidbody를 물리 엔진 제어 상태로 변경

        Vector3 startPosition = mainRig.position;
        Vector3 endPosition;

        // 2. 최종 도착 위치(End Position) 결정
        if (playerController.targetPosition != Vector3.zero)
        {
            // 특정 목표 위치가 설정된 경우
            endPosition = playerController.targetPosition;
            playerController.targetPosition = Vector3.zero; // 사용 후 초기화
        }
        else
        {
            // 목표 위치가 없는 경우, 발의 중간 지점을 계산하여 최종 위치로 설정
            Vector3 footMidPosition = (animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + animator.GetBoneTransform(HumanBodyBones.RightFoot).position) / 2;
            endPosition = footMidPosition + Vector3.up * 0.8f;
        }

        // 3. PlayerController를 통해 이동 코루틴 시작 요청
        if (playerController != null)
        {
            // 기존의 이동 코루틴을 중지 (새로운 이동이 시작되므로)
            playerController.StopAllCoroutines();

            // 새로운 이동 코루틴 시작
            playerController.StartCoroutine(MovePlayerOverTime(startPosition, endPosition, transitionDuration));
        }

        // 전환이 진행 중일 때만 위치 보정을 수행합니다.
        //if (animator.IsInTransition(layerIndex))
        //{
        //    Debug.Log("OnStateExit: 위치 보정 시작");
        //    // 1. 전환 정보 및 지속 시간을 가져옵니다.
        //    AnimatorTransitionInfo transitionInfo = animator.GetAnimatorTransitionInfo(layerIndex);
        //    float transitionDuration = transitionInfo.duration;

        //    mainRig.isKinematic = false; // Rigidbody를 물리 엔진 제어 상태로 변경

        //    Vector3 startPosition = mainRig.position;
        //    Vector3 endPosition;

        //    // 2. 최종 도착 위치(End Position) 결정
        //    if (playerController.targetPosition != Vector3.zero)
        //    {
        //        // 특정 목표 위치가 설정된 경우
        //        endPosition = playerController.targetPosition;
        //        playerController.targetPosition = Vector3.zero; // 사용 후 초기화
        //    }
        //    else
        //    {
        //        // 목표 위치가 없는 경우, 발의 중간 지점을 계산하여 최종 위치로 설정
        //        Vector3 footMidPosition = (animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + animator.GetBoneTransform(HumanBodyBones.RightFoot).position) / 2;
        //        endPosition = footMidPosition + Vector3.up * 0.8f;
        //    }

        //    // 3. PlayerController를 통해 이동 코루틴 시작 요청
        //    if (playerController != null)
        //    {
        //        // 기존의 이동 코루틴을 중지 (새로운 이동이 시작되므로)
        //        playerController.StopAllCoroutines();

        //        // 새로운 이동 코루틴 시작
        //        playerController.StartCoroutine(playerController.MovePlayerOverTime(startPosition, endPosition, transitionDuration));
        //    }

        //    // OnStateExit에서는 isMoveAble을 true로 바꾸지 않습니다. (전환 중에는 움직임 제한이 유지되어야 할 수 있습니다.)
        //    // 이동 코루틴이 끝난 후 isMoveAble을 true로 설정하는 것이 좋습니다.
        //}
    }

    // StateMachineBehaviour는 코루틴을 직접 실행할 수 없으므로,
    // 이 메서드는 PlayerController 내부에 구현되어야 합니다.
    // 이 주석을 제거하고 아래 코드를 PlayerController.cs에 붙여넣으세요.
    public IEnumerator MovePlayerOverTime(Vector3 startPos, Vector3 endPos, float duration)
    {
        Debug.Log($"Moving player from {startPos} to {endPos} over {duration} seconds.");
        float startTime = Time.time;
        float elapsed = 0f;

        playerIKController.enabled = false;

        while (elapsed < duration)
        {
            elapsed = Time.time - startTime;
            float t = elapsed / duration;

            // Rigidbody 위치를 보간
            mainRig.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        // 정확한 목표 위치에 도달하도록 보장
        mainRig.position = endPos;

        // 이동 완료 후, 움직임 제어를 다시 활성화
        playerController.isMoveAble = true;
        playerIKController.enabled = true;
    }
}