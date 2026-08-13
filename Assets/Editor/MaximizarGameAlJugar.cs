using UnityEditor;
using UnityEngine;

/// <summary>
/// Maximiza la pestaña Game al entrar en Play y restaura el editor al salir.
/// </summary>
[InitializeOnLoad]
public static class MaximizarGameAlJugar
{
    private static EditorWindow gameView;

    static MaximizarGameAlJugar()
    {
        EditorApplication.playModeStateChanged -= AlCambiarModoPlay;
        EditorApplication.playModeStateChanged += AlCambiarModoPlay;
    }

    private static void AlCambiarModoPlay(PlayModeStateChange estado)
    {
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.delayCall += Maximizar;
        }
        else if (estado == PlayModeStateChange.ExitingPlayMode &&
                 gameView != null)
        {
            gameView.maximized = false;
        }
    }

    private static void Maximizar()
    {
        System.Type tipoGameView = typeof(EditorWindow).Assembly.GetType(
            "UnityEditor.GameView"
        );

        if (tipoGameView == null)
            return;

        Object[] vistas = Resources.FindObjectsOfTypeAll(tipoGameView);
        gameView = vistas.Length > 0
            ? vistas[0] as EditorWindow
            : EditorWindow.GetWindow(tipoGameView);

        if (gameView == null)
            return;

        gameView.maximized = true;
        gameView.Focus();
    }
}
