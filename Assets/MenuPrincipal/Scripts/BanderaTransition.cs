using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BanderaTransition : MonoBehaviour
{
    [Header("Navegación")]
    [SerializeField] private string escenaJuego = "SampleScene";

    [Header("Referencias")]
    [Tooltip("Canvas Group del objeto BanderaParaguay")]
    public CanvasGroup bandera;

    [Tooltip("Canvas Group del objeto Mainmenu")]
    public CanvasGroup menu;

    [Tooltip("Panel oscuro a pantalla completa. Es opcional.")]
    public CanvasGroup fondoOscuro;

    [Range(0f, 1f)]
    public float opacidadFondoOscuro = 0.35f;


    [Header("Apariencia de la bandera")]
    [Range(0.4f, 1f)]
    public float tinteOscuroBandera = 0.85f;

    private Image imagenBandera;


    // =========================================================
    // AUDIO ANTIGUO
    // Lo dejamos para no romper tus referencias anteriores
    // =========================================================

    [Header("Audio - Sonido del botón")]
    public AudioSource audioSource;
    public AudioClip sonidoBoton;


    [Header("Audio - Música del menú")]
    public AudioSource musicaMenu;

    [Range(0f, 1f)]
    public float volumenMusicaNormal = 1f;

    [Range(0f, 1f)]
    public float volumenMusicaDucking = 0.25f;


    // =========================================================
    // NUEVO SISTEMA DE AUDIO
    // =========================================================

    [Header("Nuevo sistema de música")]
    [Tooltip("Arrastra aquí el objeto que tiene el script MusicMenu")]
    public MusicMenu controladorMusica;


    [Header("Tiempos")]
    public float tiempoFadeMenu = 0.75f;
    public float tiempoEntradaBandera = 0.85f;
    public float tiempoEspera = 2f;
    public float tiempoSalidaBandera = 0.75f;


    [Header("Entrada de la bandera")]
    [Tooltip("Distancia desde la izquierda")]
    public float distanciaEntrada = 300f;


    private bool transicionEnCurso = false;

    private RectTransform banderaRect;
    private Vector2 posicionOriginalBandera;
    private bool posicionGuardada = false;


    // =========================================================
    // GUARDAR POSICIÓN ORIGINAL
    // =========================================================

    private void GuardarPosicionOriginal()
    {
        if (posicionGuardada || bandera == null)
            return;

        banderaRect = bandera.GetComponent<RectTransform>();
        imagenBandera = bandera.GetComponent<Image>();

        if (imagenBandera != null)
        {
            float t = tinteOscuroBandera;

            Color colorActual = imagenBandera.color;

            imagenBandera.color = new Color(
                t,
                t,
                t,
                colorActual.a
            );
        }

        if (banderaRect != null)
        {
            posicionOriginalBandera =
                banderaRect.anchoredPosition;

            posicionGuardada = true;
        }
    }


    // =========================================================
    // ABRIR PANEL CON TRANSICIÓN DE BANDERA
    // =========================================================

    public void IniciarTransicion(GameObject panelDestino)
    {
        if (transicionEnCurso)
            return;

        StartCoroutine(
            TransicionCoroutine(panelDestino)
        );
    }

    // Entrada exclusiva del botón Jugar. Al no recibir argumentos, Unity no
    // intenta convertir un Object nulo a GameObject antes de ejecutar la acción.
    public void IniciarJuego()
    {
        if (transicionEnCurso)
            return;

        StartCoroutine(
            TransicionCoroutine(null)
        );
    }


    private IEnumerator TransicionCoroutine(GameObject panelDestino)
    {
        transicionEnCurso = true;

        GuardarPosicionOriginal();


        // Bloqueamos los botones del menú
        if (menu != null)
        {
            menu.interactable = false;
            menu.blocksRaycasts = false;
        }


        // =====================================================
        // CLICK + DUCKING
        // =====================================================

        // El sistema nuevo reproduce el clic desde cada Button y mantiene
        // la musica continua. Conservamos el sistema anterior como respaldo.
        if (controladorMusica == null)
        {
            // Sistema anterior como respaldo

            if (musicaMenu != null)
            {
                musicaMenu.volume =
                    volumenMusicaDucking;
            }

            float duracionClip = 0f;

            if (audioSource != null &&
                sonidoBoton != null)
            {
                audioSource.PlayOneShot(sonidoBoton);

                duracionClip =
                    sonidoBoton.length;
            }

            if (duracionClip > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    duracionClip
                );
            }

            if (musicaMenu != null)
            {
                musicaMenu.Stop();
            }
        }


        // =====================================================
        // PREPARAMOS BANDERA
        // DESDE AQUÍ NO HAY MÚSICA
        // =====================================================

        if (bandera != null)
        {
            bandera.gameObject.SetActive(true);
            bandera.alpha = 0f;
        }


        // Fondo oscuro
        if (fondoOscuro != null)
        {
            fondoOscuro.gameObject.SetActive(true);
            fondoOscuro.alpha = 0f;

            StartCoroutine(
                FadeCanvasGroup(
                    fondoOscuro,
                    0f,
                    opacidadFondoOscuro,
                    tiempoEntradaBandera
                )
            );
        }


        // =====================================================
        // CROSSFADE
        // Menú desaparece mientras entra la bandera
        // =====================================================

        if (menu != null)
        {
            StartCoroutine(
                FadeCanvasGroup(
                    menu,
                    menu.alpha,
                    0f,
                    tiempoFadeMenu
                )
            );
        }


        // Bandera entra
        yield return EntradaBanderaSuave();


        // Nos aseguramos de ocultar el menú
        if (menu != null)
        {
            menu.alpha = 0f;
        }


        // =====================================================
        // BANDERA VISIBLE
        // TODAVÍA SIN MÚSICA
        // =====================================================

        yield return new WaitForSecondsRealtime(
            tiempoEspera
        );


        // El botón Jugar de la escena original envía un destino nulo. En ese
        // caso la intención es salir del menú y cargar la escena jugable.
        if (panelDestino == null)
        {
            if (!Application.CanStreamedLevelBeLoaded(escenaJuego))
            {
                Debug.LogError(
                    $"No se puede iniciar el juego: la escena '{escenaJuego}' no está incluida en Build Settings.");
                RestaurarMenuTrasError();
                yield break;
            }

            AsyncOperation carga = SceneManager.LoadSceneAsync(escenaJuego);
            if (carga == null)
            {
                Debug.LogError($"Unity no pudo comenzar a cargar la escena '{escenaJuego}'.");
                RestaurarMenuTrasError();
                yield break;
            }

            yield return carga;
            yield break;
        }


        // =====================================================
        // ACTIVAMOS PANEL DETRÁS DE BANDERA
        // =====================================================

        if (panelDestino != null)
        {
            panelDestino.SetActive(true);
        }


        // Ahora podemos apagar el menú
        if (menu != null)
        {
            menu.gameObject.SetActive(false);
        }


        // =====================================================
        // BANDERA DESAPARECE SOBRE EL PANEL
        // =====================================================

        if (fondoOscuro != null)
        {
            StartCoroutine(
                FadeCanvasGroup(
                    fondoOscuro,
                    fondoOscuro.alpha,
                    0f,
                    tiempoSalidaBandera
                )
            );
        }


        yield return SalidaBandera();


        if (bandera != null)
        {
            bandera.gameObject.SetActive(false);
        }


        if (fondoOscuro != null)
        {
            fondoOscuro.gameObject.SetActive(false);
        }


        // =====================================================
        // TERMINÓ LA BANDERA
        // AHORA VUELVE LA MÚSICA
        // =====================================================

        if (controladorMusica != null)
        {
            controladorMusica.ReproducirMusica();
        }
        else if (musicaMenu != null)
        {
            musicaMenu.volume =
                volumenMusicaNormal;

            if (!musicaMenu.isPlaying)
            {
                musicaMenu.Play();
            }
        }


        transicionEnCurso = false;
    }

    private void RestaurarMenuTrasError()
    {
        if (bandera != null)
        {
            bandera.alpha = 0f;
            bandera.gameObject.SetActive(false);
        }

        if (fondoOscuro != null)
        {
            fondoOscuro.alpha = 0f;
            fondoOscuro.gameObject.SetActive(false);
        }

        if (menu != null)
        {
            menu.gameObject.SetActive(true);
            menu.alpha = 1f;
            menu.interactable = true;
            menu.blocksRaycasts = true;
        }

        if (controladorMusica != null)
        {
            controladorMusica.ReproducirMusica();
        }

        transicionEnCurso = false;
    }


    // =========================================================
    // ENTRADA DE BANDERA
    // =========================================================

    private IEnumerator EntradaBanderaSuave()
    {
        if (bandera == null ||
            banderaRect == null)
        {
            yield break;
        }


        Vector2 posFinal =
            posicionOriginalBandera;


        // Desplazamiento pequeño desde la izquierda
        Vector2 posInicial =
            posFinal +
            new Vector2(
                -distanciaEntrada,
                0f
            );


        Vector3 escalaInicial =
            Vector3.one * 0.96f;

        Vector3 escalaFinal =
            Vector3.one;


        banderaRect.anchoredPosition =
            posInicial;

        banderaRect.localScale =
            escalaInicial;

        bandera.alpha = 0f;


        float tiempo = 0f;


        while (tiempo < tiempoEntradaBandera)
        {
            tiempo += Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    tiempo /
                    tiempoEntradaBandera
                );


            float suave =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            banderaRect.anchoredPosition =
                Vector2.Lerp(
                    posInicial,
                    posFinal,
                    suave
                );


            banderaRect.localScale =
                Vector3.Lerp(
                    escalaInicial,
                    escalaFinal,
                    suave
                );


            bandera.alpha =
                Mathf.Lerp(
                    0f,
                    1f,
                    suave
                );


            yield return null;
        }


        banderaRect.anchoredPosition =
            posFinal;

        banderaRect.localScale =
            escalaFinal;

        bandera.alpha = 1f;
    }


    // =========================================================
    // SALIDA DE BANDERA
    // =========================================================

    private IEnumerator SalidaBandera()
    {
        if (bandera == null ||
            banderaRect == null)
        {
            yield break;
        }


        float tiempo = 0f;

        float alphaInicial =
            bandera.alpha;


        Vector3 escalaInicial =
            banderaRect.localScale;


        Vector3 escalaFinal =
            Vector3.one * 0.97f;


        while (tiempo < tiempoSalidaBandera)
        {
            tiempo += Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    tiempo /
                    tiempoSalidaBandera
                );


            float suave =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            bandera.alpha =
                Mathf.Lerp(
                    alphaInicial,
                    0f,
                    suave
                );


            banderaRect.localScale =
                Vector3.Lerp(
                    escalaInicial,
                    escalaFinal,
                    suave
                );


            yield return null;
        }


        bandera.alpha = 0f;

        banderaRect.localScale =
            Vector3.one;
    }


    // =========================================================
    // CERRAR PANEL
    // =========================================================

    public void CerrarPanel(GameObject panel)
    {
        if (transicionEnCurso)
            return;

        StartCoroutine(
            CerrarPanelCoroutine(panel)
        );
    }


    private IEnumerator CerrarPanelCoroutine(GameObject panel)
    {
        transicionEnCurso = true;


        // =====================================================
        // CLICK + DUCKING
        // =====================================================

        // El clic ya se reproduce desde el Button con el sistema nuevo.
        if (controladorMusica == null)
        {
            yield return PlayClickConDuckeo();
        }


        // =====================================================
        // MOSTRAMOS MAINMENU PRIMERO
        // Esto evita el fondo gris/celeste
        // =====================================================

        if (menu != null)
        {
            menu.gameObject.SetActive(true);

            menu.alpha = 1f;

            menu.interactable = true;
            menu.blocksRaycasts = true;

            TituloAnimacion titulo =
                menu.GetComponentInChildren<TituloAnimacion>(true);

            if (titulo != null)
            {
                titulo.ReiniciarEntrada();
            }
        }


        // Ahora sí ocultamos el panel
        if (panel != null)
        {
            panel.SetActive(false);
        }


        // =====================================================
        // ASEGURAMOS MÚSICA NORMAL AL VOLVER
        // =====================================================

        if (controladorMusica != null)
        {
            controladorMusica.ReproducirMusica();
        }
        else if (musicaMenu != null)
        {
            musicaMenu.volume =
                volumenMusicaNormal;

            if (!musicaMenu.isPlaying)
            {
                musicaMenu.Play();
            }
        }


        transicionEnCurso = false;
    }


    // =========================================================
    // CLICK ANTIGUO DE RESPALDO
    // =========================================================

    // Compatibilidad con los botones que ya tienen este metodo
    // asignado en el Inspector.
    public void ReproducirSonidoBoton()
    {
        if (controladorMusica != null)
        {
            controladorMusica.ReproducirClick();
        }
        else if (audioSource != null && sonidoBoton != null)
        {
            audioSource.PlayOneShot(sonidoBoton);
        }
    }

    private IEnumerator PlayClickConDuckeo()
    {
        if (musicaMenu != null)
        {
            musicaMenu.volume =
                volumenMusicaDucking;
        }


        float duracionClip = 0f;


        if (audioSource != null &&
            sonidoBoton != null)
        {
            audioSource.PlayOneShot(
                sonidoBoton
            );

            duracionClip =
                sonidoBoton.length;
        }


        if (duracionClip > 0f)
        {
            yield return new WaitForSecondsRealtime(
                duracionClip
            );
        }


        if (musicaMenu != null)
        {
            musicaMenu.volume =
                volumenMusicaNormal;
        }
    }


    // =========================================================
    // FADE GENERAL
    // =========================================================

    private IEnumerator FadeCanvasGroup(
        CanvasGroup cg,
        float desde,
        float hasta,
        float duracion)
    {
        if (cg == null)
            yield break;


        float tiempo = 0f;

        cg.alpha = desde;


        while (tiempo < duracion)
        {
            tiempo +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    tiempo / duracion
                );


            float suave =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            cg.alpha =
                Mathf.Lerp(
                    desde,
                    hasta,
                    suave
                );


            yield return null;
        }


        cg.alpha = hasta;
    }
}
