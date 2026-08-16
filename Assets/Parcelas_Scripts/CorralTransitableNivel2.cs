using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reemplaza los BoxCollider gigantes generados por el OBJ del corral por
/// paredes perimetrales simples. No elimina ni mueve ningún modelo.
/// </summary>
public static class CorralTransitableNivel2
{
    public static void Preparar()
    {
        Animator[] animadores = Object.FindObjectsByType<Animator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        HashSet<Transform> preparados = new HashSet<Transform>();

        foreach (Animator animator in animadores)
        {
            if (animator == null || !EsAnimatorPuerta(animator))
                continue;

            Transform raiz = BuscarRaizModeloCorral(animator.transform);
            if (raiz != null && preparados.Add(raiz))
                PrepararRaiz(raiz, animator);
        }
    }

    private static bool EsAnimatorPuerta(Animator animator)
    {
        bool abrir = false;
        bool cerrar = false;

        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            if (parametro.name == "abrirpuerta") abrir = true;
            if (parametro.name == "Cerrarpuerta") cerrar = true;
        }

        return abrir && cerrar;
    }

    private static Transform BuscarRaizModeloCorral(Transform puerta)
    {
        Transform actual = puerta;

        while (actual != null)
        {
            if (ContarCollidersDefectuosos(actual) >= 4)
                return actual;

            actual = actual.parent;
        }

        return null;
    }

    private static int ContarCollidersDefectuosos(Transform raiz)
    {
        int cantidad = 0;
        BoxCollider[] cajas = raiz.GetComponentsInChildren<BoxCollider>(true);

        foreach (BoxCollider caja in cajas)
        {
            if (EsColliderDefectuoso(caja))
                cantidad++;
        }

        return cantidad;
    }

    private static bool EsColliderDefectuoso(BoxCollider caja)
    {
        return caja != null && !caja.isTrigger &&
               (caja.size.z > 50f || caja.size.x > 50f);
    }

    private static void PrepararRaiz(Transform raiz, Animator animatorPuerta)
    {
        if (raiz.Find("ColisionesPerimetroNivel2") != null)
            return;

        BoxCollider[] cajas = raiz.GetComponentsInChildren<BoxCollider>(true);
        List<BoxCollider> defectuosas = new List<BoxCollider>();
        Bounds limitesLocales = new Bounds();
        bool tieneLimites = false;

        foreach (BoxCollider caja in cajas)
        {
            if (!EsColliderDefectuoso(caja))
                continue;

            defectuosas.Add(caja);
            EncapsularColliderLocal(
                caja,
                raiz,
                ref limitesLocales,
                ref tieneLimites
            );
        }

        if (!tieneLimites || defectuosas.Count < 4)
            return;

        BoxCollider puerta = animatorPuerta.GetComponent<BoxCollider>();
        Bounds hueco = new Bounds(
            new Vector3(limitesLocales.max.x, limitesLocales.center.y, 0f),
            new Vector3(2f, limitesLocales.size.y, 36f)
        );
        bool tienePuerta = puerta != null;
        if (tienePuerta)
        {
            bool inicializado = false;
            EncapsularColliderLocal(
                puerta,
                raiz,
                ref hueco,
                ref inicializado
            );
        }

        foreach (BoxCollider caja in defectuosas)
            caja.enabled = false;

        GameObject contenedor = new GameObject("ColisionesPerimetroNivel2");
        contenedor.transform.SetParent(raiz, false);

        CrearPerimetro(
            contenedor.transform,
            limitesLocales,
            hueco,
            tienePuerta
        );
    }

    private static void EncapsularColliderLocal(
        BoxCollider caja,
        Transform raiz,
        ref Bounds limites,
        ref bool inicializado)
    {
        Vector3 mitad = caja.size * 0.5f;

        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 esquina = caja.center + Vector3.Scale(
                mitad,
                new Vector3(x, y, z)
            );
            Vector3 localRaiz = raiz.InverseTransformPoint(
                caja.transform.TransformPoint(esquina)
            );

            if (!inicializado)
            {
                limites = new Bounds(localRaiz, Vector3.zero);
                inicializado = true;
            }
            else
            {
                limites.Encapsulate(localRaiz);
            }
        }
    }

    private static void CrearPerimetro(
        Transform padre,
        Bounds limites,
        Bounds puerta,
        bool tienePuerta)
    {
        float grosor = 1.8f;
        float altura = Mathf.Max(8f, limites.size.y);
        float centroY = limites.center.y;
        float minX = limites.min.x;
        float maxX = limites.max.x;
        float minZ = limites.min.z;
        float maxZ = limites.max.z;

        float dMinX = Mathf.Abs(puerta.center.x - minX);
        float dMaxX = Mathf.Abs(puerta.center.x - maxX);
        float dMinZ = Mathf.Abs(puerta.center.z - minZ);
        float dMaxZ = Mathf.Abs(puerta.center.z - maxZ);
        float menor = Mathf.Min(dMinX, dMaxX, dMinZ, dMaxZ);

        CrearPared(padre, "BordeMinX",
            new Vector3(minX, centroY, limites.center.z),
            new Vector3(grosor, altura, limites.size.z));
        CrearPared(padre, "BordeMaxX",
            new Vector3(maxX, centroY, limites.center.z),
            new Vector3(grosor, altura, limites.size.z));
        CrearPared(padre, "BordeMinZ",
            new Vector3(limites.center.x, centroY, minZ),
            new Vector3(limites.size.x, altura, grosor));
        CrearPared(padre, "BordeMaxZ",
            new Vector3(limites.center.x, centroY, maxZ),
            new Vector3(limites.size.x, altura, grosor));

        if (!tienePuerta)
            return;

        // La puerta necesita un hueco real en el borde más cercano.
        if (menor == dMinX || menor == dMaxX)
        {
            Transform pared = padre.Find(menor == dMinX ? "BordeMinX" : "BordeMaxX");
            if (pared != null) Object.Destroy(pared.gameObject);

            float bordeX = menor == dMinX ? minX : maxX;
            CrearSegmentosZ(
                padre, bordeX, centroY, altura, grosor,
                minZ, maxZ, puerta.min.z, puerta.max.z
            );
        }
        else
        {
            Transform pared = padre.Find(menor == dMinZ ? "BordeMinZ" : "BordeMaxZ");
            if (pared != null) Object.Destroy(pared.gameObject);

            float bordeZ = menor == dMinZ ? minZ : maxZ;
            CrearSegmentosX(
                padre, bordeZ, centroY, altura, grosor,
                minX, maxX, puerta.min.x, puerta.max.x
            );
        }
    }

    private static void CrearSegmentosZ(
        Transform padre, float x, float y, float alto, float grosor,
        float minimo, float maximo, float huecoMin, float huecoMax)
    {
        huecoMin = Mathf.Clamp(huecoMin - 1f, minimo, maximo);
        huecoMax = Mathf.Clamp(huecoMax + 1f, minimo, maximo);
        CrearPared(padre, "BordePuertaA",
            new Vector3(x, y, (minimo + huecoMin) * 0.5f),
            new Vector3(grosor, alto, Mathf.Max(0.1f, huecoMin - minimo)));
        CrearPared(padre, "BordePuertaB",
            new Vector3(x, y, (huecoMax + maximo) * 0.5f),
            new Vector3(grosor, alto, Mathf.Max(0.1f, maximo - huecoMax)));
    }

    private static void CrearSegmentosX(
        Transform padre, float z, float y, float alto, float grosor,
        float minimo, float maximo, float huecoMin, float huecoMax)
    {
        huecoMin = Mathf.Clamp(huecoMin - 1f, minimo, maximo);
        huecoMax = Mathf.Clamp(huecoMax + 1f, minimo, maximo);
        CrearPared(padre, "BordePuertaA",
            new Vector3((minimo + huecoMin) * 0.5f, y, z),
            new Vector3(Mathf.Max(0.1f, huecoMin - minimo), alto, grosor));
        CrearPared(padre, "BordePuertaB",
            new Vector3((huecoMax + maximo) * 0.5f, y, z),
            new Vector3(Mathf.Max(0.1f, maximo - huecoMax), alto, grosor));
    }

    private static void CrearPared(
        Transform padre,
        string nombre,
        Vector3 centro,
        Vector3 tamano)
    {
        GameObject pared = new GameObject(nombre);
        pared.transform.SetParent(padre, false);
        BoxCollider collider = pared.AddComponent<BoxCollider>();
        collider.center = centro;
        collider.size = tamano;
    }
}
