using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Arinc424Manager
{
    public partial class StatsForm : Form
    {
        public StatsForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void StatsForm_Load(object sender, EventArgs e)
        {
            
            try
            {
                showContentsBox.Checked=!(new FileInfo(FilePath).Length>5000000);
            }
            catch
            {}
            
            foreach (var key in Source.Keys)
            {
                ListViewItem lvi = new ListViewItem(key.ToString());
                lvi.SubItems.Add(Source[key].Count.ToString());
                listView1.Items.Add(lvi);
            }

            for (int i = 0; i < listView1.Columns.Count; i++ )
            {
                listView1.Columns[i].Width = -2;
            }
        }
        Dictionary<string, List<MainForm.Line>> Source { get; set; }
        string FilePath { get; set; }
        public StatsForm(Dictionary<string, List<MainForm.Line>> source, string filePath)
        {
            InitializeComponent();
            Source = source;
            FilePath = filePath;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowContents();
        }

        void ShowContents()
        {
            listBox1.Items.Clear();
            if (listView1.SelectedIndices.Count > 0 && showContentsBox.Checked)
            {
                foreach (var line in Source.ElementAt(listView1.SelectedIndices[0]).Value)
                {
                    this.InvokeEx(() => { listBox1.Items.Add(line.ToString()); });
                }
            }
        }

        private void showContentsBox_CheckedChanged(object sender, EventArgs e)
        {
            ShowContents();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        string GenerateReport()
        {
                string result = "=============================================================" +
                                "\r\nFile name: "+FilePath + 
                                "\r\nReport date: "+ DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                                "\r\n=============================================================";
                foreach (var key in Source.Keys)
                {
                    result+= "\r\nKey: \"" + key.ToString() + "\" occured " + Source[key].Count.ToString() + " times.";           
                }
                return result;
        }
        void SaveReport()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Text Files | *.txt";
            sfd.DefaultExt = "txt";

            if (sfd.ShowDialog()==DialogResult.OK)
            {
                    File.WriteAllText(sfd.FileName, GenerateReport());
                    MessageBox.Show("Report saved!");
            }
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveReport();
        }

        
    }
}
