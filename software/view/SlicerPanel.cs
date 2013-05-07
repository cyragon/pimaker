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
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using PiMakerHost.model;
using PiMakerHost.view.utils;

namespace PiMakerHost.view
{
    public partial class SlicerPanel : UserControl
    {
        bool updating = false;
        public SlicerPanel()
        {
            InitializeComponent();
            //UpdateSelection();
            if (Main.main != null)
            {
                Main.main.languageChanged += translate;
                translate();
            }
        }
        void translate() {
            buttonKillSlicing.Text = Trans.T("B_KILL_SLICING_PROCESS");
            buttonSetupSkeinforge.Text = Trans.T("B_SETUP_SKEINFORGE");
            buttonSetupSlic3r.Text = Trans.T("B_SETUP_SLIC3R");
            buttonSkeinConfigure.Text = Trans.T("B_CONFIGURE_SKEINFORGE");
            buttonSlic3rConfigure.Text = Trans.T("B_CONFIGURE_SLIC3R");
            switchSkeinforge.TextOn = switchSkeinforge.TextOff = Trans.T("B_ACTIVE");
            switchSlic3rActive.TextOn = switchSlic3rActive.TextOff = Trans.T("B_ACTIVE");
            labelFilamentSettings.Text = Trans.T("L_FILAMENT_SETTINGS");
            labelPrinterSettings.Text = Trans.T("L_PRINTER_SETTINGS");
            labelPrintSettings.Text = Trans.T("L_PRINT_SETTINGS");
            labelProfile.Text = Trans.T("L_PROFILE");
            labelSlic3rExtruder1.Text = Trans.T1("L_EXTRUDER_X:", "1");
            labelSlic3rExtruder2.Text = Trans.T1("L_EXTRUDER_X:", "2");
            labelSlic3rExtruder3.Text = Trans.T1("L_EXTRUDER_X:", "3");
            if(Main.slicer!=null)
                buttonStartSlicing.Text = Trans.T1("L_SLICE_WITH", Main.slicer.SlicerName);
        }
        private string noINI(string ini)
        {
            if (ini.EndsWith(".ini"))
                return ini.Substring(0, ini.Length - 4);
            return ini;
        }
        public string slic3rDirectory
        {
            get
            {
                BasicConfiguration b = BasicConfiguration.basicConf;
                string cdir = b.Slic3rConfigDir;
                if (cdir.Trim().Length == 0)
                {
                    if (Main.IsMono)
                    {
                        cdir =  System.Environment.GetEnvironmentVariable("HOME")+"/.Slic3r";
                        //Console.WriteLine("Slic3r home:" + cdir);
                        return cdir;
                        //return "~/.Slic3r";
                        //cdir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    }
                    else
                    {
                        cdir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    }
                }
                //Console.WriteLine("App dir:" + cdir);
                return cdir + Path.DirectorySeparatorChar + "Slic3r";
            }
        }
        public MethodInvoker UpdateSelectionInvoker = delegate
        {
            Main.main.slicerPanel.UpdateSelection();
        };
        public void UpdateSelection()
        {
            BasicConfiguration b = BasicConfiguration.basicConf;
            string slic3rConf = slic3rDirectory;
            updating = true;
            // Printer folder
            string printerFolder = slic3rConf + Path.DirectorySeparatorChar + "printer";
            DirectoryInfo di = new DirectoryInfo(printerFolder);
            if (di.Exists)
            {
                FileInfo[] rgFiles = di.GetFiles("*.ini");
                comboSlic3rPrinterSettings.Items.Clear();
                foreach (FileInfo fi in rgFiles)
                {
                    comboSlic3rPrinterSettings.Items.Add(noINI(fi.Name));
                }
                comboSlic3rPrinterSettings.Enabled = true;
                if (b.Slic3rPrinterSettings.Length > 0)
                    comboSlic3rPrinterSettings.SelectedItem = b.Slic3rPrinterSettings;
                if(comboSlic3rPrinterSettings.SelectedIndex<0 && rgFiles.Count() > 0) {
                    b.Slic3rPrinterSettings = noINI(rgFiles[0].Name);
                    comboSlic3rPrinterSettings.SelectedIndex = 0;
                }
            }
            else
            {
                comboSlic3rPrinterSettings.Enabled = false;
            }
            string printFolder = slic3rConf + Path.DirectorySeparatorChar + "print";
            di = new DirectoryInfo(printFolder);
            if (di.Exists)
            {
                FileInfo[] rgFiles = di.GetFiles("*.ini");
                comboSlic3rPrintSettings.Items.Clear();
                foreach (FileInfo fi in rgFiles)
                {
                    comboSlic3rPrintSettings.Items.Add(noINI(fi.Name));
                }
                comboSlic3rPrintSettings.Enabled = true;
                if (b.Slic3rPrintSettings.Length > 0)
                    comboSlic3rPrintSettings.SelectedItem = b.Slic3rPrintSettings;
                if (comboSlic3rPrintSettings.SelectedIndex<0 && rgFiles.Count() > 0)
                {
                    b.Slic3rPrintSettings = noINI(rgFiles[0].Name);
                    comboSlic3rPrintSettings.SelectedIndex = 0;
                }
            }
            else
            {
                comboSlic3rPrintSettings.Enabled = false;
            }
            string filamentFolder = slic3rConf + Path.DirectorySeparatorChar + "filament";
            di = new DirectoryInfo(filamentFolder);
            if (di.Exists)
            {
                FileInfo[] rgFiles = di.GetFiles("*.ini");
                comboSlic3rFilamentSettings.Items.Clear();
                comboSlic3rFilamentSettings2.Items.Clear();
                comboSlic3rFilamentSettings3.Items.Clear();
                // L_FILAMENT_NONE
                //comboSlic3rFilamentSettings2.Items.Add(Trans.T("L_FILAMENT_NONE"));
                //comboSlic3rFilamentSettings3.Items.Add(Trans.T("L_FILAMENT_NONE"));
                foreach (FileInfo fi in rgFiles)
                {
                    comboSlic3rFilamentSettings.Items.Add(noINI(fi.Name));
                    comboSlic3rFilamentSettings2.Items.Add(noINI(fi.Name));
                    comboSlic3rFilamentSettings3.Items.Add(noINI(fi.Name));
                }
                comboSlic3rFilamentSettings.Enabled = true;
                comboSlic3rFilamentSettings2.Enabled = true;
                comboSlic3rFilamentSettings3.Enabled = true;
                if (b.Slic3rFilamentSettings.Length > 0)
                    comboSlic3rFilamentSettings.SelectedItem = b.Slic3rFilamentSettings;
                if (comboSlic3rFilamentSettings.SelectedIndex<0 && rgFiles.Count() > 0)
                {
                    b.Slic3rFilamentSettings = noINI(rgFiles[0].Name);
                    comboSlic3rFilamentSettings.SelectedIndex = 0;
                }

                if (b.Slic3rFilament2Settings.Length > 0)
                    comboSlic3rFilamentSettings2.SelectedItem = b.Slic3rFilament2Settings;
                if (comboSlic3rFilamentSettings2.SelectedIndex < 0 && rgFiles.Count() > 0)
                {
                    b.Slic3rFilament2Settings = noINI(rgFiles[0].Name);
                    comboSlic3rFilamentSettings2.SelectedIndex = 0;
                }

                if (b.Slic3rFilament3Settings.Length > 0)
                    comboSlic3rFilamentSettings3.SelectedItem = b.Slic3rFilament3Settings;
                if (comboSlic3rFilamentSettings3.SelectedIndex < 0 && rgFiles.Count() > 0)
                {
                    b.Slic3rFilament3Settings = noINI(rgFiles[0].Name);
                    comboSlic3rFilamentSettings3.SelectedIndex = 0;
                }
            }
            else
            {
                comboSlic3rFilamentSettings.Enabled = false;
            }
            // Skeinforge selection
            string skeinProfFolder = b.SkeinforgeProfileDir + Path.DirectorySeparatorChar + "extrusion";
            di = new DirectoryInfo(skeinProfFolder);
            if (di.Exists)
            {
                DirectoryInfo[] rgFiles = di.GetDirectories();
                comboSkeinProfile.Items.Clear();
                foreach (DirectoryInfo fi in rgFiles)
                {
                    comboSkeinProfile.Items.Add(fi.Name);
                }
                comboSkeinProfile.Enabled = true;
                if (b.SkeinforgeProfile.Length > 0)
                    comboSkeinProfile.SelectedItem = b.SkeinforgeProfile;
                if (comboSkeinProfile.SelectedIndex<0 && rgFiles.Count() > 0)
                {
                    b.SkeinforgeProfile = rgFiles[0].Name;
                    comboSkeinProfile.SelectedIndex = 0;
                }
            }
            else
            {
                comboSkeinProfile.Enabled = false;
            }


            if (Main.slicer.ActiveSlicer == Slicer.SlicerID.Slic3r)
            {
                switchSlic3rActive.On = true;
                switchSkeinforge.On = false;
                //buttonStartSlicing.Text = "Slice with Slic3r\r\n\r\nPrinter = " + b.Slic3rPrinterSettings + "\r\nFilament = " + b.Slic3rFilamentSettings + "\r\nPrint = " + b.Slic3rPrintSettings;
            }
            else if (Main.slicer.ActiveSlicer == Slicer.SlicerID.Skeinforge)
            {
                switchSlic3rActive.On = false;
                switchSkeinforge.On = true;
                //buttonStartSlicing.Text = "Slice with Skeinforge\r\n\r\nProfile = " + b.SkeinforgeProfile;
            }
            buttonStartSlicing.Text = Trans.T1("L_SLICE_WITH", Main.slicer.SlicerName);
            if (BasicConfiguration.basicConf.SkeinforgeProfileDir.IndexOf("sfact") >= 0)
                groupSkeinforge.Text = "SFACT";
            else groupSkeinforge.Text = "Skeinforge";
            updating = false;
        }

        private void comboSlic3rPrintSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating)
            {
                BasicConfiguration.basicConf.Slic3rPrintSettings = (string)comboSlic3rPrintSettings.SelectedItem;
                UpdateSelection();
            }
        }

