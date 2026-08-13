using UnityEngine;

/// <summary>
/// Agrupa las cajas de una huerta. Recoger se habilita solamente cuando ya
/// no queda ningun cultivo plantado, creciendo o maduro en esa zona.
/// </summary>
[DisallowMultipleComponent]
public class ZonaCosechaNivel2 : MonoBehaviour
{
    private IParcelaConsultable parcela;
    private GameObject[] cajas;
    private string producto;
    public bool EstaListaParaRecoger =>
        GestorNivel2.NivelActivo &&
        parcela != null &&
        parcela.CultivosPendientes == 0 &&
        ContarCajasVisibles() > 0;

    public static ZonaCosechaNivel2 Preparar(
        MonoBehaviour componenteParcela,
        GameObject[] cajasZona,
        string tipoProducto)
    {
        if (componenteParcela == null)
            return null;

        ZonaCosechaNivel2 zona =
            componenteParcela.GetComponent<ZonaCosechaNivel2>();

        if (zona == null)
            zona = componenteParcela.gameObject.AddComponent<ZonaCosechaNivel2>();

        zona.parcela = componenteParcela as IParcelaConsultable;
        zona.cajas = cajasZona;
        zona.producto = tipoProducto;
        CajaCosechaRecolectable.PrepararArray(cajasZona);
        return zona;
    }

    public void RegistrarCosecha()
    {
        CajaCosechaRecolectable caja =
            CajaCosechaRecolectable.MostrarSiguiente(
                cajas,
                producto,
                1,
                this
            );

        // Cada caja queda disponible individualmente cuando toda la zona
        // termino de cosecharse.
    }

    public bool RecogerTodas(InventarioProductos inventario)
    {
        // Se conserva el nombre para no romper llamadas antiguas, pero ahora
        // recoge una sola caja por pulsacion.
        if (!EstaListaParaRecoger)
            return false;

        if (cajas != null)
        {
            foreach (GameObject objetoCaja in cajas)
            {
                if (objetoCaja == null || !objetoCaja.activeInHierarchy)
                    continue;

                CajaCosechaRecolectable caja =
                    objetoCaja.GetComponent<CajaCosechaRecolectable>();

                if (caja != null && caja.RecogerDesdeZona(inventario))
                    return true;
            }
        }

        return false;
    }

    private int ContarCajasVisibles()
    {
        int cantidad = 0;

        if (cajas == null)
            return cantidad;

        foreach (GameObject caja in cajas)
        {
            if (caja != null && caja.activeInHierarchy)
                cantidad++;
        }

        return cantidad;
    }
}
