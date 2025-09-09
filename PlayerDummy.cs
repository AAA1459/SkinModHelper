using Celeste.Mod.Entities;
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
using Microsoft.Build.Framework;

namespace Celeste.Mod.SkinModHelper {
    [Tracked(false)]
    public class PlayerDummy : BadelineDummy {

        public int Dashes = 1;
        private MirrorReflection reflection;
        public PlayerDummy(Player player, string spriteName, string id, string frame)
            : base(player.Position) {

            Tag = Tags.Persistent;

            Wave.Active = AutoAnimator.Enabled = false;
            Hair.Color = player.Hair.Color;

            Depth = player.Depth - 1;
            Dashes = player.Dashes;

            string _spriteName = spriteName ?? player.Sprite.spriteName ?? "player";
            GFX.SpriteBank.CreateOn(Sprite, _spriteName);
            if (_spriteName == "player_playback") {
                Sprite.Mode = PlayerSpriteMode.Playback;
            }
            Sprite.Scale = player.Sprite.Scale;
            Sprite.Scale.X *= (float)player.Facing;

            Sprite.Play(id ?? player.Sprite.LastAnimationID ?? "idle");
            Sprite.SetAnimationFrame(int.TryParse(frame, out int f) ? f : player.Sprite.CurrentAnimationFrame);

            Sprite.Animating = false;

            Light.StartRadius = player.Light.StartRadius;
            Light.EndRadius = player.Light.EndRadius;
            Light.Color = Color.White;
            Add(reflection = new MirrorReflection());
        }


        public static void RemoveAllDummy() {
            foreach (Entity e in Engine.Scene.Tracker.GetEntities<PlayerDummy>()) {
                e.RemoveSelf();
            }
        }
    }
}