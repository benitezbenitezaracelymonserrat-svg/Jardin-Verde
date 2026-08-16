using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla los desafios del nivel 1 y crea una pizarra desplegable en pantalla.
/// Se crea automaticamente en SampleScene para evitar modificar la escena manualmente.
/// </summary>
public class GestorDesafiosNivel1 : MonoBehaviour
{
    public static GestorDesafiosNivel1 Instancia { get; private set; }
    public static float Progreso01 =>
        Instancia != null ? Instancia.CalcularProgreso01() : 0f;

    private const int ObjetivoParcelas = 6;

    // Cada zona se guarda una sola vez aunque tenga muchos slots individuales.
    private readonly HashSet<int> zonasPlantadas = new HashSet<int>();
    private readonly HashSet<int> zonasRegadas = new HashSet<int>();
    private readonly HashSet<int> slotsPlantados = new HashSet<int>();
    private readonly HashSet<int> slotsRegados = new HashSet<int>();
    private Animal[] animales = new Animal[0];

    private GameObject botonDesafios;
    private GameObject panelDesafios;
    private GameObject panelInventario;
    private TextMeshProUGUI textoProgreso;
    private TextMeshProUGUI textoCompletado;
    private GameObject botonContinuarNivel2;
    private GameObject botonReiniciarNivel;
    private GameObject botonPausaNivel;
    private TextMeshProUGUI textoBotonPausa;
    private bool anuncioFinalMostrado;
    private bool pausadoDesdePizarra;
    private float proximaRevisionAnimales;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearAutomaticamente()
    {
        // Por ahora SampleScene es el nivel 1. Esto impide que la pizarra aparezca
        // accidentalmente en el menu o en los futuros niveles 2 y 3.
        if (SceneManager.GetActiveScene().name != "SampleScene")
            return;

        if (FindFirstObjectByType<GestorDesafiosNivel1>() != null)
            return;

        GameObject gestor = new GameObject("GestorDesafiosNivel1");
        gestor.AddComponent<GestorDesafiosNivel1>();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
    }

    private IEnumerator Start()
    {
        // Espera un frame para que el Canvas y los animales de la escena terminen de iniciar.
        yield return null;

        animales = FindObjectsByType<Animal>(FindObjectsSortMode.None);
        ConstruirInterfaz();
        ActualizarInterfaz();
    }

    private void Update()
    {
        if (Time.unscaledTime < proximaRevisionAnimales)
            return;

        proximaRevisionAnimales = Time.unscaledTime + 0.25f;
        ActualizarInterfaz();
    }

    public static void RegistrarCultivoGlobal(
        string herramienta,
        Object zonaCultivo,
        SlotParcela slot = null)
    {
        if (zonaCultivo == null)
            return;

        GestorDesafiosNivel1 gestor = Instancia;

        if (gestor == null)
            gestor = FindFirstObjectByType<GestorDesafiosNivel1>();

        if (gestor != null)
            gestor.RegistrarCultivo(
                herramienta,
                zonaCultivo.GetInstanceID(),
                slot != null ? slot.GetInstanceID() : 0
            );
    }

    private void RegistrarCultivo(string herramienta, int idZona, int idSlot)
    {
        if (string.Equals(herramienta, "semilla", System.StringComparison.OrdinalIgnoreCase))
        {
            zonasPlantadas.Add(idZona);

            if (idSlot != 0)
                slotsPlantados.Add(idSlot);
        }
        else if (string.Equals(herramienta, "regadera", System.StringComparison.OrdinalIgnoreCase))
        {
            zonasRegadas.Add(idZona);

            if (idSlot != 0)
                slotsRegados.Add(idSlot);
        }

        ActualizarInterfaz();
    }

