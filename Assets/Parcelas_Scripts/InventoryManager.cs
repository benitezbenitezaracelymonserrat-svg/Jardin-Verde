using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class InventoryManager : MonoBehaviour
{
    public Transform content;
    public GameObject itemPrefab;

    private Dictionary<string, int> productos = new Dictionary<string, int>();
    private Dictionary<string, Sprite> iconos = new Dictionary<string, Sprite>();
    private ScrollRect scrollProductos;

    private static readonly string[] OrdenProductos =
    {
        "Tomate", "Zanahoria", "Calabaza", "Lechuga", "Papa", "Cebolla",
        "Huevo", "LecheVaca", "LecheCabra"
    };

    void Awake()
    {
        ConfigurarContenido();
    }


    public void AgregarProducto(string nombre, Sprite icono, int cantidad = 1)
    {
        if (string.IsNullOrWhiteSpace(nombre) || cantidad <= 0)
            return;

        if (productos.ContainsKey(nombre))
        {
            productos[nombre] += cantidad;
        }
        else
        {
            productos.Add(nombre, cantidad);
        }

        if (icono != null)
            iconos[nombre] = icono;

        ActualizarInventario();
    }

    public void EstablecerCantidad(string nombre, int cantidad)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return;

        productos[nombre] = Mathf.Max(0, cantidad);
        ActualizarInventario();
    }

    public void Refrescar()
    {
        ActualizarInventario();
    }


    void ActualizarInventario()
    {
        if (content == null || itemPrefab == null)
            return;

        ConfigurarContenido();

        // borrar lista actual
        foreach (Transform hijo in content)
        {
            Destroy(hijo.gameObject);
        }


        List<KeyValuePair<string, int>> lista =
            new List<KeyValuePair<string, int>>(productos);

        lista.Sort((a, b) =>
        {
            int ordenA = System.Array.FindIndex(
                OrdenProductos,
                nombre => string.Equals(
                    nombre,
                    a.Key,
                    System.StringComparison.OrdinalIgnoreCase)
            );
            int ordenB = System.Array.FindIndex(
                OrdenProductos,
                nombre => string.Equals(
                    nombre,
                    b.Key,
                    System.StringComparison.OrdinalIgnoreCase)
            );

            if (ordenA < 0) ordenA = int.MaxValue;
            if (ordenB < 0) ordenB = int.MaxValue;

            int comparacion = ordenA.CompareTo(ordenB);
            return comparacion != 0
                ? comparacion
                : string.Compare(a.Key, b.Key, System.StringComparison.OrdinalIgnoreCase);
        });

        foreach (var producto in lista)
        {
            GameObject item = Instantiate(itemPrefab, content);
            ConfigurarItem(item);


            TextMeshProUGUI[] textos = item.GetComponentsInChildren<TextMeshProUGUI>();

            foreach (TextMeshProUGUI texto in textos)
            {
                if (texto.name == "Nombre")
                    texto.text = producto.Key;

                if (texto.name == "Cantidad")
                    texto.text = "x" + producto.Value;
            }

            Transform iconoTransform = item.transform.Find("Icono");
            UnityEngine.UI.Image imagen = iconoTransform != null
                ? iconoTransform.GetComponent<UnityEngine.UI.Image>()
                : null;

            if (imagen != null)
            {
                iconos.TryGetValue(producto.Key, out Sprite iconoProducto);

                if (iconoProducto == null)
                {
                    InventarioProductos inventario =
                        InventarioProductos.BuscarPrincipal();

                    if (inventario != null)
                    {
                        iconoProducto = inventario.ObtenerIcono(producto.Key);

                        if (iconoProducto != null)
                            iconos[producto.Key] = iconoProducto;
                    }
                }

                imagen.sprite = iconoProducto;
                imagen.preserveAspect = true;
            }

            ConfigurarBotonVenta(item, producto.Key);
        }

        if (content is RectTransform rectContenido)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectContenido);
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ConfigurarBotonVenta(GameObject item, string producto)
    {
        Transform contenedor = item.transform.Find("Ventas");
        if (contenedor == null)
            return;

        bool nivelTres = GestorNivel3.NivelActivo;
        contenedor.gameObject.SetActive(nivelTres);

        if (!nivelTres)
            return;

        Button boton = contenedor.GetComponent<Button>();
        TextMeshProUGUI texto =
            contenedor.GetComponentInChildren<TextMeshProUGUI>(true);

        int precio = GestorNivel3.ObtenerPrecio(producto);
        bool puedeVender = GestorNivel3.PuedeVenderProducto(producto);

        if (texto != null)
        {
            texto.text = puedeVender
                ? $"VENDER\n${precio}"
                : "NO PEDIDO";
            texto.fontSize = puedeVender ? 17f : 13f;
        }

        if (boton == null)
            return;

        boton.interactable = puedeVender;
        boton.onClick.RemoveAllListeners();

        string productoCapturado = producto;
        boton.onClick.AddListener(
            () => GestorNivel3.IntentarVenderProducto(productoCapturado)
        );
    }

    private void ConfigurarContenido()
    {
        if (content == null)
            return;

        scrollProductos = content.GetComponentInParent<ScrollRect>();

        if (scrollProductos != null)
        {
            scrollProductos.content = content as RectTransform;
            scrollProductos.horizontal = false;
            scrollProductos.vertical = true;
            scrollProductos.movementType = ScrollRect.MovementType.Clamped;
            scrollProductos.inertia = true;
            scrollProductos.scrollSensitivity = 28f;

            RectTransform scrollRect =
                scrollProductos.GetComponent<RectTransform>();

            RectTransform panelProductos = scrollRect.parent as RectTransform;
            if (panelProductos != null && panelProductos.name == "PanelProductos")
            {
                LayoutElement panelLayout =
                    panelProductos.GetComponent<LayoutElement>();
                if (panelLayout == null)
                    panelLayout = panelProductos.gameObject.AddComponent<LayoutElement>();

                // El VerticalLayoutGroup del PanelInventario lo reducía a
                // 10 px. Se reserva toda el área debajo del título/pestañas.
                panelLayout.ignoreLayout = true;
                panelProductos.anchorMin = Vector2.zero;
                panelProductos.anchorMax = Vector2.one;
                panelProductos.pivot = new Vector2(0.5f, 0.5f);
                panelProductos.offsetMin = new Vector2(20f, 20f);
                panelProductos.offsetMax = new Vector2(-20f, -115f);
                panelProductos.localScale = Vector3.one;
            }

            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.offsetMin = new Vector2(8f, 8f);
            scrollRect.offsetMax = new Vector2(-8f, -42f);
            scrollRect.localScale = Vector3.one;

            RectTransform viewport = scrollProductos.viewport;
            if (viewport != null)
            {
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.pivot = new Vector2(0.5f, 0.5f);
                viewport.offsetMin = Vector2.zero;
                viewport.offsetMax = scrollProductos.verticalScrollbar != null
                    ? new Vector2(-22f, 0f)
                    : Vector2.zero;
                viewport.localScale = Vector3.one;

                if (viewport.GetComponent<RectMask2D>() == null)
                    viewport.gameObject.AddComponent<RectMask2D>();
            }

            if (scrollProductos.verticalScrollbar != null)
            {
                Scrollbar barra = scrollProductos.verticalScrollbar;
                barra.direction = Scrollbar.Direction.BottomToTop;

                RectTransform barraRect = barra.GetComponent<RectTransform>();
                barraRect.anchorMin = new Vector2(1f, 0f);
                barraRect.anchorMax = Vector2.one;
                barraRect.pivot = new Vector2(1f, 0.5f);
                barraRect.anchoredPosition = Vector2.zero;
                barraRect.sizeDelta = new Vector2(18f, 0f);
                barraRect.localScale = Vector3.one;
            }
        }

        VerticalLayoutGroup vertical = content.GetComponent<VerticalLayoutGroup>();
        if (vertical == null)
            vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();

        vertical.padding = new RectOffset(8, 8, 8, 8);
        vertical.spacing = 4f;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        ContentSizeFitter ajustador = content.GetComponent<ContentSizeFitter>();
        if (ajustador == null)
            ajustador = content.gameObject.AddComponent<ContentSizeFitter>();

        ajustador.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        ajustador.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (content is RectTransform rect)
        {
            // PanelInventario mide 900x600. La lista empieza debajo del
            // titulo y de las pestañas, conservando sus margenes originales.
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
            rect.localScale = Vector3.one;
        }
    }

    private static void ConfigurarItem(GameObject item)
    {
        if (item == null)
            return;

        item.transform.localScale = Vector3.one;

        LayoutElement fila = item.GetComponent<LayoutElement>();
        if (fila == null)
            fila = item.AddComponent<LayoutElement>();

        fila.minHeight = 48f;
        fila.preferredHeight = 48f;
        fila.flexibleWidth = 1f;

        HorizontalLayoutGroup horizontal = item.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
        {
            horizontal.padding = new RectOffset(10, 10, 4, 4);
            horizontal.spacing = 10f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;
        }

        ConfigurarHijo(item.transform.Find("Icono"), 40f, 40f, 0f);
        ConfigurarHijo(item.transform.Find("Nombre"), 150f, 40f, 1f);
        ConfigurarHijo(item.transform.Find("Cantidad"), 70f, 40f, 0f);
        ConfigurarHijo(item.transform.Find("Ventas"), 105f, 40f, 0f);
    }

    private static void ConfigurarHijo(
        Transform hijo,
        float ancho,
        float alto,
        float flexible)
    {
        if (hijo == null)
            return;

        hijo.localScale = Vector3.one;

        LayoutElement elemento = hijo.GetComponent<LayoutElement>();
        if (elemento == null)
            elemento = hijo.gameObject.AddComponent<LayoutElement>();

        elemento.minWidth = ancho;
        elemento.preferredWidth = ancho;
        elemento.minHeight = alto;
        elemento.preferredHeight = alto;
        elemento.flexibleWidth = flexible;
    }
}
