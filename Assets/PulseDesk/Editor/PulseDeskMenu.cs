using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PulseDesk.EditorTools
{
    /// <summary>
    /// The PulseDesk menu: one-click project setup, feature switches for the Meta-specific code,
    /// and the Android build. Feature switches are scripting defines, so a part that does not
    /// compile against your SDK version can be turned off without touching the rest.
    /// </summary>
    public static class PulseDeskMenu
    {
        private const string Meta = "PULSEDESK_META";
        private const string Micro = "PULSEDESK_MICROGESTURES";
        private const string Mruk = "PULSEDESK_MRUK";
        private const string Depth = "PULSEDESK_DEPTH";
        private const string MaterialPath = "Assets/PulseDesk/Resources/PulseDeskSurface.mat";

        // ---------- 1. project setup ----------

        [MenuItem("PulseDesk/1. Set up project (material + scene object)", priority = 1)]
        public static void SetUpProject()
        {
            CreateMaterial();

            PulseDeskApp app = UnityEngine.Object.FindFirstObjectByType<PulseDeskApp>();
            if (app == null)
            {
                GameObject go = new GameObject("PulseDesk");
                go.AddComponent<PulseDeskApp>();
                Undo.RegisterCreatedObjectUndo(go, "Add PulseDesk");
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("PulseDesk: added a 'PulseDesk' object to the open scene. Save the scene (Ctrl+S).");
            }
            else
            {
                Debug.Log("PulseDesk: the scene already has a PulseDesk object.");
            }
        }

        private const string GlassPath = "Assets/PulseDesk/Resources/PulseDeskGlass.mat";

        /// <summary>Material assets must exist on disk so their shader variants are included in the build.</summary>
        public static void CreateMaterial()
        {
            CreateSolidMaterial();
            Material solid = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (solid != null && AssetDatabase.LoadAssetAtPath<Material>(GlassPath) == null)
            {
                Material glass = new Material(solid);
                Make.ConfigureGlass(glass);
                AssetDatabase.CreateAsset(glass, GlassPath);
                AssetDatabase.SaveAssets();
                Debug.Log("PulseDesk: created " + GlassPath);
            }
        }

        private static void CreateSolidMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null) return;

            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Shader.Find(srp ? "Universal Render Pipeline/Unlit" : "Unlit/Color");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                Debug.LogError("PulseDesk: no unlit shader found. Is the project using URP or the built-in pipeline?");
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.CreateAsset(new Material(shader), MaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log("PulseDesk: created " + MaterialPath + " using shader " + shader.name);
        }

        // ---------- 2. feature switches ----------

        [MenuItem("PulseDesk/2. Detect Meta SDK features", priority = 2)]
        public static void DetectFeatures()
        {
            Type ovrHand = FindType("OVRHand");
            bool hasMeta = ovrHand != null && FindType("OVRCameraRig") != null && FindType("OVRSkeleton") != null;
            bool hasMicro = hasMeta && ovrHand.GetNestedType("MicrogestureType") != null
                            && ovrHand.GetMethod("GetMicrogestureType") != null;
            bool hasMruk = FindType("MRUK") != null;
            bool hasDepth = FindType("EnvironmentRaycastManager") != null;

            SetDefine(Meta, hasMeta);
            SetDefine(Micro, hasMicro);
            SetDefine(Mruk, hasMruk);
            SetDefine(Depth, hasDepth);

            Debug.Log("PulseDesk features -> hands: " + hasMeta + ", microgestures: " + hasMicro
                      + ", room scan (MRUK): " + hasMruk + ", depth raycast: " + hasDepth
                      + ". Unity will now recompile. If errors appear, switch the failing feature off under PulseDesk > Features.");
        }

        [MenuItem("PulseDesk/Features/Hands (Meta core SDK)", priority = 20)]
        private static void ToggleMeta() { SetDefine(Meta, !HasDefine(Meta)); }
        [MenuItem("PulseDesk/Features/Hands (Meta core SDK)", true)]
        private static bool ToggleMetaCheck() { Menu.SetChecked("PulseDesk/Features/Hands (Meta core SDK)", HasDefine(Meta)); return true; }

        [MenuItem("PulseDesk/Features/Thumb microgestures", priority = 21)]
        private static void ToggleMicro() { SetDefine(Micro, !HasDefine(Micro)); }
        [MenuItem("PulseDesk/Features/Thumb microgestures", true)]
        private static bool ToggleMicroCheck() { Menu.SetChecked("PulseDesk/Features/Thumb microgestures", HasDefine(Micro)); return true; }

        [MenuItem("PulseDesk/Features/Room scan placement (MRUK)", priority = 22)]
        private static void ToggleMruk() { SetDefine(Mruk, !HasDefine(Mruk)); }
        [MenuItem("PulseDesk/Features/Room scan placement (MRUK)", true)]
        private static bool ToggleMrukCheck() { Menu.SetChecked("PulseDesk/Features/Room scan placement (MRUK)", HasDefine(Mruk)); return true; }

        [MenuItem("PulseDesk/Features/Depth placement (unscanned rooms)", priority = 23)]
        private static void ToggleDepth() { SetDefine(Depth, !HasDefine(Depth)); }
        [MenuItem("PulseDesk/Features/Depth placement (unscanned rooms)", true)]
        private static bool ToggleDepthCheck() { Menu.SetChecked("PulseDesk/Features/Depth placement (unscanned rooms)", HasDefine(Depth)); return true; }

        [MenuItem("PulseDesk/Features/Turn all Meta features off", priority = 40)]
        private static void AllOff()
        {
            SetDefine(Meta, false);
            SetDefine(Micro, false);
            SetDefine(Mruk, false);
            SetDefine(Depth, false);
        }

        private static readonly NamedBuildTarget[] Targets = { NamedBuildTarget.Android, NamedBuildTarget.Standalone };

        private static bool HasDefine(string define)
        {
            string current = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            return new List<string>(current.Split(';')).Contains(define);
        }

        private static void SetDefine(string define, bool on)
        {
            for (int i = 0; i < Targets.Length; i++)
            {
                List<string> list = new List<string>(
                    PlayerSettings.GetScriptingDefineSymbols(Targets[i]).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                bool has = list.Contains(define);
                if (on && !has) list.Add(define);
                else if (!on && has) list.Remove(define);
                else continue;
                PlayerSettings.SetScriptingDefineSymbols(Targets[i], string.Join(";", list.ToArray()));
            }
        }

        private static Type FindType(string simpleName)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = a.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch (Exception) { continue; }
                if (types == null) continue;
                for (int i = 0; i < types.Length; i++)
                    if (types[i] != null && types[i].Name == simpleName && !types[i].IsNested) return types[i];
            }
            return null;
        }

        // ---------- 3. Android settings ----------

        [MenuItem("PulseDesk/3. Apply Android player settings (IL2CPP, ARM64)", priority = 3)]
        public static void ApplyAndroidSettings()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if (string.IsNullOrEmpty(PlayerSettings.productName) || PlayerSettings.productName.StartsWith("My project"))
                PlayerSettings.productName = "PulseDesk XR";
            Debug.Log("PulseDesk: Android set to IL2CPP + ARM64. Still to do by hand: package name, release keystore, "
                      + "and Meta > Tools > Project Setup Tool > Fix All.");
        }

        // ---------- 4. build ----------

        [MenuItem("PulseDesk/4. Build APK (Builds∕PulseDeskXR.apk)", priority = 4)]
        public static void BuildAndroid()
        {
            List<string> scenes = new List<string>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.enabled && !string.IsNullOrEmpty(s.path)) scenes.Add(s.path);
            if (scenes.Count == 0)
            {
                string active = SceneManager.GetActiveScene().path;
                if (string.IsNullOrEmpty(active))
                {
                    Debug.LogError("PulseDesk: save the scene first, then build.");
                    return;
                }
                scenes.Add(active);
            }
            CreateMaterial();

            ApplyAndroidSettings();
            PulseDeskAutomation.ApplySigning();
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.bundleVersionCode = PlayerSettings.Android.bundleVersionCode + 1;

            Directory.CreateDirectory("Builds");
            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = scenes.ToArray();
            options.locationPathName = "Builds/PulseDeskXR.apk";
            options.target = BuildTarget.Android;
            options.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("PulseDesk: build OK -> " + options.locationPathName + " (" + (report.summary.totalSize / 1048576UL)
                          + " MB), version code " + PlayerSettings.Android.bundleVersionCode
                          + ". Check that it is signed with the release keystore before uploading.");
            }
            else
            {
                Debug.LogError("PulseDesk: build " + report.summary.result + " with " + report.summary.totalErrors + " error(s). See the Console.");
            }
        }

        // ---------- helpers ----------

        [MenuItem("PulseDesk/Reset saved tasks (Editor)", priority = 60)]
        public static void ResetSave()
        {
            string path = Path.Combine(Application.persistentDataPath, "pulsedesk.json");
            if (File.Exists(path)) File.Delete(path);
            Debug.Log("PulseDesk: saved tasks cleared. Next Play starts with the three starter cards.");
        }
    }
}
