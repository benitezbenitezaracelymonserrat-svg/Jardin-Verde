using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Nivel 3: administra la fila, los pedidos, las ventas desde el inventario,
/// las monedas y el desafio final del juego.
/// </summary>
public class GestorNivel3 : MonoBehaviour
{
    [Serializable]
    private class PedidoProducto
    {
        public string tipo;
        public int restante;
        public int precio;
    }

    public static GestorNivel3 Instancia { get; private set; }
    public static bool NivelActivo => Instancia != null && Instancia.nivelActivo;

    private const int ObjetivoClientes = 5;
    private const int CantidadClientesFila = 10;

    private static readonly string[] ProductosVendibles =
    {
        "Tomate", "Cebolla", "Zanahoria", "Papa", "Calabaza",
        "Lechuga", "Huevo", "LecheVaca", "LecheCabra"
    };

    private static readonly Dictionary<string, int> Precios =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Tomate", 5 },
            { "Cebolla", 5 },
            { "Zanahoria", 5 },
            { "Papa", 5 },
            { "Calabaza", 8 },
            { "Lechuga", 8 },
            { "Huevo", 3 },
            { "LecheVaca", 6 },
            { "LecheCabra", 6 }
        };

    private readonly List<Transform> clientes = new List<Transform>();
    private readonly List<Vector3> posicionesFila = new List<Vector3>();
    private readonly List<PedidoProducto> pedidoActual =
        new List<PedidoProducto>();

    private bool nivelActivo;
    private bool jugadorEnZona;
    private bool procesandoCliente;
    private int clientesAtendidos;
    private int monedas;

    private Transform puntoInicioFila;
    private Transform puntoFinal;
    private Transform puntoSalida;
    private Transform clienteActual;
    private InventarioProductos inventario;
    private InventoryManager inventarioUI;

    private GameObject botonDesafios;
    private GameObject panelDesafios;
    private GameObject panelMonedas;
    private TextMeshProUGUI textoProgreso;
    private TextMeshProUGUI textoFinal;
    private TextMeshProUGUI textoMonedas;
    private GameObject nubePedido;
    private Transform contenidoIconosNube;
    private TextMeshProUGUI textoNube;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearAutomaticamente()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
            return;

        if (FindFirstObjectByType<GestorNivel3>() == null)
            new GameObject("GestorNivel3").AddComponent<GestorNivel3>();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        ConfigurarVisibilidadZonaVentas(false);
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Atajo de prueba del editor. En el juego normal se entra desde NIVEL 3.
        if (!nivelActivo && Input.GetKeyDown(KeyCode.F3))
        {
            PrepararProductosDePruebaSiEstaVacio();
            IniciarNivel3();
        }
    }

    private static void PrepararProductosDePruebaSiEstaVacio()
    {
        InventarioProductos inventarioPrueba =
            InventarioProductos.BuscarPrincipal();

        if (inventarioPrueba == null ||
            ProductosVendibles.Any(tipo => inventarioPrueba.GetCantidad(tipo) > 0))
        {
            return;
        }

        foreach (string tipo in ProductosVendibles)
            inventarioPrueba.AgregarProducto(tipo, 3);

        Debug.Log(
            "Nivel 3 (prueba F3): se agregaron 3 unidades de cada producto."
        );
    }
