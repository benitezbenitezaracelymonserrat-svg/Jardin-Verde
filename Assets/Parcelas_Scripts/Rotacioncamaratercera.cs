using UnityEngine;
using Unity.Cinemachine;

public class RotacionCamaraTercera : MonoBehaviour
{
    public CinemachineOrbitalFollow orbitalFollow;
    public float sensibilidad = 200f;
    public bool bloquearCursor = true;

    void Start()
    {
        if (bloquearCursor)
            Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (orbitalFollow == null) return;

        // Solo eje horizontal (izquierda/derecha). Se quitó el vertical (arriba/abajo).
        float mouseX = Input.GetAxis("Mouse X") * sensibilidad * Time.deltaTime;
        orbitalFollow.HorizontalAxis.Value += mouseX;
    }
}