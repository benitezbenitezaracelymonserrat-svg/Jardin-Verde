using UnityEngine;

/// <summary>
/// Poner en un GameObject con un Collider grande (Is Trigger) que
/// envuelva TODAS las parcelas de la huerta. Al entrar el jugador,
/// cambia a primera persona. Al salir, vuelve a tercera.
/// </summary>
public class ZonaHuerta : MonoBehaviour
{
    public ControlCamara controlCamara;
    public string tagJugador = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador) && controlCamara != null)
            controlCamara.CambiarACamara(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(tagJugador) && controlCamara != null)
            controlCamara.CambiarACamara(false);
    }
}