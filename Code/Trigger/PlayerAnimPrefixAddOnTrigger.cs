using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using FMOD.Studio;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    [CustomEntity("SkinModHelper/PlayerAnimPrefixAddOnTrigger")]
    public class PlayerAnimPrefixAddOnTrigger : Trigger {

        private string lastAnimPrefixAddOn;
        private readonly string animPrefixAddOn;
        private readonly bool revertOnLeave;

        public PlayerAnimPrefixAddOnTrigger(EntityData data, Vector2 offset) 
            : base(data, offset) {
            revertOnLeave = data.Bool("revertOnLeave", false);
            animPrefixAddOn = data.Attr("animPrefixAddOn", null);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if (player?.Sprite == null) {
                return;
            }
            if (revertOnLeave)
                lastAnimPrefixAddOn = smh_Session.Player_animPrefixAddOn;

            smh_Session.Player_animPrefixAddOn = animPrefixAddOn;
            DynamicData.For(player.Sprite).Set("smh_AnimPrefix", animPrefixAddOn);
            player.Sprite.Play("idle");
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (player?.Sprite == null) {
                return;
            }
            if (revertOnLeave) {
                smh_Session.Player_animPrefixAddOn = lastAnimPrefixAddOn;

                // Make sure the player is leave, I mean... not is "leave"
                if (SceneAs<Level>()?.Tracker?.GetEntity<PlayerDeadBody>() == null) {
                    DynamicData.For(player.Sprite).Set("smh_AnimPrefix", lastAnimPrefixAddOn);
                    player.Sprite.Play("idle");
                }
                lastAnimPrefixAddOn = null;
            }
        }
        public override void SceneEnd(Scene scene) {
            if (revertOnLeave && CollideCheck<Player>()) {
                smh_Session.Player_animPrefixAddOn = lastAnimPrefixAddOn;
            }
            base.SceneEnd(scene);
        }
    }
}