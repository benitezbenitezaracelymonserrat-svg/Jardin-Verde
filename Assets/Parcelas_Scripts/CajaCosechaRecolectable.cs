using UnityEngine;

[DisallowMultipleComponent]
public class CajaCosechaRecolectable : MonoBehaviour
{
    public string tipoProducto;
    public int cantidad = 1;

    private bool recogida;
    private ZonaCosechaNivel2 zonaCosecha;

    public ZonaCosechaNivel2 ZonaCosecha => zonaCosecha;

    public bool EstaDisponible =>
        !recogida &&
        gameObject.activeInHierarchy &&
        !string.IsNullOrWhiteSpace(tipoProducto) &&
        (zonaCosecha == null || zonaCosecha.EstaListaParaRecoger);

    public static void PrepararArray(GameObject[] cajas)
    {
        if (cajas == null)
            return;

        foreach (GameObject caja in cajas)
        {
            if (caja != null)
                caja.SetActive(false);
        }
    }

    public static CajaCosechaRecolectable MostrarSiguiente(
        GameObject[] cajas,
        string producto,
        int cantidadProducto = 1,
        ZonaCosechaNivel2 zona = null)
    {
        if (cajas == null)
            return null;

        foreach (GameObject caja in cajas)
        {
            if (caja == null || caja.activeSelf)
                continue;

            CajaCosechaRecolectable existente =
                caja.GetComponent<CajaCosechaRecolectable>();

            if (existente != null && existente.recogida)
                continue;

            CajaCosechaRecolectable mostrada =
                Mostrar(caja, null, producto, cantidadProducto);

            if (mostrada != null)
                mostrada.zonaCosecha = zona;

            return mostrada;
        }

        return null;
    }

    public static CajaCosechaRecolectable Mostrar(
        GameObject cajaExistente,
        Transform referencia,
        string producto,
        int cantidadProducto = 1)
    {
        // Se respetan estrictamente los modelos asignados en el array cajas.
        // Nunca se genera un cubo provisional ni se toma una caja de otra parcela.
        GameObject caja = cajaExistente;
        if (caja == null)
            return null;

        CajaCosechaRecolectable recolectable =
            caja.GetComponent<CajaCosechaRecolectable>();

        if (recolectable == null)
            recolectable = caja.AddComponent<CajaCosechaRecolectable>();

        recolectable.tipoProducto = producto;
        recolectable.cantidad = Mathf.Max(1, cantidadProducto);
        recolectable.recogida = false;

        if (caja.GetComponentInChildren<Collider>(true) == null)
        {
            BoxCollider colliderCaja = caja.AddComponent<BoxCollider>();
            colliderCaja.isTrigger = true;
        }

        caja.SetActive(true);
        return recolectable;
    }

    public bool Recoger(InventarioProductos inventario)
    {
        if (!EstaDisponible)
            return false;

        // IMPORTANTE: cada pulsacion recoge solamente ESTA caja/canasta.
        // Las otras cajas de la misma huerta permanecen visibles.
        return RecogerInterno(inventario);
    }

    public bool RecogerDesdeZona(InventarioProductos inventario)
    {
        if (recogida || string.IsNullOrWhiteSpace(tipoProducto))
            return false;

        return RecogerInterno(inventario);
    }

    private bool RecogerInterno(InventarioProductos inventario)
    {

        if (inventario == null)
            inventario = InventarioProductos.BuscarPrincipal();

        if (inventario == null)
        {
            Debug.LogWarning("No se encontro el inventario principal para recoger la caja.");
            return false;
        }

        recogida = true;
        inventario.AgregarProducto(tipoProducto, cantidad);
        GestorNivel2.RegistrarProductoGlobal(tipoProducto, cantidad);

        Debug.Log($"Recogiste caja de {tipoProducto} x{cantidad}");

        gameObject.SetActive(false);

        return true;
    }
}
