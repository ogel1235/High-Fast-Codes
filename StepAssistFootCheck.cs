using UnityEngine;

public class StepAssistFootCheck : MonoBehaviour
{
    private StepAssist stepAssist;

    [SerializeField]
    private LayerMask groundLayer;

    private void Awake()
    {
        stepAssist = GetComponentInParent<StepAssist>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((groundLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            stepAssist.stepDetected = true;
            Debug.Log("Foot Check Triggered");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((groundLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            stepAssist.stepDetected = false;
            Debug.Log("Foot Check Untriggered");
        }
    }
}
