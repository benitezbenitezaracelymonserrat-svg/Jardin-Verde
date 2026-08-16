using System.Collections;
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
    private Coroutine ajusteVisualPendiente;

    private static readonly string[] OrdenProductos =
    {
        "Tomate", "Zanahoria", "Calabaza", "Lechuga", "Papa", "Cebolla",
        "Huevo", "LecheVaca", "LecheCabra"
    };

    void Awake()
    {
        ConfigurarContenido();
        ProgramarAjusteVisual();
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

        List<KeyValuePair<string, int>> lista =
            new List<KeyValuePair<string, int>>(productos);

        // Se reutilizan las filas existentes. Destruir y recrear todos los
        // botones en cada venta terminaba dañando la lista interna Selectable
        // de UGUI durante una recarga de scripts.
        Dictionary<string, GameObject> filasExistentes =
            new Dictionary<string, GameObject>(
                System.StringComparer.OrdinalIgnoreCase
            );

        foreach (Transform hijo in content)
        {
            TextMeshProUGUI[] textosFila =
                hijo.GetComponentsInChildren<TextMeshProUGUI>(true);
            string nombreProducto = null;

            foreach (TextMeshProUGUI textoFila in textosFila)
            {
                if (textoFila.name == "Nombre")
                {
                    nombreProducto = textoFila.text;
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(nombreProducto) &&
                !filasExistentes.ContainsKey(nombreProducto))
            {
                filasExistentes[nombreProducto] = hijo.gameObject;
            }
        }

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

        for (int indiceProducto = 0; indiceProducto < lista.Count; indiceProducto++)
        {
            KeyValuePair<string, int> producto = lista[indiceProducto];
            GameObject item;

            if (!filasExistentes.TryGetValue(producto.Key, out item) || item == null)
            {
                item = Instantiate(itemPrefab, content);
                item.name = "ItemProducto_" + producto.Key;
            }

            if (!item.activeSelf)
                item.SetActive(true);
            item.transform.SetSiblingIndex(indiceProducto);
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

        MantenerCierreVisible();

        if (scrollProductos != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollProductos.StopMovement();
            scrollProductos.verticalNormalizedPosition = 1f;
        }

        ProgramarAjusteVisual();
    }

    private void ProgramarAjusteVisual()
    {
        if (!isActiveAndEnabled)
            return;

        if (ajusteVisualPendiente != null)
            StopCoroutine(ajusteVisualPendiente);

        ajusteVisualPendiente = StartCoroutine(AjustarVisualSiguienteFrame());
    }

    private IEnumerator AjustarVisualSiguienteFrame()
    {
        yield return null;

        RectTransform panelInventario = BuscarPanelInventario();
        if (panelInventario != null)
        {
            ConfigurarTamanoInventario(panelInventario);
            MantenerCierreVisible();
            Canvas.ForceUpdateCanvases();
        }

        if (scrollProductos != null)
        {
            scrollProductos.StopMovement();
            scrollProductos.verticalNormalizedPosition = 1f;
        }

        ajusteVisualPendiente = null;
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

        RectTransform panelInventario = BuscarPanelInventario();
        if (panelInventario != null)
            ConfigurarTamanoInventario(panelInventario);

        scrollProductos = content.GetComponentInParent<ScrollRect>();

        if (scrollProductos != null)
        {
            AsegurarBarraVertical();
            scrollProductos.content = content as RectTransform;
            scrollProductos.horizontal = false;
            scrollProductos.vertical = true;
            scrollProductos.movementType = ScrollRect.MovementType.Clamped;
            scrollProductos.inertia = true;
            scrollProductos.scrollSensitivity = 28f;
            scrollProductos.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.Permanent;

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
                panelProductos.offsetMax = new Vector2(-20f, -180f);
                panelProductos.localScale = Vector3.one;
            }

            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.offsetMin = new Vector2(8f, 8f);
            scrollRect.offsetMax = new Vector2(-8f, -8f);
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

        MantenerCierreVisible();
    }

    private void AsegurarBarraVertical()
    {
        if (scrollProductos == null)
            return;

        if (scrollProductos.verticalScrollbar == null)
        {
            Scrollbar[] barras =
                scrollProductos.GetComponentsInChildren<Scrollbar>(true);

            foreach (Scrollbar barraExistente in barras)
            {
                if (barraExistente != null &&
                    barraExistente.name.ToLowerInvariant().Contains("vertical"))
                {
                    scrollProductos.verticalScrollbar = barraExistente;
                    break;
                }
            }
        }

        if (scrollProductos.verticalScrollbar != null)
            return;

        GameObject barraObjeto = new GameObject(
            "Scrollbar Vertical Productos",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Scrollbar)
        );
        barraObjeto.layer = scrollProductos.gameObject.layer;
        barraObjeto.transform.SetParent(scrollProductos.transform, false);

        RectTransform barraRect = barraObjeto.GetComponent<RectTransform>();
        barraRect.anchorMin = new Vector2(1f, 0f);
        barraRect.anchorMax = Vector2.one;
        barraRect.pivot = new Vector2(1f, 0.5f);
        barraRect.anchoredPosition = Vector2.zero;
        barraRect.sizeDelta = new Vector2(18f, 0f);

        Image fondo = barraObjeto.GetComponent<Image>();
        fondo.color = new Color(0.30f, 0.16f, 0.06f, 0.45f);

        GameObject areaObjeto = new GameObject(
            "Sliding Area",
            typeof(RectTransform)
        );
        areaObjeto.layer = barraObjeto.layer;
        areaObjeto.transform.SetParent(barraObjeto.transform, false);
        RectTransform area = areaObjeto.GetComponent<RectTransform>();
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(2f, 2f);
        area.offsetMax = new Vector2(-2f, -2f);

        GameObject controlObjeto = new GameObject(
            "Handle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        controlObjeto.layer = barraObjeto.layer;
        controlObjeto.transform.SetParent(area, false);
        RectTransform control = controlObjeto.GetComponent<RectTransform>();
        control.anchorMin = Vector2.zero;
        control.anchorMax = Vector2.one;
        control.offsetMin = Vector2.zero;
        control.offsetMax = Vector2.zero;

        Image imagenControl = controlObjeto.GetComponent<Image>();
        imagenControl.color = new Color(0.73f, 0.43f, 0.16f, 1f);

        Scrollbar barra = barraObjeto.GetComponent<Scrollbar>();
        barra.targetGraphic = imagenControl;
        barra.handleRect = control;
        barra.direction = Scrollbar.Direction.BottomToTop;
        scrollProductos.verticalScrollbar = barra;
    }

    private void MantenerCierreVisible()
    {
        if (content == null)
            return;

        Transform panel = content;
        while (panel != null && panel.name != "PanelInventario")
            panel = panel.parent;

        if (panel == null)
            return;

        RectTransform cierre = null;
        RectTransform[] elementos =
            panel.GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform elemento in elementos)
        {
            if (elemento != null && elemento.name == "BtnCerrar")
            {
                cierre = elemento;
                break;
            }
        }

        if (cierre == null)
            return;

        if (cierre.GetComponentInParent<ScrollRect>() != null)
            cierre.SetParent(panel, false);

        cierre.anchorMin = Vector2.one;
        cierre.anchorMax = Vector2.one;
        cierre.pivot = Vector2.one;
        cierre.anchoredPosition = new Vector2(-16f, -14f);
        cierre.sizeDelta = new Vector2(46f, 42f);
        cierre.localScale = Vector3.one;

        Transform barraSuperior = cierre.parent;
        if (barraSuperior != null && barraSuperior.name == "BarraSuperior")
        {
            RectTransform barraRect = barraSuperior as RectTransform;
            if (barraRect != null)
            {
                LayoutElement layout =
                    barraRect.GetComponent<LayoutElement>();
                if (layout == null)
                    layout = barraRect.gameObject.AddComponent<LayoutElement>();
                layout.ignoreLayout = true;

                barraRect.anchorMin = new Vector2(0f, 1f);
                barraRect.anchorMax = Vector2.one;
                barraRect.pivot = new Vector2(0.5f, 1f);
                barraRect.anchoredPosition = new Vector2(0f, -20f);
                barraRect.sizeDelta = new Vector2(-40f, 80f);
                barraRect.localScale = Vector3.one;
            }

            barraSuperior.SetAsLastSibling();
        }

        cierre.SetAsLastSibling();
    }

    private RectTransform BuscarPanelInventario()
    {
        Transform panel = content;
        while (panel != null && panel.name != "PanelInventario")
            panel = panel.parent;

        return panel as RectTransform;
    }

    private static void ConfigurarTamanoInventario(RectTransform panel)
    {
        // Distribucion fija: titulo, pestanas y contenido, sin superposiciones.
        panel.sizeDelta = new Vector2(1200f, 820f);
        panel.localScale = Vector3.one;

        VerticalLayoutGroup layoutRaiz = panel.GetComponent<VerticalLayoutGroup>();
        if (layoutRaiz != null)
            layoutRaiz.enabled = false;

        RectTransform fondo = ConfigurarZonaAbsoluta(
            panel, "Fondo", Vector2.zero, Vector2.zero);
        RectTransform barraCategorias = ConfigurarFranjaSuperior(
            panel, "BarraCategorias", 110f, 58f);

        ConfigurarZonaAbsoluta(
            panel,
            "PanelProductos",
            new Vector2(20f, 20f),
            new Vector2(-20f, -180f)
        );
        ConfigurarZonaAbsoluta(
            panel,
            "PanelVentas",
            new Vector2(20f, 20f),
            new Vector2(-20f, -180f)
        );

        if (fondo != null && fondo.GetSiblingIndex() != 0)
            fondo.SetAsFirstSibling();

        ConfigurarBotonesCategorias(barraCategorias);
        ConfigurarPanelProductos(panel.Find("PanelProductos") as RectTransform);
        ConfigurarPanelVentas(panel.Find("PanelVentas") as RectTransform);

        if (barraCategorias != null)
        {
            Transform barraSuperior = panel.Find("BarraSuperior");
            if (barraSuperior != null)
            {
                barraSuperior.SetAsLastSibling();
                int indicePestanas = Mathf.Max(1, panel.childCount - 2);
                if (barraCategorias.GetSiblingIndex() != indicePestanas)
                    barraCategorias.SetSiblingIndex(indicePestanas);
            }
            else if (barraCategorias.GetSiblingIndex() != panel.childCount - 1)
            {
                barraCategorias.SetAsLastSibling();
            }
        }
    }

    private static RectTransform ConfigurarZonaAbsoluta(
        RectTransform panel,
        string nombre,
        Vector2 margenInferior,
        Vector2 margenSuperior)
    {
        Transform hijo = panel.Find(nombre);
        RectTransform rect = hijo as RectTransform;
        if (rect == null)
            return null;

        LayoutElement elemento = rect.GetComponent<LayoutElement>();
        if (elemento == null)
            elemento = rect.gameObject.AddComponent<LayoutElement>();
        elemento.ignoreLayout = true;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = margenInferior;
        rect.offsetMax = margenSuperior;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static RectTransform ConfigurarFranjaSuperior(
        RectTransform panel,
        string nombre,
        float distanciaDesdeArriba,
        float altura)
    {
        RectTransform rect = panel.Find(nombre) as RectTransform;
        if (rect == null)
            return null;

        LayoutElement elemento = rect.GetComponent<LayoutElement>();
        if (elemento == null)
            elemento = rect.gameObject.AddComponent<LayoutElement>();
        elemento.ignoreLayout = true;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -distanciaDesdeArriba);
        rect.sizeDelta = new Vector2(-40f, altura);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void ConfigurarBotonesCategorias(RectTransform barra)
    {
        if (barra == null)
            return;

        HorizontalLayoutGroup layout = barra.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
            layout.enabled = false;

        ContentSizeFitter fitter = barra.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;

        ConfigurarHijoEstirado(
            barra, "Fondocategorias", Vector2.zero, Vector2.one);
        ConfigurarHijoEstirado(
            barra, "BtnProductos", Vector2.zero, new Vector2(0.5f, 1f));
        ConfigurarHijoEstirado(
            barra, "BtnVentas", new Vector2(0.5f, 0f), Vector2.one);

        Transform fondo = barra.Find("Fondocategorias");
        Transform productos = barra.Find("BtnProductos");
        Transform ventas = barra.Find("BtnVentas");
        if (fondo != null && fondo.GetSiblingIndex() != 0)
            fondo.SetAsFirstSibling();
        if (productos != null)
        {
            int indiceProductos = Mathf.Max(1, barra.childCount - 2);
            if (productos.GetSiblingIndex() != indiceProductos)
                productos.SetSiblingIndex(indiceProductos);
        }
        if (ventas != null && ventas.GetSiblingIndex() != barra.childCount - 1)
            ventas.SetAsLastSibling();
    }

    private static void ConfigurarHijoEstirado(
        RectTransform padre,
        string nombre,
        Vector2 anclaMinima,
        Vector2 anclaMaxima)
    {
        RectTransform rect = padre.Find(nombre) as RectTransform;
        if (rect == null)
            return;

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout != null)
            layout.ignoreLayout = true;

        rect.anchorMin = anclaMinima;
        rect.anchorMax = anclaMaxima;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(2f, 0f);
        rect.offsetMax = new Vector2(-2f, 0f);
        rect.localScale = Vector3.one;
    }

    private static void ConfigurarPanelVentas(RectTransform panelVentas)
    {
        if (panelVentas == null)
            return;

        VerticalLayoutGroup layout = panelVentas.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.enabled = false;

        ContentSizeFitter fitter = panelVentas.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;

        RectTransform titulo = panelVentas.Find("TituloVentas") as RectTransform;
        if (titulo != null)
        {
            titulo.anchorMin = new Vector2(0f, 1f);
            titulo.anchorMax = Vector2.one;
            titulo.pivot = new Vector2(0.5f, 1f);
            titulo.anchoredPosition = Vector2.zero;
            titulo.sizeDelta = new Vector2(0f, 46f);
        }

        RectTransform total = panelVentas.Find("TextoTotal") as RectTransform;
        if (total != null)
        {
            total.anchorMin = Vector2.zero;
            total.anchorMax = new Vector2(1f, 0f);
            total.pivot = new Vector2(0.5f, 0f);
            total.anchoredPosition = Vector2.zero;
            total.sizeDelta = new Vector2(0f, 48f);
        }

        RectTransform scroll = panelVentas.Find("ScrollVentas") as RectTransform;
        if (scroll != null)
        {
            scroll.anchorMin = Vector2.zero;
            scroll.anchorMax = Vector2.one;
            scroll.pivot = new Vector2(0.5f, 0.5f);
            scroll.offsetMin = new Vector2(8f, 54f);
            scroll.offsetMax = new Vector2(-8f, -52f);
            scroll.localScale = Vector3.one;
        }
    }

    private static void ConfigurarPanelProductos(RectTransform panelProductos)
    {
        if (panelProductos == null)
            return;

        RectTransform scroll = panelProductos.Find("ScrollProductos") as RectTransform;
        if (scroll == null)
            return;

        scroll.anchorMin = Vector2.zero;
        scroll.anchorMax = Vector2.one;
        scroll.pivot = new Vector2(0.5f, 0.5f);
        scroll.offsetMin = new Vector2(8f, 8f);
        scroll.offsetMax = new Vector2(-8f, -8f);
        scroll.localScale = Vector3.one;
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
