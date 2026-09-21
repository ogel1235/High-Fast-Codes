using UnityEngine;

public class FPCameraShake : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;

    [Header("Settings")]
    public Transform cameraTransform; // 흔들릴 카메라
    public float bobFrequency = 14f;  // 흔들림 속도 (질주 시 빠르게)
    public float bobHorizontalAmplitude = 0.1f; // 좌우 흔들림 강도
    public float bobVerticalAmplitude = 0.1f;   // 상하 흔들림 강도
    public float smoothReturnSpeed = 10f; // 멈췄을 때 원위치로 돌아오는 속도

    private Vector3 defaultLocalPos; // 카메라의 기본 위치
    private float timer = 0f;

    private float horizontalInput;
    private float verticalInput;
    private bool isMoving;

    void Start()
    {
        // 시작 시 카메라의 로컬 위치 저장
        defaultLocalPos = cameraTransform.localPosition;
    }

    void Update()
    {
        HandleHeadBob();
    }

    void HandleHeadBob()
    {
        // 플레이어가 움직이고 있는지 확인 (W,A,S,D 입력 및 Shift 키)
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isMoving = Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f;

        if (isMoving && playerController.isGrounded)
        {
            // 타이머 증가 (속도 조절)
            timer += Time.deltaTime * bobFrequency;

            // 사인(Sin)과 코사인(Cos)을 이용해 8자 형태의 궤적 생성
            float xOffset = Mathf.Cos(timer / 2) * bobHorizontalAmplitude; // 좌우는 상하보다 느리게
            float yOffset = Mathf.Sin(timer) * bobVerticalAmplitude;

            // 카메라 위치 적용
            Vector3 newPos = defaultLocalPos + new Vector3(xOffset, yOffset, 0);
            cameraTransform.localPosition = newPos;
        }
        else
        {
            // 멈췄거나 걷는 중일 때는 부드럽게 원위치로 복귀
            timer = 0;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, defaultLocalPos, Time.deltaTime * smoothReturnSpeed);
        }
    }
}