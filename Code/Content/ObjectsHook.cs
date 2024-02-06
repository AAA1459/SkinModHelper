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
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public static void Load() {
            On.Celeste.Lookout.Interact += on_Lookout_Interact;

            IL.Celeste.Booster.Added += Celeste_Booster_ILHook;
            On.Celeste.FlyFeather.Added += Celeste_flyFeather_Hook;
            On.Celeste.Cloud.Added += Celeste_Cloud_Hook;

            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool += Celeste_DreamBlock_ILHook;

            On.Celeste.Refill.Added += Celeste_Refill_Hook;

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
        }


        //-----------------------------Lookout-----------------------------
        public static void on_Lookout_Interact(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player) {
            orig(self, player);
            if (Player_Skinid_verify != 0) {
                DynData<Lookout> selfData = new DynData<Lookout>(self);
                if (selfData.Get<string>("animPrefix") == "badeline_" || selfData.Get<string>("animPrefix") == "nobackpack_") {
                    selfData["animPrefix"] = "";
                }
            }
            return;
        }

        //-----------------------------flyFeather-----------------------------
        public static void Celeste_flyFeather_Hook(On.Celeste.FlyFeather.orig_Added orig, FlyFeather self, Scene scene) {
            orig(self, scene);
            DynData<FlyFeather> selfData = new DynData<FlyFeather>(self);

            string SpritePath = getAnimationRootPath(selfData["sprite"] as Sprite) + "outline";
            if (GFX.Game.Has(SpritePath)) {
                Image outline = selfData["outline"] as Image;
                outline.Texture = GFX.Game[SpritePath];
            }
        }
        //-----------------------------Cloud-----------------------------
        public static void Celeste_Cloud_Hook(On.Celeste.Cloud.orig_Added orig, Cloud self, Scene scene) {
            orig(self, scene);
            Sprite sprite = new DynData<Cloud>(self)["sprite"] as Sprite;

            string SpritePath = getAnimationRootPath(sprite);

            if (GFX.Game.Has(SpritePath + "clouds")) {
                ParticleType particleType = new(new DynData<Cloud>(self)["particleType"] as ParticleType);

                particleType.Source = GFX.Game[SpritePath + "clouds"];
                particleType.Color = Color.White;
                new DynData<Cloud>(self)["particleType"] = particleType;
            }
        }
        //-----------------------------Booster-----------------------------
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
                        if (GFX.Game.Has(SpritePath + "outline")) { return SpritePath + "outline"; }
                    }
                    return orig;
                });
            }
        }
        //-----------------------------DreamBlock-----------------------------
        public static void Celeste_DreamBlock_ILHook(ILContext il) {
            ILCursor cursor = new(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/dreamblock/particles"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    string SpriteID = "dreamblock_particles";
                    if (OtherSkins_records.ContainsKey(SpriteID)) {
                        RefreshSkinValues_OtherExtra(SpriteID, null, true, false);
                        return getOtherSkin_ReskinPath(GFX.Game, "objects/dreamblock/particles", SpriteID);
                    }
                    return orig;
                });
            }
        }
        //-----------------------------Refill-----------------------------
        private static void Celeste_Refill_Hook(On.Celeste.Refill.orig_Added orig, Refill self, Scene scene) {
            orig(self, scene);
            DynData<Refill> selfData = new DynData<Refill>(self);

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

                GFX.SpriteBank.CreateOn(sprite, SpriteID);
                GFX.SpriteBank.CreateOn(flash, SpriteID);
                flash.Visible = false; // we need reset it's visible after CreateOn...

                PatchSprite(backup, sprite);

                if (selfData.Get<bool>("oneUse")) {
                    if (SpritePath == getAnimationRootPath(sprite) && SpriteExt_TryPlay(sprite, "idlenr", true)) {
                        // I guess we don't want to make the BetterRefillGems cannot work, but only for vanilla.
                    } else {
                        sprite.Play("oneuse_idle", true);
                    }
                } else {
                    sprite.Play("idle", true);
                }
            }

            SpritePath = getAnimationRootPath(sprite) + "outline";
            if (GFX.Game.Has(SpritePath)) {
                Image outline = selfData["outline"] as Image;
                outline.Texture = GFX.Game[SpritePath];
            }
        }
        //-----------------------------Seeker-----------------------------
        public static void Celeste_Seeker_ILHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchNewobj("Celeste.DeathEffect"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<DeathEffect, Seeker, DeathEffect>>((orig, self) => {

                    // 'Celeste.Seeker' Created new 'Monocle.Entity' to made an 'Celeste.DeathEffect'
                    // But that let we cannot get Seeker's data by {orig.Entity} in DeathEffect, so we self to add that data
                    new DynData<DeathEffect>(orig)["sprite"] = new DynData<Seeker>(self)["sprite"];
                    return orig;
                });
            }
        }
    }
}