using UnityEngine;
using System.Collections;

public class ResetPositionOnEnter : StateMachineBehaviour
{
    CharacterRagdollController ragdollController;
    PlayerController playerController;
    PlayerIKController playerIKController;
    Rigidbody mainRig;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // --- 캐싱 처리 ---
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

        if (!playerController.isMoveAble)
        {
            mainRig.isKinematic = false;
            if (playerController.targetPosition != Vector3.zero)
            {
                Debug.Log("target position applied");
                mainRig.transform.position = playerController.targetPosition;
                mainRig.position = mainRig.transform.position;
                playerController.targetPosition = Vector3.zero;
            }
            //else
            //{
            //    Vector3 footMidPosition = (animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + animator.GetBoneTransform(HumanBodyBones.RightFoot).position) / 2;
            //    mainRig.transform.position = footMidPosition + Vector3.up * 0.7f;
            //    mainRig.position = mainRig.transform.position;
            //}
            playerController.StartCoroutine(EnableMoveAfterShortTime());
        }
    }

    private IEnumerator EnableMoveAfterShortTime()
    {
        yield return new WaitForSeconds(0.05f);
        playerController.isMoveAble = true;
        playerIKController.enabled = true;
    }
}