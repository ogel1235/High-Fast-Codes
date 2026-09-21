using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerController))]
public class StepAssist : MonoBehaviour
{
    private PlayerController playerController;
    private Rigidbody rb;

    [SerializeField]
    private Transform stepAssistFootCheck;
    [SerializeField]
    private Transform stepAssistBodyCheck;

    public bool isNotWall = false;
    public bool stepDetected = false;

    [SerializeField]
    private float ascentSpeed = 10f;

    private Vector3 tempVector;

    private bool isSteppingUp = false;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        tempVector = playerController.HorizontalInputDir * 0.25f;
        tempVector.y = stepAssistFootCheck.localPosition.y;
        stepAssistFootCheck.localPosition = tempVector;
        tempVector.y = stepAssistBodyCheck.localPosition.y;
        stepAssistBodyCheck.localPosition = tempVector;
        if (!isSteppingUp && stepDetected && isNotWall && playerController.isGrounded && playerController.HorizontalInputDir != Vector3.zero)
        {
            isSteppingUp = true;
            Debug.Log("Step Assist **Started**");
        }

        if (isSteppingUp)
        {
            // PlayerController의 중력과 충돌하지 않도록 속도를 직접 설정하는 것도 좋은 방법입니다.
            // rb.velocity = new Vector3(rb.velocity.x, ascentSpeed, rb.velocity.z);

            // 또는 MovePosition을 계속 사용
            rb.MovePosition(rb.position + Vector3.up * ascentSpeed * Time.fixedDeltaTime);
            Debug.Log("Step Assist **Activating**");

            if (!stepDetected)
            {
                isSteppingUp = false;
                Debug.Log("Step Assist **Finished**");
            }
        }
    }
}