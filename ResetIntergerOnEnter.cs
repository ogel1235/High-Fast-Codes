using UnityEngine;

public class ResetIntegerOnEnter : StateMachineBehaviour
{
    public string parameterName = "GetUpDirection"; // 리셋할 파라미터 이름
    public int resetValue = 0;                    // 리셋할 값

    private void Awake()
    {
        
    }

    // OnStateEnter는 이 상태 머신이 상태에 진입할 때 호출됩니다.
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetInteger(parameterName, resetValue);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("IsGrounded", true);
    }
}