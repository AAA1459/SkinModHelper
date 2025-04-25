using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CelesteNet.Client;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;
using Celeste.Mod.CelesteNet.Client.Entities;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;
using System;

namespace Celeste.Mod.SkinModHelper.CelesteNet {

    public class SkinModHelperData : DataType<SkinModHelperData> {
        static SkinModHelperData() {
            DataID = "SkinModHelperPlus/Data";
        }

        public DataPlayerInfo Player;
        public string Info;
        public int Dashes;

        public SkinModHelperData() { }

        public override bool FilterHandle(DataContext ctx)
            => Player != null;

        public override MetaType[] GenerateMeta(DataContext ctx) => new MetaType[] {
                new MetaPlayerUpdate(Player),
                new MetaOrderedUpdate(Player?.ID ?? uint.MaxValue)
        };
        public override void FixupMeta(DataContext ctx) {
            MetaPlayerUpdate playerUpd = Get<MetaPlayerUpdate>(ctx);
            MetaOrderedUpdate order = Get<MetaOrderedUpdate>(ctx);

            order.ID = playerUpd;
            Player = playerUpd;
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            Info = reader.ReadNetString();

            string[] array = Info.Split(',', StringSplitOptions.TrimEntries);
            if (array.Length > 0)
                int.TryParse(array[0], out Dashes);
        }
        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.WriteNetString(Info);
        }
    }

    public class InformationComponent : CelesteNetGameComponent {
        private static CelesteNetClientContext ctx;
        public static Dictionary<uint, int> Information = new();

        public InformationComponent(CelesteNetClientContext context, Game game) : base(context, game) {
            ctx = context;
        }
        public override void Initialize() {
            base.Initialize();
            SMH_NetHelper.Connected = true;
        }
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            SMH_NetHelper.Connected = false;
        }

        private int Dashes {
            get {
                if (_Player is Player player) {
                    if (player.OverrideHairColor == Player.UsedHairColor)
                        return 0;
                    if (player.MaxDashes <= 0 && player.lastDashes < 2)
                        return 1;
                    return Math.Max(player.lastDashes, 0);
                }
                return -1;
            }
        }
        public void Send(string info) {
            Client?.SendAndHandle(new SkinModHelperData {
                Player = Client.PlayerInfo,
                Info = info
            });
            ;
        }
        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (Client == null || !Client.IsReady || Engine.Scene is not Level)
                return;
            int dashes = Dashes;
            if (dashes < 0)
                return;
            Send($"{Dashes}");
        }
        public void Handle(CelesteNetConnection con, SkinModHelperData data) {
            if (data.Player != null)
                Information[data.Player.ID] = data.Dashes;
        }
    }
    public static class SMH_NetHelper {
        public static bool Connected;
        public static bool TryGetDashes(Entity e, out int dashes) {
            if (Connected) {
                return _TryGetDashes(e, out dashes);
            }
            dashes = -1;
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool _TryGetDashes(Entity e, out int dashes) {
            if (e is Ghost ghost) {
                if (InformationComponent.Information.TryGetValue(ghost.PlayerInfo.ID, out dashes)) {
                    return dashes >= 0;
                }
            }
            dashes = -1;
            return false;
        }
    }
}