using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PulseDesk.EditorTools
{
    /// <summary>
    /// Command-line entry points, so the whole setup and build can run without clicking:
    ///   Unity.exe -batchmode -projectPath . -executeMethod PulseDesk.EditorTools.PulseDeskAutomation.Stage1
    /// Each method quits the editor itself (do not pass -quit).
    /// </summary>
    public static class PulseDeskAutomation
    {
        public const string PackageName = "com.pulsedesk.pulsedeskxr";
        private const string MainScene = "Assets/PulseDesk/Scenes/Main.unity";
        private const string DevScene = "Assets/PulseDesk/Scenes/Dev_B.unity";
        private const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        // ---------- release signing (secrets live outside the repo) ----------

        /// <summary>Folder holding pulsedesk-release.keystore and keystore.properties.</summary>
        public static string KeystoreDir
        {
            get
            {
                string env = Environment.GetEnvironmentVariable("PULSEDESK_KEYSTORE_DIR");
                if (!string.IsNullOrEmpty(env)) return env;
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PulseDeskKeys");
            }
        }

        /// <summary>Points the build at the release keystore. Returns false if it is not on this machine.</summary>
        public static bool ApplySigning()
        {
            string props = Path.Combine(KeystoreDir, "keystore.properties");
            if (!File.Exists(props))
            {
                Debug.LogWarning("PulseDesk: no " + props + ". The APK will be debug-signed and cannot be uploaded.");
                return false;
            }
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (string line in File.ReadAllLines(props))
            {
                int eq = line.IndexOf('=');
                if (eq > 0) map[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            string file, alias, storePass, keyPass;
            if (!map.TryGetValue("keystoreFile", out file) || !map.TryGetValue("keyAlias", out alias)
                || !map.TryGetValue("storePassword", out storePass))
            {
                Debug.LogWarning("PulseDesk: keystore.properties is missing keystoreFile, keyAlias or storePassword.");
                return false;
            }
            if (!map.TryGetValue("keyPassword", out keyPass)) keyPass = storePass;

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.Combine(KeystoreDir, file).Replace('\\', '/');
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasPass = keyPass;
            return true;
        }

        // ---------- stage 1: identifiers + TextMeshPro resources ----------

        public static void Stage1()
        {
            Run(() =>
            {
                PlayerSettings.companyName = "PulseDesk";
                PlayerSettings.productName = "PulseDesk XR";
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageName);
                PulseDeskMenu.ApplyAndroidSettings();
                ApplySigning();
                AssetDatabase.SaveAssets();

                if (Directory.Exists("Assets/TextMesh Pro"))
                {
                    Finish(0);
                    return;
                }
                AssetDatabase.importPackageCompleted += _ => { Debug.Log("PulseDesk: TMP essentials imported."); Finish(0); };
                AssetDatabase.importPackageFailed += (n, e) => { Debug.LogError("PulseDesk: TMP import failed: " + e); Finish(1); };
                AssetDatabase.ImportPackage(TmpPackage, false);
            });
        }

        // ---------- stage 2: scenes, Meta rig, project config ----------

        public static async void Stage2()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MainScene));

                // Dev_B: no Meta rig, for mouse-and-keyboard work.
                if (!File.Exists(DevScene))
                {
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    PulseDeskMenu.SetUpProject();
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), DevScene);
                }

                Scene scene = File.Exists(MainScene)
                    ? EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single)
                    : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                if (string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene, MainScene);

                ConfigureOculusProject();

                await InstallBlock("e47682b9-c270-40b1-b16d-90b627a5ce1b", "Camera Rig");
                await InstallBlock("f0540b20-dfd6-420e-b20d-c270f88dc77e", "Passthrough");
                await InstallBlock("8b26b298-7bf4-490e-b245-a039c0184303", "Hand Tracking");
                await InstallBlock("d4413f4f-542f-451f-92ee-4801f27d676a", "MR Utility Kit");

                // The rig brings its own camera.
                foreach (Camera cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (cam.gameObject.name == "Main Camera" && cam.transform.parent == null)
                        UnityEngine.Object.DestroyImmediate(cam.gameObject);
                }

                ConfigureSceneComponents();
                PulseDeskMenu.SetUpProject();

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MainScene);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };

                FixAll();
                FixAll();
                ConfigureOculusProject();   // in case a fix reset something we need
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MainScene);

                PulseDeskMenu.DetectFeatures();
                AssetDatabase.SaveAssets();
                LogSceneRoots();
                Finish(0);
            }
            catch (Exception e)
            {
                Debug.LogError("PulseDesk Stage2 failed: " + e);
                Finish(1);
            }
        }

        private static async Task InstallBlock(string id, string label)
        {
            Type utils = FindType("Meta.XR.BuildingBlocks.Editor.Utils");
            if (utils == null) throw new Exception("Building Blocks editor API not found");
            MethodInfo get = utils.GetMethod("GetBlockData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            object data = get.Invoke(null, new object[] { id });
            if (data == null) throw new Exception("Block not registered: " + label);

            PropertyInfo present = data.GetType().GetProperty("IsSingletonAndAlreadyPresent",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo isPresent = data.GetType().GetMethod("IsBlockPresentInScene",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            bool already = (present != null && (bool)present.GetValue(data))
                           || (isPresent != null && (bool)isPresent.Invoke(data, null));
            if (already)
            {
                Debug.Log("PulseDesk: block already in scene: " + label);
                return;
            }

            MethodInfo install = data.GetType().GetMethod("InstallWithDependencies",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(GameObject) }, null);
            Task task = (Task)install.Invoke(data, new object[] { null });
            await task;
            Debug.Log("PulseDesk: installed block " + label);
        }

        private static void ConfigureOculusProject()
        {
            string[] guids = AssetDatabase.FindAssets("t:OVRProjectConfig");
            if (guids.Length == 0)
            {
                Debug.LogWarning("PulseDesk: OculusProjectConfig asset not found.");
                return;
            }
            UnityEngine.Object cfg = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]));
            SerializedObject so = new SerializedObject(cfg);
            SetInt(so, "handTrackingSupport", 1);            // Controllers and Hands
            SetInt(so, "anchorSupport", 1);                  // Enabled
            SetInt(so, "sceneSupport", 1);                   // Supported, never Required
            SetInt(so, "_insightPassthroughSupport", 1);     // Supported
            SetBool(so, "insightPassthroughEnabled", true);
            SetBool(so, "requiresSystemKeyboard", true);     // "New task" opens the system keyboard
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cfg);
        }

        private static void ConfigureSceneComponents()
        {
            Type ovrManager = FindType("OVRManager");
            if (ovrManager != null)
            {
                foreach (UnityEngine.Object m in UnityEngine.Object.FindObjectsByType(ovrManager, FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    SerializedObject so = new SerializedObject(m);
                    SetBool(so, "isInsightPassthroughEnabled", true);
                    SetBool(so, "requestScenePermissionOnStartup", true);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            Type mruk = FindType("Meta.XR.MRUtilityKit.MRUK");
            if (mruk != null)
            {
                foreach (UnityEngine.Object m in UnityEngine.Object.FindObjectsByType(mruk, FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    SerializedObject so = new SerializedObject(m);
                    SetBool(so, "EnableWorldLock", true);
                    SerializedProperty ds = so.FindProperty("SceneSettings.DataSource");
                    if (ds != null)
                    {
                        for (int i = 0; i < ds.enumNames.Length; i++)
                            if (ds.enumNames[i] == "DeviceWithPrefabFallback") ds.enumValueIndex = i;
                        Debug.Log("PulseDesk: MRUK data source = " + ds.enumNames[ds.enumValueIndex]);
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        /// <summary>Meta > Tools > Project Setup Tool > Fix All, for Android.</summary>
        private static void FixAll()
        {
            Type setup = FindType("OVRProjectSetup");
            MethodInfo fix = setup == null ? null : setup.GetMethod("FixTasks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (fix == null)
            {
                Debug.LogWarning("PulseDesk: OVRProjectSetup.FixTasks not found; run the Project Setup Tool by hand.");
                return;
            }
            ParameterInfo[] ps = fix.GetParameters();
            object[] args = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++) args[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : null;
            args[0] = BuildTargetGroup.Android;
            fix.Invoke(null, args);
            Debug.Log("PulseDesk: Project Setup Tool fixes applied.");
        }

        /// <summary>Applies every automatic fix from Project Settings > XR Plug-in Management > Project Validation.</summary>
        [MenuItem("PulseDesk/Fix OpenXR validation issues", priority = 5)]
        public static void FixOpenXR()
        {
            for (int pass = 0; pass < 3; pass++)
            {
                var issues = new List<UnityEngine.XR.OpenXR.Features.OpenXRFeature.ValidationRule>();
                UnityEditor.XR.OpenXR.OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
                if (issues.Count == 0) break;
                foreach (var issue in issues)
                {
                    bool canFix = issue.fixIt != null && issue.fixItAutomatic;
                    Debug.Log("PulseDesk: OpenXR issue (" + (issue.error ? "error" : "warning") + ", "
                              + (canFix ? "fixing" : "manual") + "): " + issue.message);
                    if (!canFix) continue;
                    try { issue.fixIt(); }
                    catch (Exception e) { Debug.LogWarning("PulseDesk: fix failed: " + e.Message); }
                }
            }
        }

        // ---------- stage 3: build ----------

        public static void Build()
        {
            Run(() =>
            {
                bool signed = ApplySigning();
                ConfigureOculusProject();
                FixAll();
                FixOpenXR();
                AssetDatabase.SaveAssets();
                PulseDeskMenu.BuildAndroid();
                bool ok = File.Exists("Builds/PulseDeskXR.apk")
                          && (DateTime.Now - File.GetLastWriteTime("Builds/PulseDeskXR.apk")).TotalMinutes < 5;
                Debug.Log("PulseDesk: build finished, apk=" + ok + ", releaseSigned=" + signed);
                Finish(ok ? 0 : 1);
            });
        }

        // ---------- play-mode smoke test (no headset) ----------

        private const string SmokeKey = "PulseDeskSmoke";
        private static int _smokeErrors;
        private static int _smokePhase;
        private static double _smokeStart;

        /// <summary>Plays Dev_B for a few seconds, moves a card, breaks a goal down, and fails on any error.</summary>
        public static void PlaySmoke()
        {
            Run(() =>
            {
                PulseDeskMenu.ResetSave();
                EditorSceneManager.OpenScene(DevScene, OpenSceneMode.Single);
                SessionState.SetBool(SmokeKey, true);
                SmokeHook();
                EditorApplication.EnterPlaymode();
            });
        }

        private static bool _smokeHooked;

        // Runs again after a domain reload, if the project reloads on Play.
        [InitializeOnLoadMethod]
        private static void SmokeHook()
        {
            if (_smokeHooked || !SessionState.GetBool(SmokeKey, false)) return;
            _smokeHooked = true;
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                _smokeErrors++;
                Debug.Log("PulseDesk smoke saw " + type + ": " + msg + "\n" + stack);
            };
            EditorApplication.update += SmokeTick;
        }

        private static void SmokeTick()
        {
            if (!EditorApplication.isPlaying) return;
            if (_smokeStart <= 0) _smokeStart = EditorApplication.timeSinceStartup;
            double t = EditorApplication.timeSinceStartup - _smokeStart;
            PulseDeskApp app = PulseDeskApp.Instance;

            if (_smokePhase == 0 && t > 4)
            {
                _smokePhase = 1;
                GameObject dock = GameObject.Find("DeskDock");
                int cards = UnityEngine.Object.FindObjectsByType<TaskCardView>(FindObjectsInactive.Exclude).Length;
                Debug.Log("PulseDesk smoke: app=" + (app != null) + " dock=" + (dock != null)
                          + " lanes=" + UnityEngine.Object.FindObjectsByType<DockLane>(FindObjectsInactive.Exclude).Length
                          + " cards=" + cards + " tasks=" + (app != null ? app.Store.Tasks.Count : -1));
                if (app == null || dock == null || cards != 3) _smokeErrors++;
                Shot("shot-1-start");
                if (app != null)
                {
                    app.AddTask("Email the landlord about the boiler");
                    app.AddTask("call the bank at 3pm");
                    TaskItem sprint = app.Store.ActiveSprint();
                    if (sprint != null) app.Store.Move(sprint.id, TaskLane.Done);
                    app.SubmitGoal("Plan my day");
                    app.Recenter();
                }
            }
            else if (_smokePhase == 1 && t > 8)
            {
                _smokePhase = 2;
                // Include inactive: a lane shows four cards and hides the rest behind a "+N" badge.
                int cards = UnityEngine.Object.FindObjectsByType<TaskCardView>(FindObjectsInactive.Include).Length;
                int done = 0;
                foreach (TaskItem item in app.Store.Tasks) if (item.lane == TaskLane.Done) done++;
                Debug.Log("PulseDesk smoke: after actions cards=" + cards + " tasks=" + app.Store.Tasks.Count
                          + " done=" + done + " doneToday=" + app.Store.DoneToday + " errors=" + _smokeErrors);
                if (cards != app.Store.Tasks.Count || done != 1 || app.Store.Tasks.Count != 8
                    || app.Store.NextDue() == null) _smokeErrors++;
                Shot("shot-2-after");
                app.RemoveFocused();
                app.ClearDone();
                app.Store.AdjustSprint(1);
            }
            else if (_smokePhase == 2 && t > 10)
            {
                _smokePhase = 3;
                int cards = UnityEngine.Object.FindObjectsByType<TaskCardView>(FindObjectsInactive.Include).Length;
                Debug.Log("PulseDesk smoke: after remove and clear cards=" + cards + " tasks=" + app.Store.Tasks.Count
                          + " default=" + app.Store.DefaultMinutes + " errors=" + _smokeErrors);
                if (cards != 6 || app.Store.Tasks.Count != 6 || app.Store.CountIn(TaskLane.Done) != 0
                    || app.Store.DoneToday != 1 || app.Store.DefaultMinutes != 10f) _smokeErrors++;
                Shot("shot-3-cleared");
                SessionState.SetBool(SmokeKey, false);
                PulseDeskMenu.ResetSave();
                Debug.Log("PulseDesk smoke: " + (_smokeErrors == 0 ? "PASS" : "FAIL"));
                EditorApplication.Exit(_smokeErrors == 0 ? 0 : 1);
            }
        }

        /// <summary>Saves what the main camera sees to Logs/name.png, so a headless run can be looked at.</summary>
        private static void Shot(string name)
        {
            try
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                RenderTexture rt = new RenderTexture(1600, 900, 24);
                RenderTexture before = cam.targetTexture;
                cam.targetTexture = rt;
                // Look from where a seated person's eyes would be: about 35 cm above and 50 cm back.
                GameObject dock = GameObject.Find("DeskDock");
                Vector3 oldPos = cam.transform.position;
                Quaternion oldRot = cam.transform.rotation;
                if (dock != null)
                {
                    Transform d = dock.transform;
                    cam.transform.position = d.position - d.forward * 0.50f + Vector3.up * 0.35f;
                    cam.transform.LookAt(d.position + Vector3.up * 0.07f);
                }
                cam.Render();
                cam.transform.SetPositionAndRotation(oldPos, oldRot);
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                tex.Apply();
                cam.targetTexture = before;
                RenderTexture.active = null;
                File.WriteAllBytes("Logs/" + name + ".png", tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
            }
            catch (Exception e)
            {
                Debug.LogWarning("PulseDesk: screenshot failed: " + e.Message);
            }
        }

        // ---------- helpers ----------

        private static void Run(Action action)
        {
            try { action(); }
            catch (Exception e)
            {
                Debug.LogError("PulseDesk automation failed: " + e);
                Finish(1);
            }
        }

        private static void Finish(int code)
        {
            AssetDatabase.SaveAssets();
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        private static void LogSceneRoots()
        {
            List<string> names = new List<string>();
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects()) names.Add(go.name);
            Debug.Log("PulseDesk: scene roots -> " + string.Join(" | ", names.ToArray()));
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) { Debug.LogWarning("PulseDesk: property not found: " + name); return; }
            p.intValue = value;
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) { Debug.LogWarning("PulseDesk: property not found: " + name); return; }
            p.boolValue = value;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = a.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }
    }
}
