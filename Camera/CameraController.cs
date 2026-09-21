using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField]
    private GameController gameController;

    PlayerController playerController;
    PlayerStats playerStats;
    CharacterRagdollController characterRagdollController;
    Animator animator;

    private Transform target;
    private Vector3 targetPos;
    [SerializeField]
    private Transform TPCamRig;
    [SerializeField]
    private Camera TPCamera;
    [SerializeField]
    private Camera FPCamera;
    [SerializeField]
    private Camera UICamera;
    [SerializeField]
    private Transform CameraRig;
    public Camera enabledCamera { get; private set; }

    private new Renderer[] renderer;
    private Color[] color;
    private MaterialPropertyBlock propBlock;
    private static readonly int colorPropertyID = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private float FPFOV = 80;
    [SerializeField]
    private float FPZoomFOV = 40;
    [SerializeField]
    private float FOVChangeSpeed = 5f;

    [SerializeField]
    private float cameraPositionSmoothTime = 1f;
    [SerializeField, Min(0.02f)]
    private float minCameraSmoothTime = 0.2f;
    Vector3 smoothPos;
    float zoomFactor;
    private Vector3 currentVelocity = Vector3.zero;
    float lagDistance;
    [SerializeField]
    float lagBoostThreshold = 2f;
    [SerializeField]
    float lagBoostPower = 2f;
    float boostedSmoothTime;
    float blendRatio;
    [SerializeField]
    private AnimationCurve blendIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [SerializeField]
    private Vector3 TPOffset = new Vector3(0, 0, -6);
    [SerializeField]
    private Vector3 FPOffset = new Vector3(0, 0.4f, 0);
    [SerializeField]
    private float cameraCollisionBuffer = 0.2f;

    private float sensitivity;
    private float currentSensitivity;
    [SerializeField]
    private float scrollSensitivity = 2f;
    [SerializeField]
    private float minYAngle = -85f;
    [SerializeField]
    private float maxYAngle = 85f;
    private float currentX;
    private float currentY;
    Vector3 lastLookTarget;

    private bool scopeMode;
    public bool ScopeMode => scopeMode;

    private AudioListener TPAudioListener;
    private AudioListener FPAudioListener;

    [SerializeField]
    private GameObject FPUI;
    [SerializeField]
    private GameObject TPUI;
    [SerializeField]
    private GameObject crosshairUI;
    [SerializeField]
    private GameObject scopeUI;
    [SerializeField]
    private ParticleSystem crossHairParticle;
    [SerializeField]
    private LayerMask layerMask;
    [SerializeField]
    private LayerMask playerLayerMask;

    RaycastHit hit;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        animator = GetComponentInChildren<Animator>();
        playerStats = GetComponent<PlayerStats>();
        characterRagdollController = GetComponent<CharacterRagdollController>();
        propBlock = new MaterialPropertyBlock();
        TPCamera.fieldOfView = playerStats.baseFOV;
        FPCamera.fieldOfView = FPFOV;
        playerController = GetComponent<PlayerController>();
        renderer = GetComponentsInChildren<Renderer>();
        color = new Color[renderer.Length];
        for (int i = 0; i < renderer.Length; i++)
        {
            if (renderer[i].material.HasProperty(colorPropertyID))
            {
                color[i] = renderer[i].material.color;
            }
            else
            {
                color[i] = Color.white;
            }
        }

        scopeMode = false;
        scopeUI.SetActive(false);
        crosshairUI.SetActive(true);
        crossHairParticle.gameObject.SetActive(false);
        UICamera.depth = 1;
        TPAudioListener = TPCamera.GetComponent<AudioListener>();
        FPAudioListener = FPCamera.GetComponent<AudioListener>();
        FPAudioListener.enabled = false;
        enabledCamera = TPCamera;
        TPUI.gameObject.SetActive(true);
        FPUI.gameObject.SetActive(false);
        sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 3.0f);
        currentSensitivity = sensitivity;
        currentX = 90;
        currentY = 0;
    }

    private void Update()
    {
        if (gameController.isPaused) return;

        if (Input.GetMouseButtonDown(1) && !characterRagdollController.isRagdollEnabled)
        {
            EnterScopeMode();
        }
        if (Input.GetMouseButtonUp(1) && scopeMode)
        {
            ExitScopeMode();
        }

        if (scopeMode)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                FPCamera.fieldOfView = FPZoomFOV;
                currentSensitivity = sensitivity / 2;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                FPCamera.fieldOfView = FPFOV;
                currentSensitivity = sensitivity;
            }
        }

        currentX += Input.GetAxis("Mouse X") * currentSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * currentSensitivity;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
        CameraRig.rotation = Quaternion.Euler(currentY, currentX, 0);

        target = playerController.isMoveAble ? transform : animator.GetBoneTransform(HumanBodyBones.Hips);
        targetPos = target.position;
    }

    private void FixedUpdate()
    {

    }

    void LateUpdate()
    {
        if (gameController.isPaused) return;

        HandleCameraRigPosition();

        if (!scopeMode)
        {
            UpdateThirdPersonCameraPosition();
        }

        UpdateTPFOV();
    }

    public void UpdateSensitivity(float newSensitivity)
    {
        sensitivity = newSensitivity;
        if (!scopeMode)
        {
            currentSensitivity = sensitivity;
        }
    }

    private void EnterScopeMode()
    {
        SetPlayerRenderers(false);
        lastLookTarget = GetAimPoint();
        scopeMode = true;
        scopeUI.SetActive(true);
        crosshairUI.SetActive(false);
        HandleCameraRigPosition();
        CorrectAimAfterTPtoFP();
        TPAudioListener.enabled = false;
        FPAudioListener.enabled = true;
        enabledCamera = FPCamera;
        TPUI.gameObject.SetActive(false);
        FPUI.gameObject.SetActive(true);
        crossHairParticle.gameObject.SetActive(true);
        StartCoroutine(HandleCrosshairParticle());
    }

    public void ExitScopeMode()
    {
        SetPlayerRenderers(true);
        // FPS 카메라가 아직 활성 카메라일 때 월드 조준점을 저장해 TPS 전환의 기준으로 삼는다.
        lastLookTarget = GetAimPoint();
        scopeMode = false;
        scopeUI.SetActive(false);
        crosshairUI.SetActive(true);
        if (playerController.isMoveAble) CorrectAimAfterFPtoTP();
        TPAudioListener.enabled = true;
        FPAudioListener.enabled = false;
        enabledCamera = TPCamera;
        TPUI.gameObject.SetActive(true);
        FPUI.gameObject.SetActive(false);
        crossHairParticle.gameObject.SetActive(false);
        FPCamera.fieldOfView = FPFOV;
        currentSensitivity = sensitivity;
    }

    private void HandleCameraRigPosition()
    {
        if (scopeMode)
        {
            CameraRig.position = transform.position + FPOffset;
        }
        else if (playerController.DevMode)
        {
            CameraRig.position = targetPos;
        }
        else
        {
            zoomFactor = Mathf.InverseLerp(-10f, -2f, TPOffset.z);
            lagDistance = Vector3.Distance(CameraRig.position, targetPos);
            boostedSmoothTime = Mathf.Max((cameraPositionSmoothTime / (1f + Mathf.Pow((lagDistance / lagBoostThreshold), lagBoostPower))), minCameraSmoothTime);
            smoothPos = Vector3.SmoothDamp(CameraRig.position, targetPos, ref currentVelocity, boostedSmoothTime);
            if (lagDistance < 0.5f)
            {
                currentVelocity *= 0.8f;
            }
            blendRatio = blendIntensityCurve.Evaluate(zoomFactor);
            CameraRig.position = Vector3.Lerp(smoothPos, targetPos, blendRatio);
        }
        UICamera.transform.position = enabledCamera.transform.position;
    }

    // TPS 카메라의 가로 → 높이 → 거리 오프셋을 순서대로 계산하며 각 구간의 충돌을 검사한다.
    // 충돌 지점에서 cameraCollisionBuffer만큼 물러나 벽과 카메라 사이에 여유를 둔다.
    private void UpdateThirdPersonCameraPosition()
    {
        TPOffset.z += Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;
        TPOffset.z = math.clamp(TPOffset.z, -10f, -2.5f);

        Vector3 desiredTPCamPosX = CameraRig.position + Quaternion.Euler(0, currentX, 0) * new Vector3(TPOffset.x + math.sign(TPOffset.x) * cameraCollisionBuffer, 0, 0);

        if (Physics.Raycast(CameraRig.position, (desiredTPCamPosX - CameraRig.position).normalized, out hit, (desiredTPCamPosX - CameraRig.position).magnitude, layerMask))
        {
            desiredTPCamPosX = hit.point - (desiredTPCamPosX - CameraRig.position).normalized * cameraCollisionBuffer;
        }
        else
        {
            desiredTPCamPosX -= (desiredTPCamPosX - CameraRig.position).normalized * cameraCollisionBuffer;
        }

        Vector3 desiredTPCamPosXY = desiredTPCamPosX + Quaternion.Euler(0, currentX, 0) * new Vector3(0, TPOffset.y + math.sign(TPOffset.y) * cameraCollisionBuffer, 0);

        if (Physics.Raycast(desiredTPCamPosX, (desiredTPCamPosXY - desiredTPCamPosX).normalized, out hit, (desiredTPCamPosXY - desiredTPCamPosX).magnitude, layerMask))
        {
            desiredTPCamPosXY = hit.point - (desiredTPCamPosXY - desiredTPCamPosX).normalized * cameraCollisionBuffer;
        }
        else
        {
            desiredTPCamPosXY -= (desiredTPCamPosXY - desiredTPCamPosX).normalized * cameraCollisionBuffer;
        }

        Vector3 desiredTPCamPosXYZ = desiredTPCamPosXY + Quaternion.Euler(currentY, currentX, 0) * new Vector3(0, 0, TPOffset.z + math.sign(TPOffset.z)  * cameraCollisionBuffer);

        if(Physics.Raycast(desiredTPCamPosXY, (desiredTPCamPosXYZ - desiredTPCamPosXY).normalized, out hit, (desiredTPCamPosXYZ - desiredTPCamPosXY).magnitude, layerMask))
        {
            desiredTPCamPosXYZ = hit.point - (desiredTPCamPosXYZ - desiredTPCamPosXY).normalized * cameraCollisionBuffer;
        }
        else
        {
            desiredTPCamPosXYZ -= (desiredTPCamPosXYZ - desiredTPCamPosXY).normalized * cameraCollisionBuffer;
        }

        TPCamRig.position = desiredTPCamPosXYZ;
    }

    private void SetPlayerRenderers(bool isEnabled)
    {
        for (int i = 0; i < renderer.Length; i++)
        {
            renderer[i].enabled = isEnabled;
        }
    }

    // 화면 중앙의 시선으로 월드 조준점을 구해, 사격 방향 계산과 손 IK에 함께 사용한다.
    // 충돌 대상이 없으면 전방 1,000m 지점을 목표로 사용한다.
    public Vector3 GetAimPoint()
    {
        if (Physics.Raycast(enabledCamera.transform.position, enabledCamera.transform.forward, out hit, 1000, layerMask))
        {
            return hit.point;
        }
        else
        {
            return enabledCamera.transform.position + enabledCamera.transform.forward * 1000f;
        }
    }

    // TPS → FPS 조준 보정: 3인칭에서 겨누던 지점을 1인칭에서 이어서 겨누게 한다.
    private void CorrectAimAfterTPtoFP()
    {
        // EnterScopeMode에서 카메라를 옮기기 전에 저장한 TPS 조준점을 사용한다.
        Vector3 directionToTarget =
            (lastLookTarget - FPCamera.transform.position).normalized;
        CameraRig.rotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

        // 다음 Update에서 이전 마우스 각도로 덮어쓰지 않도록 yaw/pitch도 역산해 갱신한다.
        Vector3 direction = CameraRig.rotation * Vector3.forward;
        currentX = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        currentY = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
    }

    // FPS → TPS 조준 보정: 카메라 위치가 달라도 1인칭에서 겨누던 지점을 향하게 한다.
    private void CorrectAimAfterFPtoTP()
    {
        CameraRig.position = target.position;

        // 회전 보정은 TPS 오프셋 위치도 바꾸므로, 위치 재계산과 방향 보정을 10회 반복한다.
        for (int i = 0; i < 10; i++)
        {
            UpdateThirdPersonCameraPosition();

            // lastLookTarget은 스코프 해제 직전 FPS 카메라로 얻은 월드 조준점이다.
            Vector3 currentCameraDirection = TPCamRig.rotation * Vector3.forward;
            Vector3 directionToTarget = (lastLookTarget - TPCamRig.position).normalized;

            // 현재 시선에서 목표 방향으로 향하는 회전 차이를 구해 카메라 리그에 적용한다.
            Quaternion errorRotation = Quaternion.FromToRotation(
                currentCameraDirection, directionToTarget);
            CameraRig.rotation = errorRotation * CameraRig.rotation;

            // 다음 Update에서 이전 마우스 각도로 덮어쓰지 않도록 yaw/pitch도 역산해 갱신한다.
            Vector3 direction = CameraRig.rotation * Vector3.forward;
            currentX = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            currentY = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
        }
    }

    private void UpdateTPFOV()
    {
        if(playerStats.currentFOV != TPCamera.fieldOfView)
        {
            TPCamera.fieldOfView = Mathf.Lerp(TPCamera.fieldOfView, playerStats.currentFOV, FOVChangeSpeed * Time.deltaTime);
            if(Mathf.Abs(playerStats.currentFOV - TPCamera.fieldOfView) < 0.1f)
            {
                TPCamera.fieldOfView = playerStats.currentFOV;
            }
        }
    }

    private IEnumerator HandleCrosshairParticle()
    {
        crossHairParticle.Stop();
        crossHairParticle.Clear();
        crossHairParticle.Play();
        yield return new WaitForSeconds(0.3f);
        crossHairParticle.Pause();
    }
}
