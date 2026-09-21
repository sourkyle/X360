// Program is protected under GPL Licensing and Copyrighted to alias DJ Shepherd

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using DevComponents;
using DevComponents.DotNetBar.Controls;
using DevComponents.DotNetBar;
using X360;
using X360.IO;
using X360.STFS;
using X360.Other;
using X360.Media;
using X360.Profile;
using X360.SVOD;
using X360.GDFX;

namespace Le_Fluffie
{
    public partial class MainForm : Office2007Form
    {
        public List<string> Files = new List<string>();
        public RSAParams PublicKV;
        bool adjusturl = true;

        void startss(object devmode)
        {
            SS x = null;
            if (!((bool)devmode))
            {
                x = new SS();
                x.Show();
            }
            if (x != null)
                x.labelX4.Text = "Loading Plug - In's...";
            try
            {
                ToolStripMenuItem current = new ToolStripMenuItem("Current Plugins");
                string[] flz = Directory.GetFiles(Application.StartupPath + "/plugins");
                foreach (string s in flz)
                {
                    try
                    {
                        if (Path.GetExtension(s) != ".dll" || Path.GetFileName(s).ToLower() == "x360.dll")
                            continue;
                        LFPlugIn v = new LFPlugIn(s);
                        if (v.valid)
                        {
                            current.DropDownItems.Add(v.Name, null, new EventHandler(plugclick));
                            current.DropDownItems[current.DropDownItems.Count - 1].Tag = v;
                        }
                    }
                    catch { }
                }
                pluginsToolStripMenuItem.DropDownItems.Add(current);
                pluginsToolStripMenuItem.DropDownItems.Add("Download Plugins", null, new EventHandler(download_click));

            }
            catch { }
            if (x != null)
            {
                while (x.Visible)
                    Application.DoEvents();
            }
            Thread.CurrentThread.Abort();
        }

        void download_click(object sender, EventArgs e)
        {
            PIDownloader pid = new PIDownloader();
            pid.ShowDialog();
        }

        public MainForm()
        {
            CheckForIllegalCrossThreadCalls = false;
            string proc = Process.GetCurrentProcess().ProcessName;
            int id = Process.GetCurrentProcess().Id;
            Process[] lst = Process.GetProcesses();
            foreach (Process p in lst)
            {
                if (p.ProcessName != proc || p.Id == id)
                    continue;
                MessageBox.Show("This program is already open");
                Process.GetCurrentProcess().Kill();
                return;
            }
            InitializeComponent();
            VariousFunctions.DeleteFile(Application.StartupPath + "/LFLiveUpdater.exe");
            webBrowser1.Url = new Uri("about:blank");
            richTextBox1.Text =
                "This build packages Xbox 360 DLC.\r\n\r\n" +
                "File, then Package Creation. Leave the type on STFS.\r\n" +
                "Package type defaults to MarketPlace. Signing defaults to Dev LIVE.\r\n" +
                "The Title ID box is hexadecimal. Fallout: New Vegas is 425307E0.\r\n\r\n" +
                "The FATX drive browser was removed. Copy the finished package with FATXplorer.\r\n" +
                "Opening a profile package still allows the old profile editor.";
            bool devmode = AssemblyFunctions.GrabParentProcessName() == "devenv";
            Thread x = new Thread(new ParameterizedThreadStart(startss));
            x.Start(devmode);
            VariousFunctions.DeleteTempFiles();
            PublicKV = null;
            try
            {
                string kvPath = Application.StartupPath + "/KV.bin";
                if (File.Exists(kvPath))
                    PublicKV = new RSAParams(kvPath);
            }
            catch { PublicKV = null; }
            if (PublicKV != null && !PublicKV.Valid)
                PublicKV = null;
            XAbout.WriteLegalLocally();
            while (x.IsAlive)
                Application.DoEvents();
            Show();
            Enabled = true;
            Select();
            Focus();
            this.Text = "Le Fluffie - DLC packager";
            checkForUpdatesToolStripMenuItem.Enabled = true;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Refresh();
        }

        void ReadFile(string file)
        {
            try
            {
                switch (VariousFunctions.ReadFileType(file))
                {
                    case XboxFileType.STFS:
                        {
                            LogRecord x = new LogRecord();
                            STFSPackage xPackage = new STFSPackage(file, x);
                            if (!xPackage.ParseSuccess)
                                return;
                            PackageExplorer xExplorer = new PackageExplorer(this);
                            xExplorer.listBox4.Items.AddRange(x.Log);
                            x.WhenLogged += new LogRecord.OnLog(xExplorer.xAddLog);
                            xExplorer.set(ref xPackage);
                            xExplorer.Show();
                        }
                        break;

                    case XboxFileType.SVOD:
                        {
                            SVODPackage hd = new SVODPackage(file, null);
                            if (!hd.IsValid)
                                return;
                            HDDGameForm frm = new HDDGameForm(hd, file, this);
                            frm.MdiParent = this;
                            frm.Show();
                        }
                        break;

                    case XboxFileType.Music:
                        {
                            MusicFile xfile = new MusicFile(file);
                            MusicView xview = new MusicView(this, file, xfile);
                            xview.MdiParent = this;
                            xview.Show();
                        }
                        break;

                    case XboxFileType.GPD:
                        {
                            GameGPD y = new GameGPD(file, 0xFFFFFFFF);
                            GPDViewer z = new GPDViewer(y, file, this);
                            z.MdiParent = this;
                            z.Show();
                        }
                        break;

                    case XboxFileType.FATX:
                        MessageBox.Show(
                            "This build does not browse FATX drives or images.\r\n" +
                            "Use FATXplorer to copy files, then package DLC from File → Package Creation.");
                        return;

                    case XboxFileType.GDF:
                        {
                            StatsForm x = new StatsForm(0);
                            x.Text = "Select Deviation";
                            if (x.ShowDialog() != DialogResult.OK)
                                return;
                            GDFImage ximg = new GDFImage(file, x.ChosenID);
                            if (!ximg.Valid)
                                throw new Exception("Invalid package");
                            GDFViewer xViewer = new GDFViewer(ximg, this);
                            xViewer.MdiParent = this;
                            xViewer.Show();
                        }
                        break;

                    default: MessageBox.Show("Error: Unknown file"); return;
                }
                Files.Add(file);
            }
            catch (Exception x) { MessageBox.Show(x.Message); }
        }
       
