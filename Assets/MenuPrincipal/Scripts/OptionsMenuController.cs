using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuController : MonoBehaviour
{
    private const string BrightnessKey = "AldeaVerde.Menu.Brightness";

    private static readonly Color Pergamino = new Color32(248, 222, 158, 238);
    private static readonly Color Marron = new Color32(91, 48, 19, 255);
    private static readonly Color MarronClaro = new Color32(155, 94, 42, 255);
    private static readonly Color VerdeOscuro = new Color32(57, 91, 34, 255);
    private static readonly Color VerdeClaro = new Color32(111, 178, 57, 255);
    private static readonly Color Crema = new Color32(255, 244, 207, 255);
    private static readonly Color Dorado = new Color32(232, 161, 48, 255);

    [Header("Decoracion de Opciones")]
    [SerializeField] private Sprite perillaMadera;
    [SerializeField] private Sprite rielMadera;

    private MusicMenu audioMenu;
    private Image capaBrillo;
    private float brillo = 1f;
    private bool inicializado;
    private static Sprite spriteRedondeado;

    public void Inicializar(MusicMenu controladorAudio)
    {
        if (inicializado)
        {
            return;
        }

        audioMenu = controladorAudio;
        brillo = PlayerPrefs.GetFloat(BrightnessKey, 1f);

        GameObject panelOpciones = BuscarObjetoInclusoInactivo("panOpciones");
        if (panelOpciones == null)
        {
            Debug.LogWarning("No se encontro el panel panOpciones.");
            return;
        }

        PrepararPanel(panelOpciones.transform);
        PrepararEncabezado(panelOpciones.transform);

        PrepararSeccion(
            panelOpciones.transform,
            "SeccionMusica",
            new Vector2(0f, 112f),
            audioMenu.VolumenMusica,
            audioMenu.CambiarVolumenMusica
        );

        PrepararSeccion(
            panelOpciones.transform,
            "SeccionSonido",
            new Vector2(0f, -18f),
            audioMenu.VolumenEfectos,
            audioMenu.CambiarVolumenEfectos
        );

        PrepararSeccion(
            panelOpciones.transform,
            "SeccionBrillo",
            new Vector2(0f, -148f),
            brillo,
            CambiarBrillo
        );

        CrearCapaDeBrillo(panelOpciones.transform.root);
        AplicarBrillo();
        inicializado = true;
    }

    private void PrepararPanel(Transform panel)
    {
        // panOpciones tenia una segunda ilustracion semitransparente. El marco
        // verdadero es FondoOpciones, por eso se desactiva solo esta copia.
        Image fondoDuplicado = panel.GetComponent<Image>();
        if (fondoDuplicado != null)
        {
            fondoDuplicado.enabled = false;
            fondoDuplicado.raycastTarget = false;
        }

        Transform fondoCorrecto = panel.Find("FondoOpciones");
        if (fondoCorrecto != null)
        {
            Image imagen = fondoCorrecto.GetComponent<Image>();
            if (imagen != null)
            {
                imagen.raycastTarget = false;
            }
        }
    }

    private void PrepararEncabezado(Transform panel)
    {
        Transform titulo = panel.Find("TextOpciones");
        if (titulo == null)
        {
            return;
        }

        RectTransform rectTitulo = titulo.GetComponent<RectTransform>();
        // Se deja respirar el borde superior del marco y se centra el titulo
        // sobre el contenido, no sobre el boton de cierre.
        ConfigurarRect(rectTitulo, new Vector2(0f, 250f), new Vector2(560f, 96f));

        TMP_Text textoTitulo = titulo.GetComponent<TMP_Text>();
        if (textoTitulo != null)
        {
            textoTitulo.text = "OPCIONES";
            textoTitulo.alignment = TextAlignmentOptions.Center;
            textoTitulo.fontSize = 62f;
            textoTitulo.fontStyle = FontStyles.Bold;
            textoTitulo.color = Color.white;
            textoTitulo.enableVertexGradient = true;
            textoTitulo.colorGradient = new VertexGradient(
                new Color32(255, 239, 196, 255),
                new Color32(255, 239, 196, 255),
                new Color32(210, 139, 43, 255),
                new Color32(210, 139, 43, 255));
            textoTitulo.raycastTarget = false;

            Outline bordeTitulo = titulo.GetComponent<Outline>();
            if (bordeTitulo == null)
            {
                bordeTitulo = titulo.gameObject.AddComponent<Outline>();
            }

            bordeTitulo.effectColor = new Color32(103, 53, 18, 255);
            bordeTitulo.effectDistance = new Vector2(2.5f, -2.5f);
            bordeTitulo.useGraphicAlpha = true;

            Shadow sombra = null;
            foreach (Shadow efecto in titulo.GetComponents<Shadow>())
            {
                if (!(efecto is Outline))
                {
                    sombra = efecto;
                    break;
                }
            }

            if (sombra == null)
            {
                sombra = titulo.gameObject.AddComponent<Shadow>();
            }

            sombra.effectColor = new Color32(91, 45, 13, 205);
            sombra.effectDistance = new Vector2(4f, -4f);
            sombra.useGraphicAlpha = true;
        }

        Transform cartelExistente = panel.Find("CartelTituloOpciones");
        if (cartelExistente != null)
        {
            cartelExistente.gameObject.SetActive(false);
        }

        titulo.SetAsLastSibling();
        Transform cerrar = panel.Find("Cerrar");
        if (cerrar != null)
        {
            cerrar.SetAsLastSibling();
        }
    }

    private void PrepararSeccion(
        Transform panel,
        string nombre,
        Vector2 posicion,
        float valorInicial,
        UnityEngine.Events.UnityAction<float> alCambiar)
    {
        Transform seccion = BuscarDescendiente(panel, nombre);
        if (seccion == null)
        {
            Debug.LogWarning($"No se encontro {nombre} dentro de Opciones.");
            return;
        }

        RectTransform rect = seccion.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(820f, 116f);
        rect.anchoredPosition = posicion;

        Image fondoSeccion = seccion.GetComponent<Image>();
        if (fondoSeccion != null)
        {
            fondoSeccion.sprite = ObtenerSpriteRedondeado();
            fondoSeccion.type = Image.Type.Sliced;
            fondoSeccion.color = Pergamino;
            fondoSeccion.raycastTarget = false;

            Outline borde = seccion.GetComponent<Outline>();
            if (borde == null)
            {
                borde = seccion.gameObject.AddComponent<Outline>();
            }

            borde.effectColor = new Color32(165, 105, 45, 150);
            borde.effectDistance = new Vector2(2f, -2f);
            borde.useGraphicAlpha = true;
        }

        AcomodarContenidoExistente(seccion);

        Transform controlExistente = seccion.Find("ControlVolumen");
        Slider slider = controlExistente != null
            ? controlExistente.GetComponent<Slider>()
            : null;

        if (slider == null)
        {
            slider = CrearSlider(seccion);
        }

        Transform porcentajeExistente = seccion.Find("Porcentaje");
        TMP_Text porcentaje = porcentajeExistente != null
            ? porcentajeExistente.GetComponent<TMP_Text>()
            : null;

        if (porcentaje == null)
        {
            porcentaje = CrearPorcentaje(seccion);
        }

        Image rellenoBateria = EstilizarSlider(slider);
        EstilizarPorcentaje(seccion, porcentaje);
        CrearMarcasDeEscala(seccion, porcentaje.font);

        slider.SetValueWithoutNotify(valorInicial);
        ActualizarBateria(rellenoBateria, slider, valorInicial);
        porcentaje.text = FormatearPorcentaje(valorInicial);
        slider.onValueChanged.AddListener(valor =>
        {
            alCambiar.Invoke(valor);
            ActualizarBateria(rellenoBateria, slider, valor);
            porcentaje.text = FormatearPorcentaje(valor);
        });
    }

    private void AcomodarContenidoExistente(Transform seccion)
    {
        foreach (RectTransform hijo in seccion.GetComponentsInChildren<RectTransform>(true))
        {
            if (hijo == seccion)
            {
                continue;
            }

            if (hijo.name.StartsWith("Icon"))
            {
                hijo.anchorMin = hijo.anchorMax = new Vector2(0.5f, 0.5f);
                hijo.sizeDelta = new Vector2(96f, 96f);
                hijo.anchoredPosition = new Vector2(-350f, 0f);

                Image icono = hijo.GetComponent<Image>();
                if (icono != null)
                {
                    icono.preserveAspect = true;
                    icono.raycastTarget = false;
                }
            }
            else if (hijo.name.StartsWith("Tex"))
            {
                hijo.anchorMin = hijo.anchorMax = new Vector2(0.5f, 0.5f);
                hijo.sizeDelta = new Vector2(190f, 60f);
                hijo.anchoredPosition = new Vector2(-200f, 2f);

                TMP_Text etiqueta = hijo.GetComponent<TMP_Text>();
                if (etiqueta != null)
                {
                    if (seccion.name == "SeccionMusica") etiqueta.text = "MÚSICA";
                    if (seccion.name == "SeccionSonido") etiqueta.text = "SONIDO";
                    if (seccion.name == "SeccionBrillo") etiqueta.text = "BRILLO";
                    etiqueta.alignment = TextAlignmentOptions.MidlineLeft;
                    etiqueta.fontSize = 34f;
                    etiqueta.fontStyle = FontStyles.Bold;
                    etiqueta.color = Marron;
                    etiqueta.raycastTarget = false;
                }
            }
        }
    }

    private Image EstilizarSlider(Slider slider)
    {
        RectTransform rect = slider.GetComponent<RectTransform>();
        ConfigurarRect(rect, new Vector2(92f, 9f), new Vector2(344f, 56f));

        Image areaInteractiva = slider.GetComponent<Image>();
        if (areaInteractiva != null)
        {
            areaInteractiva.color = new Color(1f, 1f, 1f, 0.001f);
            areaInteractiva.raycastTarget = true;
        }

        Transform pistaExistente = slider.transform.Find("Pista");
        GameObject pista = pistaExistente != null
            ? pistaExistente.gameObject
            : CrearImagen("Pista", slider.transform, MarronClaro);
        ConfigurarRect(pista.GetComponent<RectTransform>(), Vector2.zero, new Vector2(420f, 56f));

        Image imagenPista = pista.GetComponent<Image>();
        imagenPista.sprite = rielMadera != null
            ? rielMadera
            : ObtenerSpriteRedondeado();
        imagenPista.type = Image.Type.Simple;
        imagenPista.preserveAspect = false;
        imagenPista.color = Color.white;
        imagenPista.raycastTarget = false;
        pista.transform.SetAsFirstSibling();

        // El relleno original del Slider se estiraba por encima de la madera.
        // Se conserva el control y su tirador, pero el nivel se dibuja aparte
        // dentro de la ranura del riel, como el indicador de una bateria.
        if (slider.fillRect != null)
        {
            Image imagenRelleno = slider.fillRect.GetComponent<Image>();
            if (imagenRelleno != null)
            {
                imagenRelleno.enabled = false;
                imagenRelleno.raycastTarget = false;
            }
        }

        slider.fillRect = null;

        Transform fondoBateriaExistente = slider.transform.Find("BateriaFondo");
        GameObject fondoBateria = fondoBateriaExistente != null
            ? fondoBateriaExistente.gameObject
            : CrearImagen("BateriaFondo", slider.transform, VerdeOscuro);
        ConfigurarRect(fondoBateria.GetComponent<RectTransform>(), Vector2.zero, new Vector2(318f, 18f));
        Image imagenFondoBateria = fondoBateria.GetComponent<Image>();
        imagenFondoBateria.sprite = ObtenerSpriteRedondeado();
        imagenFondoBateria.type = Image.Type.Sliced;
        imagenFondoBateria.color = new Color32(48, 75, 27, 235);
        imagenFondoBateria.raycastTarget = false;
        fondoBateria.transform.SetSiblingIndex(1);

        Transform rellenoBateriaExistente = slider.transform.Find("BateriaRelleno");
        GameObject bateria = rellenoBateriaExistente != null
            ? rellenoBateriaExistente.gameObject
            : CrearImagen("BateriaRelleno", slider.transform, VerdeClaro);
        ConfigurarRect(bateria.GetComponent<RectTransform>(), Vector2.zero, new Vector2(310f, 14f));
        Image imagenBateria = bateria.GetComponent<Image>();
        imagenBateria.sprite = ObtenerSpriteRedondeado();
        imagenBateria.type = Image.Type.Filled;
        imagenBateria.fillMethod = Image.FillMethod.Horizontal;
        imagenBateria.fillOrigin = 0;
        imagenBateria.fillClockwise = true;
        imagenBateria.color = new Color32(116, 194, 53, 255);
        imagenBateria.raycastTarget = false;
        bateria.transform.SetSiblingIndex(2);

        Shadow brilloBateria = bateria.GetComponent<Shadow>();
        if (brilloBateria == null)
        {
            brilloBateria = bateria.AddComponent<Shadow>();
        }

        brilloBateria.effectColor = new Color32(255, 234, 121, 105);
        brilloBateria.effectDistance = new Vector2(0f, 2f);
        brilloBateria.useGraphicAlpha = true;

        for (int indice = 1; indice <= 4; indice++)
        {
            string nombreSeparador = $"BateriaSeparador{indice}";
            Transform separadorExistente = slider.transform.Find(nombreSeparador);
            GameObject separador = separadorExistente != null
                ? separadorExistente.gameObject
                : CrearImagen(nombreSeparador, slider.transform, Color.white);
            float posicionX = Mathf.Lerp(-155f, 155f, indice / 5f);
            ConfigurarRect(separador.GetComponent<RectTransform>(), new Vector2(posicionX, 0f), new Vector2(2f, 12f));
            Image imagenSeparador = separador.GetComponent<Image>();
            imagenSeparador.color = new Color32(245, 216, 120, 115);
            imagenSeparador.raycastTarget = false;
            separador.transform.SetSiblingIndex(2 + indice);
        }

        if (slider.handleRect != null)
        {
            RectTransform tirador = slider.handleRect;
            tirador.anchorMin = new Vector2(tirador.anchorMin.x, 0.5f);
            tirador.anchorMax = new Vector2(tirador.anchorMax.x, 0.5f);
            tirador.sizeDelta = new Vector2(64f, 68f);
            tirador.anchoredPosition = Vector2.zero;

            Image imagenTirador = tirador.GetComponent<Image>();
            if (imagenTirador != null)
            {
                imagenTirador.sprite = perillaMadera != null
                    ? perillaMadera
                    : ObtenerSpriteCircular();
                imagenTirador.type = Image.Type.Simple;
                imagenTirador.preserveAspect = true;
                imagenTirador.color = Color.white;
                imagenTirador.raycastTarget = true;

                Outline borde = tirador.GetComponent<Outline>();
                if (borde == null)
                {
                    borde = tirador.gameObject.AddComponent<Outline>();
                }

                borde.effectColor = Marron;
                borde.effectDistance = new Vector2(2f, -2f);
            }

            tirador.SetAsLastSibling();
        }

        return imagenBateria;
    }

    private static void ActualizarBateria(Image bateria, Slider slider, float valor)
    {
        if (bateria == null || slider == null)
        {
            return;
        }

        bateria.fillAmount = Mathf.InverseLerp(slider.minValue, slider.maxValue, valor);
    }

    private void EstilizarPorcentaje(Transform seccion, TMP_Text porcentaje)
    {
        RectTransform rectTexto = porcentaje.GetComponent<RectTransform>();
        ConfigurarRect(rectTexto, new Vector2(350f, 10f), new Vector2(100f, 46f));
        porcentaje.alignment = TextAlignmentOptions.Center;
        porcentaje.fontSize = 27f;
        porcentaje.fontStyle = FontStyles.Bold;
        porcentaje.color = Crema;
        porcentaje.raycastTarget = false;

        Transform fondoExistente = seccion.Find("FondoPorcentaje");
        GameObject fondo = fondoExistente != null
            ? fondoExistente.gameObject
            : CrearImagen("FondoPorcentaje", seccion, MarronClaro);
        ConfigurarRect(fondo.GetComponent<RectTransform>(), new Vector2(350f, 10f), new Vector2(100f, 46f));

        Image imagenFondo = fondo.GetComponent<Image>();
        imagenFondo.sprite = ObtenerSpriteRedondeado();
        imagenFondo.type = Image.Type.Sliced;
        imagenFondo.color = MarronClaro;
        imagenFondo.raycastTarget = false;
        fondo.transform.SetSiblingIndex(Mathf.Max(0, porcentaje.transform.GetSiblingIndex()));
        porcentaje.transform.SetAsLastSibling();
    }

    private void CrearMarcasDeEscala(Transform seccion, TMP_FontAsset fuente)
    {
        CrearEtiquetaEscala(seccion, "Minimo", "0%", new Vector2(-98f, -34f), fuente);
        CrearEtiquetaEscala(seccion, "Maximo", "100%", new Vector2(285f, -34f), fuente);

        for (int i = 0; i <= 5; i++)
        {
            string nombre = $"Marca{i}";
            Transform existente = seccion.Find(nombre);
            GameObject marca = existente != null
                ? existente.gameObject
                : CrearImagen(nombre, seccion, MarronClaro);

            float posicionX = Mathf.Lerp(-98f, 285f, i / 5f);
            ConfigurarRect(marca.GetComponent<RectTransform>(), new Vector2(posicionX, -10f), new Vector2(5f, 10f));

            Image imagen = marca.GetComponent<Image>();
            imagen.color = new Color(MarronClaro.r, MarronClaro.g, MarronClaro.b, 0.7f);
            imagen.raycastTarget = false;
        }
    }

    private void CrearEtiquetaEscala(
        Transform seccion,
        string nombre,
        string contenido,
        Vector2 posicion,
        TMP_FontAsset fuente)
    {
        Transform existente = seccion.Find(nombre);
        GameObject objeto = existente != null
            ? existente.gameObject
            : CrearUI(nombre, seccion);
        ConfigurarRect(objeto.GetComponent<RectTransform>(), posicion, new Vector2(70f, 28f));

        TMP_Text texto = objeto.GetComponent<TMP_Text>();
        if (texto == null)
        {
            texto = objeto.AddComponent<TextMeshProUGUI>();
        }

        texto.text = contenido;
        texto.font = fuente;
        texto.fontSize = 19f;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Marron;
        texto.raycastTarget = false;
    }

    private Slider CrearSlider(Transform padre)
    {
        GameObject raiz = CrearUI("ControlVolumen", padre);
        RectTransform rect = raiz.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(344f, 56f);
        rect.anchoredPosition = new Vector2(92f, 9f);

        Slider slider = raiz.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fondo = CrearImagen("Pista", raiz.transform, MarronClaro);
        ConfigurarRect(fondo.GetComponent<RectTransform>(), Vector2.zero, new Vector2(420f, 56f));

        GameObject areaRelleno = CrearUI("AreaRelleno", raiz.transform);
        ConfigurarRect(areaRelleno.GetComponent<RectTransform>(), Vector2.zero, new Vector2(344f, 20f));

        GameObject relleno = CrearImagen("Relleno", areaRelleno.transform, VerdeClaro);
        RectTransform rectRelleno = relleno.GetComponent<RectTransform>();
        rectRelleno.anchorMin = Vector2.zero;
        rectRelleno.anchorMax = Vector2.one;
        rectRelleno.offsetMin = Vector2.zero;
        rectRelleno.offsetMax = Vector2.zero;

        GameObject areaHandle = CrearUI("AreaHandle", raiz.transform);
        ConfigurarRect(areaHandle.GetComponent<RectTransform>(), Vector2.zero, new Vector2(344f, 56f));

        GameObject handle = CrearImagen("Handle", areaHandle.transform, Dorado);
        ConfigurarRect(handle.GetComponent<RectTransform>(), Vector2.zero, new Vector2(64f, 68f));

        slider.fillRect = rectRelleno;
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();

        ColorBlock colores = slider.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = Crema;
        colores.pressedColor = Dorado;
        colores.selectedColor = Color.white;
        slider.colors = colores;

        return slider;
    }

    private TMP_Text CrearPorcentaje(Transform padre)
    {
        GameObject objeto = CrearUI("Porcentaje", padre);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(100f, 60f);
        rect.anchoredPosition = new Vector2(350f, 10f);

        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.fontSize = 27f;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Crema;
        texto.raycastTarget = false;
        return texto;
    }

    private void CrearCapaDeBrillo(Transform raiz)
    {
        Transform canvas = BuscarDescendiente(raiz, "Canvas");
        if (canvas == null && raiz.GetComponent<Canvas>() != null)
        {
            canvas = raiz;
        }

        if (canvas == null)
        {
            return;
        }

        Transform existente = canvas.Find("CapaBrilloMenu");
        GameObject objeto = existente != null
            ? existente.gameObject
            : CrearImagen("CapaBrilloMenu", canvas, Color.black);

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        capaBrillo = objeto.GetComponent<Image>();
        capaBrillo.raycastTarget = false;
    }

    private void CambiarBrillo(float valor)
    {
        brillo = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat(BrightnessKey, brillo);
        PlayerPrefs.Save();
        AplicarBrillo();
    }

    private void AplicarBrillo()
    {
        if (capaBrillo != null)
        {
            capaBrillo.color = new Color(0f, 0f, 0f, (1f - brillo) * 0.65f);
        }
    }

    private static string FormatearPorcentaje(float valor)
    {
        return $"{Mathf.RoundToInt(valor * 100f)}%";
    }

    private static GameObject CrearUI(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.layer = 5;
        objeto.transform.SetParent(padre, false);
        return objeto;
    }

    private static GameObject CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject objeto = CrearUI(nombre, padre);
        Image imagen = objeto.AddComponent<Image>();
        imagen.color = color;
        return objeto;
    }

    private static void ConfigurarRect(RectTransform rect, Vector2 posicion, Vector2 tamano)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
    }

    private static Sprite ObtenerSpriteCircular()
    {
        return null;
    }

    private static Sprite ObtenerSpriteRedondeado()
    {
        if (spriteRedondeado != null)
        {
            return spriteRedondeado;
        }

        const int tamano = 32;
        const float radio = 8f;
        Texture2D textura = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        textura.name = "AldeaVerde_Redondeado";
        textura.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixeles = new Color[tamano * tamano];
        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                float dx = Mathf.Max(Mathf.Max(radio - x, 0f), x - (tamano - 1f - radio));
                float dy = Mathf.Max(Mathf.Max(radio - y, 0f), y - (tamano - 1f - radio));
                float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radio + 0.75f - distancia);
                pixeles[y * tamano + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        textura.SetPixels(pixeles);
        textura.Apply();

        spriteRedondeado = Sprite.Create(
            textura,
            new Rect(0f, 0f, tamano, tamano),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(9f, 9f, 9f, 9f)
        );
        spriteRedondeado.name = "AldeaVerde_Redondeado";
        spriteRedondeado.hideFlags = HideFlags.HideAndDontSave;
        return spriteRedondeado;
    }

    private static Transform BuscarDescendiente(Transform padre, string nombre)
    {
        foreach (Transform hijo in padre.GetComponentsInChildren<Transform>(true))
        {
            if (hijo.name == nombre)
            {
                return hijo;
            }
        }

        return null;
    }

    private static GameObject BuscarObjetoInclusoInactivo(string nombre)
    {
        GameObject[] objetos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject objeto in objetos)
        {
            if (objeto.name == nombre && objeto.scene.IsValid())
            {
                return objeto;
            }
        }

        return null;
    }
}

