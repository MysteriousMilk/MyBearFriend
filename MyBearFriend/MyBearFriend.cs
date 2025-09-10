using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MyBearFriend
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class MyBearFriend : BaseUnityPlugin
    {
        public const string PluginGUID = "com.milkwyzard.MyBearFriend";
        public const string PluginName = "MyBearFriend";
        public const string PluginVersion = "0.1.0";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private const string bearNameLocalizedFormat = "name_randBearName";

        private Harmony harmony;
        private int randomNameCount = 0;
        private bool creaturesAvailable = false;
        private bool prefabsAvailable = false;

        #region Config Variables
        /// <summary>
        /// Items that the Bear can consume/eat.
        /// </summary>
        public static ConfigEntry<string> BearConsumableItems;

        /// <summary>
        /// Amount of time (in seconds) it takes to tame a Bear.
        /// </summary>
        public static ConfigEntry<float> BearTameTime;

        /// <summary>
        /// Amount of time (in seconds) after feeding a Bear before it becomes hungry again.
        /// </summary>
        public static ConfigEntry<float> BearFedDuration;
        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MyBearFriend()
        {
            harmony = new Harmony(PluginGUID);
        }

        public void Start()
        {
            // patch this dll
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void Awake()
        {
            ConfigurationManagerAttributes isAdminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };

            BearConsumableItems = Config.Bind(
                "General",
                "BearConsumableItems",
                "Blueberries; Honey; RawMeat; DeerMeat",
                    new ConfigDescription("Items that the Bear can consume/eat. Must be the name of the prefab. See Jotunn docs.",
                    null,
                    isAdminOnly)
                );

            BearTameTime = Config.Bind("General", "BearTameTime", 1600f, 
                new ConfigDescription("Amount of time (in seconds) it takes to tame a Bear (default is slightly less than wolf).", new AcceptableValueRange<float>(0f, 10000f), isAdminOnly));

            BearFedDuration = Config.Bind("General", "BearFedDuration", 400f,
                new ConfigDescription("Amount of time (in seconds) after feeding a Bear before it becomes hungry again (default is slightly less than wolf).", new AcceptableValueRange<float>(0f, 1000f), isAdminOnly));

            // Hook creature manager to get access to vanilla creature prefabs
            CreatureManager.OnVanillaCreaturesAvailable += OnVanillaCreaturesAvailable;
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            LocalizationManager.OnLocalizationAdded += OnLocalizationsAdded;

            Jotunn.Logger.LogInfo($"MyBearFriend v{PluginVersion} loaded and patched.");
        }

        private void OnLocalizationsAdded()
        {
            ResolveLocalizations();

            var firstLang = Localization.GetLanguages().FirstOrDefault();
            if (firstLang != null)
            {
                randomNameCount = Localization.GetTranslations(firstLang).Count(kvp => kvp.Key.StartsWith(bearNameLocalizedFormat));
                Jotunn.Logger.LogWarning($"Random name count: {randomNameCount}");
            }
        }

        private void OnVanillaPrefabsAvailable()
        {
            prefabsAvailable = true;

            if (prefabsAvailable && creaturesAvailable)
                MakeBearTameable();
        }

        private void OnVanillaCreaturesAvailable()
        {
            creaturesAvailable = true;

            if (prefabsAvailable && creaturesAvailable)
                MakeBearTameable();
        }

        private void MakeBearTameable()
        {
            var wolfPrefab = CreatureManager.Instance.GetCreaturePrefab("Wolf");
            if (wolfPrefab == null)
            {
                Jotunn.Logger.LogWarning("Could not Wolf creature prefab.");
                return;
            }

            var wolfTameable = wolfPrefab.GetComponent<Tameable>();
            if (wolfTameable == null)
            {
                Jotunn.Logger.LogWarning("Could not find Tameable Component for the Wolf prefab.");
                return;
            }

            var bearPrefab = CreatureManager.Instance.GetCreaturePrefab("Bjorn");
            if (bearPrefab == null)
            {
                Jotunn.Logger.LogWarning("Could not find Bear creature prefab.");
                return;
            }

            var bearAi = bearPrefab.GetComponent<MonsterAI>();
            if (bearAi == null)
            {
                Jotunn.Logger.LogWarning("Expected Bear prefab to have Monster AI.");
                return;
            }

            bearAi.m_consumeItems.Clear();
            foreach (string itemName in EnumerateConsumables(BearConsumableItems))
                AddConsumableItem(bearAi, itemName);

            var bearTameable = bearPrefab.GetComponent<Tameable>();
            if (bearTameable != null)
            {
                Jotunn.Logger.LogWarning("Expected Bear prefab to not have a Tameable component.");
                return;
            }

            if (wolfTameable.m_tamedEffect.m_effectPrefabs.Length == 0 ||
                wolfTameable.m_petEffect.m_effectPrefabs.Length == 0 ||
                wolfTameable.m_sootheEffect.m_effectPrefabs.Length == 0)
            {
                Jotunn.Logger.LogWarning("Could not get taming effect prefabs from Wolf prefab type.");
                return;
            }

            var tameFx = DuplicateEffectData(wolfTameable.m_tamedEffect.m_effectPrefabs[0]);
            if (tameFx == null)
                return;

            var petFx = DuplicateEffectData(wolfTameable.m_petEffect.m_effectPrefabs[0]);
            if (petFx == null)
                return;

            var sootheFx = DuplicateEffectData(wolfTameable.m_sootheEffect.m_effectPrefabs[0]);
            if (sootheFx == null)
                return;

            Jotunn.Logger.LogDebug($"m_levelUpOwnerSkill - {wolfTameable.m_levelUpOwnerSkill}");
            Jotunn.Logger.LogDebug($"m_tamingTime - {wolfTameable.m_tamingTime}");
            Jotunn.Logger.LogDebug($"m_fedDuration - {wolfTameable.m_fedDuration}");

            bearTameable = bearPrefab.AddComponent<Tameable>();
            bearTameable.m_fedDuration = BearFedDuration.Value;
            bearTameable.m_tamingTime = BearTameTime.Value;
            bearTameable.m_startsTamed = false;
            bearTameable.m_tamedEffect.m_effectPrefabs = new EffectList.EffectData[1] { tameFx };
            bearTameable.m_sootheEffect.m_effectPrefabs = new EffectList.EffectData[1] { sootheFx };
            bearTameable.m_petEffect.m_effectPrefabs = new EffectList.EffectData[1] { petFx };
            bearTameable.m_commandable = wolfTameable.m_commandable;
            bearTameable.m_unsummonDistance = wolfTameable.m_unsummonDistance;
            bearTameable.m_unsummonOnOwnerLogoutSeconds = wolfTameable.m_unsummonOnOwnerLogoutSeconds;
            bearTameable.m_levelUpOwnerSkill = wolfTameable.m_levelUpOwnerSkill;
            bearTameable.m_levelUpFactor = wolfTameable.m_levelUpFactor;
            bearTameable.m_saddleItem = wolfTameable.m_saddleItem;
            bearTameable.m_saddle = wolfTameable.m_saddle;
            bearTameable.m_dropSaddleOnDeath = wolfTameable.m_dropSaddleOnDeath;
            bearTameable.m_dropSaddleOffset = wolfTameable.m_dropSaddleOffset;
            bearTameable.m_dropItemVel = wolfTameable.m_dropItemVel;
            bearTameable.m_tamingSpeedMultiplierRange = wolfTameable.m_tamingSpeedMultiplierRange;
            bearTameable.m_tamingBoostMultiplier = wolfTameable.m_tamingBoostMultiplier;

            SetRandomStartingNames(bearTameable);
        }

        private static void AddConsumableItem(MonsterAI monsterAi, string itemName)
        {
            ItemDrop itemDrop = null;
            var itemPrefab = PrefabManager.Instance.GetPrefab(itemName);
            if (itemPrefab != null)
            {
                itemDrop = itemPrefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                    monsterAi.m_consumeItems.Add(itemDrop);
            }

            if (itemDrop == null)
                Jotunn.Logger.LogWarning($"Could not find item: {itemName}");
            else
                Jotunn.Logger.LogDebug($"Bear can consume {itemDrop.name}");
        }

        private void SetRandomStartingNames(Tameable tameable)
        {
            tameable.m_randomStartingName = new List<string>();
            for (int i = 0; i < randomNameCount; i++)
                tameable.m_randomStartingName.Add($"${bearNameLocalizedFormat}{i}");
        }

        private EffectList.EffectData DuplicateEffectData(EffectList.EffectData fxData)
        {
            if (fxData.m_prefab == null)
                return null;

            var fxPrefab = PrefabManager.Instance.GetPrefab(fxData.m_prefab.name);
            if (fxPrefab == null)
            {
                Jotunn.Logger.LogWarning($"Could not get effect prefab {fxData.m_prefab.name}");
                return null;
            }

            EffectList.EffectData clonedData = new EffectList.EffectData();
            clonedData.m_prefab = fxPrefab;
            clonedData.m_enabled = fxData.m_enabled;
            clonedData.m_variant = fxData.m_variant;
            clonedData.m_attach = fxData.m_attach;
            clonedData.m_follow = fxData.m_follow;
            clonedData.m_inheritParentRotation = fxData.m_inheritParentRotation;
            clonedData.m_inheritParentScale = fxData.m_inheritParentScale;
            clonedData.m_multiplyParentVisualScale = fxData.m_multiplyParentVisualScale;
            clonedData.m_randomRotation = fxData.m_randomRotation;
            clonedData.m_scale = fxData.m_scale;
            clonedData.m_childTransform = fxData.m_childTransform;

            return clonedData;
        }

        private IEnumerable<string> EnumerateConsumables(ConfigEntry<string> configConsumables)
        {
            string[] consumables = configConsumables.Value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string consumable in consumables)
                yield return consumable.Trim();
        }

        private void ResolveLocalizations()
        {
            try
            {
                DirectoryInfo pluginDir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                Dictionary<string, List<FileInfo>> languageFiles = new Dictionary<string, List<FileInfo>>();

                foreach (var jsonFile in pluginDir.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly))
                {
                    string[] nameTokens = jsonFile.Name.Split(new char[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (nameTokens.Length > 2 &&
                        nameTokens.Last() == "json")
                    {
                        string lang = nameTokens[nameTokens.Length - 2].Trim();
                        //Logger.LogInfo($"{jsonFile.Name} - {lang}");
                        if (LocalizationHelper.IsLanguageSupported(lang))
                        {
                            if (!languageFiles.ContainsKey(lang))
                                languageFiles.Add(lang, new List<FileInfo>());

                            languageFiles[lang].Add(jsonFile);
                        }
                    }
                }

                foreach (var lang in  languageFiles.Keys)
                {
                    if (!Localization.GetLanguages().Contains(lang))
                    {
                        foreach (var jsonFile in languageFiles[lang])
                        {
                            Localization.AddJsonFile(lang, File.ReadAllText(jsonFile.FullName));
                            Logger.LogInfo($"Added localization file [{jsonFile.Name}] from non-standard location.");
                        }
                    }
                }

                foreach (var lang in Localization.GetLanguages())
                    Logger.LogInfo($"{lang} localization loaded and available.");
            }
            catch (Exception)
            {
                Logger.LogError("Error resolving localizations.");
            }
        }
    }
}

