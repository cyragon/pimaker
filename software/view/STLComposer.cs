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
using System.Globalization;
using System.IO;
using OpenTK;
using PiMakerHost.model;

namespace PiMakerHost.view
{
    public partial class STLComposer : UserControl
    {
        public ThreeDView cont;
        private bool autosizeFailed = false;
        private CopyObjectsDialog copyDialog = new CopyObjectsDialog();
        public STLComposer()
        {
            InitializeComponent();
            try
            {
                cont = new ThreeDView();
              //  cont.Dock = DockStyle.None;
              //  cont.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
              //  cont.Width = Width - panelControls.Width;
              //  cont.Height = Height;
              //  Controls.Add(cont);
                cont.SetEditor(true);
                cont.objectsSelected = false;
                cont.eventObjectMoved += objectMoved;
                cont.eventObjectSelected += objectSelected;
                cont.autoupdateable = true;
                updateEnabled();
                if (Main.main != null)
                {
                    Main.main.languageChanged += translate;
                    translate();
                }
            }
            catch { }
        }
        public void translate()
        {
            labelTranslation.Text = Trans.T("L_TRANSLATION:");
            labelScale.Text = Trans.T("L_SCALE:");
            labelRotate.Text = Trans.T("L_ROTATE:");
            labelSTLObjects.Text = Trans.T("L_STL_OBJECTS");
            buttonSave.Text = Trans.T("B_SAVE_AS_STL");
            buttonRemoveSTL.Text = Trans.T("B_REMOVE_STL_OBJECT");
            buttonAddSTL.Text = Trans.T("B_ADD_STL_OBJECT");
            buttonAutoplace.Text = Trans.T("B_AUTOPOSITION");
            buttonLand.Text = Trans.T("B_DROP_OBJECT");
            buttonCopyObjects.Text = Trans.T("B_COPY_OBJECTS");
            buttonCenter.Text = Trans.T("B_CENTER_OBJECT");
            checkScaleAll.Text = Trans.T("L_LOCK_ASPECT_RATIO");
            if(Main.slicer!=null)
                buttonSlice.Text = Trans.T1("L_SLICE_WITH", Main.slicer.SlicerName);
        }
        public void Update3D()
        {
            Main.main.threedview.UpdateChanges();
        }
        private void float_Validating(object sender, CancelEventArgs e)
        {
            TextBox box = (TextBox)sender;
            try
            {
                float.Parse(box.Text);
                errorProvider.SetError(box, "");
            }
            catch
            {
                errorProvider.SetError(box, "Not a number.");
            }
        }
        private void updateEnabled()
        {
            int n = listSTLObjects.SelectedItems.Count;
            if (n != 1)
            {
                textRotX.Enabled = false;
                textRotY.Enabled = false;
                textRotZ.Enabled = false;
                textScaleX.Enabled = false;
                textScaleY.Enabled = false;
                textScaleZ.Enabled = false;
                checkScaleAll.Enabled = false;
                textTransX.Enabled = false;
                textTransY.Enabled = false;
                textTransZ.Enabled = false;
                buttonCenter.Enabled = false;
                buttonAutoplace.Enabled = listSTLObjects.Items.Count > 1;
                buttonLand.Enabled = n > 0;
                if(Main.main.threedview!=null)
                    Main.main.threedview.SetObjectSelected(n > 0);
                buttonCopyObjects.Enabled = n>0;
            }
            else
            {
                buttonAutoplace.Enabled = listSTLObjects.Items.Count > 1;
                buttonCopyObjects.Enabled = true;
                textRotX.Enabled = true;
                textRotY.Enabled = true;
                textRotZ.Enabled = true;
                textScaleX.Enabled = true;
                textScaleY.Enabled = !checkScaleAll.Checked;
                textScaleZ.Enabled = !checkScaleAll.Checked;
                checkScaleAll.Enabled = true;
                textTransX.Enabled = true;
                textTransY.Enabled = true;
                textTransZ.Enabled = true;
                buttonCenter.Enabled = true;
                buttonLand.Enabled = true;
                if (Main.main.threedview != null)
                    Main.main.threedview.SetObjectSelected(true);
            }
            buttonRemoveSTL.Enabled = n!=0;
            buttonSlice.Enabled = listSTLObjects.Items.Count > 0;
            buttonSave.Enabled = listSTLObjects.Items.Count > 0;
        }
        public void openAndAddObject(string file)
        {
            STL stl = new STL();
            stl.Load(file);
            stl.Center(Main.printerSettings.PrintAreaWidth / 2, Main.printerSettings.PrintAreaDepth / 2);
            stl.Land();
            if (stl.list.Count > 0)
            {
                listSTLObjects.Items.Add(stl);
                cont.models.AddLast(stl);
                listSTLObjects.SelectedItem = stl;
                Autoposition();
                stl.addAnimation(new DropAnimation("drop"));
                updateSTLState(stl);
            }
        }
        private void buttonAddSTL_Click(object sender, EventArgs e)
        {
            if (openFileSTL.ShowDialog() == DialogResult.OK)
            {
                foreach(string fname in openFileSTL.FileNames)
                openAndAddObject(fname);
            }
        }
        /// <summary>
        /// Checks the state of the object.
        /// If it is outside print are it starts pulsing
        /// </summary>
        public void updateSTLState(STL stl)
        {
            FormPrinterSettings ps = Main.printerSettings;
            stl.UpdateBoundingBox();
            if (stl.xMin < ps.BedLeft || stl.yMin < ps.BedFront || stl.zMin < -0.001 || stl.xMax > ps.BedLeft+Main.printerSettings.PrintAreaWidth ||
                stl.yMax > ps.BedFront+Main.printerSettings.PrintAreaDepth || stl.zMax > Main.printerSettings.PrintAreaHeight)
            {
                stl.outside = true;
                if (Main.threeDSettings.pulseOutside.Checked && !stl.hasAnimationWithName("pulse"))
                    stl.addAnimation(new PulseAnimation("pulse", 0.03, 0.03, 0.03, 0.3));
            }
            else
            {
                stl.outside = false;
                stl.removeAnimationWithName("pulse");
            }
        }
        private void listSTLObjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateEnabled();
            STL stl = (STL)listSTLObjects.SelectedItem;
            foreach (STL s in cont.models)
            {
                s.Selected = listSTLObjects.SelectedItems.Contains(s);
            }
            if (listSTLObjects.SelectedItems.Count > 1) stl = null;
            if (stl != null)
            {
                textRotX.Text = stl.Rotation.x.ToString(GCode.format);
                textRotY.Text = stl.Rotation.y.ToString(GCode.format);
                textRotZ.Text = stl.Rotation.z.ToString(GCode.format);
                textScaleX.Text = stl.Scale.x.ToString(GCode.format);
                textScaleY.Text = stl.Scale.y.ToString(GCode.format);
                textScaleZ.Text = stl.Scale.z.ToString(GCode.format);
                textTransX.Text = stl.Position.x.ToString(GCode.format);
                textTransY.Text = stl.Position.y.ToString(GCode.format);
                textTransZ.Text = stl.Position.z.ToString(GCode.format);
                checkScaleAll.Checked = (stl.Scale.x == stl.Scale.y && stl.Scale.x == stl.Scale.z);
            }
            Main.main.threedview.UpdateChanges();
        }

