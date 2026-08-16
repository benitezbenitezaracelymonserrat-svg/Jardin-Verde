using UnityEngine;

/// <summary>
/// Mantiene las interfaces del mundo orientadas hacia la camara.
/// </summary>
public class BillboardNivel3 : MonoBehaviour
{
    private Camera camara;
    private float proximaRevisionCamara;
    private static Camera[] bufferCamaras = new Camera[8];

    private void LateUpdate()
    {
        // Usa la camara activa que realmente dibuja por encima de las demas.
        // En el nivel 3 esa es la camara fija de ventas, no Camera.main.
        if (camara == null || !camara.isActiveAndEnabled ||
            Time.unscaledTime >= proximaRevisionCamara)
        {
            int cantidadCamaras = Camera.allCamerasCount;
            if (cantidadCamaras > bufferCamaras.Length)
                bufferCamaras = new Camera[cantidadCamaras];

            int cantidadCopiada = Camera.GetAllCameras(bufferCamaras);
            Camera mejorCamara = null;
            for (int i = 0; i < cantidadCopiada; i++)
            {
                Camera candidata = bufferCamaras[i];
                if (candidata == null || !candidata.isActiveAndEnabled)
                    continue;

                if (mejorCamara == null || candidata.depth > mejorCamara.depth)
                    mejorCamara = candidata;
            }

            camara = mejorCamara != null ? mejorCamara : Camera.main;
            proximaRevisionCamara = Time.unscaledTime + 0.5f;
        }

        if (camara == null)
            return;

        transform.rotation = Quaternion.LookRotation(
            transform.position - camara.transform.position,
            Vector3.up
        );
    }
}
