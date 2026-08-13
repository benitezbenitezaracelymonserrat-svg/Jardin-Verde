using UnityEngine;

[DisallowMultipleComponent]
public class NidoGallina : MonoBehaviour
{
    private GameObject[] huevos = new GameObject[0];
    private bool disponible;
    private bool recogido;

    public bool EstaDisponible => disponible && !recogido;
    public int CantidadHuevos => huevos != null ? huevos.Length : 0;

    public void ConfigurarDesdeJerarquia()
    {
        Transform zonaHuevos = BuscarHijo(transform, "Zonahuevo");

        if (zonaHuevos == null)
        {
            Debug.LogWarning("El nido no contiene un objeto Zonahuevo.", this);
            huevos = new GameObject[0];
            return;
        }

        huevos = new GameObject[zonaHuevos.childCount];

        for (int i = 0; i < zonaHuevos.childCount; i++)
        {
            huevos[i] = zonaHuevos.GetChild(i).gameObject;
            huevos[i].SetActive(false);
        }

        disponible = false;
        recogido = false;

        if (GetComponentInChildren<Collider>(true) == null)
        {
            BoxCollider zonaRecogida = gameObject.AddComponent<BoxCollider>();
            zonaRecogida.isTrigger = true;
            zonaRecogida.size = new Vector3(1.2f, 0.7f, 1.2f);
            zonaRecogida.center = new Vector3(0f, 0.3f, 0f);
        }
    }

    public void PrepararNivel2()
    {
        recogido = false;
        disponible = CantidadHuevos > 0;

        foreach (GameObject huevo in huevos)
        {
            if (huevo != null)
                huevo.SetActive(true);
        }
    }

    public bool Recoger(InventarioProductos inventario)
    {
        if (!EstaDisponible || CantidadHuevos <= 0)
            return false;

        if (inventario == null)
            inventario = InventarioProductos.BuscarPrincipal();

        if (inventario == null)
        {
            Debug.LogWarning("No se encontro el inventario para recoger huevos.");
            return false;
        }

        int cantidad = CantidadHuevos;
        disponible = false;
        recogido = true;

        foreach (GameObject huevo in huevos)
        {
            if (huevo != null)
                huevo.SetActive(false);
        }

        inventario.AgregarProducto("Huevo", cantidad);
        GestorNivel2.RegistrarProductoGlobal("Huevo", cantidad);
        Debug.Log($"Recogiste huevos x{cantidad}");
        return true;
    }

    private static Transform BuscarHijo(Transform raiz, string nombre)
    {
        foreach (Transform hijo in raiz)
        {
            if (hijo.name == nombre)
                return hijo;

            Transform encontrado = BuscarHijo(hijo, nombre);
            if (encontrado != null)
                return encontrado;
        }

        return null;
    }
}
