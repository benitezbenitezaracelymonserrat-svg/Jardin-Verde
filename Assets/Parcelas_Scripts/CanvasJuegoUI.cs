using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Localiza el Canvas principal que recibe eventos de puntero.
/// Evita que los botones dinámicos se creen dentro de Canvas decorativos,
/// como el de la barra de progreso, que no tienen GraphicRaycaster.
/// </summary>
public static class CanvasJuegoUI
{
    public static Canvas BuscarInteractivo()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Canvas alternativa = null;
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.GetComponent<GraphicRaycaster>() == null)
                continue;

            if (canvas.name == "Canvas")
                return canvas;

            if (canvas.name != "CanvasBarraProgreso" && alternativa == null)
                alternativa = canvas;
        }

        return alternativa;
    }
}
