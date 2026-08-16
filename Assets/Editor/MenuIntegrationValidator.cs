using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MenuIntegrationValidator
{
    private const string MenuScenePath = "Assets/MenuPrincipal/Scenes/MenuPrincipal.unity";
    private const string GameScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Herramientas/Jardín Verde/Validar integración del menú")]
    public static void RunFromMenu()
    {
        Validate();
        Debug.Log("Integración del menú validada correctamente.");
    }

    public static void RunBatch()
    {
        try
        {
            Validate();
            Debug.Log("MENU_INTEGRATION_VALIDATION_OK");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void Validate()
    {
        List<string> errors = new List<string>();
        ValidateBuildSettings(errors);
        ValidateAssetGuids(errors);

        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);

        try
        {
            GameObject[] objects = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.gameObject)
                .Distinct()
                .ToArray();

            foreach (GameObject item in objects)
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item);
                if (missing > 0)
                {
                    errors.Add($"{item.name} contiene {missing} script(s) faltante(s).");
                }
            }

            ValidateButtons(objects, errors);
            ValidateControllers(objects, errors);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        if (errors.Count > 0)
        {
            throw new BuildFailedException(
                "La integración del menú no es válida:\n- " + string.Join("\n- ", errors));
        }
    }

    private static void ValidateBuildSettings(List<string> errors)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length < 2 ||
            !scenes[0].enabled || scenes[0].path != MenuScenePath ||
            !scenes[1].enabled || scenes[1].path != GameScenePath)
        {
            errors.Add("Build Settings debe contener primero el menú y luego SampleScene, ambas habilitadas.");
        }
    }

    private static void ValidateAssetGuids(List<string> errors)
    {
        Dictionary<string, string> requiredAssets = new Dictionary<string, string>
        {
            { "53ed6e3a221043f4ea4fa69a9a9a8c72", "escena del menú" },
            { "77590bb65414cc440aa15c2946603e96", "música del menú" },
            { "3acc35e42cdf36245b1968be4f94a703", "sonido de botón" },
            { "ad52c7575db4d854482816129e6664c3", "primera página del manual" }
        };

        foreach (KeyValuePair<string, string> asset in requiredAssets)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(asset.Key)))
            {
                errors.Add($"No se pudo resolver {asset.Value} ({asset.Key}).");
            }
        }
    }

    private static void ValidateButtons(GameObject[] objects, List<string> errors)
    {
        string[] requiredNames =
        {
            "Jugar", "Creditos", "Ayuda", "opciones", "Anterior", "Siguiente", "Cerrar"
        };

        Button[] buttons = objects
            .Select(item => item.GetComponent<Button>())
            .Where(item => item != null)
            .ToArray();

        foreach (string requiredName in requiredNames)
        {
            if (!buttons.Any(button => button.name == requiredName && button.onClick.GetPersistentEventCount() > 0))
            {
                errors.Add($"El botón {requiredName} no existe o no tiene una acción conectada.");
            }
        }

        foreach (Button button in buttons)
        {
            for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            {
                if (button.onClick.GetPersistentTarget(index) == null ||
                    string.IsNullOrWhiteSpace(button.onClick.GetPersistentMethodName(index)))
                {
                    errors.Add($"El botón {button.name} contiene una acción persistente inválida.");
                }
            }
        }

        Button playButton = buttons.FirstOrDefault(button => button.name == "Jugar");
        if (playButton != null)
        {
            bool loadsGame = Enumerable.Range(0, playButton.onClick.GetPersistentEventCount())
                .Any(index =>
                    playButton.onClick.GetPersistentTarget(index) is BanderaTransition &&
                    playButton.onClick.GetPersistentMethodName(index) == nameof(BanderaTransition.IniciarJuego));

            if (!loadsGame)
            {
                errors.Add("El botón Jugar no está conectado a BanderaTransition.IniciarJuego.");
            }
        }
    }

    private static void ValidateControllers(GameObject[] objects, List<string> errors)
    {
        MenuManager menu = objects.Select(item => item.GetComponent<MenuManager>()).FirstOrDefault(item => item != null);
        if (menu == null || menu.mainMenu == null || menu.panCreditos == null ||
            menu.panAyuda == null || menu.panOpciones == null)
        {
            errors.Add("MenuManager no tiene todos sus paneles conectados.");
        }

        TutorialManager tutorial = objects
            .Select(item => item.GetComponent<TutorialManager>())
            .FirstOrDefault(item => item != null);
        if (tutorial == null || tutorial.paginas == null || tutorial.paginas.Length != 5 ||
            tutorial.paginas.Any(item => item == null) || tutorial.btnAnterior == null ||
            tutorial.btnSiguiente == null || tutorial.btnCerrar == null)
        {
            errors.Add("TutorialManager debe tener cinco páginas y sus tres botones conectados.");
        }

        BanderaTransition transition = objects
            .Select(item => item.GetComponent<BanderaTransition>())
            .FirstOrDefault(item => item != null && item.enabled);
        if (transition == null || transition.bandera == null || transition.menu == null)
        {
            errors.Add("La transición de bandera no está configurada.");
        }

        MusicMenu music = objects
            .Select(item => item.GetComponent<MusicMenu>())
            .FirstOrDefault(item => item != null && item.enabled);
        if (music == null)
        {
            errors.Add("No existe un controlador de música habilitado.");
        }
        else
        {
            SerializedObject serializedMusic = new SerializedObject(music);
            if (serializedMusic.FindProperty("musicaClip").objectReferenceValue == null ||
                serializedMusic.FindProperty("efectoClickClip").objectReferenceValue == null)
            {
                errors.Add("El controlador de música no tiene asignados sus clips.");
            }
        }
    }
}
