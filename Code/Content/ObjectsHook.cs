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
            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool += Celeste_flyFeather_ILHook;
            On.Celeste.Cloud.Added += Celeste_Cloud_Hook;

            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool += Celeste_DreamBlock_ILHook;

            On.Celeste.Refill.Added += Celeste_Refill_Hook;
        }

        public static void Unload() {
            On.Celeste.Lookout.Interact -= on_Lookout_Interact;

            IL.Celeste.Booster.Added -= Celeste_Booster_ILHook;
            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool -= Celeste_flyFeather_ILHook;
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
        public static void Celeste_flyFeather_ILHook(ILContext il) {
            ILCursor cursor = new(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/flyFeather/outline"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    string SpritePath = getAnimationRootPath("flyFeather") + "outline";
                    return !GFX.Game.Has(SpritePath) ? orig : SpritePath;
                });
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
                    var Field_sprite = self.GetType().GetField("sprite", BindingFlags.NonPublic | BindingFlags.Instance);
                    Field_sprite = Field_sprite ?? self.GetType().GetField("sprite", BindingFlags.Public | BindingFlags.Instance);
                    if (Field_sprite != null) {

                        Sprite sprite = Field_sprite.GetValue(self) as Sprite;
                        string SpritePath = getAnimationRootPath(sprite);

                        // At the same time, reskin particles if its exist.
                        if (GFX.Game.Has(SpritePath + "blob")) {
                            var Field_particle = self.GetType().GetField("particleType", BindingFlags.NonPublic | BindingFlags.Instance);

                            if (Field_particle != null) {
                                // Clone object to prevent lost of vanilla
                                ParticleType particleType = new(Field_particle.GetValue(self) as ParticleType);
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
                        Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                        return getOtherSkin_ReskinPath(GFX.Game, "objects/dreamblock/particles", SpriteID, OtherSkin_record[SpriteID]);
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

            // Filter out the refill of helpers
            if (self.GetType().FullName == "Celeste.Refill") {
                SpriteID = (bool)selfData["twoDashes"] ? "refillTwo" : "refill";
            }

            if (SpriteID != null) {
                Sprite sprite = selfData["sprite"] as Sprite;
                sprite = GFX.SpriteBank.CreateOn(sprite, SpriteID);

                Sprite flash = selfData["flash"] as Sprite;
                flash = GFX.SpriteBank.CreateOn(flash, SpriteID);

                string SpritePath = getAnimationRootPath(sprite) + "outline";
                if (GFX.Game.Has(SpritePath)) {
                    Image outline = selfData["outline"] as Image;
                    outline.Texture = GFX.Game[SpritePath];
                }
            }
        }

    }
}