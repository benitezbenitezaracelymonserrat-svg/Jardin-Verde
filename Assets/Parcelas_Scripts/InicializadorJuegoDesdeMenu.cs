using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Inicializa los sistemas de juego cada vez que se entra a SampleScene.
/// Es necesario porque la aplicación comienza en MenuPrincipal y los
/// RuntimeInitializeOnLoadMethod(AfterSceneLoad) individuales solo se
/// ejecutan después de la primera escena.
/// </summary>
public static class InicializadorJuegoDesdeMenu
{
    private const string EscenaJuego = "SampleScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegistrarCambioDeEscena()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != EscenaJuego)
            return;

        CrearSiFalta<GestorDesafiosNivel1>("GestorDesafiosNivel1");
        CrearSiFalta<GestorNivel2>("GestorNivel2");
        CrearSiFalta<GestorNivel3>("GestorNivel3");
        CrearSiFalta<CinematicaNiveles>("CinematicaNiveles");
        CrearSiFalta<LimiteVenados>("LimiteVenadosControl");
        CrearSiFalta<ManualJuegoUI>("ManualJuegoUI");
        CrearSiFalta<BarraProgresoNiveles>("BarraProgresoNivelesControl");
    }

    private static void CrearSiFalta<T>(string nombre) where T : Component
    {
        if (Object.FindFirstObjectByType<T>() != null)
            return;

        new GameObject(nombre).AddComponent<T>();
    }
}