        private void openAFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string file = VariousFunctions.GetUserFileLocale("Open an Xbox File", "", true);
            if (file == null || Files.Contains(file))
                return;
            ReadFile(file);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About xAbout = new About();
            xAbout.ShowDialog();
            xAbout.Dispose();
        }

        private void packageCreationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PackageTypeSel xSel = new PackageTypeSel();
            if (xSel.ShowDialog() != DialogResult.OK)
                return;
            PackageCreatez xCreate = new PackageCreatez(this, xSel.SelectedType);
            xCreate.Show();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            VariousFunctions.DeleteTempFiles();
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The original update server (skunkiebutt.com) is offline. This branch does not check for updates.");
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            BringToFront();
        }

        private void donateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(XAbout.Donate);
        }

        private void multiSTFSFixerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            multiSTFSFixerToolStripMenuItem.Enabled = false;
            MultiSTFS x = new MultiSTFS(this);
            x.MdiParent = this;
            x.Show();
        }

        private void achievementsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string xtext = "How to Edit Achievements and Add Games\r\n\r\n" +
                "After you open your profile, locate the Profile>Profile Tab." +
                "  You will see a list of games, which is all your games played.\r\n\r\n" +
                "To unlock any of those achievements, simply click on that game and go " +
                " to the Profile>Achievements tab where you will be able to mod the achievements." +
                "  Just select an achievement, mod it, hit Save Achievement, do all that you want, " +
                " and then hit Save to Profile, Save Dash GPD, vwallah.\r\n\r\nAfter unlocking all that you want, " +
                "go to the Security tab and click Fix, you are all good to go.\r\n\r\n" +
                "To add games, simple open the multiadder, locate all that you want, and hit OK.";
            Helper xhlp = new Helper(xtext, (ToolStripMenuItem)sender);
            xhlp.MdiParent = this;
            xhlp.Show();
        }

        private void packageCreationToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string xtext = "Package Creation\r\n\r\n" +
                "For Fallout: New Vegas mods, choose STFS. Package type MarketPlace " +
                "is content type 00000002. Set Title ID to 425307E0 (the box is hex). " +
                "Sign with Dev LIVE. Save the file and copy it with FATXplorer to " +
                "Content\\0000000000000000\\425307E0\\00000002\\.\r\n\r\n" +
                "Do not rebuild the package after it is signed.\r\n\r\n" +
                "SVOD packages are disc images, not DLC. Deviation 0 is a clean image. " +
                "An image extracted from an older SVOD package keeps that package's deviation.";
            Helper xhlp = new Helper(xtext, (ToolStripMenuItem)sender);
            xhlp.MdiParent = this;
            xhlp.Show();
        }

        private void gamertagEditingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string xtext = "How to Edit Your Gamertag\r\n\r\n" +
                "After you open your profile, locate the Profile>Profile Tab." +
                "  You will see a textbox wif your gamertag in that.  Simply edit the gamertag " +
                "and hit Save Account.  Be sure to go to Security and hit Fix <3";
            Helper xhlp = new Helper(xtext, (ToolStripMenuItem)sender);
            xhlp.MdiParent = this;
            xhlp.Show();
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string x in files)
                ReadFile(x);
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
        }

        void plugclick(object sender, EventArgs e)
        {
            string locale = VariousFunctions.GetUserFileLocale("Open a File", "", true);
            if (locale == null)
                return;
            // Integrate log choice
            STFSPackage x = new STFSPackage(locale, null);
            if (!x.ParseSuccess)
                return;
            try { ((LFPlugIn)((ToolStripItem)sender).Tag).xConst.Invoke(new object[] { x, (Form)this }); }
            catch (Exception z) { x.CloseIO(); MessageBox.Show(z.Message); }
        }

        private void expandablePanel1_ExpandedChanged(object sender, ExpandedChangeEventArgs e)
        {
            Refresh();
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;
            Process.Start((string)listView1.SelectedItems[0].Tag);
        }

        private void refreshChatPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The original chat page is offline.");
        }

        private void webBrowser1_NewWindow(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (webBrowser1.Url.Host == "skunkiebutt.com")
                return;
            if (!adjusturl)
                return;
            try
            {
                while (webBrowser1.Document == null)
                    Application.DoEvents();
                HtmlElement xele = webBrowser1.Document.GetElementById("linkSkip");
                if (xele != null)
                {
                    adjusturl = false;
                    webBrowser1.Navigate(xele.GetAttribute("href"));
                }
            }
            catch { }
        }
    }
}
