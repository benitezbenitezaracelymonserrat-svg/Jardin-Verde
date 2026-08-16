using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Barrera virtual exclusiva para los venados. No crea colliders que puedan
/// bloquear al jugador, los corrales, las puertas ni los animales de granja.
/// </summary>
public class LimiteVenados : MonoBehaviour
{
    // Rectangulo central reservado para la granja en SampleScene.
    private const float MinXGranja = -5f;
    private const float MaxXGranja = 68f;
    private const float MinZGranja = -52f;
    private const float MaxZGranja = 48f;

    private readonly List<Transform> venados = new List<Transform>();
    private readonly Dictionary<Transform, Vector3> ultimaPosicionExterior =
        new Dictionary<Transform, Vector3>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearAutomaticamente()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene" ||
            FindFirstObjectByType<LimiteVenados>() != null)
        {
            return;
        }

        GameObject control = new GameObject("BarrerasInvisiblesVenados");
        control.AddComponent<LimiteVenados>();
    }

    private void Start()
    {
        GameObject raiz = BuscarObjeto("Venados");
        if (raiz == null)
        {
            Debug.LogWarning("No se encontro el objeto padre Venados.");
            enabled = false;
            return;
        }

        foreach (Transform hijo in raiz.transform)
        {
            if (hijo == null)
                continue;

            venados.Add(hijo);
            ultimaPosicionExterior[hijo] = hijo.position;
        }
    }

    private void LateUpdate()
    {
        foreach (Transform venado in venados)
        {
            if (venado == null)
                continue;

            Vector3 posicion = venado.position;
            bool dentroGranja =
                posicion.x > MinXGranja && posicion.x < MaxXGranja &&
                posicion.z > MinZGranja && posicion.z < MaxZGranja;

            if (!dentroGranja)
            {
                ultimaPosicionExterior[venado] = posicion;
                continue;
            }

            Vector3 segura = ultimaPosicionExterior[venado];
            CharacterController controlador =
                venado.GetComponent<CharacterController>();

            if (controlador != null)
                controlador.enabled = false;

            venado.position = segura;
            venado.Rotate(0f, 180f, 0f, Space.World);

            if (controlador != null)
                controlador.enabled = true;
        }
    }

    private static GameObject BuscarObjeto(string nombre)
    {
        Transform[] objetos = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform objeto in objetos)
        {
            if (objeto != null && objeto.name == nombre)
                return objeto.gameObject;
        }

        return null;
    }
}
