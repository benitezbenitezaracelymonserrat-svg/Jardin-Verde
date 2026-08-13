using UnityEngine;

[DisallowMultipleComponent]
public class BaldeLecheRecolectable : MonoBehaviour
{
    private string tipoProducto;
    private bool disponible;
    private bool recogido;

    public bool EstaDisponible => disponible && !recogido;

    public void Configurar(string tipo)
    {
        tipoProducto = tipo;
        disponible = false;
        recogido = false;

        // Garantiza que el jugador pueda detectar el balde aunque el modelo
        // importado no traiga collider propio.
        if (GetComponentInChildren<Collider>(true) == null)
        {
            BoxCollider zonaRecogida = gameObject.AddComponent<BoxCollider>();
            zonaRecogida.isTrigger = true;
            zonaRecogida.center = new Vector3(0f, 0.25f, 0f);
            zonaRecogida.size = new Vector3(0.75f, 0.65f, 0.75f);
        }

        gameObject.SetActive(false);
    }

    public void MostrarEn(Transform puntoBalde)
    {
        if (puntoBalde != null)
        {
            transform.SetPositionAndRotation(
                puntoBalde.position,
                puntoBalde.rotation
            );
        }

        recogido = false;
        disponible = true;
        gameObject.SetActive(true);
    }

    public bool Recoger(InventarioProductos inventario)
    {
        if (!EstaDisponible || string.IsNullOrWhiteSpace(tipoProducto))
            return false;

        if (inventario == null)
            inventario = InventarioProductos.BuscarPrincipal();

        if (inventario == null)
        {
            Debug.LogWarning("No se encontro el inventario para recoger leche.");
            return false;
        }

        disponible = false;
        recogido = true;
        inventario.AgregarProducto(tipoProducto, 1);
        GestorNivel2.RegistrarProductoGlobal(tipoProducto, 1);
        gameObject.SetActive(false);

        Debug.Log($"Recogiste {tipoProducto} x1");
        return true;
    }
}
