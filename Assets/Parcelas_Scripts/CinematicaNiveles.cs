using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Construye recorridos cinematográficos para los niveles 2 y 3 usando las
/// zonas reales del mapa. El último encuadre siempre coincide exactamente
/// con la cámara del jugador después de su teletransporte.
/// </summary>
public class CinematicaNiveles : MonoBehaviour
{
    private struct PoseCamara
    {
        public Vector3 posicion;
        public Quaternion rotacion;

        public PoseCamara(Vector3 posicionNueva, Quaternion rotacionNueva)
        {
            posicion = posicionNueva;
            rotacion = rotacionNueva;
        }
    }

    private static CinematicaNiveles instancia;
    private bool reproduciendo;
    private const float DuracionTransicion = 2.8f;
    private const float PausaEncuadre = 0.45f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearAutomaticamente()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
            return;

        if (FindFirstObjectByType<CinematicaNiveles>() == null)
            instancia = new GameObject("CinematicaNiveles").AddComponent<CinematicaNiveles>();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
    }

    public static void ReproducirNivel2()
    {
        ObtenerInstancia().Iniciar(2);
    }

    public static void ReproducirNivel3()
    {
        ObtenerInstancia().Iniciar(3);
    }

    private static CinematicaNiveles ObtenerInstancia()
    {
        if (instancia == null)
            instancia = FindFirstObjectByType<CinematicaNiveles>();

        if (instancia == null)
            instancia = new GameObject("CinematicaNiveles").AddComponent<CinematicaNiveles>();

        return instancia;
    }

    private void Iniciar(int nivel)
    {
        if (reproduciendo)
            return;

        StartCoroutine(Reproducir(nivel));
    }

    private IEnumerator Reproducir(int nivel)
    {
        reproduciendo = true;
        Time.timeScale = 1f;

        PlayerController jugador = FindFirstObjectByType<PlayerController>();
        Camera camaraCinematica = BuscarCamaraCinematica();

        if (jugador == null || camaraCinematica == null)
        {
            Debug.LogWarning(
                $"Cinemática nivel {nivel}: falta Player o CameraCinematica."
            );
            reproduciendo = false;
            yield break;
        }

        Transform camaraJugador = jugador.camara;
        if (camaraJugador == null)
        {
            Camera camaraHija = jugador.GetComponentInChildren<Camera>(true);
            if (camaraHija != null)
                camaraJugador = camaraHija.transform;
        }

        if (camaraJugador == null)
        {
            Debug.LogWarning("No se encontró la cámara del jugador.");
            reproduciendo = false;
            yield break;
        }

        // El gestor del nivel ya teletransportó al jugador. Esta pose será
        // el final exacto y evita un salto al devolver el control.
        PoseCamara poseFinal = new PoseCamara(
            camaraJugador.position,
            camaraJugador.rotation
        );

        if (nivel == 3 && GestorNivel3.ObtenerPoseCamaraVentas(
                out Vector3 posicionVentas,
                out Quaternion rotacionVentas))
        {
            poseFinal = new PoseCamara(posicionVentas, rotacionVentas);
        }

        List<PoseCamara> recorrido = nivel == 2
            ? ConstruirRecorridoNivel2(poseFinal)
            : ConstruirRecorridoNivel3(poseFinal);

        if (recorrido.Count < 2)
        {
            reproduciendo = false;
            yield break;
        }

        CinematicaIntro.NotificarInicioExterno();
        OcultarPanelesDeJuego();

        GameObject jugadorObjeto = jugador.gameObject;
        jugadorObjeto.SetActive(false);
        camaraCinematica.gameObject.SetActive(true);
        camaraCinematica.transform.SetPositionAndRotation(
            recorrido[0].posicion,
            recorrido[0].rotacion
        );

        yield return new WaitForSeconds(PausaEncuadre);

        for (int i = 0; i < recorrido.Count - 1; i++)
        {
            yield return MoverCamara(
                camaraCinematica.transform,
                recorrido[i],
                recorrido[i + 1],
                DuracionTransicion
            );

            if (i < recorrido.Count - 2)
                yield return new WaitForSeconds(PausaEncuadre);
        }

        // Se fuerza la coincidencia exacta antes de cambiar de cámara.
        camaraCinematica.transform.SetPositionAndRotation(
            poseFinal.posicion,
            poseFinal.rotacion
        );
        yield return null;

        camaraCinematica.gameObject.SetActive(false);
        jugadorObjeto.SetActive(true);
        CinematicaIntro.NotificarFinExterno();
        reproduciendo = false;
    }

    private static IEnumerator MoverCamara(
        Transform camara,
        PoseCamara desde,
        PoseCamara hasta,
        float duracion)
    {
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            t = t * t * (3f - 2f * t);

            camara.position = Vector3.Lerp(desde.posicion, hasta.posicion, t);
            camara.rotation = Quaternion.Slerp(desde.rotacion, hasta.rotacion, t);
            yield return null;
        }

        camara.SetPositionAndRotation(hasta.posicion, hasta.rotacion);
    }

    private static List<PoseCamara> ConstruirRecorridoNivel2(PoseCamara final)
    {
        List<PoseCamara> recorrido = new List<PoseCamara>();

        SlotParcela[] parcelas = FindObjectsByType<SlotParcela>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (parcelas.Length > 0)
        {
            Vector3 centro = Vector3.zero;
            foreach (SlotParcela parcela in parcelas)
                centro += parcela.transform.position;
            centro /= parcelas.Length;

            recorrido.Add(CrearEncuadre(
                centro,
                new Vector3(-12f, 10f, -12f)
            ));
        }

        AgregarEncuadreZona(
            recorrido,
            BuscarTransformPorNombre("ZonaGallinas"),
            new Vector3(-8f, 5.5f, -8f)
        );
        AgregarEncuadreZona(
            recorrido,
            BuscarTransformPorNombre("Zonas_de_Vacas"),
            new Vector3(-9f, 6f, -9f)
        );
        AgregarEncuadreZona(
            recorrido,
            BuscarTransformPorNombre("ZonaCabras"),
            new Vector3(9f, 6f, -9f)
        );

        recorrido.Add(final);
        return recorrido;
    }

    private static List<PoseCamara> ConstruirRecorridoNivel3(PoseCamara final)
    {
        List<PoseCamara> recorrido = new List<PoseCamara>();

        AgregarEncuadreZona(
            recorrido,
            BuscarTransformPorNombre("MarketStand_1"),
            new Vector3(-9f, 6f, -10f)
        );
        AgregarEncuadreZona(
            recorrido,
            BuscarTransformPorNombre("Persoanjes de fila"),
            new Vector3(8f, 5f, -10f)
        );
        AgregarEncuadreZona(
            recorrido,
            BuscarTransformPorNombre("Atencionalcliente"),
            new Vector3(-6f, 4.5f, -7f)
        );

        recorrido.Add(final);
        return recorrido;
    }

    private static void AgregarEncuadreZona(
        List<PoseCamara> recorrido,
        Transform zona,
        Vector3 desplazamiento)
    {
        if (zona == null)
            return;

        Vector3 objetivo = CalcularCentroVisual(zona);
        recorrido.Add(CrearEncuadre(objetivo, desplazamiento));
    }

    private static PoseCamara CrearEncuadre(
        Vector3 objetivo,
        Vector3 desplazamiento)
    {
        Vector3 posicion = objetivo + desplazamiento;
        Vector3 direccion = objetivo - posicion;
        Quaternion rotacion = direccion.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(direccion.normalized, Vector3.up)
            : Quaternion.identity;

        return new PoseCamara(posicion, rotacion);
    }

    private static Vector3 CalcularCentroVisual(Transform raiz)
    {
        Renderer[] renderers = raiz.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return raiz.position + Vector3.up * 1.5f;

        Bounds limites = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            limites.Encapsulate(renderers[i].bounds);

        Vector3 centro = limites.center;
        centro.y = Mathf.Lerp(limites.min.y, limites.max.y, 0.55f);
        return centro;
    }

    private static Camera BuscarCamaraCinematica()
    {
        Camera[] camaras = FindObjectsByType<Camera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        return camaras.FirstOrDefault(c =>
            c != null && c.name == "CameraCinematica"
        );
    }

    private static Transform BuscarTransformPorNombre(string nombre)
    {
        return FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        ).FirstOrDefault(t => t != null && t.name == nombre);
    }

    private static void OcultarPanelesDeJuego()
    {
        string[] nombres =
        {
            "Panel Cultivo",
            "PanelAlimentar",
            "PanelRecolectar",
            "PanelOrdenarNivel2",
            "PanelDesafiosNivel1",
            "PanelDesafiosNivel2",
            "PanelDesafiosNivel3",
            "PanelInventario"
        };

        Transform[] elementos = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform elemento in elementos)
        {
            if (elemento != null && nombres.Contains(elemento.name))
                elemento.gameObject.SetActive(false);
        }
    }
}
