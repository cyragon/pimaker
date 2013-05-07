/*
   Copyright 2011 PiMaker

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
using System.Linq;
using System.Text;
using PiMakerHost.view;
using PiMakerHost.model;

namespace PiMakerHost.model
{
    public delegate void OnPosChange(GCode code, float x, float y, float z);
    public delegate void OnPosChangeFast(float x, float y, float z, float e);
    public delegate void OnAnalyzerChange();

    public class GCodeAnalyzer
    {
        public event OnPosChange eventPosChanged;
        public event OnPosChangeFast eventPosChangedFast;
        public event OnAnalyzerChange eventChange;
        public int activeExtruder = 0;
        //public float extruderTemp = 0;
        public Dictionary<int,float> extruderTemp = new Dictionary<int,float>();  
        public bool uploading = false;
        public float bedTemp = 0;
        public float x = 0, y = 0, z = 0, e = 0, emax = 0,f=1000;
        public float radius = 0, angle = 0; //PiMaker Radius and Angle from (X,Y)
        public float lastX=0, lastY=0, lastZ=0, lastE=0;
        public float xOffset = 0, yOffset = 0, zOffset = 0, eOffset = 0,lastZPrint = 0;
        public bool fanOn = false;
        public int fanVoltage = 0;
        public bool powerOn = true;
        public bool relative = false;
        public bool eRelative = false;
        public int debugLevel = 6;
        public int lastline = 0;
        public bool hasXHome = false, hasYHome = false, hasZHome = false;
        public bool privateAnalyzer = false;
        public int maxDrawMethod = 2;
        public bool drawing = true;
        public int layer = 0;
        public bool isG1Move = false;
        public int speedMultiply = 100;
        public float printerWidth, printerHeight, printerDepth;
        public int tempMonitor = 0;
        public double printingTime = 0;
        public bool eChanged;

        public GCodeAnalyzer(bool privAnal)
        {
            privateAnalyzer = privAnal;
            foreach(int k in extruderTemp.Keys)
                extruderTemp[k] = 0;
            bedTemp = 0;
        }
        public float getTemperature(int extr) {
            if (extr < 0) extr = activeExtruder;
            if(!extruderTemp.ContainsKey(extr))
                extruderTemp.Add(extr,0.0f);
            return extruderTemp[extr];
        }
        public void setTemperature(int extr,float t) {
            if (extr < 0) extr = activeExtruder;
            if (!extruderTemp.ContainsKey(extr))
                extruderTemp.Add(extr,t);
            else extruderTemp[extr] = t;
        }
        public void fireChanged()
        {
            if (eventChange != null)
            {
                try
                {
                    Main.main.Invoke(eventChange);
                }
                catch { }
            }
        }
        // set to start condition
        public void start()
        {
            relative = false;
            eRelative = false;
            activeExtruder = 0;
            List<int> keys = new List<int>();
            foreach (int k in extruderTemp.Keys)
                keys.Add(k);
            foreach(int k in keys)
                extruderTemp[k] = 0;
            bedTemp = 0;
            fanOn = false;
            powerOn = true;
            fanVoltage = 0;
            maxDrawMethod = 2;
            drawing = true;
            lastline = 0;
            layer = 0;
            x = y = z = e = emax = lastZPrint = 0;
            xOffset = yOffset = zOffset = eOffset = 0;
            lastX = 0; lastY = 0; lastZ = 0; lastE = 0;
            hasXHome = hasYHome = hasZHome = false;
            printerWidth = Main.printerSettings.PrintAreaWidth;
            printerDepth = Main.printerSettings.PrintAreaDepth;
            printerHeight = Main.printerSettings.PrintAreaHeight;
            if (!privateAnalyzer)
                Main.main.jobVisual.ResetQuality();
            fireChanged();
        }
        public void StartJob() {
            layer = 0;
            lastZPrint = 0;
            printingTime = 0;
            lastX = 0; lastY = 0; lastZ = 0; lastE = 0;
            eOffset = 0; emax = 0; e = 0;
            drawing = true;
            uploading = false;
            if (!privateAnalyzer)
                Main.main.jobVisual.ResetQuality();
        }
        public void Analyze(GCode code)
        {
            if (code.hostCommand)
            {
                string cmd = code.getHostCommand();
                if (cmd.Equals("@hide"))
                    drawing = false;
                else if (cmd.Equals("@show"))
                    drawing = true;
                else if (cmd.Equals("@isathome"))
                {
                    hasXHome = hasYHome = hasZHome = true;
                    x = Main.printerSettings.XHomePos;
                    y = Main.printerSettings.YHomePos;
                    z = Main.printerSettings.ZHomePos;
                    xOffset = yOffset = zOffset = 0;
                }
                return;
            }
            //if (code.forceAscii) return; // Don't analyse host commands and unknown commands
            if (code.hasN)
                lastline = code.N;
            if (uploading && !code.hasM && code.M != 29) return; // ignore upload commands
            if (code.hasG)
            {
                switch (code.G)
                {
                    case 0:
                    case 1:
                        eChanged = false;
                        if (code.hasF) f = code.F;
                        if (relative)
                        {
                            if (code.hasX) x += code.X;
                            if (code.hasY) y += code.Y;
                            if (code.hasZ) z += code.Z;
                            if (code.hasE) { eChanged = code.E != 0; e += code.E; }
                        }
                        else
                        {
                            if (code.hasX) x = xOffset + code.X;
                            if (code.hasY) y = yOffset + code.Y;
                            if (code.hasZ)
                            {
                                 z = zOffset + code.Z; 
                            }
                            if (code.hasE)
                            {
                                if (eRelative)
                                { eChanged = code.E != 0; e += code.E; }
                                else
                                {
                                    eChanged = e != (eOffset + code.E);
                                    e = eOffset + code.E;
                                }
                            }
                        }
                        if (x < Main.printerSettings.XMin) { x = Main.printerSettings.XMin; hasXHome = false; }
                        if (y < Main.printerSettings.YMin) { y = Main.printerSettings.YMin; hasYHome = false; }
                        if (z < 0) { z = 0; hasZHome = false; }
                        if (x > Main.printerSettings.XMax) { hasXHome = false; }
                        if (y > Main.printerSettings.YMax) { hasYHome = false; }
                        if (z > printerHeight) { hasZHome = false; }
                        if (e > emax)
                        {
                            emax = e;
                            if (z != lastZPrint)
                            {
                                layer++;
                                lastZPrint = z;
                                if (!privateAnalyzer && Main.conn.job.hasData() && Main.conn.job.maxLayer >= 0)
                                {
                                    //PrinterConnection.logInfo("Printing layer " + layer.ToString() + " of " + Main.conn.job.maxLayer.ToString());
                                    PrinterConnection.logInfo(Trans.T2("L_PRINTING_LAYER_X_OF_Y",layer.ToString(), Main.conn.job.maxLayer.ToString()));
                                }
                            }
                        }
                        if (eventPosChanged != null)
                            if (privateAnalyzer)
                                eventPosChanged(code, x, y, z);
                            else
                                Main.main.Invoke(eventPosChanged, code, x, y, z);
                        float dx = Math.Abs(x - lastX);
                        float dy = Math.Abs(y - lastY);
                        float dz = Math.Abs(z - lastZ);
                        float de = Math.Abs(e - lastE);
                        if (dx + dy + dz > 0.001)
                        {
                            printingTime += Math.Sqrt(dx * dx + dy * dy + dz * dz) * 60.0f / f;
                        }
                        else printingTime += de * 60.0f / f;
                        lastX = x;
                        lastY = y;
                        lastZ = z;
                        lastE = e;
                        break;
                    case 28:
                    case 161:
                        {
                            bool homeAll = !(code.hasX || code.hasY || code.hasZ);
                            if (code.hasX || homeAll) { xOffset = 0; x = Main.printerSettings.XHomePos; hasXHome = true; }
                            if (code.hasY || homeAll) { yOffset = 0; y = Main.printerSettings.YHomePos; hasYHome = true; }
                            if (code.hasZ || homeAll) { zOffset = 0; z = Main.printerSettings.ZHomePos; hasZHome = true; }
                            if (code.hasE) { eOffset = 0; e = 0; emax = 0; }
                            if (eventPosChanged != null)
                                if (privateAnalyzer)
                                    eventPosChanged(code, x, y, z);
                                else
                                    Main.main.Invoke(eventPosChanged, code, x, y, z);
                        }
                        break;
                    case 162:
                        {
                            bool homeAll = !(code.hasX || code.hasY || code.hasZ);
                            if (code.hasX || homeAll) { xOffset = 0; x = Main.printerSettings.XMax; hasXHome = true; }
                            if (code.hasY || homeAll) { yOffset = 0; y = Main.printerSettings.YMax; hasYHome = true; }
                            if (code.hasZ || homeAll) { zOffset = 0; z = Main.printerSettings.PrintAreaHeight; hasZHome = true; }
                            if (eventPosChanged != null)
                                if (privateAnalyzer)
                                    eventPosChanged(code, x, y, z);
                                else
                                    Main.main.Invoke(eventPosChanged, code, x, y, z);
                        }
                        break;
                    case 90:
                        relative = false;
                        break;
                    case 91:
                        relative = true;
                        break;
                    case 92:
                        if (code.hasX) { xOffset = x - code.X; x = xOffset; }
                        if (code.hasY) { yOffset = y - code.Y; y = yOffset; }
                        if (code.hasZ) { zOffset = z - code.Z; z = zOffset; }
                        if (code.hasE) { eOffset = e - code.E; lastE = e = eOffset; }
                        if (eventPosChanged != null)
                            if (privateAnalyzer)
                                eventPosChanged(code, x, y, z);
                            else
                                Main.main.Invoke(eventPosChanged, code, x, y, z);
                        break;
                }
            }
            else if (code.hasM)
            {
                switch (code.M)
                {
                    case 28:
                        uploading = true;
                        break;
                    case 29:
                        uploading = false;
                        break;
                    case 80:
                        powerOn = true;
                        fireChanged();
                        break;
                    case 81:
                        powerOn = false;
                        fireChanged();
                        break;
                    case 82:
                        eRelative = false;
                        break;
                    case 83:
                        eRelative = true;
                        break;
                    case 104:
                    case 109:
                        {
                            int idx = activeExtruder;
                            if (code.hasT) idx = code.T;
                            if (code.hasS) setTemperature(idx,code.S);
                        }
                        fireChanged();
                        break;
                    case 106:
                        fanOn = true;
                        if (code.hasS) fanVoltage = code.S;
                        fireChanged();
                        break;
                    case 107:
                        fanOn = false;
                        fireChanged();
                        break;
                    case 110:
                        lastline = code.N;
                        break;
                    case 111:
                        if (code.hasS)
                        {
                            debugLevel = code.S;
                        }
                        break;
                    case 140:
                    case 190:
                        if (code.hasS) bedTemp = code.S;
                        fireChanged();
                        break;
                    case 203: // Temp monitor
                        if(code.hasS)
                            tempMonitor = code.S;
                        break;
                    case 220:
                        if(code.hasS)
                            speedMultiply = code.S;
                        break;
                }
            }
            else if (code.hasT)
            {
                activeExtruder = code.T;
                fireChanged();
            }
        }
        public void analyzeShort(GCodeShort code)
        {
            isG1Move = false;
            switch (code.compressedCommand)
            {
                case 1:
                    isG1Move = true;
                    eChanged = false;
                    if (code.hasF) f = code.f;
                    if (relative)
                    {
                        if (code.hasX)
                        {
                            x += code.x;
                            //if (x < 0) { x = 0; hasXHome = NO; }
                            //if (x > printerWidth) { hasXHome = NO; }
                        }
                        if (code.hasY)
                        {
                            y += code.y;
                            //if (y < 0) { y = 0; hasYHome = NO; }
                            //if (y > printerDepth) { hasYHome = NO; }
                        }
                        if (code.hasZ)
                        {
                            z += code.z;
                            //if (z < 0) { z = 0; hasZHome = NO; }
                            //if (z > printerHeight) { hasZHome = NO; }
                        }
                        if (code.hasE)
                        {
                            eChanged = code.e != 0;
                            e += code.e; 
                            if (e > emax)
                            {
                                emax = e;
                                if (z > lastZPrint)
                                {
                                    lastZPrint = z;
                                    layer++;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (code.x != -99999)
                        {
                            x = xOffset + code.x;
                            //if (x < 0) { x = 0; hasXHome = NO; }
                            //if (x > printerWidth) { hasXHome = NO; }
                        }
                        if (code.y != -99999)
                        {
                            y = yOffset + code.y;
                            //if (y < 0) { y = 0; hasYHome = NO; }
                            //if (y > printerDepth) { hasYHome = NO; }
                        }
                        if (code.z != -99999)
                        {
                            z = zOffset + code.z;
                            //if (z < 0) { z = 0; hasZHome = NO; }
                            //if (z > printerHeight) { hasZHome = NO; }
                        }
                        if (code.e != -99999)
                        {
                            if (eRelative)
                            { eChanged = code.e != 0; e += code.e; }
                            else
                            {
                                eChanged = e != (eOffset + code.e);
                                e = eOffset + code.e;
                            }
                            if (e > emax)
                            {
                                emax = e;
                                if (z != lastZPrint)
                                {
                                    lastZPrint = z;
                                    layer++;
                                }
                            }
                        }
                    }
                    if(eventPosChangedFast!=null)
                        eventPosChangedFast(x, y, z, e);
                    float dx = Math.Abs(x - lastX);
                    float dy = Math.Abs(y - lastY);
                    float dz = Math.Abs(z - lastZ);
                    float de = Math.Abs(e - lastE);
                    if (dx + dy + dz > 0.001)
                    {
                        printingTime += Math.Sqrt(dx * dx + dy * dy + dz * dz) * 60.0f / f;
                    }
                    else printingTime += de * 60.0f / f;
                    lastX = x;
                    lastY = y;
                    lastZ = z;
                    lastE = e;
                    break;
                case 4:
                    {
                        bool homeAll = !(code.hasX || code.hasY || code.hasZ);
                        if (code.hasX || homeAll) { xOffset = 0; x = Main.printerSettings.XHomePos; hasXHome = true; }
                        if (code.hasY || homeAll) { yOffset = 0; y = Main.printerSettings.YHomePos; hasYHome = true; }
                        if (code.hasZ || homeAll) { zOffset = 0; z = Main.printerSettings.ZHomePos; hasZHome = true; }
                        if (code.hasE) { eOffset = 0; e = 0; emax = 0; }
                        // [delegate positionChangedFastX:x y:y z:z e:e];
                    }
                    break;
                case 5:
                    {
                        bool homeAll = !(code.hasX || code.hasY || code.hasZ);
                        if (code.hasX || homeAll) { xOffset = 0; x = Main.printerSettings.XMax; hasXHome = true; }
                        if (code.hasY || homeAll) { yOffset = 0; y = Main.printerSettings.YMax; hasYHome = true; }
                        if (code.hasZ || homeAll) { zOffset = 0; z = Main.printerSettings.PrintAreaHeight; hasZHome = true; }
                        //[delegate positionChangedFastX:x y:y z:z e:e];
                    }
                    break;
                case 6:
                    relative = false;
                    break;
                case 7:
                    relative = true;
                    break;
                case 8:
                    if (code.hasX) { xOffset = x - code.x; x = xOffset; }
                    if (code.hasY) { yOffset = y - code.y; y = yOffset; }
                    if (code.hasZ) { zOffset = z - code.z; z = zOffset; }
                    if (code.hasE) { eOffset = e - code.e; lastE = e = eOffset; }
                    break;
                case 12: // Host command
                    {
                        string hc = code.text.Trim();
                        if (hc == "@hide")
                            drawing = false;
                        else if (hc == "@show")
                            drawing = true;
                        else if (hc == "@isathome")
                        {
                            hasXHome = hasYHome = hasZHome = true;
                            x = xOffset = Main.printerSettings.XHomePos;
                            y = yOffset = Main.printerSettings.YHomePos;
                            z = zOffset = Main.printerSettings.ZHomePos;
                        }
                    }
                    break;
                case 9:
                    eRelative = false;
                    break;
                case 10:
                    eRelative = true;
                    break;
                case 11:
                    activeExtruder = code.tool;
                    break;
            }
            code.layer = layer;
            code.tool = activeExtruder;
            code.emax = emax;
        }
    }
}
