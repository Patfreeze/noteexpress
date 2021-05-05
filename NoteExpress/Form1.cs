﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using NoteExpress;



namespace NotepadExpress
{
    

    public partial class Form1 : Form
    {

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        const int SW_RESTORE = 9;
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        public String sProgramName = "Note Express";
        public String sDefaultPath = "\\NoteExpress\\";
        private String sCurrentNamefile = "";
        private String sOpenFile = "";
        private Timer SearchTextBoxTimer;
        private Timer CloseAlreadyNoteTimer;
        private String sUserDocPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private int iFunnyFac = 0;
        private int iDelay = 500; // Delay after writting text
        
        public Form1(string[] args)
        {
            InitializeComponent();

            richTextBox1.Font = new Font(FontFamily.GenericMonospace, richTextBox1.Font.Size);
            System.IO.Directory.CreateDirectory(sUserDocPath + sDefaultPath);

            var date1 = DateTime.Now;

            sCurrentNamefile = date1.ToString("yyyy-MM-dd_HHmmss") + "";
            this.Text = sCurrentNamefile + " - " + sProgramName;
            updateFunnyFact();

            if (args.Length != 0)
            {
                
                /*
                for (int i = 0; i < args.Length; i++)
                {
                    Console.WriteLine("args[{0}] == {1}", i, args[i]);
                }
                 **/
                
                // args[0] = path to file

                sOpenFile = args[0];
                this.Text = sOpenFile + " - " + sProgramName;
                addFileToText(args[0]);
            }

            // Check the preference if is on openAll
            ClassPreference classPreference = new ClassPreference();
            classPreference.loadFilePreference();

            // We open All if we have on one process
            //var myProcess = Process.GetProcessesByName("NoteExpress");
            var myProcess = Process.GetProcesses();

            int iCount = 0;
            for (int i = 0; i < myProcess.Length; i++) {
                if(myProcess[i].MainWindowTitle.ToLower().Contains("note express")) {
                    iCount++;
                }
            }

            // If so open All Now
            //Console.WriteLine("Process: "+myProcess.Length);
            //Console.WriteLine("Process Note: "+iCount);

            if (iCount == 0 && classPreference.getOpenAllAtStart())
            {
                openAllNote(false);
                //Console.WriteLine("OPEN ALL: " + iCount);
            }

            // Do we add Date in note ?
            if (richTextBox1.TextLength == 0)
            {
                switch(classPreference.getNewNoteChoice()) {
                    case "Add Date-Time":
                        addStringAtCurrentSelection(getCurrentDateTime() + "\n");
                        break;

                    case "Add Date":
                        addStringAtCurrentSelection(getCurrentDateTime()+"\n");
                        break;

                    case "Add Title Date-Time":
                        addStringAtCurrentSelection("====================\n" + getCurrentDateTime() + "\n====================\n");
                        break;

                    default:
                        // Nothing to do
                        break;
                }
            }

            // handle the console show/hide
            var handle = GetConsoleWindow();

            // Hide the console
            ShowWindow(handle, SW_HIDE);

            // Show the console
            //ShowWindow(handle, SW_SHOW);


        }

        private void updateFunnyFact()
        {
            toolStripStatusLabel1.Text = "Note Express - " + getNextFunnyFact();
        }
        /**
         * MAKE SOME SHORTCUT BY HAND
         * */
        /*
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.N))
            {
                this.newFile();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
         **/

        /* GET FILE NAME */
        public String getFileName() {
            return sCurrentNamefile + ".txt";
        }

        /* GET PATH FILE NAME */
        public String getPathFile() {
            if (sOpenFile != "")
            {
                return sOpenFile;
            }
            else {
                return sUserDocPath + sDefaultPath + getFileName();
            }
        }