        private void textTransX_TextChanged(object sender, EventArgs e)
        {
            STL stl = (STL)listSTLObjects.SelectedItem;
            if (stl == null) return;
            float.TryParse(textTransX.Text, NumberStyles.Float, GCode.format, out stl.Position.x);
            updateSTLState(stl);
            Main.main.threedview.UpdateChanges();
        }

        private void textTransY_TextChanged(object sender, EventArgs e)
        {
            STL stl = (STL)listSTLObjects.SelectedItem;
            if (stl == null) return;
            float.TryParse(textTransY.Text, NumberStyles.Float, GCode.format, out stl.Position.y);
            updateSTLState(stl);
            Main.main.threedview.UpdateChanges();
        }

        private void textTransZ_TextChanged(object sender, EventArgs e)
        {
            STL stl = (STL)listSTLObjects.SelectedItem;
            if (stl == null) return;
            float.TryParse(textTransZ.Text, NumberStyles.Float, GCode.format, out stl.Position.z);
            updateSTLState(stl);
            Main.main.threedview.UpdateChanges();
        }
        private void objectMoved(float dx, float dy)
        {
            //STL stl = (STL)listSTLObjects.SelectedItem;
            //if (stl == null) return;
            foreach (STL stl in listSTLObjects.SelectedItems)
            {
                stl.Position.x += dx;
                stl.Position.y += dy;
                if (listSTLObjects.SelectedItems.Count == 1)
                {
                    textTransX.Text = stl.Position.x.ToString(GCode.format);
                    textTransY.Text = stl.Position.y.ToString(GCode.format);
                }
                updateSTLState(stl);
            }
            Main.main.threedview.UpdateChanges();
        }
        private void objectSelected(ThreeDModel sel)
        {
            if (Control.ModifierKeys == Keys.Shift)
            {
                if (!sel.Selected)
                    listSTLObjects.SelectedItems.Add(sel);
            }
            else
                if (Control.ModifierKeys == Keys.Control)
                {
                    if (sel.Selected)
                        listSTLObjects.SelectedItems.Remove(sel);
                    else
                        listSTLObjects.SelectedItems.Add(sel);
                }
                else
                {
                    listSTLObjects.SelectedItems.Clear();
                    listSTLObjects.SelectedItem = sel;
                }
        }
        private void textScaleX_TextChanged(object sender, EventArgs e)
        {
            STL stl = (STL)listSTLObjects.SelectedItem;
            if (stl == null) return;
            float.TryParse(textScaleX.Text, NumberStyles.Float, GCode.format, out stl.Scale.x);
            if (checkScaleAll.Checked)
            {
                stl.Scale.y = stl.Scale.z = stl.Scale.x;
                textScaleY.Text = stl.Scale.y.ToString(GCode.format);
                textScaleZ.Text = stl.Scale.z.ToString(GCode.format);
            }
            updateSTLState(stl);
            Main.main.threedview.UpdateChanges();
        }

