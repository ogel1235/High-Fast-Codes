using UnityEngine;

public class StepAssistBodyCheck : MonoBehaviour
{
    private StepAssist stepAssist;
    private CapsuleCollider capsuleCollider;
    private float capsuleHeight;
    private Vector3 capsuleLocalBottom;
    private Vector3 capsuleWorldBottom;
    private float capsuleRadius;

    private bool triggered = false;

    private RaycastHit hit;

    [SerializeField]
    private LayerMask groundLayer;

    private void Awake()
    {
        stepAssist = GetComponentInParent<StepAssist>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        capsuleHeight = capsuleCollider.height;
        capsuleLocalBottom = capsuleCollider.center - new Vector3(0, capsuleHeight / 2, 0);
        capsuleWorldBottom = transform.parent.TransformPoint(capsuleLocalBottom);
        capsuleRadius = capsuleCollider.radius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((groundLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            triggered = true;
            Debug.Log("Body Check Triggered");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((groundLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            triggered = false;
            Debug.Log("Body Check Untriggered");
        }
    }

    private void Update()
    {
        if (!triggered)
        {
            if(Physics.Raycast(capsuleWorldBottom, Vector3.down, out hit, capsuleRadius, groundLayer))
            {
                if(hit.normal.y > 0.707f)
                {
                    stepAssist.isNotWall = true;
                }
                else
                {
                    stepAssist.isNotWall = false;
                }
            }
            else
            {
                stepAssist.isNotWall = false;
            }
        }
        else
        {
            stepAssist.isNotWall = false;
        }
    }
}