using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using NCrontab;
namespace TShockCron
{
    public partial class ViewCronTab : Form
    {
        private string cronTabPath;
        public ViewCronTab(string path)
        {
            cronTabPath = path;
            InitializeComponent();

            lblSaveStatus.Text = "";
            dataCronTab.RowHeadersWidth = 22;
            loadData(path);
        }

        private void loadData(string path)
        {
            string line;
            bool comment = false;
            string[] lineOptions;
            string command;
            string options = "";

            lblSaveStatus.Text = "";

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

            dataCronTab.Rows.Clear();
            listSchedule.Items.Clear();
            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                comment = false;

                if (line.StartsWith("#"))
                {
                    comment = true;
                    options = line;
                    command = "";
                }
                else if (line.ToLower().StartsWith("@reboot"))
                {
                    command = "";
                    try
                    {
                        lineOptions = line.Split(default(Char[]), 2, StringSplitOptions.RemoveEmptyEntries);
                        command = lineOptions[lineOptions.Length - 1];
                        options = lineOptions[0];
                    }
                    catch { }
                }
                else
                {
                    {
                        options = "";
                        command = "";
                        try
                        {
                            lineOptions = line.Split(default(Char[]), 6, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < lineOptions.Length - 1; i++)
                                options += lineOptions[i] + " ";
                            command = lineOptions[lineOptions.Length - 1];
                        }
                        catch { }
                    }
                }
                try
                {
                    dataCronTab.Rows.Add(options, command);
                }
                catch { }
            }
            file.Close();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            //            Cron.cronTab.reloadCronTab();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string command;
            string options;
            string backupPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(cronTabPath), System.IO.Path.GetFileNameWithoutExtension(cronTabPath) + ".bak");

            // Ensure that the target does not exist.
            File.Delete(backupPath);

            // Copy the file.
            File.Copy(cronTabPath, backupPath);

            using (System.IO.StreamWriter fileWriter = new System.IO.StreamWriter(cronTabPath))
            {
                foreach (DataGridViewRow row in dataCronTab.Rows)
                {
                    if (row != null)
                        if (row.Cells != null)
                        {
                            if (row.Index < dataCronTab.Rows.Count - 1)
                            {
                                options = "";
                                if (row.Cells[0].Value != null)
                                    options = row.Cells[0].Value.ToString().Trim();
                                command = "";
                                if (row.Cells[1].Value != null)
                                    command = row.Cells[1].Value.ToString().Trim();
                                fileWriter.WriteLine(options + " " + command);
                            }
                        }
                }
            }
            lblSaveStatus.Text = "Saved.";
        }
        private void tabRefresh_Click(object sender, EventArgs e)
        {
            loadData(cronTabPath);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            int row;
            if ((row = dataCronTab.CurrentRow.Index) == 0)
                return;
            listSchedule.Items.Clear();
            lblOptions.Text = "Future Schedule Date/Times";
            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now.AddYears(1);
            string intervalOptions;
            IEnumerable<DateTime> occurrence;

            intervalOptions = dataCronTab[0, row].Value.ToString();
            if (intervalOptions.Length == 0)
                return;
            if (intervalOptions.StartsWith("#"))
                return;

            try
            {
                var schedule = CrontabSchedule.Parse(intervalOptions);

                occurrence = schedule.GetNextOccurrences(startDate, endDate);
                lblOptions.Text = "Future Schedule Date/Times for " + intervalOptions;

                ListViewItem scheduleItem;
                int count = 0;
                foreach (DateTime futureEvent in schedule.GetNextOccurrences(startDate, endDate))
                {
                    if (count++ >= 10)
                        break;
                    scheduleItem = new ListViewItem(futureEvent.ToString("g"));
                    listSchedule.Items.Add(scheduleItem);
                }
            }
            catch (CrontabException ex)
            {
                ListViewItem scheduleItem;
                scheduleItem = new ListViewItem(ex.Message);
                listSchedule.Items.Add(scheduleItem);
            }

        }
        class CronOptions
        {
            public bool Comment { get; set; }
            public string IntervalOptions { get; set; }
            public string Command { get; set; }

            public CronOptions(bool comment, string options, string command)
            {
                Comment = comment;
                IntervalOptions = options;
                Command = command;
            }
            public CronOptions()
            {
                Comment = false;
                IntervalOptions = "";
                Command = "";
            }
        }




    }
}