        private void textScaleY_TextChanged(object sender, EventArgs e)
        {
            STL stl = (STL)listSTLObjects.SelectedItem;
            if (stl == null) return;
            float.TryParse(textScaleY.Text, NumberStyles.Float, GCode.format, out stl.Scale.y);
            updateSTLState(stl);
            Main.main.threedview.UpdateChanges();
        }

        private void textScaleZ_TextChanged(object sender, EventArgs e)
        {
            STL stl = (STL)listSTLObjects.SelectedItem;
            if (stl == null) return;
            float.TryParse(textScaleZ.Text, NumberStyles.Float, GCode.format, out stl.Scale.z);
            updateSTLState(stl);
            Main.main.threedview.UpdateChanges();
        }

        private void textRotX_TextChanged(object sender, EventArgs e)
        {
            STL stl = (STL)listSTLObjects.SelectedItem;
            if (stl == null) return;
            float.TryParse(textRotX.Text, NumberStyles.Float, GCode.format, out stl.Rotation.x);
            updateSTLState(stl);
            Main.main.threedview.UpdateChanges();
        }

        private void textRotY_TextChanged(object sender, EventArgs e)
        {
            STL stl = (STL)listSTLObjects.SelectedItem;
            if (stl == null) return;
            float.TryParse(textRotY.Text, NumberStyles.Float, GCode.format, out stl.Rotation.y);
            updateSTLState(stl);
            Main.main.threedview.UpdateChanges();
        }

        private void textRotZ_TextChanged(object sender, EventArgs e)
        {
            STL stl = (STL)listSTLObjects.SelectedItem;
            if (stl == null) return;
            float.TryParse(textRotZ.Text, NumberStyles.Float, GCode.format, out stl.Rotation.z);
            updateSTLState(stl);
            Main.main.threedview.UpdateChanges();
        }