    private void ConstruirInterfaz()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("GestorDesafiosNivel1: no se encontro un Canvas en la escena.");
            return;
        }

        ConfigurarEscaladoCanvas(canvas);
        AjustarBotonesCultivoNivel1(canvas);

        GameObject botonObjeto = CrearObjetoUI("BotonDesafios", canvas.transform);
        botonDesafios = botonObjeto;
        RectTransform botonRect = botonObjeto.GetComponent<RectTransform>();
        botonRect.anchorMin = new Vector2(1f, 1f);
        botonRect.anchorMax = new Vector2(1f, 1f);
        botonRect.pivot = new Vector2(1f, 1f);
        botonRect.anchoredPosition = new Vector2(-96f, -22f);
        botonRect.sizeDelta = new Vector2(140f, 60f);

        Image imagenBoton = botonObjeto.AddComponent<Image>();
        imagenBoton.sprite = CrearFondoRedondeado(140, 60, 16);
        imagenBoton.color = new Color(0.22f, 0.42f, 0.24f, 0.96f);
        AgregarBordeMarron(botonObjeto);

        Button botonAbrir = botonObjeto.AddComponent<Button>();
        botonAbrir.targetGraphic = imagenBoton;

        TextMeshProUGUI textoBotonTareas = CrearTexto(
            "TextoBotonTareas",
            botonObjeto.transform,
            "TAREAS",
            22f,
            TextAlignmentOptions.Center
        );
        Estirar(textoBotonTareas.rectTransform, 3f);
        textoBotonTareas.fontStyle = FontStyles.Bold;
        textoBotonTareas.color = new Color(1f, 0.93f, 0.68f, 1f);

        GameObject inventarioObjeto = CrearObjetoUI("BotonInventarioNivel1", canvas.transform);
        RectTransform inventarioRect = inventarioObjeto.GetComponent<RectTransform>();
        inventarioRect.anchorMin = new Vector2(1f, 1f);
        inventarioRect.anchorMax = new Vector2(1f, 1f);
        inventarioRect.pivot = new Vector2(1f, 1f);
        inventarioRect.anchoredPosition = new Vector2(-22f, -22f);
        inventarioRect.sizeDelta = new Vector2(64f, 60f);

        Image fondoInventario = inventarioObjeto.AddComponent<Image>();
        fondoInventario.sprite = CrearFondoRedondeado(64, 60, 16);
        fondoInventario.color = new Color(0.96f, 0.91f, 0.82f, 0.98f);
        AgregarBordeMarron(inventarioObjeto);

        GameObject iconoInventarioObjeto = CrearObjetoUI("IconoBotonInventario", inventarioObjeto.transform);
        RectTransform iconoInventarioRect = iconoInventarioObjeto.GetComponent<RectTransform>();
        Estirar(iconoInventarioRect, 5f);

        RawImage imagenInventario = iconoInventarioObjeto.AddComponent<RawImage>();
        imagenInventario.texture = Resources.Load<Texture2D>("UI/IconoInventario");
        imagenInventario.color = Color.white;
        // Acerca el dibujo central de la imagen para que sea legible en el boton.
        imagenInventario.uvRect = new Rect(0.18f, 0.30f, 0.64f, 0.47f);
        imagenInventario.raycastTarget = false;

        Button botonInventario = inventarioObjeto.AddComponent<Button>();
        botonInventario.targetGraphic = fondoInventario;

        panelDesafios = CrearObjetoUI("PanelDesafiosNivel1", canvas.transform);
        RectTransform panelRect = panelDesafios.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(22f, -86f);
        panelRect.sizeDelta = new Vector2(500f, 370f);

        Image imagenPanel = panelDesafios.AddComponent<Image>();
        imagenPanel.color = new Color(0.055f, 0.19f, 0.13f, 0.97f);

        Outline bordePanel = panelDesafios.AddComponent<Outline>();
        bordePanel.effectColor = new Color(0.48f, 0.28f, 0.10f, 1f);
        bordePanel.effectDistance = new Vector2(5f, -5f);

        TextMeshProUGUI titulo = CrearTexto(
            "TituloDesafios",
            panelDesafios.transform,
            "DESAFIOS - NIVEL 1",
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

        GameObject cerrarObjeto = CrearObjetoUI("BotonCerrarDesafios", panelDesafios.transform);
        RectTransform cerrarRect = cerrarObjeto.GetComponent<RectTransform>();
        cerrarRect.anchorMin = new Vector2(1f, 1f);
        cerrarRect.anchorMax = new Vector2(1f, 1f);
        cerrarRect.pivot = new Vector2(1f, 1f);
        cerrarRect.anchoredPosition = new Vector2(-12f, -12f);
        cerrarRect.sizeDelta = new Vector2(42f, 42f);

        Image imagenCerrar = cerrarObjeto.AddComponent<Image>();
        imagenCerrar.color = new Color(0.48f, 0.12f, 0.10f, 0.95f);

        Button botonCerrar = cerrarObjeto.AddComponent<Button>();
        botonCerrar.targetGraphic = imagenCerrar;

        TextMeshProUGUI textoCerrar = CrearTexto(
            "TextoCerrarDesafios",
            cerrarObjeto.transform,
            "X",
            23f,
            TextAlignmentOptions.Center
        );
        Estirar(textoCerrar.rectTransform, 2f);

        textoProgreso = CrearTexto(
            "TextoProgresoDesafios",
            panelDesafios.transform,
            string.Empty,
            23f,
            TextAlignmentOptions.TopLeft
        );
        RectTransform progresoRect = textoProgreso.rectTransform;
        progresoRect.anchorMin = new Vector2(0f, 0f);
        progresoRect.anchorMax = new Vector2(1f, 1f);
        progresoRect.offsetMin = new Vector2(34f, 160f);
        progresoRect.offsetMax = new Vector2(-28f, -82f);
        textoProgreso.color = Color.white;
        textoProgreso.lineSpacing = 14f;

        textoCompletado = CrearTexto(
            "TextoNivelCompletado",
            panelDesafios.transform,
            "¡FELICIDADES!\nCompletaste todos los desafios del nivel 1.",
            21f,
            TextAlignmentOptions.Center
        );
        RectTransform completadoRect = textoCompletado.rectTransform;
        completadoRect.anchorMin = new Vector2(0f, 0f);
        completadoRect.anchorMax = new Vector2(1f, 0f);
        completadoRect.pivot = new Vector2(0.5f, 0f);
        completadoRect.offsetMin = new Vector2(24f, 76f);
        completadoRect.offsetMax = new Vector2(-24f, 142f);
        textoCompletado.fontStyle = FontStyles.Bold;
        textoCompletado.color = new Color(0.46f, 1f, 0.48f, 1f);

        botonReiniciarNivel = CrearBotonFinal(
            "BotonReiniciarNivel1",
            panelDesafios.transform,
            "REINICIAR",
            new Vector2(-148f, 14f),
            new Color(0.55f, 0.25f, 0.15f, 1f),
            ReiniciarNivel
        );

        botonPausaNivel = CrearBotonFinal(
            "BotonPausaNivel1",
            panelDesafios.transform,
            "PAUSA",
            new Vector2(0f, 14f),
            new Color(0.63f, 0.45f, 0.16f, 1f),
            AlternarPausa
        );
        textoBotonPausa = botonPausaNivel
            .GetComponentInChildren<TextMeshProUGUI>(true);

        botonContinuarNivel2 = CrearBotonFinal(
            "BotonContinuarNivel2",
            panelDesafios.transform,
            "NIVEL 2",
            new Vector2(148f, 14f),
            new Color(0.31f, 0.55f, 0.25f, 1f),
            PasarAlNivel2
        );

        botonReiniciarNivel.SetActive(false);
        botonPausaNivel.SetActive(false);
        botonContinuarNivel2.SetActive(false);
        textoCompletado.gameObject.SetActive(false);

        ConfigurarInventario(botonInventario);

        botonAbrir.onClick.AddListener(() =>
        {
            if (panelInventario != null)
                panelInventario.SetActive(false);

            panelDesafios.SetActive(true);
            panelDesafios.transform.SetAsLastSibling();
        });
        botonCerrar.onClick.AddListener(() => panelDesafios.SetActive(false));

        panelDesafios.transform.SetAsLastSibling();
        panelDesafios.SetActive(false);
    }

    private GameObject CrearBotonFinal(
        string nombre,
        Transform padre,
        string etiqueta,
        Vector2 posicion,
        Color color,
        UnityEngine.Events.UnityAction accion)
    {
        GameObject objeto = CrearObjetoUI(nombre, padre);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = new Vector2(136f, 50f);

        Image fondo = objeto.AddComponent<Image>();
        fondo.sprite = CrearFondoRedondeado(136, 50, 13);
        fondo.color = color;
        AgregarBordeMarron(objeto);

        Button boton = objeto.AddComponent<Button>();
        boton.targetGraphic = fondo;
        boton.onClick.AddListener(accion);

        TextMeshProUGUI texto = CrearTexto(
            "Texto" + nombre,
            objeto.transform,
            etiqueta,
            17f,
            TextAlignmentOptions.Center
        );
        Estirar(texto.rectTransform, 3f);
        texto.fontStyle = FontStyles.Bold;
        texto.color = new Color(1f, 0.93f, 0.68f, 1f);
        return objeto;
    }

    private void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void AlternarPausa()
    {
        pausadoDesdePizarra = !pausadoDesdePizarra;
        Time.timeScale = pausadoDesdePizarra ? 0f : 1f;

        if (textoBotonPausa != null)
            textoBotonPausa.text = pausadoDesdePizarra ? "REANUDAR" : "PAUSA";
    }

    private void PasarAlNivel2()
    {
        pausadoDesdePizarra = false;
        Time.timeScale = 1f;
        panelDesafios.SetActive(false);

        if (botonDesafios != null)
            botonDesafios.SetActive(false);

        // El nivel 2 pide recoger exactamente lo que el jugador preparo
        // durante el nivel 1, sin contar cajas decorativas o slots no regados.
        GestorNivel2.IniciarNivel2Global(slotsRegados.Count);
    }

    private static void AjustarBotonesCultivoNivel1(Canvas canvas)
    {
        RectTransform panelCultivo = null;
        RectTransform panelAlimentar = null;
        GameObject botonSemilla = null;
        GameObject botonRegadera = null;
        GameObject botonCanasta = null;
        GameObject botonAlimentar = null;

        RectTransform[] elementos = canvas.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform elemento in elementos)
        {
            if (elemento.name == "Panel Cultivo")
                panelCultivo = elemento;
            else if (elemento.name == "PanelAlimentar")
                panelAlimentar = elemento;
            else if (elemento.name == "BotonSemilla")
                botonSemilla = elemento.gameObject;
            else if (elemento.name == "ButtonRegaderaNuevo")
                botonRegadera = elemento.gameObject;
            else if (elemento.name == "BotonCanasta")
                botonCanasta = elemento.gameObject;
            else if (elemento.name == "BotonAlimentar")
                botonAlimentar = elemento.gameObject;
        }

        if (panelCultivo == null || botonSemilla == null || botonRegadera == null)
        {
            Debug.LogWarning("GestorDesafiosNivel1: no se encontraron los botones de cultivo.");
            return;
        }

        // El panel se coloca directamente en el Canvas para que su posicion no dependa
        // del panel grande y desplazado que contiene los distintos grupos de acciones.
        panelCultivo.SetParent(canvas.transform, false);
        panelCultivo.anchorMin = new Vector2(0.5f, 0f);
        panelCultivo.anchorMax = new Vector2(0.5f, 0f);
        panelCultivo.pivot = new Vector2(0.5f, 0f);
        panelCultivo.anchoredPosition = new Vector2(0f, 18f);
        panelCultivo.sizeDelta = new Vector2(620f, 80f);
        panelCultivo.localScale = Vector3.one;

        botonSemilla.SetActive(true);
        botonRegadera.SetActive(true);

        // Cosechar se conserva para utilizarlo en el nivel 2, pero no aparece en nivel 1.
        if (botonCanasta != null)
            botonCanasta.SetActive(false);

        HorizontalLayoutGroup distribucion = panelCultivo.GetComponent<HorizontalLayoutGroup>();
        if (distribucion != null)
        {
            distribucion.childAlignment = TextAnchor.MiddleCenter;
            distribucion.spacing = 20f;
            distribucion.childForceExpandWidth = false;
            distribucion.childForceExpandHeight = false;
            distribucion.childControlWidth = false;
            distribucion.childControlHeight = false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelCultivo);

        if (panelAlimentar != null && botonAlimentar != null)
        {
            panelAlimentar.SetParent(canvas.transform, false);
            panelAlimentar.anchorMin = new Vector2(0.5f, 0f);
            panelAlimentar.anchorMax = new Vector2(0.5f, 0f);
            panelAlimentar.pivot = new Vector2(0.5f, 0f);
            panelAlimentar.anchoredPosition = new Vector2(0f, 18f);
            panelAlimentar.sizeDelta = new Vector2(300f, 80f);
            panelAlimentar.localScale = Vector3.one;
            botonAlimentar.SetActive(true);

            HorizontalLayoutGroup distribucionAlimentar =
                panelAlimentar.GetComponent<HorizontalLayoutGroup>();

            if (distribucionAlimentar != null)
            {
                distribucionAlimentar.childAlignment = TextAnchor.MiddleCenter;
                distribucionAlimentar.childForceExpandWidth = false;
                distribucionAlimentar.childForceExpandHeight = false;
                distribucionAlimentar.childControlWidth = false;
                distribucionAlimentar.childControlHeight = false;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelAlimentar);
        }
        else
        {
            Debug.LogWarning("GestorDesafiosNivel1: no se encontro el boton Alimentar.");
        }
    }

    private static void ConfigurarEscaladoCanvas(Canvas canvas)
    {
        CanvasScaler escalador = canvas.GetComponent<CanvasScaler>();
        if (escalador == null)
            return;

        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;
        escalador.referencePixelsPerUnit = 100f;
    }

    private static void AgregarBordeMarron(GameObject boton)
    {
        Outline borde = boton.AddComponent<Outline>();
        borde.effectColor = new Color(0.35f, 0.19f, 0.07f, 1f);
        borde.effectDistance = new Vector2(3f, -3f);
        borde.useGraphicAlpha = true;
    }

    private static Sprite CrearFondoRedondeado(int ancho, int alto, int radio)
    {
        Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        textura.name = $"FondoRedondeado_{ancho}x{alto}";
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
                    (esquinaX * esquinaX) + (esquinaY * esquinaY) <= radio * radio;

                pixeles[(y * ancho) + x] = dentro ? blanco : transparente;
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
        sprite.name = textura.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private void ConfigurarInventario(Button botonInventario)
    {
        InventarioUI controladorInventario = FindFirstObjectByType<InventarioUI>();

        if (controladorInventario == null || controladorInventario.panelInventario == null)
        {
            botonInventario.interactable = false;
            Debug.LogWarning("GestorDesafiosNivel1: no se encontro PanelInventario.");
            return;
        }

        panelInventario = controladorInventario.panelInventario;

        botonInventario.onClick.AddListener(() =>
        {
            if (panelDesafios != null)
                panelDesafios.SetActive(false);

            panelInventario.SetActive(true);
            panelInventario.transform.SetAsLastSibling();
        });

        Button botonCerrarInventario = null;
        Button[] botonesInventario = panelInventario.GetComponentsInChildren<Button>(true);

        foreach (Button boton in botonesInventario)
        {
            if (boton.name == "BtnCerrar")
            {
                botonCerrarInventario = boton;
                break;
            }
        }

        if (botonCerrarInventario != null)
        {
            botonCerrarInventario.onClick.AddListener(() => panelInventario.SetActive(false));
        }
        else
        {
            Debug.LogWarning("GestorDesafiosNivel1: no se encontro BtnCerrar dentro de PanelInventario.");
        }
    }

    private void ActualizarInterfaz()
    {
        if (textoProgreso == null)
            return;

        int totalAnimales = animales != null ? animales.Length : 0;
        int alimentados = 0;

        if (animales != null)
        {
            foreach (Animal animal in animales)
            {
                if (animal != null && animal.FueAlimentado)
                    alimentados++;
            }
        }

        textoProgreso.text =
            $"{Marca(zonasPlantadas.Count, ObjetivoParcelas)} Plantar zonas: " +
            $"{Mathf.Min(zonasPlantadas.Count, ObjetivoParcelas)}/{ObjetivoParcelas}\n\n" +
            $"{Marca(zonasRegadas.Count, ObjetivoParcelas)} Regar zonas: " +
            $"{Mathf.Min(zonasRegadas.Count, ObjetivoParcelas)}/{ObjetivoParcelas}\n\n" +
            $"{Marca(alimentados, totalAnimales)} Alimentar animales: " +
            $"{alimentados}/{totalAnimales}";

        bool completado =
            zonasPlantadas.Count >= ObjetivoParcelas &&
            zonasRegadas.Count >= ObjetivoParcelas &&
            totalAnimales > 0 &&
            alimentados >= totalAnimales;

        if (textoCompletado != null)
            textoCompletado.gameObject.SetActive(completado);

        if (botonReiniciarNivel != null)
            botonReiniciarNivel.SetActive(completado);

        if (botonPausaNivel != null)
            botonPausaNivel.SetActive(completado);

        if (botonContinuarNivel2 != null)
            botonContinuarNivel2.SetActive(completado);

        if (completado && !anuncioFinalMostrado && panelDesafios != null)
        {
            anuncioFinalMostrado = true;

            if (panelInventario != null)
                panelInventario.SetActive(false);

            panelDesafios.SetActive(true);
            panelDesafios.transform.SetAsLastSibling();
        }
    }

    private float CalcularProgreso01()
    {
        int totalAnimales = animales != null ? animales.Length : 0;
        int alimentados = 0;

        if (animales != null)
        {
            foreach (Animal animal in animales)
            {
                if (animal != null && animal.FueAlimentado)
                    alimentados++;
            }
        }

        float plantado = Mathf.Clamp01(
            zonasPlantadas.Count / (float)ObjetivoParcelas
        );
        float regado = Mathf.Clamp01(
            zonasRegadas.Count / (float)ObjetivoParcelas
        );
        float animalesListos = totalAnimales > 0
            ? Mathf.Clamp01(alimentados / (float)totalAnimales)
            : 0f;

        return (plantado + regado + animalesListos) / 3f;
    }

    private static string Marca(int actual, int objetivo)
    {
        return objetivo > 0 && actual >= objetivo ? "[LISTO]" : "[ ]";
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

    private static void Estirar(RectTransform rectTransform, float margen)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(margen, margen);
        rectTransform.offsetMax = new Vector2(-margen, -margen);
    }
}
