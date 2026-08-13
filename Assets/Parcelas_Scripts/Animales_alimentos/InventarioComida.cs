using TMPro;
using UnityEngine;

public class InventarioComida : MonoBehaviour
{
    public int cantidadComida = 99;
    public TextMeshProUGUI textoCantidad;

    void Start()
    {
        ActualizarUI();
    }

    public bool UsarComida()
    {
        return UsarComida(1);
    }

    public bool UsarComida(int cantidad)
    {
        cantidad = Mathf.Max(1, cantidad);

        if (cantidadComida < cantidad)
            return false;

        cantidadComida -= cantidad;
        ActualizarUI();
        return true;
    }

    void ActualizarUI()
    {
        if (textoCantidad != null)
            textoCantidad.text = "x" + cantidadComida;
    }
}