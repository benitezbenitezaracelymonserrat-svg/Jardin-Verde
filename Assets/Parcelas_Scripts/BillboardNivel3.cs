using UnityEngine;

/// <summary>
/// Mantiene las interfaces del mundo orientadas hacia la camara.
/// </summary>
public class BillboardNivel3 : MonoBehaviour
{
    private Camera camara;

    private void LateUpdate()
    {
        if (camara == null)
            camara = Camera.main;

        if (camara == null)
            return;

        transform.rotation = Quaternion.LookRotation(
            transform.position - camara.transform.position,
            Vector3.up
        );
    }
}