        private void SaveFile() {
            //using (File.Create(getPathFile()));
            if (richTextBox1.TextLength > 0)
            {
                richTextBox1.SaveFile(getPathFile(), RichTextBoxStreamType.PlainText);
            }
            else if (sOpenFile == "")
            {
                File.Delete(getPathFile());
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
            //Console.Write(sCurrentNamefile+"\n");
            //this.Dispose();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Dispose();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAbout formAbout = new FormAbout();
            formAbout.Show();
        }

        /**
         * Find the good path for the ProgramFiles x86 
         **/
        static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private void newFile() {
            System.Diagnostics.Process noteExpress = new System.Diagnostics.Process();
            noteExpress.StartInfo.FileName = ProgramFilesx86() + sDefaultPath + "noteexpress.exe";
            noteExpress.Start();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.newFile();
        }

        private void CloseAlreadyNoteTimer_Tick(object sender, EventArgs e)
        {
            List<String> list = new List<String>();
            var myProcess = Process.GetProcessesByName("NoteExpress");
            var myProcess2 = Process.GetProcessesByName("NoteExpress");
            for (int i = 0; i < myProcess.Length; i++)
            {
                for (int z = 0; z < myProcess2.Length; z++)
                {
                    if (myProcess[i].Id != myProcess2[z].Id && myProcess[i].MainWindowTitle == myProcess2[z].MainWindowTitle)
                    {
                        if (!list.Contains(myProcess[z].MainWindowTitle))
                        {
                            list.Add(myProcess[z].MainWindowTitle);
                            //Console.WriteLine("Closing: " + myProcess[z].MainWindowTitle);
                            myProcess[z].CloseMainWindow();
                        }
                    }
                }
            }

            CloseAlreadyNoteTimer.Stop();
            CloseAlreadyNoteTimer.Dispose();
            CloseAlreadyNoteTimer = null;
            if (File.Exists(getPathFile()))
            {
                this.Dispose(); // If this note was already saved close it
            }

        }

        private void SearchTextBoxTimer_Tick(object sender, EventArgs e)
        {
            //Console.WriteLine("The user finished typing.");

            //SearchTextBox_TextChanged();
            SaveFile();
            SearchTextBoxTimer.Stop();
            SearchTextBoxTimer.Dispose();
            SearchTextBoxTimer = null;
            iDelay = 500;
            toolStripStatusLabel1.Text = "Saving at " + getPathFile();
            outputTextInTitle();

        }

        private String getNextFunnyFact() {

            String[] a_sFunnyFac = {
                "Best express note never made. :P",
                "The power of a note, is to be keep it somewhere easy to find...",
                "Do or do with note, there is no try...",
                "Dont forget to save this note! To late I saved for you. XD",
                "Do you remember the first note you write?",
                "Remove the pain in the ice! Yes, ice! I dont want bad word here.",
                "Look around, your note is somewhere.",
                "Oh! It's you again! \\o/",
                "Wonderful, My note is there!",
                "Simple way, simple think!",
                "Notamment! You need me again.",
                "Hey! Don't forget to take note.",
                "You think... You write, I saved!",
                "Face the future, take a note.",
                "Bon appetit!",
                "POWER EXPRESS",
                "Better than an Expresso.",
                "Note today! Note tomorrow.",
                "Better than your wife... Na!",
                "Throw new... Note",
                "Take a little snack!",
                "Little thing matters!",
                "Search this note, where is it!"
            };

            Random rnd = new Random();
            iFunnyFac = rnd.Next(a_sFunnyFac.Length);

            if (iFunnyFac >= a_sFunnyFac.Length) {
                iFunnyFac = 0;
            }

            return a_sFunnyFac[iFunnyFac];
        }

        private void richTextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (SearchTextBoxTimer != null)
            {
                //Console.WriteLine("The user is currently typing. " + SearchTextBoxTimer.Interval);
                if (SearchTextBoxTimer.Interval < iDelay)
                {
                    SearchTextBoxTimer.Interval += iDelay;
                    iDelay = iDelay + 500;
                    //Console.WriteLine("Delaying..." + SearchTextBoxTimer.Interval);
                    updateFunnyFact();
                }
            }
            else
            {
                //Console.WriteLine("The user just started typing.");
                SearchTextBoxTimer = new System.Windows.Forms.Timer();
                SearchTextBoxTimer.Tick += new EventHandler(SearchTextBoxTimer_Tick);
                SearchTextBoxTimer.Interval = 500;
                SearchTextBoxTimer.Start();

                updateFunnyFact();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();

        }

        private void addFileToText(string file) {
            this.Text = this.sOpenFile + " - " + sProgramName;
            //Console.WriteLine(Encoding.Default.WebName);

            string[] a_readLines = File.ReadAllLines(file, Encoding.GetEncoding(Encoding.Default.WebName));
            this.richTextBox1.Lines = a_readLines;
            outputTextInTitle();
            
        }

        private void addFileToText(string[] files)
        {
            //Console.WriteLine("File name: "+files[0]);
            this.sOpenFile = files[0];
            this.Text = this.sOpenFile + " - " + sProgramName;
            //Console.WriteLine(Encoding.Default.WebName);

            string[] a_readLines = File.ReadAllLines(files[0], Encoding.GetEncoding(Encoding.Default.WebName));
            //Console.WriteLine("FirstLine: "+a_readLines[0]);
            this.richTextBox1.Lines = a_readLines;
            //this.richTextBox1.Select(this.richTextBox1.Text.Length - 1, 0);
            //this.richTextBox1.ScrollToCaret();

            outputTextInTitle();
        }

        private void outputTextInTitle() {

            // This function take the first 60 chars of the first line who have a starting letter

            String sOutput = "";
            foreach (string word in this.richTextBox1.Lines)
            {
                
                if (Regex.Matches(word, @"[a-zA-Z]").Count != 0) {
                    //Console.WriteLine(word.Substring(0, 1));
                    sOutput = word;
                    if (sOutput.Length > 60)
                    {
                    sOutput = word.Substring(0, 60); // Max 60 chars
                    }
                    break;
                }
            }

            this.Text = sOutput+ " - " + sProgramName;
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            //Console.WriteLine(openFileDialog1.FileNames);
            // Add files
            addFileToText(openFileDialog1.FileNames);

            // Reset value to default
            openFileDialog1.FileName = "*.*";
        }

        private String getFolderPath() {
            return sUserDocPath + sDefaultPath;
        }

        private void openAllNote(Boolean bCloseCurrent) {
            // Open All in the NoteExpress folder
            DirectoryInfo d = new DirectoryInfo(this.getFolderPath());
            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files

            foreach (FileInfo file in Files)
            {
                //Console.WriteLine(file.Name);
                System.Diagnostics.Process noteExpress = new System.Diagnostics.Process();
                noteExpress.StartInfo.FileName = ProgramFilesx86() + sDefaultPath + "noteexpress.exe";
                noteExpress.StartInfo.Arguments = " " + this.getFolderPath() + file.Name;
                noteExpress.Start();
            }

            CloseAlreadyNoteTimer = new System.Windows.Forms.Timer();
            CloseAlreadyNoteTimer.Tick += new EventHandler(CloseAlreadyNoteTimer_Tick);
            CloseAlreadyNoteTimer.Interval = 500;
            CloseAlreadyNoteTimer.Start();

            // We dispose the current note if nothing in
            if (richTextBox1.TextLength == 0 && bCloseCurrent)
            {
                this.Dispose();
            }
        }

        private void openAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open All in the NoteExpress folder
            openAllNote(true);
        }

