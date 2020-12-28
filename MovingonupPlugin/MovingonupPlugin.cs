using BepInEx;
using RoR2;
using System.Reflection;
using static R2API.SoundAPI;

namespace MovingonupPlugin
{
     [BepInDependency("com.bepis.r2api")]

    [BepInPlugin(
        "com.TakesTheBiscuit.MovingOnUP",
        "MovingOnUP",
        "0.3.0")]
        
   public class MovingonupPlugin : BaseUnityPlugin
    {
        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            using (var bankStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MovingonupPlugin.bank.bnk"))
            {
                var bytes = new byte[bankStream.Length];
                bankStream.Read(bytes, 0, bytes.Length);
                SoundBanks.Add(bytes);
            }

            using (var bankStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("MovingonupPlugin.bankTeleport.bnk"))
            {
                var bytes2 = new byte[bankStream2.Length];
                bankStream2.Read(bytes2, 0, bytes2.Length);
                SoundBanks.Add(bytes2);
            }

            On.RoR2.GlobalEventManager.OnTeamLevelUp += (orig, self) =>
            {
                orig(self);

                // don't spoil the pod dropping in or enemy xp upgrades
                if (self  == TeamIndex.Player)
                {
                    ulong currentExp = TeamManager.instance.GetTeamExperience(self);
                    if (currentExp < 1)
                    {
                        // not this time
                    }
                    else
                    {
                        // only play AFTER tier1
                        Chat.AddMessage("Moving on up!");
                        Util.PlaySound("PlayUp", base.gameObject);
                    }
                }
            };
            
            On.RoR2.TeleporterInteraction.OnInteractionBegin += (orig, self, interactor) => {
                orig(self, interactor);
                if (TeleporterInteraction.instance.isCharged)
                {
                    Chat.AddMessage("Time to break free, nothin can stop me");
                    Util.PlaySound("PlayUpTeleport", base.gameObject);
                }
            };
        }


        public void Update()
        {
        }
    }
}