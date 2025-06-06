using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Celeste.Mod.SkinModHelper {
    public static class Extensions {

        /// <summary> (<see cref="Type"/>, <see cref="string"/>, <see cref="bool"/>) is (Class, FieldName, <see cref="FieldInfo.IsStatic"/>)</summary>
        private static Dictionary<(Type, string, bool), FieldInfo> fieldref_cache = new();
        public static FieldInfo GetField(Type type, string name) {
            if (fieldref_cache.TryGetValue((type, name, false), out FieldInfo field)) {
                return field;
            }
            Type type2 = type;
            while (field == null && type2 != null) {
                field = type2.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                // some mods entities works based on vanilla entities, but mods entity possible don't have theis own field.
                type2 = type2.BaseType;
            }
            fieldref_cache[(type, name, false)] = field;
            return field;
        }
        public static T GetField<T>(object obj, string name) {
            FieldInfo field = GetField(obj.GetType(), name);
            if (field != null && field.GetValue(obj) is T value) {
                return value;
            }
            return default;
        }
        public static FieldInfo GetStaticField(Type type, string name) {
            if (fieldref_cache.TryGetValue((type, name, true), out FieldInfo field)) {
                return field;
            }
            field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            fieldref_cache[(type, name, true)] = field;
            return field;
        }


        private static Dictionary<string, Type> typeref_cache = new();
        public static Type GetTypeFrom(string FullName) {
            if (typeref_cache.TryGetValue(FullName, out Type type)) {
                return type;
            }
            return typeref_cache[FullName] = FakeAssembly.GetFakeEntryAssembly().GetType(FullName);
        }
    }
}