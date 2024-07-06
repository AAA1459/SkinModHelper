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
    public class ObjectsHook {
        #region
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public static void Load() {
            On.Celeste.Lookout.Interact += on_Lookout_Interact;

            IL.Celeste.Booster.Added += Celeste_Booster_ILHook;
            On.Celeste.FlyFeather.Added += Celeste_flyFeather_Hook;
            On.Celeste.Cloud.Added += Celeste_Cloud_Hook;

            On.Celeste.Refill.Added += Celeste_Refill_Hook;
            On.Monocle.Entity.Added += EntityAddedHook;

            // Hooking an anonymous delegate of Seeker
            doneILHooks.Add(new ILHook(typeof(Seeker).GetMethod("<.ctor>b__58_2", BindingFlags.NonPublic | BindingFlags.Instance), Celeste_Seeker_ILHook));
        }

        public static void Unload() {
            On.Celeste.Lookout.Interact -= on_Lookout_Interact;

            IL.Celeste.Booster.Added -= Celeste_Booster_ILHook;
            On.Celeste.FlyFeather.Added -= Celeste_flyFeather_Hook;
            On.Celeste.Cloud.Added -= Celeste_Cloud_Hook;

            On.Celeste.Refill.Added -= Celeste_Refill_Hook;
            On.Monocle.Entity.Added -= EntityAddedHook;
        }
        #endregion

        //-----------------------------Lookout-----------------------------
        #region
        public static void on_Lookout_Interact(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player) {
            orig(self, player);
            if (Player_Skinid_verify != 0) {
                self.animPrefix = "";
            }
        }
        #endregion

        //-----------------------------flyFeather-----------------------------
        #region
        public static void Celeste_flyFeather_Hook(On.Celeste.FlyFeather.orig_Added orig, FlyFeather self, Scene scene) {
            orig(self, scene);
            if (GetTextureOnSprite(self.sprite, "outline", out var outline)) {
                self.outline.Texture = outline;
            }
        }
        #endregion

        //-----------------------------Cloud-----------------------------
        #region
        public static void Celeste_Cloud_Hook(On.Celeste.Cloud.orig_Added orig, Cloud self, Scene scene) {
            orig(self, scene);
            if (GetTextureOnSprite(self.sprite, "clouds", out var clouds)) {
                self.particleType = new(self.particleType) {
                    Source = clouds,
                    Color = Color.White
                };
            }
        }
        #endregion

        //-----------------------------Booster-----------------------------
        #region
        public static void Celeste_Booster_ILHook(ILContext il) {
            ILCursor cursor = new(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/booster/outline"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<string, Booster, string>>((orig, self) => {

                    if (GetTextureOnSprite(self.sprite, "blob", out var blob)) {
                        // Clone object to prevent lost of vanilla
                        self.particleType = new(self.particleType) {
                            Source = blob,
                            Color = Color.White
                        };
                    }
                    if (GetTextureOnSprite(self.sprite, "outline", out var outline)) {
                        orig = outline.ToString();
                    }
                    return orig;
                });
            }
        }
        #endregion

        //-----------------------------Refill-----------------------------
        #region
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

        //-----------------------------Seeker-----------------------------
        #region
        public static void Celeste_Seeker_ILHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchNewobj("Celeste.DeathEffect"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<DeathEffect, Seeker, DeathEffect>>((orig, self) => {

                    // 'Celeste.Seeker' Created new 'Monocle.Entity' to made an 'Celeste.DeathEffect'
                    // But that let we cannot get Seeker's data by {orig.Entity} in DeathEffect, so we self to add that data
                    DynamicData.For(orig).Set("sprite", self.sprite);
                    return orig;
                });
            }
        }
        #endregion

        //-----------------------------BadelineBoost-----------------------------
        #region
        private static void EntityAddedHook(On.Monocle.Entity.orig_Added orig, Entity self, Scene scene) {

            // You can see... DJMapHelper and StrawberryJam's BadelineBoost works not as an BadelineBoost type... so let hooking here
            if (self.GetType().Name.Contains("BadelineBoost")) {
                BadelineBoost_stretchReskin(self);
            }

            orig(self, scene);
        }
        private static void BadelineBoost_stretchReskin(Entity self) {
            Image stretch = GetFieldPlus<Image>(self, "stretch");
            Sprite sprite = self.Get<Sprite>();

            if (stretch != null && sprite != null) {
                if (GetTextureOnSprite(sprite, "stretch", out var stretch2)) {
                    stretch.Texture = stretch2;
                }
            }
        }
        #endregion
    }
}