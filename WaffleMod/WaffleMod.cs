using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace WaffleMod
{
    // MUST have the following line for BepInEx to initialize R2API BEFORE OUR CODE (since we need to use R2API)
    // It shows we have a dependancy.
    [BepInDependency(ItemAPI.PluginGUID)]

    // MUST use the following for language files!
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // MUST use the following for plugin metadata, etc.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class WaffleMod : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // IF it already exists the mod will be deprecated.
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "MrCoolGuy640";
        public const string PluginName = "WaffleMod";
        public const string PluginVersion = "1.0.0";

        // We need our item definition to persist through our functions, and therefore make it a class field.
        private static ItemDef waffleItem;

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            // First let's define our item
            waffleItem = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            waffleItem.name = "WAFFLE_ITEM_NAME";
            waffleItem.nameToken = "WAFFLE_ITEM_NAME";
            waffleItem.pickupToken = "WAFFLE_ITEM_DESCRIPTION";
            waffleItem.descriptionToken = "WAFFLE_ITEM_DESCRIPTION";
            waffleItem.loreToken = "WAFFLE_ITEM_DESCRIPTION";

            // The tier determines what rarity the item is:
            // Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            // and finally NoTier is generally used for helper items, like the tonic affliction
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            waffleItem._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            // Instead of loading the itemtierdef directly, you can also do this like below as a workaround
            // waffleItem.deprecatedTier = ItemTier.Tier3;

            // You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks.
            waffleItem.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            waffleItem.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();

            // Can remove determines if a shrine of order, or a printer can take this item, usually true, except for NoTier items.
            waffleItem.canRemove = true;

            // Hidden means that there will be no pickup notification,
            // and it won't appear in the inventory at the top of the screen.
            // This is useful for certain noTier helper items!
            waffleItem.hidden = false;

            // You can add your own display rules here,
            // where the first argument passed are the default display rules:
            // the ones used when no specific display rules for a character are found.
            // For this example, we are omitting them,
            // as they are quite a pain to set up without tools like https://thunderstore.io/package/KingEnderBrine/ItemDisplayPlacementHelper/
            var displayRules = new ItemDisplayRuleDict(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomItem(waffleItem, displayRules));

            // But now we have defined an item, but it doesn't do anything yet. So we'll need to define that ourselves.
            // Doing += means OUR function is ADDED TO WHATS CALLED, so it does original stuff AND our function!
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            // If a character was killed by the world, we shouldn't do anything.
            if (!report.attacker || !report.attackerBody)
            {
                return;
            }

            var attackerCharacterBody = report.attackerBody;

            // We need an inventory to do check for our item
            if (attackerCharacterBody.inventory)
            {
                // Store the amount of our item we have
                var garbCount = attackerCharacterBody.inventory.GetItemCount(waffleItem.itemIndex);
                if (garbCount > 0 &&
                    // Roll for our 50% chance.
                    Util.CheckRoll(50, attackerCharacterBody.master))
                {
                    // Since we passed all checks, we now give our attacker the cloaked buff.
                    // Note how we are scaling the buff duration depending on the number of the custom item in our inventory.
                    attackerCharacterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, 3 + garbCount);
                }
            }
        }

        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.

                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(waffleItem.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
