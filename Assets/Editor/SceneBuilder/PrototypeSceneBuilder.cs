using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using VertigoShift.Audio;
using VertigoShift.Gameplay.Gravity;
using VertigoShift.Gameplay.Panic;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Targets;
using VertigoShift.Gameplay.Weapons;
using VertigoShift.Input;
using VertigoShift.UI;

namespace VertigoShift.EditorTools
{
    /// <summary>
    /// Builds the Vertigo Shift prototype scene from scratch:
    /// cubic 4 sided arena, player rig (FPS + gravity shift + revolver), targets, lighting and the
    /// post processing volume used by the dizziness meter.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/VertigoShiftPrototype.unity";
        const string MatFolder = "Assets/Materials";
        const string ProfilePath = "Assets/Settings/VertigoShiftVolumeProfile.asset";

        /// <summary>Interior half size of the cubic arena (walls sit at +/- this value).</summary>
        const float H = 20f;

        [MenuItem("Tools/Vertigo Shift/Build Prototype Scene")]
        public static void Build()
        {
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Settings");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Keep the prototype simulating even when the editor window is not focused.
            PlayerSettings.runInBackground = true;

            Material mFloor = CreateMaterial("M_ArenaFloor", new Color(0.15f, 0.17f, 0.22f), 0.20f, 0.40f);
            Material mWall = CreateMaterial("M_ArenaWall", new Color(0.19f, 0.22f, 0.29f), 0.10f, 0.40f);
            Material mBlock = CreateMaterial("M_CoverBlock", new Color(0.25f, 0.28f, 0.35f), 0.10f, 0.35f);
            Material mNeonCyan = CreateMaterial("M_NeonCyan", new Color(0.05f, 0.55f, 0.75f), 0f, 0.85f, new Color(0.2f, 3.2f, 4.5f));
            Material mNeonMagenta = CreateMaterial("M_NeonMagenta", new Color(0.65f, 0.10f, 0.50f), 0f, 0.85f, new Color(3.6f, 0.4f, 2.4f));
            Material mNeonOrange = CreateMaterial("M_NeonOrange", new Color(0.85f, 0.38f, 0.05f), 0f, 0.80f, new Color(4.0f, 1.6f, 0.15f));
            Material mTracer = CreateMaterial("M_Tracer", new Color(0.55f, 1f, 1f), 0f, 1f, new Color(2.5f, 6f, 7f));

            GameObject arena = BuildArena(mFloor, mWall, mBlock, mNeonCyan, mNeonMagenta);
            BuildLighting();

            PlayerRig player = BuildPlayer(mTracer);
            BuildTargets(player);

            BuildPostProcessing();
            BuildHud(player);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();

            Debug.Log("[VertigoShift] Prototype scene built at " + ScenePath);
        }

        // ------------------------------------------------------------------ arena

        static GameObject BuildArena(Material floor, Material wall, Material block, Material neonCyan, Material neonMagenta)
        {
            GameObject arena = new GameObject("Arena");

            // shell
            CreateBox("Floor", arena.transform, new Vector3(0f, -0.5f, 0f), new Vector3(2f * H + 2f, 1f, 2f * H + 2f), floor);
            CreateBox("Ceiling", arena.transform, new Vector3(0f, 2f * H + 0.5f, 0f), new Vector3(2f * H + 2f, 1f, 2f * H + 2f), wall);
            CreateBox("Wall_Left", arena.transform, new Vector3(-(H + 0.5f), H, 0f), new Vector3(1f, 2f * H, 2f * H + 2f), wall);
            CreateBox("Wall_Right", arena.transform, new Vector3(H + 0.5f, H, 0f), new Vector3(1f, 2f * H, 2f * H + 2f), wall);
            CreateBox("Wall_Front", arena.transform, new Vector3(0f, H, -(H + 0.5f)), new Vector3(2f * H + 2f, 2f * H, 1f), wall);
            CreateBox("Wall_Back", arena.transform, new Vector3(0f, H, H + 0.5f), new Vector3(2f * H + 2f, 2f * H, 1f), wall);

            // cover blocks (also usable as platforms once the gravity rotates)
            CreateBox("Cover_01", arena.transform, new Vector3(9f, 1.5f, 8f), new Vector3(6f, 3f, 6f), block);
            CreateBox("Cover_02", arena.transform, new Vector3(-11f, 1f, -7f), new Vector3(6f, 2f, 4f), block);
            CreateBox("Cover_03", arena.transform, new Vector3(2f, 1f, 14f), new Vector3(5f, 2f, 5f), block);
            CreateBox("Cover_04", arena.transform, new Vector3(-15f, 1.5f, 12f), new Vector3(4f, 3f, 4f), block);
            CreateBox("Cover_05", arena.transform, new Vector3(15f, 1f, -13f), new Vector3(5f, 2f, 5f), block);

            // platforms bolted to the walls / ceiling
            CreateBox("Plat_Left", arena.transform, new Vector3(-(H - 1f), 8f, 0f), new Vector3(2f, 4f, 8f), block);
            CreateBox("Plat_Right", arena.transform, new Vector3(H - 1f, 13f, -6f), new Vector3(2f, 5f, 7f), block);
            CreateBox("Plat_Ceiling", arena.transform, new Vector3(6f, 2f * H - 1f, 4f), new Vector3(7f, 2f, 5f), block);

            // neon strips
            CreateBox("Strip_N", arena.transform, new Vector3(0f, 0.03f, -(H - 0.7f)), new Vector3(2f * H, 0.06f, 0.25f), neonCyan);
            CreateBox("Strip_S", arena.transform, new Vector3(0f, 0.03f, H - 0.7f), new Vector3(2f * H, 0.06f, 0.25f), neonCyan);
            CreateBox("Strip_W", arena.transform, new Vector3(-(H - 0.7f), 0.03f, 0f), new Vector3(0.25f, 0.06f, 2f * H), neonMagenta);
            CreateBox("Strip_E", arena.transform, new Vector3(H - 0.7f, 0.03f, 0f), new Vector3(0.25f, 0.06f, 2f * H), neonMagenta);
            CreateBox("Strip_Ceiling", arena.transform, new Vector3(0f, 2f * H - 0.03f, 0f), new Vector3(2f * H, 0.06f, 0.25f), neonMagenta);

            return arena;
        }

