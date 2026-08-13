using UnityEngine;
using UnityEngine.UI;


public class ControladorPestanas : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelProductos;
    public GameObject panelVentas;

    [Header("Botones (arrastrá el componente Image de fondo de cada botón)")]
    public Image imagenBtnProductos;
    public Image imagenBtnVentas;

    [Header("Colores")]
    public Color colorActivo = new Color(0.96f, 0.87f, 0.70f);   // crema
    public Color colorInactivo = new Color(0.36f, 0.20f, 0.09f); // marrón oscuro

    void Start()
    {
        MostrarProductos(); // arranca en Productos por defecto
    }

    public void MostrarProductos()
    {
        panelProductos.SetActive(true);
        panelVentas.SetActive(false);
        ActualizarColores(true);
    }

    public void MostrarVentas()
    {
        panelProductos.SetActive(false);
        panelVentas.SetActive(true);
        ActualizarColores(false);
    }

    void ActualizarColores(bool productosActivo)
    {
        if (imagenBtnProductos != null)
            imagenBtnProductos.color = productosActivo ? colorActivo : colorInactivo;
        if (imagenBtnVentas != null)
            imagenBtnVentas.color = productosActivo ? colorInactivo : colorActivo;
    }
}