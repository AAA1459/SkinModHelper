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

            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool += Celeste_DreamBlock_ILHook;

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

            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool -= Celeste_DreamBlock_ILHook;

            On.Celeste.Refill.Added -= Celeste_Refill_Hook;
            On.Monocle.Entity.Added -= EntityAddedHook;
        }
        #endregion

        //-----------------------------Lookout-----------------------------
        #region
        public static void on_Lookout_Interact(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player) {
            orig(self, player);
            if (Player_Skinid_verify != 0) {
                DynamicData selfData = DynamicData.For(self);
                if (selfData.Get<string>("animPrefix") == "badeline_" || selfData.Get<string>("animPrefix") == "nobackpack_") {
                    selfData.Set("animPrefix", "");
                }
            }
            return;
        }
        #endregion

        //-----------------------------flyFeather-----------------------------
        #region
        public static void Celeste_flyFeather_Hook(On.Celeste.FlyFeather.orig_Added orig, FlyFeather self, Scene scene) {
            orig(self, scene);
            DynamicData selfData = DynamicData.For(self);

            string SpritePath = getAnimationRootPath(selfData.Get<Sprite>("sprite")) + "outline";
            if (GFX.Game.Has(SpritePath)) {
                Image outline = selfData.Get<Image>("outline");
                outline.Texture = GFX.Game[SpritePath];
            }
        }
        #endregion

        //-----------------------------Cloud-----------------------------
        #region
        public static void Celeste_Cloud_Hook(On.Celeste.Cloud.orig_Added orig, Cloud self, Scene scene) {
            orig(self, scene);
            DynamicData selfData = DynamicData.For(self);
            Sprite sprite = selfData.Get<Sprite>("sprite");

            string SpritePath = getAnimationRootPath(sprite);

            if (GFX.Game.Has(SpritePath + "clouds")) {
                ParticleType particleType = new(selfData.Get<ParticleType>("particleType"));

                particleType.Source = GFX.Game[SpritePath + "clouds"];
                particleType.Color = Color.White;
                selfData.Set("particleType", particleType);
            }
        }
        #endregion

        //-----------------------------Booster-----------------------------
        #region
        public static void Celeste_Booster_ILHook(ILContext il) {
            ILCursor cursor = new(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/booster/outline"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<string, Entity, string>>((orig, self) => {

                    // Maybe... I want this as a general ILhook to be directly applicable to other Helpers?

                    Sprite sprite = GetFieldPlus<Sprite>(self, "sprite");

                    if (sprite != null) {
                        string SpritePath = getAnimationRootPath(sprite);

                        // At the same time, reskin particles if its exist.
                        if (GFX.Game.Has(SpritePath + "blob")) {
                            var Field_particle = GetFieldPlus(self.GetType(), "particleType");

                            if (Field_particle != null && Field_particle.GetValue(self) is ParticleType particleType) {
                                // Clone object to prevent lost of vanilla
                                particleType = new(particleType);
                                Field_particle.SetValue(self, particleType);
                                //

                                particleType.Source = GFX.Game[SpritePath + "blob"];
                                particleType.Color = Color.White;
                            }
                        }
                        if (GFX.Game.Has(SpritePath + "outline")) return SpritePath + "outline";
                    }
                    return orig;
                });
            }
        }
        #endregion

        //-----------------------------DreamBlock-----------------------------
        #region
        public static void Celeste_DreamBlock_ILHook(ILContext il) {
            ILCursor cursor = new(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/dreamblock/particles"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    string SpriteID = "dreamblock_particles";
                    if (OtherSkins_records.ContainsKey(SpriteID)) {
                        return getOtherSkin_ReskinPath(GFX.Game, "objects/dreamblock/particles", SpriteID);
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
            DynamicData selfData = DynamicData.For(self);

            string SpriteID = null;

            Sprite sprite = selfData.Get<Sprite>("sprite");
            Sprite flash = selfData.Get<Sprite>("flash");
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
                if (selfData.Get<bool>("oneUse")) {
                    if (idlenr && SpriteExt_TryPlay(sprite, "idlenr") && SpritePath != getAnimationRootPath(sprite, "idlenr")) {

                    } else if (SpritePath != getAnimationRootPath(sprite, "oneuse_idle")) {
                        sprite.Play("oneuse_idle", true);
                    } else if (SpritePath != getAnimationRootPath(sprite, "idle")) {
                        sprite.Play("idle", true);
                    }
                }
            }

            SpritePath = getAnimationRootPath(sprite) + "outline";
            if (GFX.Game.Has(SpritePath) && selfData.TryGet("outline", out Image outline)) {
                outline.Texture = GFX.Game[SpritePath];
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
                    Sprite sprite = DynamicData.For(self).Get<Sprite>("sprite");
                    DynamicData.For(orig).Set("sprite", sprite);
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

            if (stretch != null) {
                Sprite sprite = GetFieldPlus<Sprite>(self, "sprite") ?? GetFieldPlus<Sprite>(self, "Sprite");
                string SpritePath = getAnimationRootPath(sprite);

                if (GFX.Game.Has(SpritePath + "stretch")) {
                    stretch.Texture = GFX.Game[SpritePath + "stretch"];
                }
            }
        }
        #endregion
    }
}