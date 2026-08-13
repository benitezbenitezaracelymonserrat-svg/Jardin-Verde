using UnityEngine;
using Unity.Cinemachine;

public class ControlCamara : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    public CinemachineCamera camaraTercera;
    public CinemachineCamera camaraPrimera;

    [Header("Prioridades")]
    public int prioridadActiva = 20;
    public int prioridadInactiva = 10;

    [Header("Script de giro (para pausarlo en primera persona)")]
    public RotacionCamaraTercera rotacionTercera;

    void Start()
    {
        CambiarACamara(false);
    }

    public void CambiarACamara(bool primera)
    {
        if (camaraTercera == null || camaraPrimera == null) return;

        camaraPrimera.Priority = primera ? prioridadActiva : prioridadInactiva;
        camaraTercera.Priority = primera ? prioridadInactiva : prioridadActiva;

        // Mientras estás en primera persona, pausamos el script que lee el
        // mouse para la órbita de tercera persona, así no se acumula
        // movimiento que después "explota" al volver a tercera.
        if (rotacionTercera != null)
            rotacionTercera.enabled = !primera;
    }
}