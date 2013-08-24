using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace Arinc424Manager
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Default main constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Path of the source data file (selected by user)
        /// </summary>
        private string filePath = "";
        public string FilePath
        {
            get { return filePath; }
            set
            {
                filePath = value;
                this.InvokeEx(() => { loadedFileLabel.Text = "File path: " + value; });
            }
        }

        private string status = "";
        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                this.InvokeEx(() => { statusLabel.Text = value; });
            }
        }

        public class Line
        {
            public enum RecordType
            {
                PrimaryWithoutContinuationFollowing,
                PrimaryWithContinuationFollowing,
                Continuation
            }

            public RecordType MyRecordType { get; set; }
            //  public List<Line> Children = new List<Line>();

            /// <summary>
            /// Split contents of the line
            /// </summary>
            public List<string> Contents = new List<string>();

            /// <summary>
            /// Names of columns for contents of specified layout
            /// </summary>
            public List<string> ColumnNames = new List<string>();

            /// <summary>
            /// Line layout type
            /// </summary>
            public string Layout { get; set; }

            /// <summary>
            /// Source string representing the line
            /// </summary>
            public string Source { get; set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="source">Source string</param>
            /// <param name="previousContinuationNumber">Do I really need that?</param>
            public Line(string source)
            {
                Source = source;
                int layoutFirstIndex = 4; // Layout starting symbol index in the source line 
                int layoutSecondIndex = source[5] != ' ' ? 5 : 12;   // Layout ending symbol index in the source line
                Layout = (source[layoutFirstIndex].ToString() + source[layoutSecondIndex]).Replace(" ", ""); // Getting the layout



                if (PartitionMap.ContainsKey(Layout)) // Checking if such layout exists in the partition map
                    try
                    {
                        Program.mainForm.Status = "Loading data from file...";


                        int continuationIndexLocation = Program.mainForm.GetInfoLocationInMapperList(PartitionMap[Layout], "cnum");
                        int continuationIndexTypeLocation = Program.mainForm.GetInfoLocationInMapperList(PartitionMap[Layout], "cnum_type");



                        char continuationType;


                        if (continuationIndexTypeLocation != -1)
                        {
                            MyRecordType = GetRecordType(source[continuationIndexLocation]);
                            continuationType = source[continuationIndexLocation + 1];
                        }
                        else
                            if (continuationIndexLocation != -1)
                            {
                                MyRecordType = GetRecordType(source[continuationIndexLocation]);
                                continuationType = 'A';
                            }
                            else
                            {
                                MyRecordType = RecordType.PrimaryWithoutContinuationFollowing;
                                continuationType = '\0';
                            }

                        if (MyRecordType == RecordType.Continuation)// && PartitionMap.ContainsKey(Layout + "_" + continuationType))
                        {
                            if (PartitionMap.ContainsKey(Layout + "_" + continuationType))
                            {

                                Layout += "_" + continuationType;

                            }
                            else
                                Console.WriteLine(Layout += "_" + continuationType);
                        }

                        List<TableMapper> partition = PartitionMap[Layout]; ;


                        if (partition.Count > 0)
                        {

                            for (int i = 0; i < partition.Count - 2; i++)
                            {
                                Contents.Add(source.Substring(partition[i].Index, partition[i + 1].Index - partition[i].Index)); // Getting the content according to the partition map
                                ColumnNames.Add(partition[i].Name);  // Setting the column names for the line
                            }

                            Contents.Add(source.Substring(partition[partition.Count - 1].Index)); // Adding the last content element
                            ColumnNames.Add(partition[partition.Count - 1].Name); // Adding the last column name element

                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message + " (" + Layout + ")"); // Also adding the layout name to the exception message
                    }
                else
                {
                    Program.mainForm.Status = "No layout partition template found for: " + Layout;
                }
            }

            private RecordType GetRecordType(char continuationNo)
            {

                switch (continuationNo)
                {
                    case '0':
                        return RecordType.PrimaryWithoutContinuationFollowing; //No continuation records following
                    case '1':
                        return RecordType.PrimaryWithContinuationFollowing; //Several continuation records following
                    default:
                        return RecordType.Continuation; //Number of the continuation record
                }
            }

            public override string ToString()
            {
                return Source;
            }
        }

        public int GetInfoLocationInMapperList(List<TableMapper> mapperList, string info)
        {
            for (int i = 0; i < mapperList.Count; i++)
            {
                if (mapperList[i].Info == info)
                {
                    return mapperList[i].Index;
                }
            }
            return -1;
        }

        public class TableMapper
        {
            public string Info { get; set; }
            public int Index { get; set; }
            public string Name { get; set; }
            public TableMapper(int index, string name, string info)
            {
                Index = index;
                Name = name;
                Info = info;
            }

            public TableMapper(int index, string name) : this(index, name, "") { }

        }

        public static Dictionary<string, List<TableMapper>> PartitionMap;
        public static Dictionary<string, List<Line>> LineMap = new Dictionary<string, List<Line>>();

        /// <summary>
        /// Source file split to lines
        /// </summary>
        List<Line> lines = new List<Line>();

        /// <summary>
        /// Path to the partition file
        /// </summary>
        string partitionFilePath = "partitions.txt";

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                PartitionMap = FillPartitionMap(partitionFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }


        Dictionary<string, List<TableMapper>> FillPartitionMap(string partitionFilePath)
        {
            Dictionary<string, List<TableMapper>> result = new Dictionary<string, List<TableMapper>>();
            try
            {
                string[] partition = File.ReadAllLines(partitionFilePath, Encoding.GetEncoding(1251));
                for (int i = 0; i < partition.Length; i++)
                {
                    var item = partition[i];
                    string layout = item.Substring(0, item.IndexOf(":")).Replace(" ", "");
                    item = item.Remove(0, item.IndexOf(":") + 1);

                    string[] split = item.Split((";").ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    List<TableMapper> values = new List<TableMapper>();
                    for (int j = 0; j < split.Length; j++)
                    {
                        string info = (split[j].Contains('[') && split[j].Contains(']')) ?
                            split[j].Split(new char[] { '[', ']' })[1] : "";

                        string tableName = split[j].Split(new char[] { '(', ')' })[1];

                        int parsedValue;
                        if (int.TryParse(split[j].Split(new char[] { '(', ')' })[2], out parsedValue))
                        {
                            values.Add(new TableMapper(parsedValue, tableName, info));
                        }
                    }
                    result[layout] = values;
                }

                return result;
            }
            catch
            {
                throw new IOException("Error loading partition file.");
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All files (*.*) | *.*";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FilePath = ofd.FileName;
                LineMap.Clear();
                listBox1.Items.Clear();
                listView1.Items.Clear();
                try
                {
                    this.InvokeEx(() => toolStripProgressBar1.Maximum = lines.Count);

                    this.InvokeEx(() => toolStripProgressBar1.Value = 0);
                    Status = "Loading data from file...";
                    Task.Factory.StartNew(() => FillLines()).ContinueWith((t) =>

                 //   Thread oThread = new Thread(new ThreadStart(FillLines));
                  //  oThread.Start();
                  //   oThread.Join();

                 // 
                  {
                      this.InvokeEx(() => toolStripProgressBar1.Value = 0);

                      foreach (var line in lines)
                      {
                          Status = "Filling UI Fields...";
                          this.InvokeEx(() => { listBox1.Items.Add(line.ToString()); });
                          this.InvokeEx(() => { toolStripProgressBar1.PerformStep(); });
                      }

                      this.InvokeEx(() => toolStripProgressBar1.Value = 0);
                      Status = "Idle";
                      //MessageBox.Show("Done");
                  });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
        }

        void FillLines()
        {
            lines = StringArrayToLineList(File.ReadAllLines(FilePath, Encoding.GetEncoding(1251)));
        }
        List<Line> StringArrayToLineList(string[] array)
        {
            List<Line> newList = new List<Line>();

            this.InvokeEx(() => toolStripProgressBar1.Maximum = array.Length);

            for (int i = 0; i < array.Length; i++)
            {
                this.InvokeEx(() => toolStripProgressBar1.PerformStep());
                try
                {
                    Line l = new Line(array[i]);
                    if (!LineMap.ContainsKey(l.Layout)) LineMap.Add(l.Layout, new List<Line>());
                    LineMap[l.Layout].Add(l);
                    newList.Add(l);

                }
                catch (Exception ex)
                { }

            }

            return newList;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (listBox1.SelectedIndex != -1)
            {
                listView1.Columns.Clear();
                listView1.Items.Clear();
                listView1.Columns.Add("Layout");
                ListViewItem lvi = new ListViewItem(lines[listBox1.SelectedIndex].Layout);
                int cnt = 0;

                foreach (var item in lines[listBox1.SelectedIndex].Contents)
                {
                    listView1.Columns.Add(lines[listBox1.SelectedIndex].ColumnNames[cnt]);
                    listView1.Columns[cnt].Width = -2;
                    cnt++;
                    lvi.SubItems.Add(item);
                }

                this.InvokeEx(() => listView1.Items.Add(lvi));
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices[0] != -1)
            {
                //TODO: Write setter here
            }
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void saveInGroupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                this.InvokeEx(() => { toolStripProgressBar1.Value = 0; });
                this.InvokeEx(() => { toolStripProgressBar1.Maximum = lines.Count; });
                foreach (var key in LineMap.Keys)
                {
                    string savePath = Path.Combine(fbd.SelectedPath, key + ".txt");
                    using (StreamWriter saveStream = new StreamWriter(savePath))
                    {
                        Status = "Saving: " + savePath;
                        foreach (Line line in LineMap[key])
                        {
                            saveStream.WriteLine(line.ToString());
                            this.InvokeEx(() => { toolStripProgressBar1.PerformStep(); });
                        }
                    }
                }
                this.InvokeEx(() => { toolStripProgressBar1.Value = 0; });
                MessageBox.Show("Saved!");
            }

        }

        private void statisticsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void SaveToCsv()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                this.InvokeEx(() => { toolStripProgressBar1.Value = 0; });
                this.InvokeEx(() => { toolStripProgressBar1.Maximum = lines.Count; });
                string report = "";
                int errorCounter = 0;
                foreach (var key in LineMap.Keys)
                {

                    string savePath = Path.Combine(fbd.SelectedPath, key + ".csv");
                    using (StreamWriter saveStream = new StreamWriter(savePath))
                    {

                        Status = "Saving: " + savePath;
                        report += string.Format("Saving {0} : ", key);
                        if (PartitionMap.ContainsKey(key.Replace(" ", "")))
                        {
                            saveStream.WriteLine(string.Join(";",
                                (from mapper in PartitionMap[key.Replace(" ", "")]
                                 select mapper.Name).ToArray<string>()));

                            foreach (Line line in LineMap[key])
                            {
                                saveStream.WriteLine(lineToCsvLine(line));
                                this.InvokeEx(() => { toolStripProgressBar1.PerformStep(); });
                            }
                            report += "OK\r\n";
                        }
                        else
                        {
                            errorCounter++;
                            report += "Partition not found in database! Skipping...\r\n";
                        }

                    }
                    File.WriteAllText(Path.Combine(fbd.SelectedPath, "log.txt"), report);
                }
                this.InvokeEx(() => { toolStripProgressBar1.Value = 0; });
                string msg = "Saved" + (errorCounter == 0 ? " successfully!" : " (with " + errorCounter + " errors)!\r\nMore info in log.txt");
                Status = "Idle";
                MessageBox.Show(msg);
            }

        }

        string lineToCsvLine(Line line)
        {
            return string.Join(";", line.Contents.ToArray());
        }

        private void saveInGroupsCSVAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveToCsv();
        }

        private void statisticsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            (new StatsForm(LineMap, FilePath)).Show();
        }

    }
}
