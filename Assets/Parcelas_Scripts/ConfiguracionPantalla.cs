using UnityEngine;

/// <summary>
/// La build usa toda la pantalla y conserva la resolución nativa del monitor.
/// En el editor, MaximizarGameAlJugar se ocupa de ampliar la vista Game.
/// </summary>
public static class ConfiguracionPantalla
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Aplicar()
    {
        if (Application.isEditor)
            return;

        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.SetResolution(
            Display.main.systemWidth,
            Display.main.systemHeight,
            FullScreenMode.FullScreenWindow
        );
    }
}
