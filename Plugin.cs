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
    [BepInPlugin("Finnerex.MissileTurret", "MissileTurret", "1.3.3")]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    public class Plugin : BaseUnityPlugin
    {
        
        public static SpawnableMapObject MissileTurretMapObj;
        public static GameObject MissileTurretPrefab;
        public static GameObject MissilePrefab;


        public static ManualLogSource TheLogger;
        
        // Configs
        public int MaxTurrets;
        public int MinTurrets;

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

            // initialize the prefabs
            MissileTurretAI ai = MissileTurretPrefab.AddComponent<MissileTurretAI>();
            
            ai.rod = MissileTurretPrefab.transform.Find("missileTurret/Mount/Rod");
            ai.rail = ai.rod.Find("Rod.001");
            ai.missile = ai.rail.Find("Cylinder").gameObject;
            
            ai.acquireTargetAudio = ai.rod.GetComponent<AudioSource>();
            ai.disableAudio = ai.rod.Find("DisableSound").GetComponent<AudioSource>();
            ai.enableAudio = ai.rod.Find("EnableSound").GetComponent<AudioSource>();
            ai.laser = ai.rod.Find("LaserLight").gameObject;

            
            MissilePrefab.AddComponent<MissileAI>();
            
            Utilities.FixMixerGroups(MissileTurretPrefab);
            Utilities.FixMixerGroups(MissilePrefab);
            
            NetworkPrefabs.RegisterNetworkPrefab(MissileTurretPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(MissilePrefab);
            
            
            AnimationCurve curve = new AnimationCurve(new Keyframe(0, MinTurrets, 0.267f, 0.267f, 0, 0.246f),
                new Keyframe(1, MaxTurrets, 61, 61, 0.015f * MaxTurrets, 0)); // for sure

            MissileTurretMapObj = new SpawnableMapObject
            {
                prefabToSpawn = MissileTurretPrefab,
                spawnFacingAwayFromWall = true,
                numberToSpawn = curve
            };

            MapObjects.RegisterMapObject(MissileTurretMapObj, Levels.LevelTypes.All, _ => curve);

            Logger.LogInfo("Missile Turret Loaded!!!");

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
                new ConfigDescription("Maximum speed of a missile")).Value * 100;
            MissileAI.MaxTurnSpeed = Config.Bind<float>(new ConfigDefinition("Missile Options", "Turn Rate"), 0.6f,
                new ConfigDescription("How fast the missile can turn")).Value;
            MissileAI.Acceleration = Config.Bind<float>(new ConfigDefinition("Missile Options", "Acceleration"), 0.6f,
                new ConfigDescription("Acceleration of the missile")).Value * 100;
            
            MissileAI.KillRange = Config.Bind<float>(new ConfigDefinition("Missile Options", "Explosive Kill Range"), 1f,
                new ConfigDescription("Distance from explosion to kill")).Value;
            MissileAI.DamageRange = Config.Bind<float>(new ConfigDefinition("Missile Options", "Explosive Damage Range"), 5f,
                new ConfigDescription("Distance from explosion to damage")).Value;
            
            
            MissileTurretAI.RotationRange = Config.Bind<float>(
                new ConfigDefinition("Missile Turret Options", "Rotation Range"),45f,
                new ConfigDescription("The angle the turret\'s search is restricted to in degrees left & right")).Value;
            
            MissileTurretAI.RotationSpeed = Config.Bind<float>(
                new ConfigDefinition("Missile Turret Options", "Rotation Rate"), 0.25f,
                new ConfigDescription("The speed at which the turret rotates")).Value * 100;
            
            MissileTurretAI.ReloadTimeSeconds = Config.Bind<float>(
                new ConfigDefinition("Missile Turret Options", "Reload Time"), 6f,
                new ConfigDescription("The time it takes for the turret to reload in seconds")).Value;
            
            MissileTurretAI.ChargeTimeSeconds = Config.Bind<float>(
                new ConfigDefinition("Missile Turret Options", "Charge Time"), 0.5f,
                new ConfigDescription("The time it takes for the turret to shoot at a target in seconds")).Value;

        }
        
        
    }
}
