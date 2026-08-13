using UnityEngine;

public class TestInventario : MonoBehaviour
{
    public InventoryManager inventario;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            inventario.AgregarProducto("Huevo", null);
        }
    }
}