        public void buttonRemoveSTL_Click(object sender, EventArgs e)
        {
            //STL stl = (STL)listSTLObjects.SelectedItem;
            //if (stl == null) return;
            LinkedList<STL> list = new LinkedList<STL>();
            foreach (STL stl in listSTLObjects.SelectedItems)
                list.AddLast(stl);
            foreach (STL stl in list)
            {
                cont.models.Remove(stl);
                listSTLObjects.Items.Remove(stl);
                autosizeFailed = false; // Reset autoposition
            }
            list.Clear();
            if (listSTLObjects.Items.Count > 0)
                listSTLObjects.SelectedIndex = 0;
            Main.main.threedview.UpdateChanges();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (saveSTL.ShowDialog() == DialogResult.OK)
            {
                saveComposition(saveSTL.FileName);
            }
        }
        private void saveComposition(string fname)
        {
            int n = 0;
            foreach (STL stl in listSTLObjects.Items)
            {
                n += stl.list.Count;
            }
            STLTriangle[] triList = new STLTriangle[n];
            int p = 0;
            foreach (STL stl in listSTLObjects.Items)
            {
                stl.UpdateMatrix();
                foreach (STLTriangle t2 in stl.list)
                {
                    STLTriangle t = new STLTriangle();
                    t.p1 = new Vector3();
                    t.p2 = new Vector3();
                    t.p3 = new Vector3();
                    t.normal = new Vector3();
                    stl.TransformPoint(ref t2.p1, out t.p1.X, out t.p1.Y, out t.p1.Z);
                    stl.TransformPoint(ref t2.p2, out t.p2.X, out t.p2.Y, out t.p2.Z);
                    stl.TransformPoint(ref t2.p3, out t.p3.X, out t.p3.Y, out t.p3.Z);
                    // Compute normal from p1-p3
                    float ax = t.p2.X - t.p1.X;
                    float ay = t.p2.Y - t.p1.Y;
                    float az = t.p2.Z - t.p1.Z;
                    float bx = t.p3.X - t.p1.X;
                    float by = t.p3.Y - t.p1.Y;
                    float bz = t.p3.Z - t.p1.Z;
                    t.normal.X = ay * bz - az * by;
                    t.normal.Y = az * bx - ax * bz;
                    t.normal.Z = ax * by - ay * bx;
                    Vector3.Normalize(ref t.normal, out t.normal);
                    triList[p++] = t;
                }
            }
            // STL should have increasing z for faster slicing
            Array.Sort<STLTriangle>(triList, triList[0]);
            // Write file in binary STL format
            FileStream fs = File.Open(fname, FileMode.Create);
            BinaryWriter w = new BinaryWriter(fs);
            int i;
            for (i = 0; i < 20; i++) w.Write((int)0);
            w.Write(n);
            for (i = 0; i < n; i++)
            {
                STLTriangle t = triList[i];
                w.Write(t.normal.X);
                w.Write(t.normal.Y);
                w.Write(t.normal.Z);
                w.Write(t.p1.X);
                w.Write(t.p1.Y);
                w.Write(t.p1.Z);
                w.Write(t.p2.X);
                w.Write(t.p2.Y);
                w.Write(t.p2.Z);
                w.Write(t.p3.X);
                w.Write(t.p3.Y);
                w.Write(t.p3.Z);
                w.Write((short)0);
            }
            w.Close();
            fs.Close();
        }

        private void buttonLand_Click(object sender, EventArgs e)
        {
            //STL stl = (STL)listSTLObjects.SelectedItem;
            //if (stl == null) return;
            foreach (STL stl in listSTLObjects.SelectedItems)
            {
                stl.Land();
                listSTLObjects_SelectedIndexChanged(null, null);
            }
            Main.main.threedview.UpdateChanges();
        }

        public void buttonCenter_Click(object sender, EventArgs e)
        {
            //STL stl = (STL)listSTLObjects.SelectedItem;
            //if (stl == null) return;
            foreach (STL stl in listSTLObjects.SelectedItems)
            {
                stl.Center(Main.printerSettings.BedLeft + Main.printerSettings.PrintAreaWidth / 2, Main.printerSettings.BedFront+ Main.printerSettings.PrintAreaDepth / 2);
                listSTLObjects_SelectedIndexChanged(null, null);

            }
            Main.main.threedview.UpdateChanges();
        }

