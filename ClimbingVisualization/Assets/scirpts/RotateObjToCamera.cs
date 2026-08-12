using UnityEngine;

public class RotateObjToCamera : MonoBehaviour
{
    // Wir lassen das Feld hier, falls du im Ausnahmefall eine andere Cam willst
    private Camera targetCamera;
    [SerializeField] private float fixedZ = 45f;

    void Awake()
    {
        // Automatisch Main Camera suchen, falls noch keine zugewiesen ist
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        // Falls Camera.main in manchen Szenen mal kurz null ist (oder nachgeladen wird)
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return; // Abbruch, falls wirklich keine Kamera existiert
        }

        // Richtung von Kamera zum Objekt
        Vector3 direction = transform.position - targetCamera.transform.position;
        Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);

        // X/Y aus der Camera-Orientierung, Z = fixedZ
        transform.rotation = Quaternion.Euler(
            lookRot.eulerAngles.x,
            lookRot.eulerAngles.y,
            fixedZ
        );
    }
}