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


        private List<Entity> entities = new();
        public override void Added(Scene scene) {
            base.Added(scene);

            int Index = entityIndex;
            int search = -1;
            foreach (Entity entity in Scene.Entities) {
                if (entity.GetType().FullName == entityFullName) {
                    search++;
                    if (Index < 0) {
                        entities.Add(entity);
                    } else if (search == Index) {
                        entities.Add(entity);
                        break;
                    }
                }
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            int Index = entityIndex;
            int search = -1;
            foreach (Entity entity in entities) {
                search++;
                if (Scene.Entities.Contains(entity)) {
                    Logger.Log(LogLevel.Debug, "SkinModHelper/EntityReskinTrigger", $"trying reskin Entity '{entity.GetType().FullName}' No.{(Index < 0 ? search : Index)}");
                    EntityReskin(entity, newSpriteID);
                }
            }
            if (oneUse) {
                Collidable = false;
            }
        }
        public static void EntityReskin(Entity entity, string SpriteID) {

            string search = SpriteID;
            if (search.EndsWith("_")) { search = search.Remove(search.LastIndexOf("_")); }

            if (SpriteID != null && GFX.SpriteBank.SpriteData.ContainsKey(search)) {
                Type entityType = entity.GetType();
                var Field_sprite = GetFieldPlus(entityType, "sprite");

                if (Field_sprite != null && Field_sprite.GetValue(entity) is Sprite sprite) {

                    // --------sprite--------
                    sprite = GFX.SpriteBank.CreateOn(sprite, SpriteID);
                    string SpritePath = getAnimationRootPath(sprite);
                    // ----------------
                    // --------flash--------
                    var Field_flash = GetFieldPlus(entityType, "flash");
                    if (Field_flash != null && Field_flash.GetValue(entity) is Sprite flash) {
                        flash = GFX.SpriteBank.CreateOn(flash, SpriteID);
                    }
                    // ----------------
                    // --------outline--------
                    var Field_outline = GetFieldPlus(entityType, "outline");
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
                        var Field_particle = GetFieldPlus(entityType, "particleType");
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