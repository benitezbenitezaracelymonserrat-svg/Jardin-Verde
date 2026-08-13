using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Crea el libro de instrucciones y mantiene ordenados los accesos superiores.
/// Libro y tareas quedan a la izquierda; inventario y monedas a la derecha.
/// </summary>
public class ManualJuegoUI : MonoBehaviour
{
    private GameObject botonLibro;
    private GameObject panelManual;
    private float proximaOrganizacion;
    private bool esperandoFinCinematica;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearAutomaticamente()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
            return;

        if (FindFirstObjectByType<ManualJuegoUI>() == null)
            new GameObject("ManualJuegoUI").AddComponent<ManualJuegoUI>();
    }

    private void Start()
    {
        ConstruirManual();

        esperandoFinCinematica = CinematicaIntro.cinematicaActiva;
        if (esperandoFinCinematica)
            OcultarInterfazSuperior();
        else
            OrganizarBarraSuperior();
    }

    private void OnEnable()
    {
        CinematicaIntro.CinematicaIniciada += AlIniciarCinematica;
        CinematicaIntro.CinematicaTerminada += AlTerminarCinematica;
    }

    private void OnDisable()
    {
        CinematicaIntro.CinematicaIniciada -= AlIniciarCinematica;
        CinematicaIntro.CinematicaTerminada -= AlTerminarCinematica;
    }

    private void Update()
    {
        if (Time.unscaledTime < proximaOrganizacion)
            return;

        proximaOrganizacion = Time.unscaledTime + 0.5f;

        if (CinematicaIntro.cinematicaActiva)
        {
            esperandoFinCinematica = true;
            if (panelManual != null)
                panelManual.SetActive(false);
            OcultarInterfazSuperior();
            return;
        }

        if (esperandoFinCinematica)
        {
            esperandoFinCinematica = false;
            AbrirManual();
            return;
        }

        if (panelManual != null && panelManual.activeSelf)
            OcultarInterfazSuperior();
        else
            OrganizarBarraSuperior();
    }

    private void AlIniciarCinematica()
    {
        esperandoFinCinematica = true;
        if (panelManual != null)
            panelManual.SetActive(false);
        OcultarInterfazSuperior();
    }

    private void AlTerminarCinematica()
    {
        esperandoFinCinematica = false;
        AbrirManual();
    }

    private void ConstruirManual()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        botonLibro = CrearObjetoUI("BotonManualLibro", canvas.transform);
        RectTransform botonRect = botonLibro.GetComponent<RectTransform>();
        botonRect.anchorMin = new Vector2(0f, 1f);
        botonRect.anchorMax = new Vector2(0f, 1f);
        botonRect.pivot = new Vector2(0f, 1f);
        botonRect.anchoredPosition = new Vector2(22f, -22f);
        botonRect.sizeDelta = new Vector2(64f, 60f);

        Image fondo = botonLibro.AddComponent<Image>();
        fondo.sprite = CrearFondoRedondeado(64, 60, 14);
        fondo.color = new Color(0.25f, 0.43f, 0.22f, 0.98f);
        AgregarBorde(botonLibro);
        Button boton = botonLibro.AddComponent<Button>();
        boton.targetGraphic = fondo;

        CrearPaginasIcono(botonLibro.transform);

        panelManual = CrearObjetoUI("PanelManualJuego", canvas.transform);
        RectTransform panelRect = panelManual.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(850f, 610f);

        Image panelFondo = panelManual.AddComponent<Image>();
        panelFondo.sprite = CrearFondoRedondeado(850, 610, 20);
        panelFondo.color = new Color(0.96f, 0.86f, 0.66f, 0.99f);
        AgregarBorde(panelManual);

        TextMeshProUGUI titulo = CrearTexto(
            "TituloManual",
            panelManual.transform,
            "MANUAL DE LA GRANJA",
            34f,
            TextAlignmentOptions.Center
        );
        RectTransform tituloRect = titulo.rectTransform;
        tituloRect.anchorMin = new Vector2(0f, 1f);
        tituloRect.anchorMax = new Vector2(1f, 1f);
        tituloRect.pivot = new Vector2(0.5f, 1f);
        tituloRect.offsetMin = new Vector2(70f, -72f);
        tituloRect.offsetMax = new Vector2(-70f, -16f);
        titulo.color = new Color(0.30f, 0.16f, 0.06f, 1f);
        titulo.fontStyle = FontStyles.Bold;

        CrearSeparador(panelManual.transform);
        CrearPaginaNivel(
            panelManual.transform,
            "NIVEL 1",
            "• Planta y riega las 6 zonas de cultivos.\n" +
            "• Llena los comederos para alimentar a todos los animales.\n" +
            "• Acércate y presiona E o usa los botones inferiores.",
            new Vector2(28f, -92f)
        );
        CrearPaginaNivel(
            panelManual.transform,
            "NIVEL 2",
            "• Cosecha todos los cultivos de cada zona.\n" +
            "• Recoge las cajas una por una con RECOGER o E.\n" +
            "• Abre las puertas con E, ordeña vacas y cabras.\n" +
            "• Recoge los baldes de leche y los huevos.",
            new Vector2(430f, -92f)
        );
        CrearPaginaNivel(
            panelManual.transform,
            "NIVEL 3",
            "• Mira el pedido que aparece sobre el cliente.\n" +
            "• Abre el inventario y pulsa VENDER en cada producto pedido.\n" +
            "• Atiende al menos a 5 clientes para completar el juego.\n" +
            "• Precios: verduras $5; calabaza/lechuga $8; huevo $3; leche $6.",
            new Vector2(28f, -330f),
            new Vector2(794f, 190f)
        );

        GameObject cerrarObjeto = CrearObjetoUI("CerrarManual", panelManual.transform);
        RectTransform cerrarRect = cerrarObjeto.GetComponent<RectTransform>();
        cerrarRect.anchorMin = Vector2.one;
        cerrarRect.anchorMax = Vector2.one;
        cerrarRect.pivot = Vector2.one;
        cerrarRect.anchoredPosition = new Vector2(-16f, -14f);
        cerrarRect.sizeDelta = new Vector2(46f, 42f);
        Image cerrarFondo = cerrarObjeto.AddComponent<Image>();
        cerrarFondo.sprite = CrearFondoRedondeado(46, 42, 10);
        cerrarFondo.color = new Color(0.55f, 0.15f, 0.10f, 1f);
        Button cerrar = cerrarObjeto.AddComponent<Button>();
        cerrar.targetGraphic = cerrarFondo;
        cerrar.onClick.AddListener(CerrarManual);
        TextMeshProUGUI x = CrearTexto(
            "TextoCerrarManual", cerrarObjeto.transform, "X", 24f,
            TextAlignmentOptions.Center
        );
        Estirar(x.rectTransform, 2f);
        x.fontStyle = FontStyles.Bold;

        boton.onClick.AddListener(() =>
        {
            if (panelManual.activeSelf)
                CerrarManual();
            else
                AbrirManual();
        });

        panelManual.SetActive(false);
    }

    private void AbrirManual()
    {
        if (panelManual == null || CinematicaIntro.cinematicaActiva)
            return;

        panelManual.SetActive(true);
        panelManual.transform.SetAsLastSibling();
        OcultarInterfazSuperior();
    }

    private void CerrarManual()
    {
        if (panelManual != null)
            panelManual.SetActive(false);

        RestaurarInterfazSuperior();
        OrganizarBarraSuperior();
    }

    private static readonly string[] NombresInterfazSuperior =
    {
        "BotonManualLibro",
        "BotonDesafios",
        "BotonDesafiosNivel2",
        "BotonDesafiosNivel3",
        "BotonInventarioNivel1",
        "Btn_Inventario",
        "IndicadorMonedasNivel3"
    };

    private static void OcultarInterfazSuperior()
    {
        RectTransform[] elementos = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (RectTransform elemento in elementos)
        {
            if (elemento != null &&
                System.Array.IndexOf(NombresInterfazSuperior, elemento.name) >= 0)
            {
                elemento.gameObject.SetActive(false);
            }
        }
    }

    private static void RestaurarInterfazSuperior()
    {
        bool nivel3 = GestorNivel3.NivelActivo;
        bool nivel2 = GestorNivel2.NivelActivo && !nivel3;

        EstablecerActivo("BotonManualLibro", true);
        EstablecerActivo("BotonDesafios", !nivel2 && !nivel3);
        EstablecerActivo("BotonDesafiosNivel2", nivel2);
        EstablecerActivo("BotonDesafiosNivel3", nivel3);
        EstablecerActivo("IndicadorMonedasNivel3", nivel3);

        bool existeBotonNuevo = ExisteObjeto("BotonInventarioNivel1");
        EstablecerActivo("BotonInventarioNivel1", existeBotonNuevo);
        EstablecerActivo("Btn_Inventario", !existeBotonNuevo);
    }

    private static bool ExisteObjeto(string nombre)
    {
        RectTransform[] elementos = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (RectTransform elemento in elementos)
        {
            if (elemento != null && elemento.name == nombre)
                return true;
        }

        return false;
    }

    private static void EstablecerActivo(string nombre, bool activo)
    {
        RectTransform[] elementos = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (RectTransform elemento in elementos)
        {
            if (elemento != null && elemento.name == nombre)
                elemento.gameObject.SetActive(activo);
        }
    }

    private void OrganizarBarraSuperior()
    {
        RectTransform tareas = BuscarRectActivo(
            "BotonDesafiosNivel3", "BotonDesafiosNivel2", "BotonDesafios"
        );
        RectTransform inventario = BuscarRectActivo(
            "BotonInventarioNivel1", "Btn_Inventario"
        );
        RectTransform monedas = BuscarRectActivo("IndicadorMonedasNivel3");

        if (botonLibro != null)
            ColocarIzquierda(botonLibro.GetComponent<RectTransform>(), 22f, 64f);

        if (tareas != null)
            ColocarIzquierda(tareas, 96f, 140f);

        if (monedas != null)
        {
            ColocarDerecha(monedas, 12f, 130f);
            if (inventario != null)
                ColocarDerecha(inventario, 152f, 64f);
        }
        else if (inventario != null)
        {
            ColocarDerecha(inventario, 12f, 64f);
        }
    }

    private static void ColocarIzquierda(RectTransform rect, float x, float ancho)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -22f);
        rect.sizeDelta = new Vector2(ancho, 60f);
        rect.localScale = Vector3.one;
    }

    private static void ColocarDerecha(RectTransform rect, float margen, float ancho)
    {
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-margen, -22f);
        rect.sizeDelta = new Vector2(ancho, 60f);
        rect.localScale = Vector3.one;
    }

    private static RectTransform BuscarRectActivo(params string[] nombres)
    {
        RectTransform[] rects = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (string nombre in nombres)
        {
            foreach (RectTransform rect in rects)
            {
                if (rect != null && rect.name == nombre &&
                    rect.gameObject.activeInHierarchy)
                {
                    return rect;
                }
            }
        }

        return null;
    }

    private static void CrearPaginasIcono(Transform padre)
    {
        GameObject izquierda = CrearObjetoUI("PaginaIzquierda", padre);
        RectTransform izqRect = izquierda.GetComponent<RectTransform>();
        izqRect.anchorMin = new Vector2(0.5f, 0.5f);
        izqRect.anchorMax = new Vector2(0.5f, 0.5f);
        izqRect.pivot = new Vector2(1f, 0.5f);
        izqRect.anchoredPosition = new Vector2(-1f, 0f);
        izqRect.sizeDelta = new Vector2(22f, 36f);
        Image izq = izquierda.AddComponent<Image>();
        izq.color = new Color(1f, 0.91f, 0.70f, 1f);

        GameObject derecha = CrearObjetoUI("PaginaDerecha", padre);
        RectTransform derRect = derecha.GetComponent<RectTransform>();
        derRect.anchorMin = new Vector2(0.5f, 0.5f);
        derRect.anchorMax = new Vector2(0.5f, 0.5f);
        derRect.pivot = new Vector2(0f, 0.5f);
        derRect.anchoredPosition = new Vector2(1f, 0f);
        derRect.sizeDelta = new Vector2(22f, 36f);
        Image der = derecha.AddComponent<Image>();
        der.color = new Color(1f, 0.91f, 0.70f, 1f);

        TextMeshProUGUI signo = CrearTexto(
            "SignoManual", padre, "?", 25f, TextAlignmentOptions.Center
        );
        RectTransform signoRect = signo.rectTransform;
        signoRect.anchorMin = new Vector2(0.5f, 0.5f);
        signoRect.anchorMax = new Vector2(0.5f, 0.5f);
        signoRect.pivot = new Vector2(0.5f, 0.5f);
        signoRect.anchoredPosition = new Vector2(0f, -1f);
        signoRect.sizeDelta = new Vector2(44f, 36f);
        signo.color = new Color(0.38f, 0.19f, 0.07f, 1f);
        signo.fontStyle = FontStyles.Bold;
    }

    private static void CrearSeparador(Transform padre)
    {
        GameObject separador = CrearObjetoUI("LomoLibro", padre);
        RectTransform rect = separador.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -28f);
        rect.sizeDelta = new Vector2(4f, -105f);
        Image imagen = separador.AddComponent<Image>();
        imagen.color = new Color(0.56f, 0.35f, 0.16f, 0.55f);
    }

    private static void CrearPaginaNivel(
        Transform padre,
        string titulo,
        string contenido,
        Vector2 posicion,
        Vector2? tamanoOpcional = null)
    {
        GameObject pagina = CrearObjetoUI("Pagina_" + titulo, padre);
        RectTransform rect = pagina.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamanoOpcional ?? new Vector2(390f, 222f);

        Image fondo = pagina.AddComponent<Image>();
        fondo.color = new Color(1f, 0.95f, 0.82f, 0.72f);

        TextMeshProUGUI texto = CrearTexto(
            "Texto_" + titulo,
            pagina.transform,
            $"<b>{titulo}</b>\n\n{contenido}",
            19f,
            TextAlignmentOptions.TopLeft
        );
        Estirar(texto.rectTransform, 18f);
        texto.color = new Color(0.28f, 0.15f, 0.055f, 1f);
        texto.lineSpacing = 5f;
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
