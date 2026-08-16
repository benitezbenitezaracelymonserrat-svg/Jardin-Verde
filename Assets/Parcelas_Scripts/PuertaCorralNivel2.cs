using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zona de entrada de vacas/cabras. En el nivel 2, E abre o cierra la puerta.
/// </summary>
[DisallowMultipleComponent]
public class PuertaCorralNivel2 : MonoBehaviour
{
    private Animator animatorPuerta;
    private Collider[] collidersPuerta = new Collider[0];
    private readonly List<Collider> collidersQueCierranElPaso =
        new List<Collider>();
    private Collider zonaEntrada;
    private bool abierta;
    private bool jugadorDentro;
    private bool cambiandoEstado;
    private static GameObject avisoCompartido;
    private static TextMeshProUGUI textoAviso;

    public void Configurar(Animator animator)
    {
        animatorPuerta = animator;
        abierta = false;
        zonaEntrada = GetComponent<Collider>();

        if (animatorPuerta == null)
            return;

        // Los dos portones usan clips que animan la posicion local desde 0.
        // La puerta de cabras estaba a X=-111.6, por eso saltaba de lugar.
        // Este pivote conserva su posicion mundial y normaliza la puerta a 0.
        PrepararPivoteAnimacion(animatorPuerta.transform);
        collidersPuerta = animatorPuerta.GetComponentsInChildren<Collider>(true);
        ActivarCollidersPuerta(true);
    }

    private void Update()
    {
        if (!GestorNivel2.NivelActivo || !jugadorDentro)
            return;

        MostrarAviso(true);

        if (Input.GetKeyDown(KeyCode.E) && !cambiandoEstado)
            CambiarEstado();
    }

    private void CambiarEstado()
    {
        if (animatorPuerta == null)
        {
            Debug.LogWarning("Esta zona de puerta no tiene Animator asignado.", this);
            return;
        }

        StartCoroutine(AnimarCambioEstado());
    }

    private IEnumerator AnimarCambioEstado()
    {
        cambiandoEstado = true;

        if (!abierta)
        {
            animatorPuerta.ResetTrigger("Cerrarpuerta");
            animatorPuerta.SetTrigger("abrirpuerta");

            // Se libera físicamente la entrada desde que empieza a abrirse.
            // El collider vuelve solamente cuando la puerta se cierra.
            DetectarCollidersQueCierranElPaso();
            ActivarCollidersPuerta(false);
            ActivarCollidersDelPaso(false);
            yield return new WaitForSeconds(0.45f);
            abierta = true;
        }
        else
        {
            animatorPuerta.ResetTrigger("abrirpuerta");
            animatorPuerta.SetTrigger("Cerrarpuerta");
            yield return new WaitForSeconds(0.36f);
            ActivarCollidersDelPaso(true);
            ActivarCollidersPuerta(true);
            abierta = false;
        }

        cambiandoEstado = false;
        ActualizarTexto();
    }

    private void ActivarCollidersPuerta(bool activar)
    {
        foreach (Collider colision in collidersPuerta)
        {
            if (colision != null)
                colision.enabled = activar;
        }
    }

    private void DetectarCollidersQueCierranElPaso()
    {
        collidersQueCierranElPaso.Clear();

        if (zonaEntrada == null)
            return;

        Physics.SyncTransforms();
        Bounds entrada = zonaEntrada.bounds;
        Collider[] encontrados = Physics.OverlapBox(
            entrada.center,
            entrada.extents + new Vector3(0.55f, 0.15f, 0.85f),
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider colision in encontrados)
        {
            if (colision == null || !colision.enabled || colision.isTrigger ||
                colision == zonaEntrada ||
                colision is TerrainCollider ||
                colision is CharacterController ||
                colision.GetComponentInParent<PlayerController>() != null ||
                colision.GetComponentInParent<Animal>() != null)
            {
                continue;
            }

            // El piso debe permanecer activo. Solo se liberan piezas altas
            // del porton o la cerca que ocupen fisicamente la abertura.
            if (colision.bounds.size.y < 0.65f ||
                colision.bounds.max.y <= entrada.min.y + 0.35f)
            {
                continue;
            }

            if (System.Array.IndexOf(collidersPuerta, colision) < 0)
                collidersQueCierranElPaso.Add(colision);
        }
    }

