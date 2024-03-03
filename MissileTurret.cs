using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using LethalLib.Modules;
using Unity.Netcode;
using UnityEngine;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using Object = UnityEngine.Object;

namespace MissileTurret
{
    [BepInPlugin("Finnerex.MissileTurret", "MissileTurret", "1.0.2")]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    public class MissileTurret : BaseUnityPlugin
    {
        
        private readonly Harmony _harmony = new Harmony("Finnerex.MissileTurret");

        public static SpawnableMapObject MissileTurretMapObj;
        public static GameObject MissileTurretPrefab;
        public static GameObject MissilePrefab;

        public static GameObject NetworkPrefab;

        public static ManualLogSource TheLogger;
        
        // Configs
        public int MaxTurrets;
        public int MinTurrets;
        public Dictionary<string, float> SpawnWeights = new();

        private void Awake()
        {
            TheLogger = Logger;
            
            Logger.LogInfo("Missile Turret Loading???");

            // Configs
            Configure();
            
            // ok cool
            InitializeNetworkBehaviours();

            // fuh real inniting
                
            string modPath = Path.GetDirectoryName(Info.Location);
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(modPath, "missileturretassetbundle"));

            if (bundle is null)
            {
                Logger.LogError("Failed to load assets");
                return;
            }
            
            MissileTurretPrefab = bundle.LoadAsset<GameObject>("MissileTurret");
            MissilePrefab = bundle.LoadAsset<GameObject>("Missile");
            NetworkPrefab = bundle.LoadAsset<GameObject>("Networker");

            // initialize the prefabs
            MissileTurretAI ai = MissileTurretPrefab.AddComponent<MissileTurretAI>();
            ai.missile = MissileTurretPrefab.transform.Find("missileTurret/Mount/Rod/Rod.001/Cylinder").gameObject;
            ai.rod = MissileTurretPrefab.transform.Find("missileTurret/Mount/Rod");
            ai.acquireTargetAudio = ai.rod.GetComponent<AudioSource>();

            MissilePrefab.AddComponent<MissileAI>();

            NetworkPrefab.AddComponent<NetworkHandler>();
            
            
            NetworkPrefabs.RegisterNetworkPrefab(MissileTurretPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(MissilePrefab);
            NetworkPrefabs.RegisterNetworkPrefab(NetworkPrefab);
            
            
            AnimationCurve curve = new AnimationCurve(new Keyframe(0, MinTurrets), new Keyframe(1, MaxTurrets)); // for sure

            MissileTurretMapObj = new SpawnableMapObject
            {
                prefabToSpawn = MissileTurretPrefab,
                spawnFacingAwayFromWall = true,
                numberToSpawn = curve
            };

            MapObjects.RegisterMapObject(MissileTurretMapObj, Levels.LevelTypes.All, _ => curve);

            _harmony.PatchAll(typeof(MissileTurret));
            _harmony.PatchAll(typeof(MissileAI));

            Logger.LogInfo("Missile Turret Loaded!!!");

        }
        
        
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {

            if(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                GameObject networkHandlerHost = Instantiate(NetworkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }
        
        private static void InitializeNetworkBehaviours() {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (MethodInfo method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        } 
        

        private void Configure()
        {
            
            MaxTurrets = Config.Bind<int>(new ConfigDefinition("Spawn Options", "Max Turrets"), 6,
                new ConfigDescription("Maximum number of turrets that can be spawned")).Value;
            MinTurrets = Config.Bind<int>(new ConfigDefinition("Spawn Options", "Min Turrets"), 0,
                new ConfigDescription("Minimum number of turrets that can be spawned")).Value;
            
            
            MissileAI.MaxSpeed = Config.Bind<float>(new ConfigDefinition("Missile Options", "Max Speed"), 0.7f,
                new ConfigDescription("Maximum speed of a missile")).Value;
            MissileAI.MaxTurnSpeed = Config.Bind<float>(new ConfigDefinition("Missile Options", "Turn Rate"), 0.4f,
                new ConfigDescription("How fast the missile can turn")).Value;
            MissileAI.Acceleration = Config.Bind<float>(new ConfigDefinition("Missile Options", "Acceleration"), 0.1f,
                new ConfigDescription("Acceleration of the missile")).Value / 100f;
            
            MissileAI.KillRange = Config.Bind<float>(new ConfigDefinition("Missile Options", "Explosive Kill Range"), 1f,
                new ConfigDescription("Distance from explosion to kill")).Value;
            MissileAI.DamageRange = Config.Bind<float>(new ConfigDefinition("Missile Options", "Explosive Damage Range"), 5f,
                new ConfigDescription("Distance from explosion to damage")).Value;
            
            
            MissileTurretAI.RotationRange = Config.Bind<float>(
                new ConfigDefinition("Missile Turret Options", "Rotation Range"),45f,
                new ConfigDescription("The angle the turret\'s search is restricted to in degrees left & right")).Value;
            
            MissileTurretAI.RotationSpeed = Config.Bind<float>(
                new ConfigDefinition("Missile Turret Options", "Rotation Rate"), 0.25f,
                new ConfigDescription("The speed at which the turret rotates")).Value;
            
            MissileTurretAI.ReloadTimeSeconds = Config.Bind<float>(
                new ConfigDefinition("Missile Turret Options", "Reload Time"), 6f,
                new ConfigDescription("The time it takes for the turret to reload in seconds")).Value;
            
            MissileTurretAI.ChargeTimeSeconds = Config.Bind<float>(
                new ConfigDefinition("Missile Turret Options", "Charge Time"), 0.8f,
                new ConfigDescription("The time it takes for the turret to shoot at a target in seconds")).Value;
            
        }
        
    }
}
