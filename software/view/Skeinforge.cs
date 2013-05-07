/*
   Copyright 2011 PiMaker PiMakerdev@gmail.com

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using PiMakerHost.view.utils;
using PiMakerHost.model;

namespace PiMakerHost.view
{
    public partial class Skeinforge : Form
    {
        public RegistryKey PiMakerKey;
        Process procSkein = null;
        Process procConvert = null;
        string slicefile = null;
        SkeinConfig profileConfig = null;
        SkeinConfig exportConfig = null;
        SkeinConfig extrusionConfig = null;
        SkeinConfig multiplyConfig = null;
        string name = "Skeinforge";

        public Skeinforge()
        {
            InitializeComponent();
            RegMemory.RestoreWindowPos("skeinforgeWindow", this);
            PiMakerKey = Custom.BaseKey; // Registry.CurrentUser.CreateSubKey("SOFTWARE\\PiMaker");
            regToForm();
            translate();
            if (BasicConfiguration.basicConf.SkeinforgeProfileDir.IndexOf("sfact") >= 0)
                name = "SFACT";
            else
                name = "Skeinforge";
            Main.main.languageChanged += translate;
        }
        private void translate()
        {
            Text = Trans.T("W_SKEIN_SETTINGS");
            labelApplication.Text = Trans.T("L_SKEIN_APPLICARTION");
            labelCraft.Text = Trans.T("L_SKEIN_CRAFT");
            labelProfdirInfo.Text = Trans.T("L_SKEIN_PROFDIR_INFO");
            labelProfilesDirectory.Text = Trans.T("L_SKEIN_PROFILES_DIRECTORY");
            labelPypy.Text = Trans.T("L_SKEIN_PYPY");
            labelPypyInfo.Text = Trans.T("L_SKEIN_PYPY_INFO");
            labelPython.Text = Trans.T("L_SKEIN_PYTHON");
            labelWorkdirInfo.Text = Trans.T("L_SKEIN_WORKDIR_INFO");
            labelWorkingDirectory.Text = Trans.T("L_SKEIN_WORKING_DIRECTORY");
            openFile.Title = Trans.T("L_SKEIN_OPEN_FILE");
            openPython.Title = Trans.T("L_SKEIN_OPEN_PYTHON");
            buttonAbort.Text = Trans.T("B_CANCEL");
            buttonOK.Text = Trans.T("B_OK");
            buttonSearchCraft.Text = Trans.T("B_BROWSE");
            buttonSerach.Text = Trans.T("B_BROWSE");
            buttonSerachPy.Text = Trans.T("B_BROWSE");
            buttonBrosePyPy.Text = Trans.T("B_BROWSE");
            buttonBrowseProfilesDir.Text = Trans.T("B_BROWSE");
            buttonBrowseWorkingDirectory.Text = Trans.T("B_BROWSE");
            
        }
        public string wrapQuotes(string text)
        {
            if (text.StartsWith("\"") && text.EndsWith("\"")) return text;
            return "\"" + text.Replace("\"", "\\\"") + "\"";
        }
        public void RestoreConfigs()
        {
            if (profileConfig != null)
                profileConfig.writeOriginal();
            if (exportConfig != null)
                exportConfig.writeOriginal();
            if (extrusionConfig != null)
                extrusionConfig.writeOriginal();
            if (multiplyConfig != null)
                multiplyConfig.writeOriginal();
            profileConfig = null;
            exportConfig = null;
            extrusionConfig = null;
            multiplyConfig = null;
        }
        public void RunSkeinforge()
        {
            if (procSkein != null)
            {
                return;
            }
            procSkein = new Process();
            try
            {
                procSkein.EnableRaisingEvents = true;
                procSkein.Exited += new EventHandler(SkeinExited);
                procSkein.StartInfo.FileName = Main.IsMono ? textPython.Text : wrapQuotes(textPython.Text);
                procSkein.StartInfo.Arguments = wrapQuotes(textSkeinforge.Text);
                procSkein.StartInfo.WorkingDirectory = textWorkingDirectory.Text;
                procSkein.StartInfo.UseShellExecute = false;
                procSkein.StartInfo.RedirectStandardOutput = true;
                procSkein.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
                procSkein.StartInfo.RedirectStandardError = true;
                procSkein.ErrorDataReceived += new DataReceivedEventHandler(OutputDataHandler);
                procSkein.Start();
                // Start the asynchronous read of the standard output stream.
                procSkein.BeginOutputReadLine();
                procSkein.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                Main.conn.log(e.ToString(), false, 2);
            }
        }
        public void KillSlice()
        {
            if (procConvert != null)
            {
                procConvert.Kill();
                procConvert = null;
                Main.conn.log(Trans.T1("L_SKEIN_KILLED",name),false,2); //"Skeinforge slicing process killed on user request.", false, 2);
                RestoreConfigs();
            }
        }
        public string PyPy
        {
            get
            {
                if (textPypy.Text.Length > 1) return textPypy.Text;
                return textPython.Text;
            }
        }
        public void RunSlice(string file, string profile)
        {
            if (procConvert != null)
            {
                MessageBox.Show(Trans.T("L_SKEIN_STILL_RUNNING") /*"Last slice job still running. Slicing of new job is canceled."*/,Trans.T("L_ERROR"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            profileConfig = new SkeinConfig(BasicConfiguration.basicConf.SkeinforgeProfileDir + Path.DirectorySeparatorChar + "skeinforge_profile.csv");
            extrusionConfig = new SkeinConfig(BasicConfiguration.basicConf.SkeinforgeProfileDir + Path.DirectorySeparatorChar + "extrusion.csv");
            exportConfig = new SkeinConfig(BasicConfiguration.basicConf.SkeinforgeProfileDir + Path.DirectorySeparatorChar + "extrusion" +
                Path.DirectorySeparatorChar + profile + Path.DirectorySeparatorChar + "export.csv");
            multiplyConfig = new SkeinConfig(BasicConfiguration.basicConf.SkeinforgeProfileDir + Path.DirectorySeparatorChar + "extrusion" +
                Path.DirectorySeparatorChar + profile + Path.DirectorySeparatorChar + "multiply.csv");
            // Set profile to extrusion
            /* cutting	False
extrusion	True
milling	False
winding	False
*/
            profileConfig.setValue("cutting", "False");
            profileConfig.setValue("milling", "False");
            profileConfig.setValue("extrusion", "True");
            profileConfig.setValue("winding", "False");
            profileConfig.writeModified();
            // Set used profile
            extrusionConfig.setValue("Profile Selection:", profile);
            extrusionConfig.writeModified();
            // Set export to correct values
            exportConfig.setValue("Activate Export", "True");
            exportConfig.setValue("Add Profile Extension", "False");
            exportConfig.setValue("Add Profile Name to Filename", "False");
            exportConfig.setValue("Add Timestamp Extension", "False");
            exportConfig.setValue("Add Timestamp to Filename", "False");
            exportConfig.setValue("Add Description to Filename", "False");
            exportConfig.setValue("Add Descriptive Extension", "False");
            exportConfig.writeModified();

            multiplyConfig.setValue("Activate Multiply:", "False");
            multiplyConfig.setValue("Activate Multiply: ", "False");
            multiplyConfig.setValue("Activate Multiply", "False");
            multiplyConfig.writeModified();

            string target = StlToGCode(file);
            if (File.Exists(target))
                File.Delete(target);
            procConvert = new Process();
            try
            {
                SlicingInfo.Start(name);
                SlicingInfo.SetAction(Trans.T("L_SLICING_STL_FILE...")); //"Slicing STL file ...");
                slicefile = file;
                procConvert.EnableRaisingEvents = true;
                procConvert.Exited += new EventHandler(ConversionExited);

                procConvert.StartInfo.FileName = Main.IsMono ? PyPy : wrapQuotes(PyPy);
                procConvert.StartInfo.Arguments = wrapQuotes(textSkeinforgeCraft.Text) + " " + wrapQuotes(file);
                procConvert.StartInfo.UseShellExecute = false;
                procConvert.StartInfo.WorkingDirectory = textWorkingDirectory.Text;
                procConvert.StartInfo.RedirectStandardOutput = true;
                procConvert.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
                procConvert.StartInfo.RedirectStandardError = true;
                procConvert.ErrorDataReceived += new DataReceivedEventHandler(OutputDataHandler);
                procConvert.Start();
                // Start the asynchronous read of the standard output stream.
                procConvert.BeginOutputReadLine();
                procConvert.BeginErrorReadLine();
                //Main.main.tab.SelectedTab = Main.main.tabPrint;
            }
            catch (Exception e)
            {
                Main.conn.log(e.ToString(), false, 2);
                RestoreConfigs();
            }
        }
        public delegate void LoadGCode(String myString);
        private void ConversionExited(object sender, System.EventArgs e)
        {
            if (procConvert == null) return;
            try
            {
                procConvert.Close();
                procConvert = null;
                string gcodefile = StlToGCode(slicefile);
                Main.slicer.Postprocess(gcodefile);
                RestoreConfigs();
            }
            catch { }
        }
        private void SkeinExited(object sender, System.EventArgs e)
        {
            procSkein.Close();
            procSkein = null;
            Main.main.Invoke(Main.main.slicerPanel.UpdateSelectionInvoker);
        }
        private static void OutputDataHandler(object sendingProcess,
            DataReceivedEventArgs outLine)
        {
            // Collect the net view command output.
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                string[] lines = outLine.Data.Split((char)0x0d);
                foreach (string l in lines)
                    Main.conn.log("<"+Main.main.skeinforge.name+"> " + l, false, 4);
            }
        }

        public string StlToGCode(string stl)
        {
            int p = stl.LastIndexOf('.');
            if (p > 0) stl = stl.Substring(0, p);
            string extension = exportConfig.getValue("File Extension:");
            if (extension == null)
                extension = exportConfig.getValue("File Extension (gcode):");
            string export = exportConfig.getValue("Add Export Suffix");
            if (export == null)
                export = exportConfig.getValue("Add _export to filename (filename_export)");
            if (export == null || export != "True") export = ""; else export = "_export";
            return stl + export + "." + extension;
        }
        private void regToForm()
        {

            textSkeinforge.Text = (string)PiMakerKey.GetValue("SkeinforgePath", textSkeinforge.Text);
            textSkeinforgeCraft.Text = (string)PiMakerKey.GetValue("SkeinforgeCraftPath", textSkeinforgeCraft.Text);
            textPython.Text = (string)PiMakerKey.GetValue("SkeinforgePython", textPython.Text);
            textPypy.Text = (string)PiMakerKey.GetValue("SkeinforgePypy", textPypy.Text);
            //textExtension.Text = (string)PiMakerKey.GetValue("SkeinforgeExtension", textExtension.Text);
            //textPostfix.Text = (string)PiMakerKey.GetValue("SkeinforgePostfix", textPostfix.Text);
            textWorkingDirectory.Text = (string)PiMakerKey.GetValue("SkeinforgeWorkdir", textWorkingDirectory.Text);
            textProfilesDir.Text = BasicConfiguration.basicConf.SkeinforgeProfileDir;
        }
        private void FormToReg()
        {
            BasicConfiguration.basicConf.SkeinforgeProfileDir = textProfilesDir.Text;
            PiMakerKey.SetValue("SkeinforgePath", textSkeinforge.Text);
            PiMakerKey.SetValue("SkeinforgeCraftPath", textSkeinforgeCraft.Text);
            PiMakerKey.SetValue("SkeinforgePython", textPython.Text);
            PiMakerKey.SetValue("SkeinforgePypy", textPypy.Text);
            //PiMakerKey.SetValue("SkeinforgeExtension", textExtension.Text);
            //PiMakerKey.SetValue("SkeinforgePostfix", textPostfix.Text);
            PiMakerKey.SetValue("SkeinforgeWorkdir", textWorkingDirectory.Text);
        }
        private void buttonAbort_Click(object sender, EventArgs e)
        {
            regToForm();
            Hide();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            FormToReg();
            Hide();
            if (BasicConfiguration.basicConf.SkeinforgeProfileDir.IndexOf("sfact") >= 0)
                name = "SFACT";
            else
                name = "Skeinforge";
            Main.main.languageChanged += translate;
            Main.slicer.Update();
            Main.main.slicerPanel.UpdateSelection();
        }

        private void buttonSerach_Click(object sender, EventArgs e)
        {
            openFile.Title = Trans.T("L_SKEIN_OPEN_FILE");
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                textSkeinforge.Text = openFile.FileName;
            }
        }

        private void buttonSearchCraft_Click(object sender, EventArgs e)
        {
            openFile.Title = Trans.T("L_SKEIN_OPEN_CRAFT");
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                textSkeinforgeCraft.Text = openFile.FileName;
            }
        }

        private void buttonSerachPy_Click(object sender, EventArgs e)
        {
            openPython.Title = Trans.T("L_SKEIN_OPEN_PYTHON");
            if (openPython.ShowDialog() == DialogResult.OK)
                textPython.Text = openPython.FileName;
        }

        private void Skeinforge_FormClosing(object sender, FormClosingEventArgs e)
        {
            RegMemory.StoreWindowPos("skeinforgeWindow", this, false, false);
        }

        private void buttonBrowseWorkingDirectory_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.Description = Trans.T("L_SKEIN_SELECT_WORKING_FOLDER");
            folderBrowserDialog.SelectedPath = textWorkingDirectory.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                textWorkingDirectory.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void buttonBrowseProfilesDir_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.Description = Trans.T("L_SKEIN_SELECT_PROFILE_FOLDER");
            folderBrowserDialog.SelectedPath = textProfilesDir.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                textProfilesDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void buttonBrosePyPy_Click(object sender, EventArgs e)
        {
            openPython.Title = Trans.T("L_SKEIN_OPEN_PYPY");
            if (openPython.ShowDialog() == DialogResult.OK)
                textPypy.Text = openPython.FileName;
        }
    }
}
