using UnityEngine;

public class ProductoRecolectable : MonoBehaviour
{
    [Tooltip("Animal que produce este huevo, leche u otro producto ganadero.")]
    public Animal animalDueno;

    public bool EstaDisponible =>
        animalDueno != null && animalDueno.ProductoListo;

    public bool Recoger(InventarioProductos inventario)
    {
        return animalDueno != null && animalDueno.Recolectar(inventario);
    }
}
