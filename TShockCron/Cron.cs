using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Data;
using System.ComponentModel;
using System.Reflection;
using System.Drawing;

using Terraria;
using TShockAPI;
using Newtonsoft.Json;
using System.Threading;
using TerrariaApi.Server;
using Newtonsoft.Json.Linq;

namespace TShockCron
{
    [ApiVersion(1, 17)]
    public class Cron : TerrariaPlugin
    {
        public static bool verbose = false;
        public static bool preview = false;
        private CronTab cronTab = new CronTab();

        public override string Name
        {
            get { return "Cron"; }
        }
        public override string Author
        {
            get { return "Granpa-G"; }
        }
        public override string Description
        {
            get { return "Runs commands based upon date schedule (like cron)."; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public Cron(Main game)
            : base(game)
        {
            Order = -1;
        }
        public override void Initialize()
        {
            if (TShock.Config.UseSqlLogs)
            {
                Console.WriteLine("This command only used with text log files.");
                return;
            }

            Commands.ChatCommands.Add(new Command("TCron.allow", runTCron, "cron"));

        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        private void runTCron(CommandArgs args)
        {
            if (args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("Invalid command entered. Type {0}help for a list of valid commands.", TShockAPI.Commands.Specifier);
                return;
            }

            CronListArguments arguments = new CronListArguments(args.Parameters.ToArray());

            if (arguments.Contains("-help") || args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Syntax: /cron [-help] ", Color.Red);
                args.Player.SendMessage("Flags: ", Color.LightSalmon);
                args.Player.SendMessage(" -stop/-s      stops all future cron events", Color.LightSalmon);
                args.Player.SendMessage(" -reload/-r    reloads options from crontab file", Color.LightSalmon);
                args.Player.SendMessage(" -verbose/-v   show details", Color.LightSalmon);
                args.Player.SendMessage(" -!verbose/-!v don't show details", Color.LightSalmon);
                args.Player.SendMessage(" -preview/-p   show schedule details without scheduling", Color.LightSalmon);
                args.Player.SendMessage(" -!preview/-!p negate preview  mode", Color.LightSalmon);
                args.Player.SendMessage(" -list/l       show current cron criteria", Color.LightSalmon);
                args.Player.SendMessage(" -help         this information", Color.LightSalmon);
                return;
            }

            if (arguments.Contains("-r") || arguments.Contains("-reload"))
            {
                cronTab.stopTimeEvents();

                var path = Path.Combine(TShock.SavePath, "crontab.txt");

                cronTab.Read(path);
                return;
            }

            if (arguments.Contains("-s") || arguments.Contains("-stop"))
            {
                cronTab.stopTimeEvents();
                return;
            }

            if (arguments.Contains("-v") || arguments.Contains("-verbose"))
            {
                verbose = true;
                return;
            }
            if (arguments.Contains("-!v") || arguments.Contains("-!verbose"))
            {
                verbose = false;
                return;
            }

            if (arguments.Contains("-p") || arguments.Contains("-preview"))
            {
                preview = true;
                return;
            }
            if (arguments.Contains("-!p") || arguments.Contains("-!preview"))
            {
                preview = false;
                return;
            }

            if (arguments.Contains("-l") || arguments.Contains("-list"))
            {
                Console.WriteLine("Current options for TCron version " + Assembly.GetExecutingAssembly().GetName().Version);
                Console.WriteLine(" verbose is " + verbose.ToString());
                Console.WriteLine(" preview is " + preview.ToString());

                cronTab.listTimeEvents();
                return;
            }

            Console.WriteLine(" Invalid cron option:" + string.Join(" ", args.Parameters));
        }
    }
    #region application specific commands
    public class CronListArguments : InputArguments
    {
        public string Verbose
        {
            get { return GetValue("-verbose"); }
        }
        public string VerboseShort
        {
            get { return GetValue("-v"); }
        }

        public string Help
        {
            get { return GetValue("-help"); }
        }


        public CronListArguments(string[] args)
            : base(args)
        {
        }

        protected bool GetBoolValue(string key)
        {
            string adjustedKey;
            if (ContainsKey(key, out adjustedKey))
            {
                bool res;
                bool.TryParse(_parsedArguments[adjustedKey], out res);
                return res;
            }
            return false;
        }
    }
    #endregion

}
