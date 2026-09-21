using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerHP : MonoBehaviour
{
    [SerializeField]
    private PlayerHPUI playerHPUI;
    private CharacterRagdollController ragdollController;
    private AudioProxy audioProxy;
    private PlayerAnimator playerAnimator;

    // NOTE: DamageCameraShake.Instance와 CharacterRagdollController, PlayerHPUI 등은
    // 외부 클래스로 이 코드에는 포함되어 있지 않습니다.

    [SerializeField]
    private GameObject hitEffect;

    [Header("HP Settings")]
    [SerializeField]
    private int maxHp = 3;
    private int currentHp;

    [Header("Invincibility Settings")]
    [SerializeField]
    private float invincibilityDuration = 2.0f;
    private float invincibilityTimer = 0.0f;
    [SerializeField]
    private float flashSpeed = 0.1f; // 깜빡이는 속도 (모델 깜빡임에 사용)

    [Header("Visual Feedback")]
    [SerializeField]
    private SkinnedMeshRenderer[] meshRenderers; // 플레이어의 렌더러들을 할당
    private List<Color> originalColors = new List<Color>(); // 원래 색상 저장용

    // --- [Vignette 변수 추가] ---
    [Header("Damage Vignette Settings")]
    [SerializeField]
    private Q_Vignette_Single damageVignette; // Q_Vignette_Single 컴포넌트를 인스펙터에 할당해야 함
    [SerializeField]
    [Tooltip("피해 시 Vignette의 최대 색상과 투명도(Alpha)")]
    private Color damageColor = new Color(1f, 0f, 0f, 0.6f); // 피해를 입었을 때 사용할 빨간색 (알파 0.6)
    [SerializeField]
    private float vignetteFadeInTime = 0.1f; // Vignette가 붉게 나타나는 시간 (빠르게)
    // --- [Vignette 변수 추가 끝] ---

    [SerializeField]
    private float reviveDelay = 3.0f;
    private bool isDead;
    public bool IsDead
    {
        get => isDead;
        set => isDead = value;
    }

    private void Awake()
    {
        ragdollController = GetComponent<CharacterRagdollController>();
        audioProxy = GetComponentInChildren<AudioProxy>();
        playerAnimator = GetComponent<PlayerAnimator>();
        if (hitEffect != null) hitEffect.SetActive(false);

        if (meshRenderers == null || meshRenderers.Length == 0)
        {
            meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        // 원래 색상들 저장 (나중에 복구하기 위해)
        foreach (var renderer in meshRenderers)
        {
            // _Color 속성을 가진 셰이더인 경우 해당 색상을 저장
            if (renderer.material.HasProperty("_Color"))
            {
                originalColors.Add(renderer.material.GetColor("_Color"));
            }
            else
            {
                originalColors.Add(renderer.material.color);
            }
        }

        currentHp = maxHp;
        isDead = false;

        // Vignette 초기화: 시작 시 투명하게 설정
        if (damageVignette != null)
        {
            damageVignette.mainColor = new Color(damageColor.r, damageColor.g, damageColor.b, 0f);
        }
    }

    private void Update()
    {
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isDead)
            {
                Die();
            }
        }
    }

    public void TakeDamage(int damage, Vector3? hitPoint = null)
    {
        if (isDead || invincibilityTimer > 0)
            return;

        currentHp -= damage;
        if (playerHPUI != null) playerHPUI.UpdateHPUI(currentHp);

        if (DamageCameraShake.Instance != null) DamageCameraShake.Instance.TriggerShake();

        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            invincibilityTimer = invincibilityDuration;

            // 1. 플레이어 모델 깜빡임 (기존 로직)
            StartCoroutine(FlashRedEffect());

            // 2. 비네팅 페이드 인/아웃 효과
            if (damageVignette != null)
            {
                // 기존 코루틴 중지 및 재시작하여 겹치는 것을 방지
                StopCoroutine(FadeDamageVignette());
                StartCoroutine(FadeDamageVignette());
            }

            playerAnimator.OnDamaged();
            audioProxy.PlayHurtSound();
            if (currentHp == 1) audioProxy.PlayHeartbeatSound();

            // Hit Effect 위치 설정 (기존 로직)
            if (hitEffect != null)
            {
                if (hitPoint.HasValue)
                {
                    hitEffect.transform.position = hitPoint.Value;
                }
                else
                {
                    hitEffect.transform.position = transform.position;
                }
                hitEffect.SetActive(true);
            }
        }
    }

    // [모델 색상 깜빡임 코루틴 (기존 로직)]
    private IEnumerator FlashRedEffect()
    {
        while (invincibilityTimer > 0 && !isDead)
        {
            foreach (var renderer in meshRenderers)
            {
                // 1. 빨간색으로 변경
                renderer.material.color = Color.red;
            }
            yield return new WaitForSeconds(flashSpeed);

            // 2. 원래 색상으로 복구
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (i < originalColors.Count)
                {
                    meshRenderers[i].material.color = originalColors[i];
                }
            }
            yield return new WaitForSeconds(flashSpeed);
        }
        ResetColor();
    }

    // [Vignette 투명도 조절 코루틴 (선형 보간)]
    private IEnumerator FadeDamageVignette()
    {
        // 남은 무적 시간만큼 페이드 아웃에 사용 (짧은 페이드 인 시간을 제외한 나머지)
        float fadeOutTime = invincibilityDuration - vignetteFadeInTime;

        // 현재 Vignette 색상에서 시작 (대부분 투명한 상태)
        Color startColor = damageVignette.mainColor;
        // 최종적으로 도달할 불투명한 빨간색
        Color targetColor = damageColor;

        // 1. **페이드 인 (투명도 증가)**
        float t = 0f;
        while (t < vignetteFadeInTime)
        {
            t += Time.deltaTime;
            float lerpFactor = t / vignetteFadeInTime;

            // Color.Lerp를 사용하여 알파 값(투명도)을 부드럽게 증가시킴
            damageVignette.mainColor = Color.Lerp(startColor, targetColor, lerpFactor);
            yield return null;
        }
        damageVignette.mainColor = targetColor; // 최종 색상 확정


        // 2. **페이드 아웃 (투명도 감소)**
        Color fadeOutStartColor = targetColor;
        // 최종적으로 도달할 완전히 투명한 색상 (알파 = 0)
        Color fadeOutTargetColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);

        t = 0f;
        // 남은 무적 시간 동안 페이드 아웃
        while (t < fadeOutTime && invincibilityTimer > 0)
        {
            t += Time.deltaTime;
            // invincibilityTimer가 0에 가까워질수록 lerpFactor는 0에서 1로 이동
            float lerpFactor = t / fadeOutTime;

            // 불투명한 빨간색에서 투명한 빨간색으로 보간
            damageVignette.mainColor = Color.Lerp(fadeOutStartColor, fadeOutTargetColor, lerpFactor);
            yield return null;
        }

        // 코루틴 종료 시 확실하게 투명도 0으로 설정
        damageVignette.mainColor = fadeOutTargetColor;
    }

    private void ResetColor()
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (i < originalColors.Count)
            {
                meshRenderers[i].material.color = originalColors[i];
            }
        }
    }

    private void Die()
    {
        isDead = true;
        ResetColor();

        // 죽었을 때 비네팅 효과도 제거
        if (damageVignette != null)
        {
            StopCoroutine(FadeDamageVignette());
            // 현재 색상 유지하면서 알파만 0으로 설정
            damageVignette.mainColor = new Color(damageVignette.mainColor.r, damageVignette.mainColor.g, damageVignette.mainColor.b, 0f);
        }

        ragdollController.EnableRagdoll();
        Vector3 randomExplosionPosition = transform.position + new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        );

        ragdollController.hipsBone.GetComponent<Rigidbody>().AddExplosionForce(500f, randomExplosionPosition, 2f, 0.85f, ForceMode.Impulse);

        StartCoroutine(Revive());
    }

    private IEnumerator Revive()
    {
        yield return new WaitForSeconds(reviveDelay);
        currentHp = maxHp;
        if (playerHPUI != null) playerHPUI.UpdateHPUI(currentHp);
        isDead = false;
        ragdollController.DisableRagdoll();
    }
}