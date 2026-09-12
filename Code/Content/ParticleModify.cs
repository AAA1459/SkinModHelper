using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil.Cil;
using Celeste.Mod.UI;
using System.Xml;
using System.Linq;
using Celeste.Mod.Helpers;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    public static class ParticleModify {
        #region Hooks
        public static void Load() {
            IL.Monocle.EntityList.Update += ilEntityList_Update;
            On.Celeste.PlayerCollider.Check += PlayerCollider_Check; // For Key.P_Collect.

            doneILHooks.Add(new ILHook(typeof(Key).GetMethod("UseRoutine", BindingFlags.Public | BindingFlags.Instance).GetStateMachineTarget(), il_OverrideTracked));
            doneILHooks.Add(new ILHook(typeof(NPC01_Theo).GetMethod("Yolo", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), il_OverrideTracked));
            On.Celeste.Holdable.Pickup += onHoldable_Pickup; // For Glider.P_Platform

            // Various cutscenes
            On.Celeste.BadelineDummy.Appear += onBadelineDummy_Appear;
            On.Celeste.BadelineDummy.Vanish += onBadelineDummy_Vanish;
            On.Celeste.Player.CreateSplitParticles += onPlayer_CreateSplitParticles;
            
            IL.Monocle.ParticleEmitter.Simulate += ilParticleEmitter_Simulate;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2 += onParticleSystem_Emit_PtclV2;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_float += onParticleSystem_Emit_PtclV2F;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_Color += onParticleSystem_Emit_PtclV2C;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_Color_float += onParticleSystem_Emit_PtclV2FC;
        }

        public static void Unload() {
            IL.Monocle.EntityList.Update -= ilEntityList_Update;
            On.Celeste.PlayerCollider.Check -= PlayerCollider_Check;
            On.Celeste.Holdable.Pickup -= onHoldable_Pickup;
            On.Celeste.BadelineDummy.Appear -= onBadelineDummy_Appear;
            On.Celeste.BadelineDummy.Vanish -= onBadelineDummy_Vanish;
            On.Celeste.Player.CreateSplitParticles -= onPlayer_CreateSplitParticles;

            IL.Monocle.ParticleEmitter.Simulate -= ilParticleEmitter_Simulate;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2 -= onParticleSystem_Emit_PtclV2;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_float -= onParticleSystem_Emit_PtclV2F;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_Color -= onParticleSystem_Emit_PtclV2C;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_Color_float -= onParticleSystem_Emit_PtclV2FC;
        }
        #endregion 

        private static void ilEntityList_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(1))) {
                Log(LogLevel.Verbose, $"EntityList_Update IL at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.Emit(OpCodes.Ldnull);
                cursor.Emit(OpCodes.Stsfld, typeof(ParticleModify).GetField("OverrideTracked", BindingFlags.NonPublic | BindingFlags.Static));
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate(Redirect);
            }
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLeaveS(out _))) {
                cursor.Emit(OpCodes.Ldnull);
                cursor.Emit(OpCodes.Stsfld, typeof(ParticleModify).GetField("Tracked", BindingFlags.NonPublic | BindingFlags.Static));
            }
            void Redirect(Entity entity) {
                Tracked = entity;
            };
        }
        private static bool PlayerCollider_Check(On.Celeste.PlayerCollider.orig_Check orig, PlayerCollider self, Player player) {
            OverrideTracked = self.Entity;
            bool result = orig(self, player);
            OverrideTracked = null;
            return result;
        }
        private static bool onHoldable_Pickup(On.Celeste.Holdable.orig_Pickup orig, Holdable self, Player player) {
            OverrideTracked = self.Entity;
            bool result = orig(self, player);
            OverrideTracked = null;
            return result;
        }
        private static void il_OverrideTracked(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<ParticleSystem>("Emit"))) {
                Log(LogLevel.Verbose, $"ParticleModifyIL at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.Emit(OpCodes.Ldloc_1); // NPC01_Theo or Key...
                cursor.Emit(OpCodes.Stsfld, typeof(ParticleModify).GetField("OverrideTracked", BindingFlags.NonPublic | BindingFlags.Static));
                cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<ParticleSystem>("Emit"));
                cursor.Emit(OpCodes.Ldnull);
                cursor.Emit(OpCodes.Stsfld, typeof(ParticleModify).GetField("OverrideTracked", BindingFlags.NonPublic | BindingFlags.Static));
            }
        }
        private static void onBadelineDummy_Appear(On.Celeste.BadelineDummy.orig_Appear orig, BadelineDummy self, Level level, bool silent) {
            OverrideTracked = self;
            orig(self, level, silent);
            OverrideTracked = null;
        }
        private static void onBadelineDummy_Vanish(On.Celeste.BadelineDummy.orig_Vanish orig, BadelineDummy self) {
            OverrideTracked = self;
            orig(self);
            OverrideTracked = null;
        }
        private static void onPlayer_CreateSplitParticles(On.Celeste.Player.orig_CreateSplitParticles orig, Player self) {
            OverrideTracked = self;
            orig(self);
            OverrideTracked = null;
        }


        private static Entity Tracked;

        private static Entity OverrideTracked;

        private static void ilParticleEmitter_Simulate(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            ParticleType _(ParticleType orig) => ParticleReplace(orig, out ParticleType ptcl) ? ptcl : orig;
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<ParticleEmitter>("Type"))) {
                cursor.EmitDelegate(_);
            }
        }

        private static void onParticleSystem_Emit_PtclV2(On.Monocle.ParticleSystem.orig_Emit_ParticleType_Vector2 orig, ParticleSystem self, ParticleType ptcl, Vector2 v2) {
            if (ParticleReplace(ptcl, out ParticleType ptcl2)) {
                ptcl = ptcl2;
            }
            orig(self, ptcl, v2);
        }
        private static void onParticleSystem_Emit_PtclV2F(On.Monocle.ParticleSystem.orig_Emit_ParticleType_Vector2_float orig, ParticleSystem self, ParticleType ptcl, Vector2 v2, float f) {
            if (ParticleReplace(ptcl, out ParticleType ptcl2)) {
                ptcl = ptcl2;
            }
            orig(self, ptcl, v2, f);
        }
        private static void onParticleSystem_Emit_PtclV2C(On.Monocle.ParticleSystem.orig_Emit_ParticleType_Vector2_Color orig, ParticleSystem self, ParticleType ptcl, Vector2 v2, Color c) {
            if (ParticleReplace(ptcl, out ParticleType ptcl2)) {
                ptcl = ptcl2;
            }
            orig(self, ptcl, v2, c);
        }
        private static void onParticleSystem_Emit_PtclV2FC(On.Monocle.ParticleSystem.orig_Emit_ParticleType_Vector2_Color_float orig, ParticleSystem self, ParticleType ptcl, Vector2 v2, Color c, float f) {
            if (ParticleReplace(ptcl, out ParticleType ptcl2)) {
                ptcl = ptcl2;
            }
            orig(self, ptcl, v2, c, f);
        }



        private static bool ParticleReplace(ParticleType ptcl, out ParticleType ptcl2) {
            // if ((OverrideTracked ?? Tracked) != null &&
            //     (ptcl == NPC01_Theo.P_YOLO)) {
            //     Log($"{Tracked} : {OverrideTracked} : {getAnimationRootPath((OverrideTracked ?? Tracked).Get<Sprite>())}");
            // }
            Entity tracked = Tracked;
            if (OverrideTracked is null) {
                // These exceptions only affect particle replacement. Resolve them here instead of
                // doing two type checks for every entity in every EntityList.Update.
                tracked = tracked switch {
                    NPC05_Badeline npc05 => npc05.shadow, // BadelineOldsite.P_Vanish
                    CS10_HubIntro cs10 => cs10.booster, // Booster.Appear
                    _ => tracked
                };
            }
            return ParticleReplace(ptcl, OverrideTracked ?? tracked, out ptcl2);
        }
        
        public static bool ParticleReplace(ParticleType ptcl, Entity entity, out ParticleType ptcl2) {
            if (entity?.Get<Sprite>() is Sprite sprite) {
                foreach (var ptclModifier in CharacterConfig.For(sprite).ParticleModify ?? new()) {
                    if (ptclModifier.IsStatic && ptcl == ptclModifier.BaseParticle) {
                        ptcl2 = ptclModifier.NewParticle;
                        return true;
                    }
                }
            }
            ptcl2 = null;
            return false;
        }
    }
}