        private void comboSlic3rFilamentSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating)
            {
                BasicConfiguration.basicConf.Slic3rFilamentSettings = (string)comboSlic3rFilamentSettings.SelectedItem;
                UpdateSelection();
            }
        }

        private void comboSlic3rPrinterSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating)
            {
                BasicConfiguration.basicConf.Slic3rPrinterSettings = (string)comboSlic3rPrinterSettings.SelectedItem;
                UpdateSelection();
            }
        }

        private void buttonSlic3rConfigure_Click(object sender, EventArgs e)
        {
            Main.slic3r.RunConfig();

        }

        private void buttonSkeinConfigure_Click(object sender, EventArgs e)
        {
            Main.main.skeinforge.RunSkeinforge();
        }

        
        private void switchSlic3rActive_OnChange(SwitchButton button)
        {
            if (updating) return;
                Main.slicer.ActiveSlicer = Slicer.SlicerID.Slic3r;
        }

        private void switchSkeinforge_OnChange(SwitchButton button)
        {
            if (updating) return;
            Main.slicer.ActiveSlicer = Slicer.SlicerID.Skeinforge;
        }

        private void buttonStartSlicing_Click(object sender, EventArgs e)
        {
            Main.main.stlComposer1.buttonSlice_Click(null, null);
        }

        private void comboSkeinProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (updating) return;
            BasicConfiguration.basicConf.SkeinforgeProfile = (string)comboSkeinProfile.SelectedItem;
            UpdateSelection();
        }

        private void buttonSetupSlic3r_Click(object sender, EventArgs e)
        {
            Slic3rSetup.Execute();
        }

        private void buttonSetupSkeinforge_Click(object sender, EventArgs e)
        {
            Main.main.skeinforge.Show();
            Main.main.skeinforge.BringToFront();
        }

        private void buttonKillSlicing_Click(object sender, EventArgs e)
        {
            Main.main.skeinforge.KillSlice();
            Main.slic3r.KillSlice();
            SlicingInfo.Stop();
        }

        private void comboSlic3rFilamentSettings2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating)
            {
                BasicConfiguration.basicConf.Slic3rFilament2Settings = (string)comboSlic3rFilamentSettings2.SelectedItem;
                UpdateSelection();
            }
        }

        private void comboSlic3rFilamentSettings3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updating)
            {
                BasicConfiguration.basicConf.Slic3rFilament3Settings = (string)comboSlic3rFilamentSettings3.SelectedItem;
                UpdateSelection();
            }
        }
    }
}
