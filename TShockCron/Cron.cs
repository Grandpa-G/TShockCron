using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Data;
using System.ComponentModel;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using System.Security.Permissions;

using Terraria;
using TShockAPI;
using Newtonsoft.Json;
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
        Thread thread;
        ViewCronTab cronTabForm;

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
            ServerApi.Hooks.GamePostInitialize.Register(this, OnGameInitialize);
 
            Commands.ChatCommands.Add(new Command("TCron.allow", runTCron, "cron"));
         }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnGameInitialize);
             }
            base.Dispose(disposing);
        }

        private void OnGameInitialize(EventArgs args)
        {
            cronTab.atReboot(Path.Combine(TShock.SavePath, "crontab.txt"));
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
                args.Player.SendMessage(" -edit/-e      invokes the GUI CronTab file editor", Color.LightSalmon);
                args.Player.SendMessage(" -delete/-d <n>deletes the nth scheduled event, use -list for <n>", Color.LightSalmon);
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

            if (arguments.Contains("-e") || arguments.Contains("-edit"))
            {
                var path = Path.Combine(TShock.SavePath, "crontab.txt");
                CronGUI(path);
                return;
            }

            if (arguments.Contains("-d") || arguments.Contains("-delete"))
            {
                int deleteIndex;
                string arg = "";
                if (arguments.Contains("-d"))
                    arg = arguments["-d"];
                if (arguments.Contains("-delete"))
                    arg = arguments["-d"];
                if (arg.Length == 0)
                    Console.WriteLine(" Invalid -delete value.");

                if (Int32.TryParse(arg, out deleteIndex))
                {
                    cronTab.deleteSchedule(deleteIndex);
                }
                else
                    Console.WriteLine(" Invalid -delete value, not a number.");

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
        private void CronGUI(string path)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            thread = new Thread(new ThreadStart(() =>
            {
                // this code is going to be executed in a separate thread
                cronTabForm = new ViewCronTab(path);

                Application.Run(cronTabForm);

            }));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
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
