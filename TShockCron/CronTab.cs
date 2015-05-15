using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using NCrontab;
using System.Timers;
using System.Reflection;
using Terraria;
using TShockAPI;

namespace TShockCron
{
    class CronTab
    {

        public string Command { get; set; }
        public string IntervalOptions { get; set; }
        public bool RunOnce { get; set; }
        public CronTab(string options, string command, bool runonce)
        {
            IntervalOptions = options;
            Command = command;
            RunOnce = runonce;
        }
        public CronTab()
        {
            IntervalOptions = "";
            Command = "";
            RunOnce = false;
        }

 
        public static Dictionary<string, System.Timers.Timer> currentAlertsTimers = new Dictionary<string, System.Timers.Timer>();
        public static Dictionary<string, CronTab> commandLines = new Dictionary<string, CronTab>();

        public void reloadCronTab()
        {
            stopTimeEvents();

            var path = Path.Combine(TShock.SavePath, "crontab.txt");

            Read(path);
                    } 

        public bool Read(string path)
        {
            if (!File.Exists(path))
            {
                using (System.IO.StreamWriter fileWriter = new System.IO.StreamWriter(path))
                {
                    fileWriter.WriteLine("# Classic crontab format:");
                    fileWriter.WriteLine("# TCron plugin version:" + Assembly.GetExecutingAssembly().GetName().Version);
                    fileWriter.WriteLine("# Minutes Hours Days Months WeekDays Command");
                    fileWriter.WriteLine("");
                }
            }
           
            string line;
            string[] lineOptions;
            string command;
            string options;
            int entryId = 0;
            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now.AddYears(1);
            string currentKey;
            DateTime futureEvent;
            double interval;
            IEnumerable<DateTime> occurrence;

            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                if (line.Length == 0)
                    continue;
                if (line.StartsWith("#"))
                    continue;
                options = "";
                command = "";
                try
                {
                    lineOptions = line.Split(default(Char[]), 6, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < lineOptions.Length - 1; i++)
                        options += lineOptions[i] + " ";
                    command = lineOptions[lineOptions.Length - 1];
                }
                catch
                {
                }

                try
                {
                    var schedule = CrontabSchedule.Parse(options);
                    startDate = DateTime.Now;
                    occurrence = schedule.GetNextOccurrences(startDate, endDate);
                    futureEvent = occurrence.FirstOrDefault();
                    interval = (futureEvent - startDate).TotalMilliseconds;
                     entryId++;
                     currentKey = entryId.ToString();
                     Console.WriteLine(" " + currentKey + ":" + options + " " + command + " on " + futureEvent.ToString("g"));

                    if (!Cron.preview)
                    {
                        // Hook up the Elapsed event for the timer. 
                        System.Timers.Timer currentTimersList = new System.Timers.Timer();
                        currentTimersList.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                        currentTimersList.Interval = interval;
                        currentTimersList.Enabled = true;

                        currentAlertsTimers.Add(currentKey.ToString(), currentTimersList);

                        CronTab ct = new CronTab(options, command, false);
                        commandLines.Add(currentKey, ct);

                    }
                }
                catch
                {
                    Console.WriteLine("Error in format: " + line);
                }
            }
            file.Close();
            return true;
        }
        public void listTimeEvents()
        {
            if (commandLines.Count() == 0)
            {
                Console.WriteLine(" No scheduled commands.");
                return;
            }
            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now.AddYears(1);
            DateTime futureEvent;
            IEnumerable<DateTime> occurrence;
            foreach (KeyValuePair<string, CronTab> cl in commandLines)
            {
                try
                {
                    var schedule = CrontabSchedule.Parse(cl.Value.IntervalOptions);

                    occurrence = schedule.GetNextOccurrences(startDate, endDate);
                     futureEvent = occurrence.FirstOrDefault();
                    Console.WriteLine(" " + cl.Key + ":" + cl.Value.IntervalOptions + " " + cl.Value.Command + " on " + futureEvent.ToString("g"));
                }
                catch { }
            }
        }
        public void stopTimeEvents()
        {
            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now.AddYears(1);
            DateTime futureEvent;
            IEnumerable<DateTime> occurrence;
            foreach (KeyValuePair<string, System.Timers.Timer> t in currentAlertsTimers)
            {
                t.Value.Stop();
                t.Value.Dispose();
                try
                {
                    var schedule = CrontabSchedule.Parse(commandLines[t.Key].IntervalOptions);

                    occurrence = schedule.GetNextOccurrences(startDate, endDate);
                    futureEvent = occurrence.FirstOrDefault();
                    Console.WriteLine(" Stopped " + commandLines[t.Key].IntervalOptions + " " + commandLines[t.Key].Command + " on " + futureEvent.ToString("g"));
                }
                catch { }
            }
            currentAlertsTimers.Clear();
            commandLines.Clear();

        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            var firedtimer = (from tmpTimer in currentAlertsTimers
                              where tmpTimer.Value.Equals(source)
                              select tmpTimer).FirstOrDefault();
            if (firedtimer.Key == null)
            {
                if (Cron.verbose)
                    Console.WriteLine("The Elapsed event was raised at {0}:{1}\n{2}", e.SignalTime, firedtimer.Key, "null");
            }
            else
            {
                if (Cron.verbose)
                    Console.WriteLine("The scheduled event at {0}\n{1}:{2}", e.SignalTime, firedtimer.Key, commandLines[firedtimer.Key].Command);
                string command = commandLines[firedtimer.Key].Command;
                string options = commandLines[firedtimer.Key].IntervalOptions;
                DateTime startDate = DateTime.Now;
                DateTime endDate = DateTime.Now.AddYears(1);
                DateTime futureEvent;
                double interval;
                IEnumerable<DateTime> occurrence; 
                
                Console.Write("[TCron]->");
                Commands.HandleCommand(TSPlayer.Server, (command.StartsWith("/") ? command : "/" + command));
                var schedule = CrontabSchedule.Parse(options);
                startDate = DateTime.Now;
                occurrence = schedule.GetNextOccurrences(startDate, endDate);
/*
                {
                    int count = 0;
                    foreach (var o in schedule.GetNextOccurrences(startDate, endDate))
                    {
                        if (count++ > 3)
                            break;
                        Console.WriteLine(">" + o);
                    }
                }
                 */
                futureEvent = occurrence.FirstOrDefault();
                interval = (futureEvent - startDate).TotalMilliseconds;
                firedtimer.Value.Interval = interval;
            }
        }
        DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime(((dt.Ticks + d.Ticks - 1) / d.Ticks) * d.Ticks);
        }
    }
}
