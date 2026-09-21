using UnityEngine;

public class CheckCollision : MonoBehaviour
{
    public bool hasCollided = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (gameObject.layer == collision.gameObject.layer)
        {
            return;
        }

        if (!hasCollided)
        {
            hasCollided = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        hasCollided = false;
    }
}
