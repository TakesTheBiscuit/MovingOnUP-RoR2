//If you haven't done so yet, run the setup.bat file in your project/libs folder to acquire the needed references.
using BepInEx;
using BepInEx.Bootstrap;

using R2API;
using R2API.Utils;
using RoR2;
using System.Reflection;
using UnityEngine;
using static R2API.SoundAPI;
using RoR2.WwiseUtils;

namespace ExamplePlugin
{
    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency("com.bepis.r2api")]

    [BepInPlugin(
        "com.TakesTheBiscuit.MovingOnUP",
        "MovingOnUP",
        "0.2.0")]


    //We will be using 3 modules from R2API: ItemAPI to add our item, ItemDropAPI to have our item drop ingame, and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(ItemAPI),nameof(ItemDropAPI),nameof(LanguageAPI))]
    

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class ExamplePlugin : BaseUnityPlugin
    {
        //We need our item definition to persist through our functions, and therefore make it a class field.
        private static ItemDef myItemDef;

        private bool FirstLevelInit = false;


        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //First let's define our item
            myItemDef = new ItemDef
            {
                //More on these later
                name = "EXAMPLE_CLOAKONKILL_NAME",
                nameToken = "EXAMPLE_CLOAKONKILL_NAME", //? Still needed if we are assigning name in the line above?
                pickupToken = "EXAMPLE_CLOAKONKILL_PICKUP",
                descriptionToken = "EXAMPLE_CLOAKONKILL_DESC",
                loreToken = "EXAMPLE_CLOAKONKILL_LORE",
                //The tier determines what rarity the item is: Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow, and finally NoTier is generally used for helper items, like the tonic affliction
                tier = ItemTier.Tier2,
                //You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks.
                pickupIconPath = "Textures/MiscIcons/texMysteryIcon",
                pickupModelPath = "Prefabs/PickupModels/PickupMystery",
                //Can remove determines if a shrine of order, or a printer can take this item, generally true, except for NoTier items.
                canRemove = true,
                //Hidden means that there will be no pickup notification, and it won't appear in the inventory at the top of the screen. This is useful for certain noTier helper items, such as the DrizzlePlayerHelper.
                hidden = false
            };
            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            //You can add your own display rules here, where the first argument passed are the default display rules: the ones used when no specific display rules for a character are found.
            //For this example, we are omitting them, as they are quite a pain to set up.
            var displayRules = new ItemDisplayRuleDict(null);

            //Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            //But now we have defined an item, but it doesn't do anything yet. So we'll need to define that ourselves.
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;

            using (var bankStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExamplePlugin.bank.bnk"))
            {
                var bytes = new byte[bankStream.Length];
                bankStream.Read(bytes, 0, bytes.Length);
                SoundBanks.Add(bytes);
            }

            On.RoR2.GlobalEventManager.OnTeamLevelUp += (orig, self) =>
            {
                orig(self);

                // don't spoil the pod dropping in
                if (!FirstLevelInit)
                {
                    FirstLevelInit = true;
                }
                else
                {
                    // only play AFTER tier1
                    Chat.AddMessage("Moving on up HOOK! You moved up!");
                    Util.PlaySound("PlayUp", base.gameObject);
                }

            };


        }


        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            //If a character was killed by the world, we shouldn't do anything.
            if (!report.attacker || !report.attackerBody )
                return;
            
            CharacterBody attacker = report.attackerBody;
            //We need an inventory to do check for our item
            if (attacker.inventory) {
                //store the amount of our item we have
                int garbCount = attacker.inventory.GetItemCount(myItemDef.itemIndex);
                if (garbCount > 0 &&
                    //Roll for our 5% chance.
                    Util.CheckRoll(5, attacker.master))
                {
                    //Since we passed all checks, we now give our attacker the cloaked buff.
                    attacker.AddTimedBuff(BuffIndex.Cloak, 3 + (garbCount));
                }
            }
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private void AddTokens()
        {
            //The Name should be self explanatory
            R2API.LanguageAPI.Add("EXAMPLE_CLOAKONKILL_NAME", "Cuthroat's Garb");
            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            R2API.LanguageAPI.Add("EXAMPLE_CLOAKONKILL_PICKUP", "Chance to cloak on kill");
            //The Description is where you put the actual numbers and give an advanced description.
            R2API.LanguageAPI.Add("EXAMPLE_CLOAKONKILL_DESC", "Whenever you <style=cIsDamage>kill an enemy</style>, you have a <style=cIsUtility>5%</style> chance to cloak for <style=cIsUtility>4s</style> <style=cStack>(+1s per stack)</style.");
            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            R2API.LanguageAPI.Add("EXAMPLE_CLOAKONKILL_LORE", "Those who visit in the night are either praying for a favour, or preying on a neighbour.");
        }

        //The Update() method is run on every frame of the game.
        public void Update()
        {
            //This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                //Get the player body to use a position:	
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                //And then drop our defined item in front of the player.
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
                
            }
        }
    }
}