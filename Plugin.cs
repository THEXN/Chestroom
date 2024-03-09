using Microsoft.Data.Sqlite;
using Microsoft.Xna.Framework;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace ChestroomPlugin
{
    [ApiVersion(2, 1)]
    public class Plugin : TerrariaPlugin
    {
        public static Config config = new();
        public static IDbConnection Database;
        public static bool usinginfchests;

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override string Author
        {
            get { return "Ancientgods，肝帝熙恩汉化"; }
        }

        public override string Name
        {
            get { return "Chestroom"; }
        }

        public override string Description
        {
            get { return "生成一个仓库 生成一个包含所有物品的箱子室"; }
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("chestroom.create", chestroom, "chestroom", "cr") { AllowServer = false });
            Commands.ChatCommands.Add(new Command("chestroom.reload", Reload_Config, "crreload"));

            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "ServerPlugins\\InfiniteChests.dll")))
            {
                switch (TShock.Config.Settings.StorageType.ToLower())
                {
                    case "mysql":
                        string[] host = TShock.Config.Settings.MySqlHost.Split(':');
                        Database = new MySqlConnection()
                        {
                            ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                                    host[0],
                                    host.Length == 1 ? "3306" : host[1],
                                    TShock.Config.Settings.MySqlDbName,
                                    TShock.Config.Settings.MySqlUsername,
                                    TShock.Config.Settings.MySqlPassword)
                        };
                        break;
                    case "sqlite":
                        string sql = Path.Combine(TShock.SavePath, "chests.sqlite");
                        Database = new SqliteConnection(string.Format("Data Source={0}", sql));
                        break;
                }
                usinginfchests = true;
            }
            ReadConfig();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        public Plugin(Main game)
            : base(game)
        {
            Order = 1;
        }

        private void chestroom(CommandArgs args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int X = args.Player.TileX;
            int Y = args.Player.TileY;
            string cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "";
            int offsetX = 0,
                offsetY = 0;
            bool error = false;
            if (cmd.Length != 2)
                error = true;
            for (int i = 0; i < cmd.Length; i++)
            {
                switch (cmd[i])
                {
                    case 't':
                        offsetX -= -2;
                        break;
                    case 'b':
                        offsetY -= Chestroom.RowHeight - 4;
                        break;
                    case 'l':
                        offsetX -= 2;
                        break;
                    case 'c':
                        offsetX -= (Chestroom.RowWidth - 2) / 2;
                        break;
                    case 'r':
                        offsetX -= Chestroom.RowWidth - 4;
                        break;
                    default:
                        error = true;
                        break;
                }
            }
            if (error)
            {
                args.Player.SendErrorMessage("语法无效！正确的语法： /chestroom <tl/tr/bl/br/tc/bc>");
                args.Player.SendErrorMessage("t = top(上), l = left(左), r = right(右), b = bottom, c = center(中心)");
                args.Player.SendErrorMessage("这是当箱子生成时您将站立的地方。");
                return;
            }
            Chestroom chestRoom = new(config.CustomRoom);
            args.Player.SendSuccessMessage("创建仓库...");

            if (chestRoom.Build(args.Player, X + offsetX, Y + offsetY))
            {
                sw.Stop();
                Utils.informplayers();
                args.Player.SendInfoMessage(string.Format("Chestroom 创建耗时 {0} 秒. ({1} 物品在 {2} 箱子)", sw.Elapsed.TotalSeconds, Chestroom.ActualMaxItems, Chestroom.MaxChests));
            }
        }

        public class Config
        {
            public int ChestsPerRow = (int)Math.Ceiling(Math.Sqrt(Chestroom.MaxChests));
            public bool CustomRoom;
            public byte TileId = 38;
            public short ChestId = 1;
            public byte BgWall = 4;
            public short pFrameY = 18;
            public short tFrameY = 22;
        }

        static bool ReadConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "Chestroom.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            config = JsonConvert.DeserializeObject<Config>(configString);
                            config.ChestsPerRow = config.ChestsPerRow < 2 ? 2 : Math.Min(Chestroom.MaxChests, config.ChestsPerRow);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    TShock.Log.ConsoleError("找不到仓库配置。正在创建...");
                    CreateConfig();
                    return false;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.Message);
            }

            return false;
        }

        static void CreateConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "Chestroom.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        config = new Config();
                        var configString = JsonConvert.SerializeObject(config, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.Message);
                config = new Config();
            }
        }

        void Reload_Config(CommandArgs args)
        {
            if (ReadConfig())
                args.Player.SendMessage("已成功重新加载仓库配置.", Color.Green);

            else
                args.Player.SendErrorMessage("仓库配置重新加载失败。检查日志以了解详细信息.");
        }
    }
}