        static void BuildLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.09f, 0.12f, 0.19f);
            RenderSettings.ambientEquatorColor = new Color(0.05f, 0.06f, 0.09f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.04f);

            GameObject sunGo = new GameObject("Key Light");
            sunGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.85f, 0.90f, 1f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;

            AddPointLight("Light_Cyan", new Vector3(0f, 30f, 0f), new Color(0.25f, 0.9f, 1f), 55f, 260f);
            AddPointLight("Light_Magenta", new Vector3(-14f, 9f, -14f), new Color(1f, 0.25f, 0.85f), 38f, 140f);
            AddPointLight("Light_Amber", new Vector3(14f, 6f, 12f), new Color(1f, 0.62f, 0.22f), 32f, 120f);
            AddPointLight("Light_Cyan_Back", new Vector3(14f, 26f, -13f), new Color(0.25f, 0.9f, 1f), 38f, 120f);
        }

        // ------------------------------------------------------------------ player

        class PlayerRig
        {
            public GameObject root;
            public PlayerMotor motor;
            public PlayerLook look;
            public GravityShift gravity;
            public DizzinessSystem dizziness;
            public Revolver revolver;
        }

        static PlayerRig BuildPlayer(Material tracerMaterial)
        {
            GameObject player = new GameObject("Player");
            player.layer = 2; // Ignore Raycast: ground checks and the hitscan ignore our own body
            player.transform.position = new Vector3(0f, 0.95f, 0f);

            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.35f;
            capsule.center = Vector3.zero;

            var rig = new PlayerRig();
            rig.root = player;

            rig.dizziness = player.AddComponent<DizzinessSystem>();
            rig.gravity = player.AddComponent<GravityShift>();
            rig.motor = player.AddComponent<PlayerMotor>();
            rig.look = player.AddComponent<PlayerLook>();
            player.AddComponent<SfxBank>();
            rig.revolver = player.AddComponent<Revolver>();
            player.AddComponent<CursorLockController>();

            // camera rig
            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);

            GameObject camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(pivot.transform, false);
            camGo.transform.localPosition = Vector3.zero; // the pivot drives the eye offset
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 300f;
            cam.fieldOfView = 78f;
            AudioListener listener = camGo.AddComponent<AudioListener>();
            UniversalAdditionalCameraData camData = camGo.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // tracer
            GameObject tracerGo = new GameObject("Tracer");
            tracerGo.transform.SetParent(camGo.transform, false);
            LineRenderer tracer = tracerGo.AddComponent<LineRenderer>();
            tracer.sharedMaterial = tracerMaterial;
            tracer.widthMultiplier = 0.035f;
            tracer.numCapVertices = 2;
            tracer.positionCount = 2;
            tracer.useWorldSpace = true;
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.receiveShadows = false;
            tracer.enabled = false;

            // muzzle flash
            GameObject flashGo = new GameObject("MuzzleFlash");
            flashGo.transform.SetParent(camGo.transform, false);
            flashGo.transform.localPosition = new Vector3(0.22f, -0.16f, 0.7f);
            Light muzzleFlash = flashGo.AddComponent<Light>();
            muzzleFlash.type = LightType.Point;
            muzzleFlash.range = 14f;
            muzzleFlash.intensity = 0f;
            muzzleFlash.color = new Color(1f, 0.85f, 0.55f);
            muzzleFlash.enabled = false;

            // wiring
            rig.motor.gravity = rig.gravity;

            rig.look.motor = rig.motor;
            rig.look.gravity = rig.gravity;
            rig.look.dizziness = rig.dizziness;
            rig.look.cameraPivot = pivot.transform;
            rig.look.cam = cam;

            rig.gravity.dizziness = rig.dizziness;
            rig.gravity.sfx = player.GetComponent<SfxBank>();

            rig.revolver.cam = cam;
            rig.revolver.muzzle = flashGo.transform;
            rig.revolver.sfx = player.GetComponent<SfxBank>();
            rig.revolver.dizziness = rig.dizziness;
            rig.revolver.look = rig.look;
            rig.revolver.tracer = tracer;
            rig.revolver.muzzleFlash = muzzleFlash;

            rig.dizziness.audioListener = listener;

            return rig;
        }

        // ------------------------------------------------------------------ targets

        static void BuildTargets(PlayerRig player)
        {
            GameObject targetRoot = new GameObject("Targets");

            Material orange = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/M_NeonOrange.mat");
            Material cyan = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/M_NeonCyan.mat");
            Material magenta = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/M_NeonMagenta.mat");

            CreateTarget("Target_Floor_01", targetRoot.transform, new Vector3(6f, 0.6f, 16f), Quaternion.identity, orange, player);
            CreateTarget("Target_Floor_02", targetRoot.transform, new Vector3(-13f, 0.6f, 4f), Quaternion.identity, orange, player);
            CreateTarget("Target_Floor_03", targetRoot.transform, new Vector3(15f, 0.6f, -6f), Quaternion.identity, orange, player);
            CreateTarget("Target_Block_01", targetRoot.transform, new Vector3(9f, 3.6f, 8f), Quaternion.identity, orange, player);
            CreateTarget("Target_Wall_Left", targetRoot.transform, new Vector3(-(H - 0.6f), 14f, 4f), Quaternion.identity, cyan, player);
            CreateTarget("Target_Wall_Right", targetRoot.transform, new Vector3(H - 0.6f, 18f, -6f), Quaternion.identity, cyan, player);
            CreateTarget("Target_Ceiling", targetRoot.transform, new Vector3(0f, 2f * H - 0.6f, 10f), Quaternion.identity, magenta, player);
        }

        static void CreateTarget(string name, Transform parent, Vector3 position, Quaternion rotation, Material material, PlayerRig player)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * 1.2f;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;

            TargetDummy dummy = go.AddComponent<TargetDummy>();
            dummy.dizziness = player.dizziness;
            dummy.player = player.root.transform;
            dummy.gibMaterial = material;
        }

        // ------------------------------------------------------------------ post processing & hud

        static void BuildPostProcessing()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            ChromaticAberration ca = GetOrAdd<ChromaticAberration>(profile);
            ca.intensity.Override(0f);
            ca.active = true;
            Vignette vig = GetOrAdd<Vignette>(profile);
            vig.intensity.Override(0.12f);
            vig.smoothness.Override(0.4f);
            vig.active = true;
            ColorAdjustments adj = GetOrAdd<ColorAdjustments>(profile);
            adj.saturation.Override(0f);
            adj.contrast.Override(0f);
            adj.active = true;

            EditorUtility.SetDirty(profile);

            GameObject volumeGo = new GameObject("Global Volume");
            Volume volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;
        }

        static void BuildHud(PlayerRig player)
        {
            GameObject hudGo = new GameObject("PrototypeHUD");
            PrototypeHUD hud = hudGo.AddComponent<PrototypeHUD>();
            hud.revolver = player.revolver;
            hud.gravity = player.gravity;
            hud.dizziness = player.dizziness;
            hud.motor = player.motor;
        }

        // ------------------------------------------------------------------ helpers

        static void RegisterInBuildSettings()
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            buildScenes.RemoveAll(s => s.path == ScenePath);
            buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = null;
            if (!profile.TryGet<T>(out component)) component = profile.Add<T>(true);
            return component;
        }

        static Material CreateMaterial(string name, Color color, float metallic, float smoothness, Color? emission = null)
        {
            string path = MatFolder + "/" + name + ".mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static void AddPointLight(string name, Vector3 position, Color color, float range, float intensity)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }
    }
}
