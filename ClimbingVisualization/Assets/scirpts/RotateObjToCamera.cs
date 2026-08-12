using UnityEngine;

public class RotateObjToCamera : MonoBehaviour
{
    private Camera targetCamera;
    [SerializeField] private float fixedZ = 45f;

    void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        Vector3 direction = transform.position - targetCamera.transform.position;
        Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);

        // Z fest statt aus lookRot, damit das Schild nicht mit der Kamera kippt
        transform.rotation = Quaternion.Euler(
            lookRot.eulerAngles.x,
            lookRot.eulerAngles.y,
            fixedZ
        );
    }
}