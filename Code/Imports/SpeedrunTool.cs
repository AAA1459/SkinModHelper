using System;
using System.Runtime.CompilerServices;
using AsmResolver.PE.File;
using MonoMod.ModInterop;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.SkinModHelper.Imports {

    [ModImportName("SpeedrunTool.SaveLoad")]
    public static class SaveLoadExports {

        public static void LoadContent(bool firstLoad) {
            if (!firstLoad) {
                return;
            }
                RegisterStaticTypes?.Invoke(typeof(SkinsSystem), new string[] { nameof(SkinsSystem.SpriteDataCache) });

            if (AddCustomDeepCloneProcessor != null && DeepClone != null) {
                Logger.Info("SkinModHelper", "AddCustomDeepCloneProcessor & DeepClone");

                AddCustomDeepCloneProcessor.Invoke(sourceObj => {
                    if (sourceObj == SkinsSystem.SpriteDataCache && sourceObj is ConditionalWeakTable<Sprite, SpriteData> cache) {
                        Logger.Info("SkinModHelper", "cloned spriteData");

                        Dictionary<Sprite, SpriteData> clone = new();
                        foreach (var set in cache) {
                            clone[(Sprite)DeepClone.Invoke(set.Key)] = (SpriteData)DeepClone.Invoke(set.Value);
                        }
                        foreach (var set in clone) {
                            cache.AddOrUpdate(set.Key, set.Value);
                        }
                        return cache;
                    }
                    return null;
                }
                );
            }
        }
        public static Action<Func<object, object>> AddCustomDeepCloneProcessor;

        public static Func<Type, string[], object> RegisterStaticTypes;

        public static Func<object, object> DeepClone;
    }
}