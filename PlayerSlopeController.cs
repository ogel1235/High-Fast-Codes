using UnityEngine;
using System.Collections;

// 이 스크립트를 'Player' 메인 오브젝트(Rigidbody가 있는 곳)에 붙여주세요.
public class PlayerSlopeController : MonoBehaviour // 클래스 이름 변경 (예: PlayerFoot -> PlayerSlopeController)
{
    private PlayerController playerController;
    private AudioProxy audioProxy;

    [Header("필수 컴포넌트")]
    [SerializeField]
    [Tooltip("플레이어의 '발' 역할을 하는 자식 오브젝트의 Collider를 여기에 연결하세요.")]
    private Collider footCollider;

    [Header("경사 설정")]
    [SerializeField]
    private float ledgeAngleThreshold = 45f;
    [SerializeField]
    private PhysicsMaterial highFrictionMaterial;
    [SerializeField]
    private PhysicsMaterial lowFrictionMaterial;
    [SerializeField]
    private PhysicsMaterial slipperyMaterial;
    private PhysicsMaterial currentFootMaterial;

    private bool footDetected = false;

    private float materialResetTimer = 0.1f;
    private bool materialResetTimerStopped = false;

    [SerializeField]
    private Vector3 localHorizontalVelocity = Vector3.zero;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        audioProxy = GetComponentInChildren<AudioProxy>();

        if (footCollider == null)
        {
            Debug.LogError("Foot Collider가 할당되지 않았습니다! 인스펙터에서 'footCollider'를 연결해주세요.", this);
            return;
        }

        //SetFootMaterial(lowFrictionMaterial);
    }

    private void OnCollisionEnter(Collision collision)
    {
        materialResetTimerStopped = false;
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider == footCollider)
            {
                audioProxy.PlayLandingSound();

                // ... (기존의 다른 로직)
                Vector3 zeroZVelocity = new Vector3(playerController.currentSpeedVector.x, 0, playerController.currentSpeedVector.z);
                //playerController.SetVelocity(zeroZVelocity);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider == footCollider)
            {
                footDetected = true;

                float angle = Vector3.Angle(Vector3.up, contact.normal);
                if (angle > ledgeAngleThreshold)
                {
                    materialResetTimer = 0.2f;

                    return;
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        materialResetTimerStopped = true;
    }

    private void Update()
    {
        if (materialResetTimer > 0 && !materialResetTimerStopped)
        {
            materialResetTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        localHorizontalVelocity = playerController.localVelocity.normalized;
        localHorizontalVelocity.y = 0;
        if (Vector3.Dot(localHorizontalVelocity, playerController.HorizontalInputDir) > 0.93f || localHorizontalVelocity.sqrMagnitude < 0.1f)
        {
            SetFootMaterial(lowFrictionMaterial);
        }
        else if (playerController.HorizontalInputDir != Vector3.zero && materialResetTimer > 0)
        {
            SetFootMaterial(slipperyMaterial);
        }
        else
        {
            SetFootMaterial(highFrictionMaterial);
        }

        if (footDetected)
        {
            playerController.isGrounded = true;
            playerController.RemainingAirJumpCount = playerController.AirJumpCount;
        }
        else
        {
            playerController.isGrounded = false;
        }

        
        footDetected = false;
    }

    private void SetFootMaterial(PhysicsMaterial newMaterial)
    {
        if (newMaterial == null || footCollider == null || newMaterial == currentFootMaterial) return;
        footCollider.material = newMaterial;
        currentFootMaterial = newMaterial;
        Debug.Log("Material: " + footCollider.material.name);
    }
}