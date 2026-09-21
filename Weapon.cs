using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class Weapon : MonoBehaviour
{
    [SerializeField]
    private GameController gameController;

    private PlayerIKController playerIKController;
    private CameraController cameraController;
    private PlayerController playerController;
    private PlayerAnimator playerAnimator;
    private AudioProxy audioProxy;
    [SerializeField]
    private LineRenderer laserLineRenderer;
    [SerializeField]
    private float laserLineDuration = 0.05f;

    [SerializeField]
    private float roundPerMinute = 180;
    [SerializeField]
    private int damage = 1;
    [SerializeField]
    private float accuracy = 100;
    [SerializeField]
    private float range = 1000f;
    [SerializeField]
    private Transform FPBulletPoint;
    [SerializeField]
    private Transform TPBulletPoint;
    private Transform muzzle
    {
        get
        {
            if (cameraController.ScopeMode)
            {
                return FPBulletPoint;
            }
            else
            {
                return TPBulletPoint;
            }
        }
    }
    private float fireCooldown = 0f;
    [SerializeField]
    private GameObject hitEffect;
    [SerializeField]
    private LayerMask layerMask;

    private bool fired;

    RaycastHit hit;

    void Awake()
    {
        layerMask = ~LayerMask.GetMask("Player");
        cameraController = GetComponent<CameraController>();
        playerController = GetComponent<PlayerController>();
        playerIKController = GetComponentInChildren<PlayerIKController>();
        audioProxy = GetComponentInChildren<AudioProxy>();
        playerAnimator = GetComponent<PlayerAnimator>();
        fired = false;
    }

    private void Update()
    {
        if (gameController.isPaused) return;

        if (!playerController.isMoveAble) return;

        if (Input.GetMouseButton(0) && fireCooldown <= 0f)
        {
            fired = true;
            fireCooldown = 60f / roundPerMinute;
        }
        else
        {
            fireCooldown -= Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        if (fired)
        {
            Fire();
            fired = false;
        }
    }

    // 카메라는 조준할 월드 지점을 정하고, 총구는 실제 사격 판정의 출발점이 된다.
    // 조준 모드에 따라 선택된 FPS/TPS 총구에서 같은 조준점을 향하도록 방향을 계산한다.
    private void Fire()
    {
        Vector3 direction =
            (cameraController.GetAimPoint() - muzzle.position).normalized;

        // 화면의 가로·세로 축을 기준으로 탄 퍼짐을 더한 뒤 방향을 다시 정규화한다.
        float spread = (100f - accuracy) * 0.001f;
        direction += cameraController.enabledCamera.transform.right * Random.Range(-spread, spread);
        direction += cameraController.enabledCamera.transform.up * Random.Range(-spread, spread);
        direction.Normalize();

        Vector3 hitPos = muzzle.position + direction * range;
        RaycastHit hit;

        // 총구에서 검사하므로 조준점까지의 경로에 있는 장애물도 명중 판정에 반영한다.
        if (Physics.Raycast(muzzle.position, direction, out hit, range, layerMask))
        {
            hitPos = hit.point;
            if(hit.transform.GetComponent<BreakableWallAndTrigger>() != null)
            {
                hit.transform.GetComponent<BreakableWallAndTrigger>().Damage(damage);
            }
            if(hit.transform.GetComponent<JumpButton>() != null)
            {
                hit.transform.GetComponent<JumpButton>().Damage(gameObject);
            }
        }

        // 한 번의 발사에 손 IK 반동, 사격 애니메이션, 효과음과 탄 궤적을 연결한다.
        playerIKController.OnShoot();
        playerAnimator.OnShoot();
        audioProxy.PlayGunSound();
        LineRenderer laserLine = Instantiate(laserLineRenderer);
        Vector3 muzzlePos = muzzle.position;
        laserLine.SetPosition(0, muzzlePos);
        laserLine.SetPosition(1, hitPos);
        StartCoroutine(DestroyLaserLine(laserLine));
        GameObject.Instantiate(hitEffect, hitPos, Quaternion.identity);
    }

    private IEnumerator DestroyLaserLine(LineRenderer laserLine)
    {
        yield return new WaitForSeconds(laserLineDuration);
        Destroy(laserLine.gameObject);
    }
}
