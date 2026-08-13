using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class InventarioProductos : MonoBehaviour
{
    public static InventarioProductos Instancia { get; private set; }

    public InventoryManager inventarioUI;

    [System.Serializable]
    public class IconoProducto
    {
        public string tipo;
        public Sprite icono;
    }

    [Tooltip("Iconos opcionales. Si queda vacio, se buscan por nombre en Resources/UI/IconosProductos.")]
    public List<IconoProducto> iconosProductos = new List<IconoProducto>();

        [System.Serializable]
        public class TextoProducto
    {
        public string tipo;              // "huevo", "leche", "lana"
        public TextMeshProUGUI texto;    // el TMP que muestra la cantidad
    }

    [Tooltip("Un elemento por cada tipo de producto que quieras mostrar en UI")]
    public List<TextoProducto> textosUI;

    private readonly Dictionary<string, int> productos =
        new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Sprite> iconosEnMemoria =
        new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        // En la escena actual existen dos componentes. El que tiene conectada la UI
        // es el inventario principal; el otro se conserva para no romper la escena.
        if (Instancia == null ||
            (Instancia.inventarioUI == null && inventarioUI != null))
        {
            Instancia = this;
        }
    }

    public static InventarioProductos BuscarPrincipal()
    {
        if (Instancia != null && Instancia.inventarioUI != null)
            return Instancia;

        InventarioProductos[] inventarios =
            FindObjectsByType<InventarioProductos>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        InventarioProductos primero = null;

        foreach (InventarioProductos inventario in inventarios)
        {
            if (inventario == null)
                continue;

            if (primero == null)
                primero = inventario;

            if (inventario.inventarioUI != null)
            {
                Instancia = inventario;
                return inventario;
            }
        }

        Instancia = primero;
        return primero;
    }

    public void AgregarProducto(string tipo, int cantidad = 1)
    {
        if (string.IsNullOrWhiteSpace(tipo) || cantidad <= 0)
            return;

        if (!productos.ContainsKey(tipo))
            productos[tipo] = 0;

        productos[tipo] += cantidad;

        ActualizarUI(tipo);


        if (inventarioUI != null)
        {
            inventarioUI.AgregarProducto(tipo, BuscarIcono(tipo), cantidad);
        }
    }

    public bool QuitarProducto(string tipo, int cantidad = 1)
    {
        if (string.IsNullOrWhiteSpace(tipo) || cantidad <= 0)
            return false;

        if (!productos.TryGetValue(tipo, out int actual) || actual < cantidad)
            return false;

        productos[tipo] = actual - cantidad;
        ActualizarUI(tipo);

        if (inventarioUI != null)
            inventarioUI.EstablecerCantidad(tipo, productos[tipo]);

        return true;
    }
    public int GetCantidad(string tipo)
    {
        return productos.TryGetValue(tipo, out int c) ? c : 0;
    }

    private void ActualizarUI(string tipo)
    {
        if (textosUI == null)
            return;

        foreach (var item in textosUI)
        {
            if (item.tipo == tipo && item.texto != null)
                item.texto.text = productos[tipo].ToString();
        }
    }

    public Sprite ObtenerIcono(string tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return null;

        if (iconosEnMemoria.TryGetValue(tipo, out Sprite iconoGuardado) &&
            iconoGuardado != null)
        {
            return iconoGuardado;
        }

        if (iconosProductos != null)
        {
            foreach (IconoProducto entrada in iconosProductos)
            {
                if (entrada != null &&
                    entrada.icono != null &&
                    string.Equals(
                        entrada.tipo,
                        tipo,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    iconosEnMemoria[tipo] = entrada.icono;
                    return entrada.icono;
                }
            }
        }

        Sprite icono = Resources.Load<Sprite>("UI/IconosProductos/" + tipo);

        // Los PNG tambien pueden estar importados como textura. Esta alternativa
        // permite utilizar directamente los iconos entregados para el inventario.
        if (icono == null)
        {
            Texture2D textura = Resources.Load<Texture2D>(
                "UI/IconosProductos/" + tipo
            );

            if (textura != null)
            {
                icono = Sprite.Create(
                    textura,
                    new Rect(0f, 0f, textura.width, textura.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                icono.name = "Icono_" + tipo;
            }
        }

        if (icono != null)
            iconosEnMemoria[tipo] = icono;

        return icono;
    }

    private Sprite BuscarIcono(string tipo) => ObtenerIcono(tipo);
}
