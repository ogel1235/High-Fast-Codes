using UnityEngine;

public class CharacterRootMotionHandler : MonoBehaviour
{
    private Animator animator;
    private Rigidbody parentRb;

    // 이 변수는 루트 오브젝트의 Rigidbody에 접근하기 위해 사용됩니다.
    // Inspector에서 루트 오브젝트의 Rigidbody를 직접 할당하거나, 
    // Start 함수에서 부모 오브젝트의 Rigidbody를 찾습니다.

    void Start()
    {
        animator = GetComponent<Animator>();
        // 부모 오브젝트에서 Rigidbody 컴포넌트를 가져옵니다.
        parentRb = GetComponentInParent<Rigidbody>();

        if (parentRb == null)
        {
            Debug.LogError("부모 오브젝트에 Rigidbody가 없습니다!");
        }
    }

    // Animator가 부착된 오브젝트에서 호출됩니다.
    private void OnAnimatorMove()
    {
        if (parentRb == null) return;

        // 1. 애니메이션이 이동시키려는 벡터를 추출합니다.
        Vector3 deltaPosition = animator.deltaPosition;
        Quaternion deltaRotation = animator.deltaRotation;

        // 2. 부모 Rigidbody의 현재 위치에 애니메이션 이동 값을 더합니다.
        Vector3 nextPosition = parentRb.position + deltaPosition;

        // 3. Rigidbody.MovePosition으로 부모(플레이어)를 이동시킵니다.
        // 이렇게 해야 물리 엔진이 충돌 감지를 담당하게 됩니다.
        parentRb.MovePosition(nextPosition);

        // 4. 회전은 자식 오브젝트가 아닌 부모 오브젝트의 회전에 적용합니다.
        Quaternion nextRotation = parentRb.rotation * deltaRotation;
        parentRb.MoveRotation(nextRotation);
    }
}