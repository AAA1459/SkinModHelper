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

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    [CustomEntity("SkinModHelper/EntityReskinTrigger")]
    public class EntityReskinTrigger : Trigger {

        private readonly int entityIndex;
        private readonly string entityFullName;

        private readonly bool oneUse;
        private readonly string newSpriteID;


        public EntityReskinTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {

            entityIndex = data.Int("entityIndex", -1);
            entityFullName = data.Attr("entityFullName", "Celeste.Strawberry");

            oneUse = data.Bool("oneUse", true);
            newSpriteID = data.Attr("newSpriteID", "");
        }




        public override void OnEnter(Player player) {
            base.OnEnter(player);

            int Index = entityIndex;
            int search = -1;

            EntityList entities = Scene.Entities;
            foreach (Entity entity in entities) {
                if (entity is Actor || entity is Trigger) {
                    if (entity.GetType().FullName == entityFullName) {
                        Logger.Log(LogLevel.Warn, "SkinModHelper/EntityReskinTrigger", $"Entity '{entity.GetType().FullName}' shouldn't be reskin.");
                    }
                    continue;
                } else if (entity.GetType().FullName == entityFullName) {
                    search++;
                    if (Index < 0) {
                        EntityReskin(entity, newSpriteID);
                    } else if (search == Index) {
                        EntityReskin(entity, newSpriteID);
                        break;
                    }
                } /*else if (search < 0 && newSpriteID == "" && entity.GetType().FullName.IndexOf(entityFullName) > 0) {
                    Logger.Log(LogLevel.Info, "SkinModHelper/EntityReskinTrigger", $"search-out Entity '{entity.GetType().FullName}', do you want to reskin this?");
                }*/
            }
            if (oneUse && ((Index >= 0 && search == Index) || search >= 0)) {
                Collidable = false;
            }
        }
        public static void EntityReskin(Entity entity, string SpriteID) {
            Logger.Log(LogLevel.Info, "SkinModHelper/EntityReskinTrigger", $"Entity '{entity.GetType().FullName}' trying reskin");

            string search = SpriteID;
            if (search.EndsWith("_")) { search = search.Remove(search.LastIndexOf("_")); }

            if (SpriteID != null && GFX.SpriteBank.SpriteData.ContainsKey(search)) {

                var Field_sprite = entity.GetType().GetField("sprite", BindingFlags.NonPublic | BindingFlags.Instance);
                Field_sprite = Field_sprite ?? entity.GetType().GetField("sprite", BindingFlags.Public | BindingFlags.Instance);

                if (Field_sprite != null && Field_sprite.GetValue(entity) is Sprite sprite) {

                    // --------sprite--------
                    sprite = GFX.SpriteBank.CreateOn(sprite, SpriteID);
                    string SpritePath = getAnimationRootPath(sprite);
                    // ----------------
                    // --------outline--------
                    var Field_outline = entity.GetType().GetField("outline", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (Field_outline != null && Field_outline.GetValue(entity) is Entity outline) {
                        foreach (Component component in outline) {
                            if (component is Image image && GFX.Game.Has($"{SpritePath}outline")) {
                                image.Texture = GFX.Game[$"{SpritePath}outline"];
                            }
                        }
                    } else if (Field_outline != null && Field_outline.GetValue(entity) is Image image) {
                        if (GFX.Game.Has($"{SpritePath}outline")) {
                            image.Texture = GFX.Game[$"{SpritePath}outline"];
                        }
                    }
                    // ----------------
                    // --------particle--------
                    if (entity is Booster || entity is Cloud) {
                        var Field_particle = entity.GetType().GetField("particleType", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (Field_particle != null) {

                            string particle = "blob";
                            if (entity is Cloud) {
                                particle = "clouds";
                            }

                            if (GFX.Game.Has(SpritePath + particle)) {
                                // Clone object to prevent lost of vanilla
                                ParticleType particleType = new(Field_particle.GetValue(entity) as ParticleType);
                                Field_particle.SetValue(entity, particleType);
                                // Although... them cannot get the vanilla value when back vanilla, because that too cumbersome

                                particleType.Source = GFX.Game[SpritePath + particle];
                                particleType.Color = Color.White;
                            }
                        }
                    }
                    // ----------------
                } else {
                    Logger.Log(LogLevel.Warn, "SkinModHelper/EntityReskinTrigger", $"Entity '{entity.GetType().FullName}' Not compatible with this trigger");
                }
            } else {
                Logger.Log(LogLevel.Warn, "SkinModHelper/EntityReskinTrigger", $"'{search}' is not defined in Sprites.xml!");
            }
        }

    }
}