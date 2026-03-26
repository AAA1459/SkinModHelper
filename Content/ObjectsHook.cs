using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Reflection;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    public static class ObjectsHook {
        #region Hooks


        public static void Load() {
            On.Celeste.Lookout.Interact += on_Lookout_Interact;

            IL.Celeste.Booster.Added += Celeste_Booster_ILHook;
            On.Celeste.FlyFeather.Added += Celeste_flyFeather_Hook;

            On.Celeste.Refill.Added += Celeste_Refill_Hook;

            IL.Monocle.EntityList.UpdateLists += ilEntityList_UpdateList;
            // Hooking an anonymous delegate of Seeker
            doneILHooks.Add(new ILHook(typeof(Seeker).GetMethod("<.ctor>b__58_2", BindingFlags.NonPublic | BindingFlags.Instance), Celeste_Seeker_ILHook));
        }

        public static void Unload() {
            On.Celeste.Lookout.Interact -= on_Lookout_Interact;

            IL.Celeste.Booster.Added -= Celeste_Booster_ILHook;
            On.Celeste.FlyFeather.Added -= Celeste_flyFeather_Hook;

            On.Celeste.Refill.Added -= Celeste_Refill_Hook;
            IL.Monocle.EntityList.UpdateLists -= ilEntityList_UpdateList;
        }
        #endregion

        #region Lookout
        public static void on_Lookout_Interact(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player) {
            orig(self, player);
            if (Player_Skinid_verify != 0) {
                self.animPrefix = "";
            }
        }
        #endregion

        #region flyFeather
        public static void Celeste_flyFeather_Hook(On.Celeste.FlyFeather.orig_Added orig, FlyFeather self, Scene scene) {
            orig(self, scene);
            if (GetTextureOnSprite(self.sprite, "outline", out var outline)) {
                self.outline.Texture = outline;
            }
        }
        #endregion

        #region Booster
        public static void Celeste_Booster_ILHook(ILContext il) {
            ILCursor cursor = new(il);
            string _(string orig, Booster self) {
                if (GetTextureOnSprite(self.sprite, "outline", out var outline)) {
                    return outline.ToString();
                }
                return orig;
            }

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/booster/outline"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(_);
            }
        }
        #endregion

        #region Refill
        private static void Celeste_Refill_Hook(On.Celeste.Refill.orig_Added orig, Refill self, Scene scene) {
            orig(self, scene);
            string SpriteID = null;

            Sprite sprite = self.sprite;
            Sprite flash = self.flash;
            string SpritePath = getAnimationRootPath(sprite);
            
            // Filter the refills that using texture different than vanilla.
            if (SpritePath == "objects/refill/") { SpriteID = "refill"; } else
                if (SpritePath == "objects/refillTwo/") { SpriteID = "refillTwo"; }

            if (SpriteID != null) {
                Sprite backup = new Sprite(null, null);
                PatchSprite(sprite, backup);
                backup.Y = sprite.Y;

                // let we know that BetterRefillGems working...
                bool idlenr = sprite.CurrentAnimationID == "idlenr";
                GFX.SpriteBank.CreateOn(sprite, SpriteID);
                GFX.SpriteBank.CreateOn(flash, SpriteID);

                // we need recover somethings after CreateOn...
                flash.Visible = false;
                flash.Y = sprite.Y = backup.Y;
                PatchSprite(backup, sprite);

                sprite.Play("idle", true);
                if (self.oneUse) {
                    if (idlenr && SpriteExt_TryPlay(sprite, "idlenr") && SpritePath != getAnimationRootPath(sprite, "idlenr")) {

                    } else if (SpritePath != getAnimationRootPath(sprite, "oneuse_idle")) {
                        sprite.Play("oneuse_idle", true);
                    } else if (SpritePath != getAnimationRootPath(sprite, "idle")) {
                        sprite.Play("idle", true);
                    }
                }
            }

            if (GetTextureOnSprite(sprite, "outline", out var outline2)) {
                self.outline.Texture = outline2;
            }
        }
        #endregion

        #region Seeker
        public static void Celeste_Seeker_ILHook(ILContext il) {
            ILCursor cursor = new(il);
            DeathEffect _(DeathEffect orig, Seeker self) {
                DynamicData.For(orig).Set("sprite", self.sprite);
                return orig;
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchNewobj("Celeste.DeathEffect"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(_);
            }
        }
        #endregion

        #region BadelineBoost
        public static void ilEntityList_UpdateList(ILContext il) {
            ILCursor cursor = new(il);

            var addedEntityLoc = -1;
            cursor.GotoNext(MoveType.Before, instr => instr.MatchCallvirt<Entity>("Added"));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdloc(out addedEntityLoc));
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Entity>("Added"));

            cursor.Emit(OpCodes.Ldloc, addedEntityLoc);
            cursor.EmitDelegate(BadelineBoost_stretchReskin);
        }
        private static void BadelineBoost_stretchReskin(Entity entity) {
            if (!entity.GetType().Name.Contains("BadelineBoost")) {
                return;
            }
            Image stretch = Extensions.GetField<Image>(entity, "stretch");
            Sprite sprite = entity.Get<Sprite>();

            if (stretch != null && sprite != null) {
                if (GetTextureOnSprite(sprite, "stretch", out var stretch2)) {
                    stretch.Texture = stretch2;
                }
            }
        }
        #endregion
    }
}