        public void buttonSlice_Click(object sender, EventArgs e)
        {
            string dir = Main.globalSettings.Workdir;
            if (!Directory.Exists(dir))
            {
                MessageBox.Show("Workdir does not exists. Slicing aborted.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Main.globalSettings.Show();
                return;
            }
            if (listSTLObjects.Items.Count == 0) return;
            string t = listSTLObjects.Items[0].ToString();
            if (listSTLObjects.Items.Count > 1)
                t += " + " + (listSTLObjects.Items.Count - 1).ToString();
            Main.main.Title = t;
            dir += Path.DirectorySeparatorChar + "composition.stl";
            saveComposition(dir);
            Main.slicer.RunSlice(dir); // Slice it and load
        }

        private void checkScaleAll_CheckedChanged(object sender, EventArgs e)
        {
            textScaleY.Enabled = !checkScaleAll.Checked;
            textScaleZ.Enabled = !checkScaleAll.Checked;
        }
        public void Autoposition()
        {
            if (autosizeFailed) return;
            RectPacker packer = new RectPacker(1, 1);
            int border = 3;
            FormPrinterSettings ps = Main.printerSettings;
            float maxW = ps.PrintAreaWidth;
            float maxH = ps.PrintAreaDepth;
            float xOff = ps.BedLeft, yOff = ps.BedFront;
            if (ps.HasDumpArea)
            {
                if (ps.DumpAreaFront <= 0)
                {
                    yOff = ps.BedFront+ps.DumpAreaDepth - ps.DumpAreaFront;
                    maxH -= yOff;
                }
                else if (ps.DumpAreaDepth + ps.DumpAreaFront >= maxH)
                {
                    yOff = ps.BedFront + -(maxH - ps.DumpAreaFront);
                    maxH += yOff;
                }
                else if (ps.DumpAreaLeft <= 0)
                {
                    xOff = ps.BedLeft+ps.DumpAreaWidth - ps.DumpAreaLeft;
                    maxW -= xOff;
                }
                else if (ps.DumpAreaWidth + ps.DumpAreaLeft >= maxW)
                {
                    xOff = ps.BedLeft + maxW - ps.DumpAreaLeft;
                    maxW += xOff;
                }
            }
            foreach (STL stl in listSTLObjects.Items)
            {
                int w = 2 * border + (int)Math.Ceiling(stl.xMax - stl.xMin);
                int h = 2 * border + (int)Math.Ceiling(stl.yMax - stl.yMin);
                if (!packer.addAtEmptySpotAutoGrow(new PackerRect(0, 0, w, h, stl), (int)maxW, (int)maxH))
                {
                    autosizeFailed = true;
                }
            }
            if (autosizeFailed)
            {
                MessageBox.Show("Too many objects on printer bed for automatic packing.\r\nPacking disabled until elements are removed.",
                "Printer bed full", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            float xAdd = (maxW - packer.w) / 2.0f;
            float yAdd = (maxH - packer.h) / 2.0f;
            foreach (PackerRect rect in packer.vRects)
            {
                STL s = (STL)rect.obj;
                float xPos = xOff + xAdd + rect.x + border;
                float yPos = yOff + yAdd + rect.y + border;
                s.Position.x += xPos - s.xMin;
                s.Position.y += yPos - s.yMin;
                s.UpdateBoundingBox();
            }
            Main.main.threedview.UpdateChanges();
        }
        public void buttonAutoplace_Click(object sender, EventArgs e)
        {
            Autoposition();
        }

        private void buttonCopyObjects_Click(object sender, EventArgs e)
        {
            if (copyDialog.ShowDialog(Main.main) == DialogResult.Cancel) return;
            int numberOfCopies = (int)copyDialog.numericCopies.Value;

            List<STL> newSTL = new List<STL>();
            foreach (STL act in listSTLObjects.SelectedItems)
            {
                STL last = act;
                for (int i = 0; i < numberOfCopies; i++)
                {
                    STL stl = last.copySTL();
                    last = stl;
                    newSTL.Add(stl);
                }
            }
            foreach (STL stl in newSTL)
            {
                listSTLObjects.Items.Add(stl);
                cont.models.AddLast(stl);
            }
            if (copyDialog.checkAutoposition.Checked)
            {
                Autoposition();
            }
            Main.main.threedview.UpdateChanges();
        }
        static bool inRecheckFiles = false;
        public void recheckChangedFiles()
        {
            if (inRecheckFiles) return;
            inRecheckFiles = true;
            bool changed = false;
            foreach (STL stl in listSTLObjects.Items)
            {
                if (stl.changedOnDisk())
                {
                    changed = true;
                    break;
                }
            }
            if (changed)
            {
                if (MessageBox.Show("One or more objects files are changed.\r\nReload objects?", "Files changed", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (STL stl in listSTLObjects.Items)
                    {
                        if (stl.changedOnDisk())
                            stl.reload();
                    }
                    Main.main.threedview.UpdateChanges();
                }
                else
                {
                    foreach (STL stl in listSTLObjects.Items)
                    {
                        if (stl.changedOnDisk())
                            stl.resetModifiedDate();
                    }
                }
            }
            inRecheckFiles = false;
        }

        public void listSTLObjects_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
            {
                for(int i=0;i<listSTLObjects.Items.Count;i++)
                    listSTLObjects.SetSelected(i,true);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                buttonRemoveSTL_Click(sender,null);
                e.Handled = true;
            }
        }
    }
}
