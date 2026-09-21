using UnityEngine;

public class DamageCameraShake : MonoBehaviour
{
    // 싱글톤 패턴 (어디서든 쉽게 접근하기 위함, 선택사항)
    public static DamageCameraShake Instance { get; private set; }

    [Header("Default Settings")]
    public float defaultDuration = 0.2f;  // 기본 흔들림 지속 시간 (짧고 굵게)
    public float defaultMagnitude = 0.3f; // 기본 흔들림 강도

    // 내부 변수
    private float shakeTimer = 0f;
    private float currentMagnitude = 0f;
    private Vector3 initialLocalPos; // 흔들리기 전의 원래 로컬 위치 (보통 0,0,0)

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 시작 시 로컬 위치 저장 (부모인 SprintPivot 기준의 위치)
        initialLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (shakeTimer > 0)
        {
            // 타이머 감소
            shakeTimer -= Time.deltaTime;

            // 1. 불규칙한 랜덤 위치 생성 (원 안의 랜덤 좌표)
            // insideUnitCircle은 반경 1의 원 안의 랜덤한 (x,y)값을 줍니다.
            Vector2 randomPoint = Random.insideUnitCircle * currentMagnitude;

            // 2. Z축(깊이)은 흔들지 않고 X, Y만 흔듦 (3인칭에 더 적합)
            Vector3 shakeOffset = new Vector3(randomPoint.x, randomPoint.y, 0);

            // 3. 적용 (원래 위치 + 랜덤 오프셋)
            transform.localPosition = initialLocalPos + shakeOffset;

            // 시간 경과에 따라 강도를 부드럽게 줄임 (선택 사항)
            // currentMagnitude = Mathf.Lerp(currentMagnitude, 0f, Time.deltaTime * 5f);
        }
        else if (transform.localPosition != initialLocalPos)
        {
            // 흔들림이 끝나면 정확히 원래 위치로 복귀
            shakeTimer = 0f;
            transform.localPosition = initialLocalPos;
        }
    }

    // ★ 외부에서 호출하는 함수 ★
    // 약한 공격 맞으면: TriggerShake(0.1f, 0.1f);
    // 강한 공격 맞으면: TriggerShake(0.4f, 0.5f);
    public void TriggerShake(float duration, float magnitude)
    {
        shakeTimer = duration;
        currentMagnitude = magnitude;
        // 맞자마자 바로 초기 위치 갱신 (혹시 모를 틀어짐 방지)
        initialLocalPos = Vector3.zero;
    }

    // 매개변수 없이 호출하면 기본값 사용
    public void TriggerShake()
    {
        TriggerShake(defaultDuration, defaultMagnitude);
    }
}