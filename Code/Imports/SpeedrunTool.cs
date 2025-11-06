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
        }
        //public static Action<Func<object, object>> AddCustomDeepCloneProcessor;

        public static Func<Type, string[], object> RegisterStaticTypes;

        //public static Func<object, object> DeepClone;
    }
}