#endif

    public static void IniciarNivel3Global()
    {
        GestorNivel3 gestor = Instancia;

        if (gestor == null)
            gestor = FindFirstObjectByType<GestorNivel3>();

        if (gestor == null)
            gestor = new GameObject("GestorNivel3").AddComponent<GestorNivel3>();

        gestor.IniciarNivel3();
    }

    public static int ObtenerPrecio(string producto)
    {
        return !string.IsNullOrWhiteSpace(producto) &&
               Precios.TryGetValue(producto, out int precio)
            ? precio
            : 0;
    }

    public static bool PuedeVenderProducto(string producto)
    {
        return NivelActivo && Instancia.PuedeVender(producto);
    }

    public static void IntentarVenderProducto(string producto)
    {
        if (NivelActivo)
            Instancia.IntentarVender(producto);
    }

    public static void NotificarJugadorEnZona(bool dentro)
    {
        if (!NivelActivo)
            return;

        Instancia.jugadorEnZona = dentro;
        Instancia.RefrescarInventario();
        Instancia.ActualizarNube();
    }

    private void IniciarNivel3()
    {
        if (nivelActivo)
            return;

        Time.timeScale = 1f;
        nivelActivo = true;
        monedas = 0;
        clientesAtendidos = 0;
        jugadorEnZona = false;

        // El puesto, los clientes y la zona de atencion existen desde el
        // principio del mapa, pero solamente se muestran en el nivel 3.
        ConfigurarVisibilidadZonaVentas(true);

        GestorNivel2.PrepararSalidaNivel3Global();
        OcultarInterfazAnterior();

        inventario = InventarioProductos.BuscarPrincipal();
        inventarioUI = inventario != null ? inventario.inventarioUI : null;

        puntoInicioFila = BuscarTransformPorNombre("PuntoInicioFila");
        puntoFinal = BuscarTransformPorNombre("PuntoFinal01");
        puntoSalida = BuscarTransformPorNombre("PuntoSalidaCliente");

        ConfigurarZonaAtencion();
        PrepararFilaClientes();
        TeletransportarJugador();
        ConstruirInterfaz();
        CrearPedidoParaClienteActual();
        ActualizarInterfaz();

        Debug.Log("Nivel 3 iniciado: atende 5 clientes y vende desde el inventario.");
    }

    private static void ConfigurarVisibilidadZonaVentas(bool visible)
    {
        string[] nombres =
        {
            "MarketStand_1",
            "Persoanjes de fila",
            "Atencionalcliente"
        };

        Transform[] elementos = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (string nombre in nombres)
        {
            Transform encontrado = elementos.FirstOrDefault(
                t => t != null && t.name == nombre
            );

            if (encontrado != null)
                encontrado.gameObject.SetActive(visible);
        }
    }

    private void ConfigurarZonaAtencion()
    {
        Transform zona = BuscarTransformPorNombre("Atencionalcliente");
        if (zona == null)
        {
            Debug.LogWarning("Nivel 3: no se encontro Atencionalcliente.");
            return;
        }

        Collider colliderZona = zona.GetComponent<Collider>();
        if (colliderZona != null)
            colliderZona.isTrigger = true;

        if (zona.GetComponent<ZonaAtencionVentasNivel3>() == null)
            zona.gameObject.AddComponent<ZonaAtencionVentasNivel3>();
    }

    private void PrepararFilaClientes()
    {
        Transform padre = BuscarTransformPorNombre("Persoanjes de fila");

        if (padre != null)
        {
            foreach (Transform hijo in padre)
            {
                if (hijo != null)
                    clientes.Add(hijo);
            }
        }

        if (clientes.Count == 0)
        {
            string[] nombresModelos =
            {
                "Adventurer", "Formal", "Casual", "Beach", "Casual_2",
                "Casual_Hoodie", "Female_Alternative", "Female_Casual",
                "Female_Dress"
            };

            foreach (string nombre in nombresModelos)
            {
                Transform encontrado = BuscarTransformPorNombre(nombre);
                if (encontrado != null && !clientes.Contains(encontrado))
                    clientes.Add(encontrado);
            }
        }

        if (clientes.Count == 0 || puntoInicioFila == null || puntoFinal == null)
        {
            Debug.LogWarning(
                "Nivel 3: faltan clientes, PuntoInicioFila o PuntoFinal01."
            );
            return;
        }

        clientes.Sort((a, b) =>
            Vector3.SqrMagnitude(a.position - puntoFinal.position).CompareTo(
                Vector3.SqrMagnitude(b.position - puntoFinal.position)
            )
        );

        while (clientes.Count < CantidadClientesFila)
        {
            Transform copia = Instantiate(
                clientes[clientes.Count % Mathf.Max(1, clientes.Count)],
                padre != null ? padre : clientes[0].parent
            );
            copia.name = "Cliente10";
            clientes.Add(copia);
        }

        if (clientes.Count > CantidadClientesFila)
            clientes.RemoveRange(
                CantidadClientesFila,
                clientes.Count - CantidadClientesFila
            );

        posicionesFila.Clear();
        Vector3 inicioFila = puntoInicioFila.position;
        Vector3 finalFila = puntoFinal.position;
        inicioFila.y = finalFila.y;

        for (int i = 0; i < CantidadClientesFila; i++)
        {
            float t = CantidadClientesFila == 1
                ? 0f
                : i / (float)(CantidadClientesFila - 1);
            posicionesFila.Add(
                Vector3.Lerp(finalFila, inicioFila, t)
            );
        }

        for (int i = 0; i < clientes.Count; i++)
        {
            clientes[i].gameObject.SetActive(true);
            clientes[i].position = posicionesFila[i];
            OrientarClienteAlPuesto(clientes[i]);
        }

        clienteActual = clientes[0];
    }

    private void CrearPedidoParaClienteActual()
    {
        pedidoActual.Clear();

        if (clienteActual == null || inventario == null)
        {
            CrearNubePedido();
            ActualizarNube();
            return;
        }

        List<string> disponibles = ProductosVendibles
            .Where(tipo => inventario.GetCantidad(tipo) > 0)
            .OrderBy(_ => UnityEngine.Random.value)
            .ToList();

        int cantidad = Mathf.Min(
            disponibles.Count,
            UnityEngine.Random.Range(1, 4)
        );

        for (int i = 0; i < cantidad; i++)
        {
            string tipo = disponibles[i];
            pedidoActual.Add(new PedidoProducto
            {
                tipo = tipo,
                restante = 1,
                precio = ObtenerPrecio(tipo)
            });
        }

        CrearNubePedido();
        ActualizarNube();
        RefrescarInventario();
    }

    private bool PuedeVender(string producto)
    {
        if (!nivelActivo || !jugadorEnZona || procesandoCliente ||
            inventario == null || string.IsNullOrWhiteSpace(producto))
        {
            return false;
        }

        return pedidoActual.Any(p =>
            p.restante > 0 &&
            string.Equals(p.tipo, producto, StringComparison.OrdinalIgnoreCase)) &&
            inventario.GetCantidad(producto) > 0;
    }

    private void IntentarVender(string producto)
    {
        if (!PuedeVender(producto))
            return;

        PedidoProducto pedido = pedidoActual.FirstOrDefault(p =>
            p.restante > 0 &&
            string.Equals(p.tipo, producto, StringComparison.OrdinalIgnoreCase)
        );

        if (pedido == null || !inventario.QuitarProducto(producto, 1))
            return;

        pedido.restante--;
        monedas += pedido.precio;

        ActualizarInterfaz();
        ActualizarNube();
        RefrescarInventario();

        if (pedidoActual.Count > 0 && pedidoActual.All(p => p.restante <= 0))
            StartCoroutine(CompletarClienteActual());
    }

    private IEnumerator CompletarClienteActual()
    {
        if (procesandoCliente || clienteActual == null)
            yield break;

        procesandoCliente = true;
        clientesAtendidos++;

        CrearBolsaCompra(clienteActual, pedidoActual);

        if (textoNube != null)
            textoNube.text = "¡Gracias por mi compra!";

        ActualizarInterfaz();
        RefrescarInventario();

        yield return new WaitForSeconds(1.1f);

        if (nubePedido != null)
            Destroy(nubePedido);

        Transform atendido = clienteActual;
        clientes.RemoveAt(0);

        if (puntoSalida != null)
            StartCoroutine(MoverYRetirarCliente(atendido, puntoSalida.position));
        else
            atendido.gameObject.SetActive(false);

        for (int i = 0; i < clientes.Count && i < posicionesFila.Count; i++)
            StartCoroutine(MoverCliente(clientes[i], posicionesFila[i], 2.2f));

        yield return new WaitForSeconds(1.25f);

        procesandoCliente = false;

        if (clientesAtendidos >= ObjetivoClientes)
        {
            clienteActual = null;
            MostrarFinal();
            yield break;
        }

        if (clientes.Count > 0)
        {
            clienteActual = clientes[0];
            CrearPedidoParaClienteActual();
        }
    }

    private IEnumerator MoverYRetirarCliente(Transform cliente, Vector3 destino)
    {
        if (cliente != null)
            destino.y = cliente.position.y;

        yield return MoverCliente(cliente, destino, 3f);

        if (cliente != null)
            cliente.gameObject.SetActive(false);
    }

    private IEnumerator MoverCliente(
        Transform cliente,
        Vector3 destino,
        float velocidad)
    {
        if (cliente == null)
            yield break;

        while (cliente != null &&
               Vector3.SqrMagnitude(cliente.position - destino) > 0.01f)
        {
            Vector3 direccion = destino - cliente.position;
            Vector3 horizontal = new Vector3(direccion.x, 0f, direccion.z);

            if (horizontal.sqrMagnitude > 0.001f)
            {
                cliente.rotation = Quaternion.Slerp(
                    cliente.rotation,
                    Quaternion.LookRotation(horizontal),
                    8f * Time.deltaTime
                );
            }

            cliente.position = Vector3.MoveTowards(
                cliente.position,
                destino,
                velocidad * Time.deltaTime
            );
            yield return null;
        }

        if (cliente != null)
        {
            cliente.position = destino;
            OrientarClienteAlPuesto(cliente);
        }
    }

    private void OrientarClienteAlPuesto(Transform cliente)
    {
        if (cliente == null || puntoFinal == null)
            return;

        Vector3 direccion = puntoFinal.position - cliente.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.001f)
            cliente.rotation = Quaternion.LookRotation(direccion);
    }

    private void CrearNubePedido()
    {
        if (nubePedido != null)
            Destroy(nubePedido);

        if (clienteActual == null)
            return;

        nubePedido = new GameObject("NubePedidoNivel3");
        nubePedido.transform.SetParent(clienteActual, false);

        float altura = CalcularAlturaCliente(clienteActual) + 0.35f;
        nubePedido.transform.localPosition = new Vector3(0f, altura, 0f);
        nubePedido.transform.localScale = Vector3.one * 0.005f;
        nubePedido.AddComponent<BillboardNivel3>();

        Canvas canvas = nubePedido.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 80;

        RectTransform rect = nubePedido.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(390f, 180f);

        Image fondo = nubePedido.AddComponent<Image>();
        fondo.sprite = CrearFondoRedondeado(390, 180, 28);
        fondo.color = new Color(1f, 0.96f, 0.83f, 0.98f);
        AgregarBorde(nubePedido);

        textoNube = CrearTexto(
            "TextoPedido",
            nubePedido.transform,
            string.Empty,
            27f,
            TextAlignmentOptions.Center
        );
        RectTransform textoRect = textoNube.rectTransform;
        textoRect.anchorMin = new Vector2(0f, 0.45f);
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(16f, 4f);
        textoRect.offsetMax = new Vector2(-16f, -12f);
        textoNube.fontStyle = FontStyles.Bold;
        textoNube.color = new Color(0.24f, 0.12f, 0.05f, 1f);

        GameObject iconos = CrearObjetoUI("IconosPedido", nubePedido.transform);
        contenidoIconosNube = iconos.transform;
        RectTransform iconosRect = iconos.GetComponent<RectTransform>();
        iconosRect.anchorMin = new Vector2(0f, 0f);
        iconosRect.anchorMax = new Vector2(1f, 0.48f);
        iconosRect.offsetMin = new Vector2(24f, 12f);
        iconosRect.offsetMax = new Vector2(-24f, -4f);

        HorizontalLayoutGroup layout = iconos.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject cola = CrearObjetoUI("ColaNube", nubePedido.transform);
        RectTransform colaRect = cola.GetComponent<RectTransform>();
        colaRect.anchorMin = new Vector2(0.5f, 0f);
        colaRect.anchorMax = new Vector2(0.5f, 0f);
        colaRect.pivot = new Vector2(0.5f, 1f);
        colaRect.anchoredPosition = new Vector2(0f, 2f);
        colaRect.sizeDelta = new Vector2(30f, 30f);
        colaRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image colaImagen = cola.AddComponent<Image>();
        colaImagen.color = fondo.color;
    }

    private void ActualizarNube()
    {
        if (textoNube == null)
            return;

        if (pedidoActual.Count == 0)
        {
            textoNube.text = inventario == null
                ? "No encuentro el inventario"
                : "No quedan productos para vender";
        }
        else if (!jugadorEnZona)
        {
            textoNube.text = "¡Hola! Acercate al puesto. Quiero:";
        }
        else
        {
            textoNube.text = "Quiero estos productos:";
        }

        if (contenidoIconosNube == null)
            return;

        foreach (Transform hijo in contenidoIconosNube)
            Destroy(hijo.gameObject);

        foreach (PedidoProducto pedido in pedidoActual)
        {
            GameObject ficha = CrearObjetoUI(
                "Pedido_" + pedido.tipo,
                contenidoIconosNube
            );
            RectTransform fichaRect = ficha.GetComponent<RectTransform>();
            fichaRect.sizeDelta = new Vector2(100f, 70f);
            LayoutElement elemento = ficha.AddComponent<LayoutElement>();
            elemento.preferredWidth = 100f;
            elemento.preferredHeight = 70f;

            GameObject iconoObjeto = CrearObjetoUI("Icono", ficha.transform);
            RectTransform iconoRect = iconoObjeto.GetComponent<RectTransform>();
            iconoRect.anchorMin = new Vector2(0.5f, 1f);
            iconoRect.anchorMax = new Vector2(0.5f, 1f);
            iconoRect.pivot = new Vector2(0.5f, 1f);
            iconoRect.anchoredPosition = Vector2.zero;
            iconoRect.sizeDelta = new Vector2(45f, 45f);
            Image imagen = iconoObjeto.AddComponent<Image>();
            imagen.sprite = ObtenerIcono(pedido.tipo);
            imagen.preserveAspect = true;
            imagen.color = pedido.restante > 0
                ? Color.white
                : new Color(0.45f, 1f, 0.45f, 1f);

            TextMeshProUGUI nombre = CrearTexto(
                "Nombre",
                ficha.transform,
                pedido.restante > 0
                    ? NombreVisible(pedido.tipo)
                    : "LISTO",
                14f,
                TextAlignmentOptions.Center
            );
            RectTransform nombreRect = nombre.rectTransform;
            nombreRect.anchorMin = new Vector2(0f, 0f);
            nombreRect.anchorMax = new Vector2(1f, 0f);
            nombreRect.pivot = new Vector2(0.5f, 0f);
            nombreRect.offsetMin = Vector2.zero;
            nombreRect.offsetMax = new Vector2(0f, 24f);
            nombre.color = pedido.restante > 0
                ? new Color(0.24f, 0.12f, 0.05f, 1f)
                : new Color(0.12f, 0.48f, 0.15f, 1f);
        }
    }

    private void CrearBolsaCompra(
        Transform cliente,
        IEnumerable<PedidoProducto> productos)
    {
        if (cliente == null)
            return;

        GameObject bolsa = new GameObject("BolsaCompraNivel3");
        bolsa.transform.SetParent(cliente, false);
        bolsa.transform.localPosition = new Vector3(0f, 0.92f, 0.30f);

        CrearParteBolsa(
            "CuerpoBolsa",
            bolsa.transform,
            Vector3.zero,
            new Vector3(0.38f, 0.42f, 0.20f)
        );
        CrearParteBolsa(
            "CierreBolsa",
            bolsa.transform,
            new Vector3(0f, 0.25f, 0f),
            new Vector3(0.25f, 0.10f, 0.17f)
        );

        GameObject canvasObjeto = new GameObject("ProductosBolsa");
        canvasObjeto.transform.SetParent(bolsa.transform, false);
        canvasObjeto.transform.localPosition = new Vector3(0f, 0f, -0.13f);
        canvasObjeto.transform.localScale = Vector3.one * 0.0023f;
        canvasObjeto.AddComponent<BillboardNivel3>();

        Canvas canvas = canvasObjeto.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 70;
        RectTransform canvasRect = canvasObjeto.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(150f, 55f);

        HorizontalLayoutGroup layout =
            canvasObjeto.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        foreach (PedidoProducto producto in productos)
        {
            GameObject iconoObjeto = CrearObjetoUI(
                "Contenido_" + producto.tipo,
                canvasObjeto.transform
            );
            RectTransform rect = iconoObjeto.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(45f, 45f);
            LayoutElement elemento = iconoObjeto.AddComponent<LayoutElement>();
            elemento.preferredWidth = 45f;
            elemento.preferredHeight = 45f;
            Image imagen = iconoObjeto.AddComponent<Image>();
            imagen.sprite = ObtenerIcono(producto.tipo);
            imagen.preserveAspect = true;
        }
    }

    private static void CrearParteBolsa(
        string nombre,
        Transform padre,
        Vector3 posicion,
        Vector3 escala)
    {
        GameObject parte = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parte.name = nombre;
        parte.transform.SetParent(padre, false);
        parte.transform.localPosition = posicion;
        parte.transform.localScale = escala;

        Collider colision = parte.GetComponent<Collider>();
        if (colision != null)
            Destroy(colision);

        Renderer renderer = parte.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(0.55f, 0.30f, 0.12f, 1f);
                renderer.material = material;
            }
        }
    }

    private void ConstruirInterfaz()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        botonDesafios = CrearObjetoUI("BotonDesafiosNivel3", canvas.transform);
        RectTransform botonRect = botonDesafios.GetComponent<RectTransform>();
        botonRect.anchorMin = new Vector2(1f, 1f);
        botonRect.anchorMax = new Vector2(1f, 1f);
        botonRect.pivot = new Vector2(1f, 1f);
        botonRect.anchoredPosition = new Vector2(-96f, -22f);
        botonRect.sizeDelta = new Vector2(140f, 60f);

        Image fondoBoton = botonDesafios.AddComponent<Image>();
        fondoBoton.sprite = CrearFondoRedondeado(140, 60, 16);
        fondoBoton.color = new Color(0.22f, 0.42f, 0.24f, 0.96f);
        AgregarBorde(botonDesafios);
        Button abrir = botonDesafios.AddComponent<Button>();
        abrir.targetGraphic = fondoBoton;

        TextMeshProUGUI textoBoton = CrearTexto(
            "TextoBotonNivel3",
            botonDesafios.transform,
            "TAREAS",
            22f,
            TextAlignmentOptions.Center
        );
        Estirar(textoBoton.rectTransform, 3f);
        textoBoton.fontStyle = FontStyles.Bold;
        textoBoton.color = new Color(1f, 0.93f, 0.68f, 1f);

        CrearIndicadorMonedas(canvas);

        panelDesafios = CrearObjetoUI("PanelDesafiosNivel3", canvas.transform);
        RectTransform panelRect = panelDesafios.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(22f, -86f);
        panelRect.sizeDelta = new Vector2(540f, 405f);

        Image fondoPanel = panelDesafios.AddComponent<Image>();
        fondoPanel.color = new Color(0.055f, 0.19f, 0.13f, 0.97f);
        AgregarBorde(panelDesafios);

        TextMeshProUGUI titulo = CrearTexto(
            "TituloNivel3",
            panelDesafios.transform,
            "DESAFIOS - NIVEL 3",
            27f,
            TextAlignmentOptions.Center
        );
        RectTransform tituloRect = titulo.rectTransform;
        tituloRect.anchorMin = new Vector2(0f, 1f);
        tituloRect.anchorMax = new Vector2(1f, 1f);
        tituloRect.pivot = new Vector2(0.5f, 1f);
        tituloRect.offsetMin = new Vector2(24f, -62f);
        tituloRect.offsetMax = new Vector2(-60f, -14f);
        titulo.fontStyle = FontStyles.Bold;
        titulo.color = new Color(1f, 0.91f, 0.62f, 1f);

        GameObject cerrarObjeto = CrearObjetoUI(
            "BotonCerrarNivel3",
            panelDesafios.transform
        );
        RectTransform cerrarRect = cerrarObjeto.GetComponent<RectTransform>();
        cerrarRect.anchorMin = Vector2.one;
        cerrarRect.anchorMax = Vector2.one;
        cerrarRect.pivot = Vector2.one;
        cerrarRect.anchoredPosition = new Vector2(-12f, -12f);
        cerrarRect.sizeDelta = new Vector2(42f, 42f);
        Image fondoCerrar = cerrarObjeto.AddComponent<Image>();
        fondoCerrar.color = new Color(0.48f, 0.12f, 0.10f, 0.95f);
        Button cerrar = cerrarObjeto.AddComponent<Button>();
        cerrar.targetGraphic = fondoCerrar;
        cerrar.onClick.AddListener(() => panelDesafios.SetActive(false));
        TextMeshProUGUI textoCerrar = CrearTexto(
            "TextoCerrarNivel3",
            cerrarObjeto.transform,
            "X",
            23f,
            TextAlignmentOptions.Center
        );
        Estirar(textoCerrar.rectTransform, 2f);

        textoProgreso = CrearTexto(
            "ProgresoNivel3",
            panelDesafios.transform,
            string.Empty,
            23f,
            TextAlignmentOptions.TopLeft
        );
        RectTransform progresoRect = textoProgreso.rectTransform;
        progresoRect.anchorMin = Vector2.zero;
        progresoRect.anchorMax = Vector2.one;
        progresoRect.offsetMin = new Vector2(34f, 80f);
        progresoRect.offsetMax = new Vector2(-28f, -82f);
        textoProgreso.lineSpacing = 10f;

        textoFinal = CrearTexto(
            "Nivel3Completado",
            panelDesafios.transform,
            "¡FELICIDADES!\nHas completado exitosamente los desafíos de nuestro juego.",
            23f,
            TextAlignmentOptions.Center
        );
        RectTransform finalRect = textoFinal.rectTransform;
        finalRect.anchorMin = new Vector2(0f, 0f);
        finalRect.anchorMax = new Vector2(1f, 0f);
        finalRect.pivot = new Vector2(0.5f, 0f);
        finalRect.offsetMin = new Vector2(24f, 14f);
        finalRect.offsetMax = new Vector2(-24f, 110f);
        textoFinal.fontStyle = FontStyles.Bold;
        textoFinal.color = new Color(0.46f, 1f, 0.48f, 1f);
        textoFinal.gameObject.SetActive(false);

        abrir.onClick.AddListener(() =>
        {
            panelDesafios.SetActive(true);
            panelDesafios.transform.SetAsLastSibling();
        });

        panelDesafios.SetActive(true);
        panelDesafios.transform.SetAsLastSibling();
    }

    private void CrearIndicadorMonedas(Canvas canvas)
    {
        panelMonedas = CrearObjetoUI("IndicadorMonedasNivel3", canvas.transform);
        RectTransform rect = panelMonedas.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-246f, -22f);
        rect.sizeDelta = new Vector2(130f, 60f);

        Image fondo = panelMonedas.AddComponent<Image>();
        fondo.sprite = CrearFondoRedondeado(130, 60, 16);
        fondo.color = new Color(0.25f, 0.42f, 0.20f, 0.97f);
        AgregarBorde(panelMonedas);

        GameObject moneda = CrearObjetoUI("IconoMoneda", panelMonedas.transform);
        RectTransform monedaRect = moneda.GetComponent<RectTransform>();
        monedaRect.anchorMin = new Vector2(0f, 0.5f);
        monedaRect.anchorMax = new Vector2(0f, 0.5f);
        monedaRect.pivot = new Vector2(0f, 0.5f);
        monedaRect.anchoredPosition = new Vector2(10f, 0f);
        monedaRect.sizeDelta = new Vector2(40f, 40f);
        Image monedaImagen = moneda.AddComponent<Image>();
        monedaImagen.sprite = CrearFondoRedondeado(40, 40, 20);
        monedaImagen.color = new Color(1f, 0.76f, 0.12f, 1f);

        TextMeshProUGUI simbolo = CrearTexto(
            "SimboloMoneda",
            moneda.transform,
            "$",
            26f,
            TextAlignmentOptions.Center
        );
        Estirar(simbolo.rectTransform, 1f);
        simbolo.fontStyle = FontStyles.Bold;
        simbolo.color = new Color(0.45f, 0.24f, 0.04f, 1f);

        textoMonedas = CrearTexto(
            "CantidadMonedas",
            panelMonedas.transform,
            "0",
            25f,
            TextAlignmentOptions.Center
        );
        RectTransform textoRect = textoMonedas.rectTransform;
        textoRect.anchorMin = new Vector2(0f, 0f);
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(50f, 4f);
        textoRect.offsetMax = new Vector2(-6f, -4f);
        textoMonedas.fontStyle = FontStyles.Bold;
        textoMonedas.color = new Color(1f, 0.93f, 0.68f, 1f);
    }

    private void ActualizarInterfaz()
    {
        if (textoMonedas != null)
            textoMonedas.text = monedas.ToString();

        if (textoProgreso != null)
        {
            textoProgreso.text =
                $"{Marca(clientesAtendidos, ObjetivoClientes)} Atender clientes: " +
                $"{Mathf.Min(clientesAtendidos, ObjetivoClientes)}/{ObjetivoClientes}\n\n" +
                $"[ ] Monedas obtenidas: ${monedas}\n\n" +
                "Abre el inventario y presiona VENDER junto a los productos pedidos.";
        }
    }

    private void MostrarFinal()
    {
        ActualizarInterfaz();

        if (textoFinal != null)
            textoFinal.gameObject.SetActive(true);

        if (panelDesafios != null)
        {
            panelDesafios.SetActive(true);
            panelDesafios.transform.SetAsLastSibling();
        }
    }

    private void TeletransportarJugador()
    {
        PlayerController jugador = FindFirstObjectByType<PlayerController>();
        Transform destino = BuscarTransformPorNombre("PuntoInicialnivel3");

        if (jugador == null || destino == null)
            return;

        CharacterController character = jugador.GetComponent<CharacterController>();
        if (character != null)
            character.enabled = false;

        jugador.transform.position = destino.position + Vector3.up * 0.1f;

        Transform atencion = BuscarTransformPorNombre("Atencionalcliente");
        if (atencion != null)
        {
            Vector3 direccion = atencion.position - jugador.transform.position;
            direccion.y = 0f;
            if (direccion.sqrMagnitude > 0.001f)
                jugador.transform.rotation = Quaternion.LookRotation(direccion);
        }

        if (character != null)
            character.enabled = true;
    }

    private void OcultarInterfazAnterior()
    {
        string[] nombres =
        {
            "BotonDesafios", "PanelDesafiosNivel1", "BotonDesafiosNivel2",
            "PanelDesafiosNivel2", "Panel Cultivo", "PanelAlimentar",
            "PanelRecolectar", "PanelOrdenarNivel2"
        };

        Transform[] todos = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform elemento in todos)
        {
            if (elemento != null && nombres.Contains(elemento.name))
                elemento.gameObject.SetActive(false);
        }
    }

    private void RefrescarInventario()
    {
        if (inventarioUI == null && inventario != null)
            inventarioUI = inventario.inventarioUI;

        if (inventarioUI != null)
            inventarioUI.Refrescar();
    }

    private Sprite ObtenerIcono(string producto)
    {
        if (inventario == null)
            inventario = InventarioProductos.BuscarPrincipal();

        return inventario != null ? inventario.ObtenerIcono(producto) : null;
    }

    private static float CalcularAlturaCliente(Transform cliente)
    {
        Renderer[] renderers = cliente.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return 2f;

        Bounds limites = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            limites.Encapsulate(renderers[i].bounds);

        return cliente.InverseTransformPoint(
            new Vector3(limites.center.x, limites.max.y, limites.center.z)
        ).y;
    }

    private static Transform BuscarTransformPorNombre(string nombre)
    {
        return FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        ).FirstOrDefault(t => t != null && t.name == nombre);
    }

    private static string NombreVisible(string tipo)
    {
        if (tipo.Equals("LecheVaca", StringComparison.OrdinalIgnoreCase))
            return "Leche vaca";
        if (tipo.Equals("LecheCabra", StringComparison.OrdinalIgnoreCase))
            return "Leche cabra";
        return tipo;
    }

    private static string Marca(int actual, int objetivo)
    {
        return actual >= objetivo ? "[LISTO]" : "[ ]";
    }

    private static GameObject CrearObjetoUI(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.layer = padre.gameObject.layer;
        objeto.transform.SetParent(padre, false);
        return objeto;
    }

    private static TextMeshProUGUI CrearTexto(
        string nombre,
        Transform padre,
        string contenido,
        float tamano,
        TextAlignmentOptions alineacion)
    {
        GameObject objeto = CrearObjetoUI(nombre, padre);
        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.alignment = alineacion;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.textWrappingMode = TextWrappingModes.Normal;
        return texto;
    }

    private static void Estirar(RectTransform rect, float margen)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margen, margen);
        rect.offsetMax = new Vector2(-margen, -margen);
    }

    private static void AgregarBorde(GameObject objeto)
    {
        Outline borde = objeto.AddComponent<Outline>();
        borde.effectColor = new Color(0.35f, 0.19f, 0.07f, 1f);
        borde.effectDistance = new Vector2(3f, -3f);
        borde.useGraphicAlpha = true;
    }

    private static Sprite CrearFondoRedondeado(int ancho, int alto, int radio)
    {
        Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;
        textura.hideFlags = HideFlags.HideAndDontSave;

        Color32 transparente = new Color32(255, 255, 255, 0);
        Color32 blanco = new Color32(255, 255, 255, 255);
        Color32[] pixeles = new Color32[ancho * alto];

        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                float esquinaX = x < radio
                    ? radio - x
                    : x >= ancho - radio ? x - (ancho - radio - 1) : 0f;
                float esquinaY = y < radio
                    ? radio - y
                    : y >= alto - radio ? y - (alto - radio - 1) : 0f;

                bool dentro = esquinaX == 0f || esquinaY == 0f ||
                    esquinaX * esquinaX + esquinaY * esquinaY <= radio * radio;

                pixeles[y * ancho + x] = dentro ? blanco : transparente;
            }
        }

        textura.SetPixels32(pixeles);
        textura.Apply();

        Sprite sprite = Sprite.Create(
            textura,
            new Rect(0f, 0f, ancho, alto),
            new Vector2(0.5f, 0.5f),
            100f
        );
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
