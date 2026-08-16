#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Evita el IndexOutOfRangeException de Selectable.OnDisable de UGUI cuando
/// Unity recarga scripts mientras el juego esta en Play. Los Selectable
/// activos se desregistran antes de reiniciar el dominio y se reactivan
/// cuando la lista estatica de UGUI ya fue reconstruida.
/// </summary>
[InitializeOnLoad]
public static class ProteccionRecargaUGUI
{
    private const string ClaveSesion = "Granja.SelectablesAntesRecarga";

    static ProteccionRecargaUGUI()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= AntesDeRecargar;
        AssemblyReloadEvents.beforeAssemblyReload += AntesDeRecargar;
        EditorApplication.delayCall += RestaurarDespuesDeRecargar;
    }

    private static void AntesDeRecargar()
    {
        if (!EditorApplication.isPlaying)
            return;

        Selectable[] selectables =
            Resources.FindObjectsOfTypeAll<Selectable>();
        List<string> ids = new List<string>();

        foreach (Selectable selectable in selectables)
        {
            if (selectable == null ||
                !selectable.isActiveAndEnabled ||
                !selectable.gameObject.scene.IsValid())
            {
                continue;
            }

            ids.Add(selectable.GetInstanceID().ToString());
            selectable.enabled = false;
        }

        SessionState.SetString(ClaveSesion, string.Join(",", ids));
    }

    private static void RestaurarDespuesDeRecargar()
    {
        string guardados = SessionState.GetString(ClaveSesion, string.Empty);
        SessionState.EraseString(ClaveSesion);

        if (string.IsNullOrWhiteSpace(guardados))
            return;

        string[] ids = guardados.Split(',');
        foreach (string idTexto in ids)
        {
            if (!int.TryParse(idTexto, out int id))
                continue;

#pragma warning disable CS0618
            // InstanceIDToObject se conserva para que funcione con el ID
            // guardado antes del domain reload de esta version de Unity.
            Selectable selectable =
                EditorUtility.InstanceIDToObject(id) as Selectable;
#pragma warning restore CS0618
            if (selectable != null)
                selectable.enabled = true;
        }
    }
}
#endif