    private void ActivarCollidersDelPaso(bool activar)
    {
        foreach (Collider colision in collidersQueCierranElPaso)
        {
            if (colision != null)
                colision.enabled = activar;
        }
    }

    private static void PrepararPivoteAnimacion(Transform puerta)
    {
        if (puerta == null ||
            (puerta.parent != null &&
             puerta.parent.name.StartsWith("PivoteAnimacionPuerta")))
        {
            return;
        }

        Transform padreOriginal = puerta.parent;
        GameObject objetoPivote = new GameObject(
            "PivoteAnimacionPuerta_" + puerta.name
        );
        Transform pivote = objetoPivote.transform;
        pivote.SetParent(padreOriginal, false);
        pivote.localPosition = puerta.localPosition;
        pivote.localRotation = puerta.localRotation;
        pivote.localScale = puerta.localScale;

        Animator animator = puerta.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        puerta.SetParent(pivote, false);
        puerta.localPosition = Vector3.zero;
        puerta.localRotation = Quaternion.identity;
        puerta.localScale = Vector3.one;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (otro.GetComponentInParent<PlayerController>() == null)
            return;

        jugadorDentro = true;
        MostrarAviso(GestorNivel2.NivelActivo);
    }

    private void OnTriggerExit(Collider otro)
    {
        if (otro.GetComponentInParent<PlayerController>() == null)
            return;

        jugadorDentro = false;
        MostrarAviso(false);
    }

    private void OnDisable()
    {
        if (jugadorDentro)
            MostrarAviso(false);
        jugadorDentro = false;
    }

    private void MostrarAviso(bool mostrar)
    {
        if (mostrar)
            CrearAvisoSiHaceFalta();

        if (avisoCompartido != null)
            avisoCompartido.SetActive(mostrar);

        if (mostrar)
            ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (textoAviso != null)
            textoAviso.text = abierta
                ? "Presiona E para cerrar la puerta"
                : "Presiona E para abrir la puerta";
    }

    private static void CrearAvisoSiHaceFalta()
    {
        if (avisoCompartido != null)
            return;

        Canvas canvas = CanvasJuegoUI.BuscarInteractivo();
        if (canvas == null)
            return;

        avisoCompartido = new GameObject(
            "AvisoPuertaNivel2",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        avisoCompartido.transform.SetParent(canvas.transform, false);

        RectTransform rect = avisoCompartido.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -125f);
        rect.sizeDelta = new Vector2(430f, 58f);

        Image fondo = avisoCompartido.GetComponent<Image>();
        fondo.color = new Color(0.16f, 0.09f, 0.035f, 0.9f);
        Outline borde = avisoCompartido.AddComponent<Outline>();
        borde.effectColor = new Color(0.72f, 0.43f, 0.16f, 1f);
        borde.effectDistance = new Vector2(2f, -2f);

        GameObject textoObjeto = new GameObject(
            "TextoAvisoPuerta",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textoObjeto.transform.SetParent(avisoCompartido.transform, false);
        RectTransform textoRect = textoObjeto.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(8f, 4f);
        textoRect.offsetMax = new Vector2(-8f, -4f);

        textoAviso = textoObjeto.GetComponent<TextMeshProUGUI>();
        textoAviso.alignment = TextAlignmentOptions.Center;
        textoAviso.fontSize = 22f;
        textoAviso.color = new Color(1f, 0.93f, 0.72f, 1f);
        textoAviso.raycastTarget = false;
    }
}