        /**
          * Handle the click on Save As
          **/
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Console.WriteLine("Save as Completed");
                richTextBox1.SaveFile(saveFileDialog1.FileName, RichTextBoxStreamType.PlainText);
                toolStripStatusLabel1.Text = "Saving at " + saveFileDialog1.FileName;
            }
        }

        /**
         * This will remove all style when copy/paste in richtextbox.
         **/
        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.Control && e.KeyCode == Keys.V)
            {
         
                DataFormats.Format plaintext_format = DataFormats.GetFormat(DataFormats.Text);
                richTextBox1.Paste(plaintext_format);
                e.Handled = true;
            }
        }

        private void deleteExitToolStripMenuItem_Click(object sender, EventArgs e)
        {

            String sPathDelete = sOpenFile;
            if (sOpenFile == "")
            {
                sPathDelete = getPathFile();
            }

           
            // This will delete the current note and exit
            DialogResult result = MessageBox.Show(
                "Do you really want to delete this note '" + sPathDelete + "' ?",
                "WARNING",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation
            );
            if (result == DialogResult.Yes)
            {
                // We look for the file and delete it Then close
                File.Delete(sPathDelete);
             }
             
            this.Dispose();
           
        }

        private String getCurrentDateTime(Boolean bShowTime = true) { 
            var date1 = DateTime.Now;

            //TODO: Have a setting to get the date as the user what?
            // Maybe a dropdown menu with some formats

            if(bShowTime) {
                return date1.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return date1.ToString("yyyy-MM-dd");
        }

        private void addStringAtCurrentSelection(String sStr) {
            richTextBox1.SelectionStart += richTextBox1.SelectionLength;
            richTextBox1.SelectionLength = 0;
            richTextBox1.SelectedText = sStr;
        }

        private void addDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addStringAtCurrentSelection(getCurrentDateTime());
        }

        private void addDateToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            addStringAtCurrentSelection(getCurrentDateTime(false));
        }

        private void addTitleDateTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addStringAtCurrentSelection("====================\n" + getCurrentDateTime() + "\n====================\n");
        }

        
        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            // I've decided to remove this feature. Hate this one :P
            //open link with default application
            //Process.Start(e.LinkText);
        }
  
        private void Form1_Load(object sender, EventArgs e)
        {
            //at window load or at the constructor
            this.Activated += OnWindowActivated;
        }



        private void OnWindowActivated(object sender, EventArgs e)
        {
            // Call when we focused on NoteExpress
            updateFunnyFact();


        }

        private void focusAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process[] processName = Process.GetProcessesByName("NoteExpress");
            if (processName.Length != 0)
            {
                //Set foreground window
                for (int i = 0; i <processName.Length; i++ )
                {
                    IntPtr handle = processName[i].MainWindowHandle;
                    if (IsIconic(handle))
                    {
                        ShowWindow(handle, SW_RESTORE);
                    }

                    SetForegroundWindow(handle);
                }

            }
        }

        private void preferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: Open preference
            FormPreference formPreference = new FormPreference();
            formPreference.Show();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        
    }
}
