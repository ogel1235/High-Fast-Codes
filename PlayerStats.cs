using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float baseSpeed = 7.0f;
    [HideInInspector]
    public float currentSpeed;

    public float baseFOV = 85;
    [HideInInspector]
    public float currentFOV;

    [Header("VFX Settings")]
    [Tooltip("전체 화면 효과를 켜고 끌 게임오브젝트 (예: FS_Speed)")]
    [SerializeField]
    private GameObject speedEffectObject;

    [Tooltip("프로젝트 창에서 FS_Speed 머터리얼을 여기에 직접 드래그해서 넣으세요.")]
    [SerializeField]
    private Material speedEffectMaterial;

    // 쉐이더 제어용 내부 변수
    private int maskSizePropertyID;
    private string maskSizeReferenceName = "_Mask_Size"; // 쉐이더 그래프의 Reference 이름과 일치해야 함
    private float originalMaskSize; // 게임 시작 전 원래 마스크 크기 저장용

    // 버프 시스템 관련
    private List<ActiveBuffInstance> activeBuffs = new List<ActiveBuffInstance>();

    private class ActiveBuffInstance
    {
        public BuffData Buff { get; }
        public Coroutine TimerCoroutine { get; }

        public ActiveBuffInstance(BuffData buff, Coroutine timer)
        {
            Buff = buff;
            TimerCoroutine = timer;
        }
    }

    void Start()
    {
        // 1. 초기 스탯 설정
        currentSpeed = baseSpeed;
        currentFOV = baseFOV;

        // 2. Material 유효성 검사 및 초기화
        if (speedEffectMaterial == null)
        {
            Debug.LogError("[PlayerStats] 오류: 인스펙터의 'Speed Effect Material'에 재질이 할당되지 않았습니다!");
        }
        else
        {
            // ID 변환 (최적화)
            maskSizePropertyID = Shader.PropertyToID(maskSizeReferenceName);

            // 프로퍼티 존재 확인 및 백업
            if (speedEffectMaterial.HasProperty(maskSizePropertyID))
            {
                originalMaskSize = speedEffectMaterial.GetFloat(maskSizePropertyID);
                Debug.Log($"[PlayerStats] Material 로드 성공. 초기 Mask Size: {originalMaskSize}");
            }
            else
            {
                Debug.LogError($"[PlayerStats] 오류: 쉐이더에서 '{maskSizeReferenceName}' 프로퍼티를 찾을 수 없습니다.");
            }
        }

        // 3. 초기 스탯 계산 및 효과 끄기
        CalculateStats();
        if (speedEffectObject != null)
            speedEffectObject.SetActive(false);
    }

    // [중요] 게임 종료 시(혹은 오브젝트 파괴 시) 재질 값을 원래대로 복구
    void OnDestroy()
    {
        if (speedEffectMaterial != null && speedEffectMaterial.HasProperty(maskSizePropertyID))
        {
            speedEffectMaterial.SetFloat(maskSizePropertyID, originalMaskSize);
            // Debug.Log("Mask Size 복구 완료");
        }
    }

    // --- 버프 시스템 로직 ---

    public void ApplyBuff(BuffData buffToApply)
    {
        Coroutine buffTimer = StartCoroutine(BuffTimer(buffToApply));
        ActiveBuffInstance newBuffInstance = new ActiveBuffInstance(buffToApply, buffTimer);
        activeBuffs.Add(newBuffInstance);
        CalculateStats();
    }

    private IEnumerator BuffTimer(BuffData buff)
    {
        yield return new WaitForSeconds(buff.duration);

        ActiveBuffInstance instanceToRemove = activeBuffs.Find(b => b.Buff == buff);
        if (instanceToRemove != null)
        {
            activeBuffs.Remove(instanceToRemove);
            CalculateStats();
        }
    }

    // --- 스탯 계산 및 VFX 제어 ---

    private void CalculateStats()
    {
        // 1. 스탯 초기화
        currentSpeed = baseSpeed;
        currentFOV = baseFOV;

        // 2. 최고 효율 버프 선정 (Type 4 로직)
        Dictionary<StatType, BuffData> bestBuffs = new Dictionary<StatType, BuffData>();

        foreach (ActiveBuffInstance instance in activeBuffs)
        {
            BuffData buff = instance.Buff;
            StatType stat = buff.statToModify;

            if (!bestBuffs.ContainsKey(stat))
            {
                bestBuffs.Add(stat, buff);
            }
            else
            {
                if (buff.value > bestBuffs[stat].value)
                {
                    bestBuffs[stat] = buff;
                }
            }
        }

        // 3. 스탯 최종 적용
        foreach (BuffData bestBuff in bestBuffs.Values)
        {
            if (bestBuff.statToModify == StatType.Speed)
            {
                if (bestBuff.isMultiplier) currentSpeed *= bestBuff.value;
                else currentSpeed += bestBuff.value;

                currentFOV = baseFOV + (currentSpeed - baseSpeed) * 3;
            }
            // 공격력 등 다른 스탯 로직은 여기에 추가
        }

        // 4. VFX (시각 효과) 제어 로직
        UpdateSpeedEffect();
    }

    private void UpdateSpeedEffect()
    {
        bool shouldShowEffect = currentSpeed > baseSpeed;

        // (1) 오브젝트 활성화/비활성화
        if (speedEffectObject != null && speedEffectObject.activeSelf != shouldShowEffect)
        {
            speedEffectObject.SetActive(shouldShowEffect);
        }

        // (2) Material 수치(Mask Size) 동적 제어
        if (shouldShowEffect && speedEffectMaterial != null)
        {
            // 속도가 빠를수록 마스크 크기를 줄임 (7 -> 14로 갈 때 0.56 -> 0.36 등)
            // 수치 조정 팁: 0.03f 값을 키우면 속도 변화에 더 민감하게 반응합니다.
            float speedDiff = currentSpeed - baseSpeed;
            float newMaskSize = originalMaskSize - (speedDiff * 0.03f);

            // 0.1 ~ 1.0 사이로 값 제한 (화면이 너무 이상해지는 것 방지)
            newMaskSize = Mathf.Clamp(newMaskSize, 0.1f, 1.0f);

            speedEffectMaterial.SetFloat(maskSizePropertyID, newMaskSize);
        }
    }
}