using UnityEngine;

/// <summary>
/// 애니메이션 이벤트가 AudioManager의 사운드를 재생할 수 있도록 중계(프록시)합니다.
/// 이 스크립트는 반드시 Animator 컴포넌트와 같은 게임 오브젝트에 있어야 합니다.
/// </summary>
public class AudioProxy : MonoBehaviour
{
    PlayerController playerController;
    PlayerStats playerStats;

    // --- (추가) 발소리 이벤트 중복 실행 방지 (Cooldown / Debounce) ---

    /// <summary>
    /// 발소리 이벤트가 최소 이 시간(초) 간격으로만 실행되도록 강제합니다.
    /// (블렌드 트리에서 이벤트가 중복 실행되는 것을 방지)
    /// </summary>

    [Header("착지 사운드 설정")]
    [SerializeField]
    [Tooltip("이 속도(절댓값) 미만이면 착지 사운드가 재생되지 않습니다.")]
    private float minFallSpeedForSound = 5f;

    [SerializeField]
    [Tooltip("이 속도(절댓값) 이상이면 착지 사운드가 최대 볼륨으로 재생됩니다.")]
    private float maxFallSpeedForFullVolume = 30f;

    [SerializeField]
    private float minFallVolumeScale = 0.5f;

    [SerializeField]
    private float footstepCooldown = 0.26375f;
    private float footstepCooldownTimer = 0f;

    // 마지막으로 발소리가 재생된 시간을 기록
    private float lastFootstepTime = 0f;

    // --- (이전과 동일) ---
    private bool hasWarned = false;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerStats = GetComponentInParent<PlayerStats>();
    }

    private bool CheckAudioManager()
    {
        if (AudioManager.Instance != null)
        {
            return true;
        }

        if (!hasWarned)
        {
            Debug.LogWarning("AnimationEventAudioProxy: AudioManager.Instance가 null입니다. " +
                             "인트로 씬(AudioManager가 있는 씬)에서 시작했는지 확인하세요.", this);
            hasWarned = true;
        }
        return false;
    }

    // --- (이전과 동일한 함수들) ---

    public void PlayButtonClickSound(float volumeScale = 1)
    {
        if (CheckAudioManager())
            AudioManager.Instance.PlayButtonClickSound(volumeScale);
    }

    // --- ⬇⬇⬇ 여기가 수정되었습니다 ⬇⬇⬇ ---

    // AudioProxy.cs - PlayFootstepSound 메서드 내부

    public void PlayFootstepSound(float volumeScale = 1)
    {
        float speedRatio = 1f;

        if (playerStats != null && playerStats.baseSpeed > 0 && playerStats.currentSpeed > 0)
        {
            speedRatio = playerStats.currentSpeed / playerStats.baseSpeed;
        }

        footstepCooldownTimer = footstepCooldown / Mathf.Max(0.01f, speedRatio);

        if (Time.time - lastFootstepTime < footstepCooldownTimer || !playerController.isGrounded)
        {
            return;
        }

        lastFootstepTime = Time.time;

        if (CheckAudioManager())
        {
            AudioManager.Instance.PlayFootstepSound(volumeScale);
        }
    }

    public void PlayLandingSound(float volumeScale = 1)
    {
        if(playerController.currentSpeedVector.y > -minFallSpeedForSound)
            return;

        lastFootstepTime = Time.time;
        footstepCooldownTimer = 0.05f;

        if (CheckAudioManager())
            AudioManager.Instance.PlayLandingSound(Mathf.Lerp(minFallVolumeScale, 1f, Mathf.InverseLerp(minFallSpeedForSound, maxFallSpeedForFullVolume, Mathf.Abs(playerController.currentSpeedVector.y))) * volumeScale);
    }

    public void PlayJumpSound(float volumeScale = 1)
    {
        if (CheckAudioManager())
            AudioManager.Instance.PlayJumpSound(volumeScale);
    }

    public void PlayGunSound(float volumeScale = 1)
    {
        if (CheckAudioManager())
            AudioManager.Instance.PlayGunSound(volumeScale);
    }

    public void PlayHurtSound(float volumeScale = 1)
    {
        if (CheckAudioManager())
            AudioManager.Instance.PlayHurtSound(volumeScale);
    }

    public void PlayHealSound(float volumeScale = 1)
    {
        if (CheckAudioManager())
            AudioManager.Instance.PlayHealSound(volumeScale);
    }

    public void PlayDieSound(float volumeScale = 1)
    {
        if (CheckAudioManager())
            AudioManager.Instance.PlayDieSound(volumeScale);
    }

    public void PlayExplosionSound(float volumeScale = 1)
    {
        if (CheckAudioManager())
            AudioManager.Instance.PlayExplosionSound(volumeScale);
    }

    public void PlayHeartbeatSound(float volumeScale = 1)
    {
        if (CheckAudioManager())
            AudioManager.Instance.PlayHeartbeatSound(volumeScale);
    }
}