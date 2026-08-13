#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CodexCrearAnimacionOrdenar
{
    private const string PrefabPath =
        "Assets/Granjeros/Prefabs/Peasant Nolant Blue(Free Version).prefab";
    private const string ControllerPath =
        "Assets/Granjeros/Animator/Animator Controller.controller";
    private const string OutputFolder = "Assets/Granjeros/Animations";
    private const string ClipPath = OutputFolder + "/Ordenar.anim";
    private const string RunKey = "Codex.CrearAnimacionOrdenar.v4";

    static CodexCrearAnimacionOrdenar()
    {
        EditorApplication.delayCall += EjecutarUnaVez;
    }

    [MenuItem("Herramientas/Granja/Regenerar animacion de ordenar")]
    private static void RegenerarDesdeMenu()
    {
        CrearAnimacionYConectar();
    }

    private static void EjecutarUnaVez()
    {
        if (EditorPrefs.GetBool(RunKey, false))
            return;

        try
        {
            CrearAnimacionYConectar();
            EditorPrefs.SetBool(RunKey, true);
        }
        catch (Exception ex)
        {
            Debug.LogError("No se pudo crear la animacion de ordenar: " + ex);
        }
    }

    private static void CrearAnimacionYConectar()
    {
        Directory.CreateDirectory(OutputFolder);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (prefab == null)
            throw new InvalidOperationException("No se encontro el prefab del granjero.");
        if (controller == null)
            throw new InvalidOperationException("No se encontro el Animator Controller real.");

        AnimationClip clip = CrearClipHumanoide(prefab, controller);

        AnimationClip anterior = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (anterior != null)
            AssetDatabase.DeleteAsset(ClipPath);

        AssetDatabase.CreateAsset(clip, ClipPath);
        ConectarAlAnimator(controller, clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        try
        {
            GenerarVistasPrevias(prefab, clip);
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "La animacion se creo correctamente, pero no se pudo " +
                "generar su vista previa: " + ex.Message
            );
        }

        Debug.Log(
            "Animacion Ordenar creada y conectada. " +
            "Usa el Trigger 'Ordenar' para reproducirla."
        );
    }

    private static AnimationClip CrearClipHumanoide(
        GameObject prefab,
        AnimatorController controller)
    {
        GameObject instancia = UnityEngine.Object.Instantiate(prefab);
        instancia.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            Animator animator = instancia.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                throw new InvalidOperationException(
                    "El granjero no tiene un Avatar Humanoid valido."
                );

            AnimationClip idle = controller.layers[0].stateMachine.defaultState.motion
                as AnimationClip;

            if (idle != null)
                idle.SampleAnimation(instancia, 0f);

            HumanPose pose = new HumanPose();
            HumanPoseHandler handler =
                new HumanPoseHandler(animator.avatar, animator.transform);
            handler.GetHumanPose(ref pose);

            float[] baseMuscles = pose.muscles != null
                ? (float[])pose.muscles.Clone()
                : new float[HumanTrait.MuscleCount];

            handler.Dispose();

            AnimationClip clip = new AnimationClip
            {
                name = "Ordenar",
                frameRate = 30f,
                legacy = false,
                wrapMode = WrapMode.Once
            };

            Dictionary<string, int> indices = new Dictionary<string, int>();
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
                indices[HumanTrait.MuscleName[i]] = i;

            float[] tiempos =
            {
                0f, 0.35f, 0.8f,
                1.05f, 1.3f, 1.55f, 1.8f,
                2.05f, 2.3f, 2.55f, 2.8f,
                3.05f, 3.25f, 3.65f, 4f
            };

            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                string nombre = HumanTrait.MuscleName[i];
                Keyframe[] keys = new Keyframe[tiempos.Length];

                for (int k = 0; k < tiempos.Length; k++)
                {
                    float valor = baseMuscles.Length > i ? baseMuscles[i] : 0f;
                    valor += ObtenerDesplazamiento(nombre, tiempos[k]);
                    keys[k] = new Keyframe(tiempos[k], Mathf.Clamp(valor, -1f, 1f));
                }

                AnimationCurve curva = new AnimationCurve(keys);
                for (int k = 0; k < curva.length; k++)
                    curva.SmoothTangents(k, 0f);

                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(Animator),
                    nombre
                );
                AnimationUtility.SetEditorCurve(clip, binding, curva);
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            clip.EnsureQuaternionContinuity();
            return clip;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instancia);
        }
    }

    private static float ObtenerDesplazamiento(string musculo, float tiempo)
    {
        float agachado = FactorAgachado(tiempo);
        float bombeo = FactorBombeo(tiempo);
        float alternado = FactorAlternado(tiempo);

        switch (musculo)
        {
            case "Spine Front-Back":
                return -0.28f * agachado;
            case "Chest Front-Back":
                return -0.18f * agachado;
            case "UpperChest Front-Back":
                return -0.08f * agachado;
            case "Neck Nod Down-Up":
                return -0.12f * agachado;

            case "Left Upper Leg Front-Back":
            case "Right Upper Leg Front-Back":
                return -0.22f * agachado;
            case "Left Lower Leg Stretch":
            case "Right Lower Leg Stretch":
                return -0.52f * agachado;
            case "Left Foot Up-Down":
            case "Right Foot Up-Down":
                return 0.12f * agachado;

            case "Left Shoulder Front-Back":
                return 0.20f * agachado;
            case "Right Shoulder Front-Back":
                return 0.20f * agachado;
            case "Left Arm Down-Up":
            case "Right Arm Down-Up":
                return -0.22f * agachado;
            case "Left Arm Front-Back":
            case "Right Arm Front-Back":
                return 0.48f * agachado;
            case "Left Arm Twist In-Out":
                return -0.18f * agachado;
            case "Right Arm Twist In-Out":
                return 0.18f * agachado;

            case "Left Forearm Stretch":
                return (-0.55f * agachado) + (-0.20f * bombeo);
            case "Right Forearm Stretch":
                return (-0.55f * agachado) + (-0.20f * (1f - alternado) * agachado);
            case "Left Hand Down-Up":
                return (0.12f * agachado) + (0.10f * alternado);
            case "Right Hand Down-Up":
                return (0.12f * agachado) + (0.10f * (1f - alternado) * agachado);
            case "Left Hand In-Out":
                return 0.20f * agachado;
            case "Right Hand In-Out":
                return -0.20f * agachado;
        }

        return 0f;
    }

    private static float FactorAgachado(float t)
    {
        if (t <= 0.35f)
            return Mathf.InverseLerp(0f, 0.35f, t) * 0.25f;
        if (t < 0.8f)
            return Mathf.Lerp(0.25f, 1f, Mathf.InverseLerp(0.35f, 0.8f, t));
        if (t <= 3.25f)
            return 1f;
        if (t < 4f)
            return 1f - Mathf.InverseLerp(3.25f, 4f, t);
        return 0f;
    }

    private static float FactorBombeo(float t)
    {
        if (t < 0.8f || t > 3.25f)
            return 0f;

        float fase = (t - 0.8f) / 0.5f;
        return 0.5f + 0.5f * Mathf.Sin(fase * Mathf.PI * 2f);
    }

    private static float FactorAlternado(float t)
    {
        if (t < 0.8f || t > 3.25f)
            return 0f;

        float fase = (t - 0.8f) / 0.5f;
        return 0.5f + 0.5f * Mathf.Sin(fase * Mathf.PI * 2f);
    }

    private static void ConectarAlAnimator(
        AnimatorController controller,
        AnimationClip clip)
    {
        AnimatorStateMachine maquina = controller.layers[0].stateMachine;
        AnimatorState estado = maquina.states
            .Select(s => s.state)
            .FirstOrDefault(s => s.name == "Ordenar");

        if (estado == null)
        {
            estado = maquina.AddState("Ordenar", new Vector3(620f, 290f, 0f));
        }

        estado.motion = clip;
        estado.speed = 1f;
        estado.writeDefaultValues = true;

        if (!controller.parameters.Any(p => p.name == "Ordenar"))
            controller.AddParameter("Ordenar", AnimatorControllerParameterType.Trigger);

        AnimatorStateTransition entrada = maquina.anyStateTransitions
            .FirstOrDefault(t => t.destinationState == estado);

        if (entrada == null)
            entrada = maquina.AddAnyStateTransition(estado);

        entrada.hasExitTime = false;
        entrada.duration = 0.08f;
        entrada.canTransitionToSelf = false;
        entrada.conditions = Array.Empty<AnimatorCondition>();
        entrada.AddCondition(AnimatorConditionMode.If, 0f, "Ordenar");

        AnimatorState idle = maquina.states
            .Select(s => s.state)
            .FirstOrDefault(s => s.name.Contains("Idle"));

        if (idle != null)
        {
            AnimatorStateTransition salida = estado.transitions
                .FirstOrDefault(t => t.destinationState == idle);

            if (salida == null)
                salida = estado.AddTransition(idle);

            salida.hasExitTime = true;
            salida.exitTime = 0.98f;
            salida.duration = 0.12f;
            salida.hasFixedDuration = true;
        }

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(clip);
    }

    private static void GenerarVistasPrevias(GameObject prefab, AnimationClip clip)
    {
        float[] tiempos = { 0.65f, 1.2f, 2.1f, 3.45f };

        for (int i = 0; i < tiempos.Length; i++)
            RenderizarPose(prefab, clip, tiempos[i], i + 1);
    }

    private static void RenderizarPose(
        GameObject prefab,
        AnimationClip clip,
        float tiempo,
        int indice)
    {
        Scene escena = EditorSceneManager.NewPreviewScene();
        RenderTexture rt = null;
        Texture2D captura = null;

        try
        {
            GameObject personaje =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            personaje.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            clip.SampleAnimation(personaje, tiempo);

            Renderer[] renderers = personaje.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                if (renderer is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = true;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            GameObject camGo = new GameObject("PreviewCamera");
            SceneManager.MoveGameObjectToScene(camGo, escena);
            Camera camara = camGo.AddComponent<Camera>();
            AgregarComponenteHDRP(
                camGo,
                "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData"
            );
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.79f, 0.91f, 1f, 1f);
            camara.orthographic = true;
            camara.orthographicSize = Mathf.Max(1f, bounds.extents.y * 1.2f);
            camara.nearClipPlane = 0.01f;
            camara.farClipPlane = 100f;

            float distancia = Mathf.Max(4f, bounds.extents.magnitude * 4f);
            Vector3 objetivo = bounds.center + Vector3.up * bounds.extents.y * 0.05f;
            camara.transform.position = objetivo + new Vector3(0f, 0.05f, distancia);
            camara.transform.LookAt(objetivo);

            GameObject luzGo = new GameObject("PreviewLight");
            SceneManager.MoveGameObjectToScene(luzGo, escena);
            Light luz = luzGo.AddComponent<Light>();
            AgregarComponenteHDRP(
                luzGo,
                "UnityEngine.Rendering.HighDefinition.HDAdditionalLightData"
            );
            luz.type = LightType.Directional;
            luz.intensity = 1.5f;
            luzGo.transform.rotation = Quaternion.Euler(35f, 145f, 0f);

            GameObject rellenoGo = new GameObject("FillLight");
            SceneManager.MoveGameObjectToScene(rellenoGo, escena);
            Light relleno = rellenoGo.AddComponent<Light>();
            AgregarComponenteHDRP(
                rellenoGo,
                "UnityEngine.Rendering.HighDefinition.HDAdditionalLightData"
            );
            relleno.type = LightType.Directional;
            relleno.intensity = 0.65f;
            rellenoGo.transform.rotation = Quaternion.Euler(20f, -35f, 0f);

            rt = new RenderTexture(640, 720, 24, RenderTextureFormat.ARGB32);
            camara.targetTexture = rt;
            camara.Render();

            RenderTexture anterior = RenderTexture.active;
            RenderTexture.active = rt;
            captura = new Texture2D(640, 720, TextureFormat.RGBA32, false);
            captura.ReadPixels(new Rect(0, 0, 640, 720), 0, 0);
            captura.Apply();
            RenderTexture.active = anterior;
            camara.targetTexture = null;

            byte[] png = captura.EncodeToPNG();
            File.WriteAllBytes(
                OutputFolder + $"/PreviewOrdenar_{indice}.png",
                png
            );

            File.WriteAllText(
                OutputFolder + $"/PreviewOrdenar_{indice}.txt",
                $"Tiempo: {tiempo}\n" +
                $"Renderers: {renderers.Length}\n" +
                $"Bounds center: {bounds.center}\n" +
                $"Bounds size: {bounds.size}\n" +
                $"Camera: {camara.transform.position}\n"
            );
        }
        finally
        {
            if (captura != null)
                UnityEngine.Object.DestroyImmediate(captura);
            if (rt != null)
            {
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    private static void AgregarComponenteHDRP(GameObject objeto, string nombreTipo)
    {
        Type tipo = Type.GetType(
            nombreTipo + ", Unity.RenderPipelines.HighDefinition.Runtime"
        );

        if (tipo != null && objeto.GetComponent(tipo) == null)
            objeto.AddComponent(tipo);
    }
}
#endif
