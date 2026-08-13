using UnityEngine;

/// <summary>
/// Habilita la venta unicamente cuando el granjero esta frente al puesto.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaAtencionVentasNivel3 : MonoBehaviour
{
    private int jugadoresDentro;

    private void OnTriggerEnter(Collider otro)
    {
        if (otro.GetComponentInParent<PlayerController>() == null)
            return;

        jugadoresDentro++;
        GestorNivel3.NotificarJugadorEnZona(true);
    }

    private void OnTriggerExit(Collider otro)
    {
        if (otro.GetComponentInParent<PlayerController>() == null)
            return;

        jugadoresDentro = Mathf.Max(0, jugadoresDentro - 1);

        if (jugadoresDentro == 0)
            GestorNivel3.NotificarJugadorEnZona(false);
    }

    private void OnDisable()
    {
        jugadoresDentro = 0;
        GestorNivel3.NotificarJugadorEnZona(false);
    }
}
