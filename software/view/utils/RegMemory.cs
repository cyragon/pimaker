using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using System.IO;
using PiMakerHost.model;
using System.Globalization;

namespace PiMakerHost.view.utils
{
    public class RegMemory
    {
        static RegistryKey mainKey=null;
        static RegistryKey windowKey = null;

        static void initKeys()
        {
            if (mainKey != null) return;
            mainKey = Custom.BaseKey; // Registry.CurrentUser.CreateSubKey("SOFTWARE\\PiMaker");
            windowKey = mainKey.CreateSubKey("window");

        }
        public static int GetInt(string r, int def)
        {
            initKeys();
            return (int)windowKey.GetValue(r, def);
        }
        public static void SetInt(string r, int val)
        {
            initKeys();
            windowKey.SetValue(r, val);
        }
        public static long GetLong(string r, long def)
        {
            initKeys();
            string v = (string)windowKey.GetValue(r, def.ToString());
            long l=0;
            long.TryParse(v, out l);
            return l;
        }
        public static void SetLong(string r, long val)
        {
            initKeys();
            windowKey.SetValue(r, val.ToString());
        }
        public static double GetDouble(string r, double def)
        {
            initKeys();
            string sval = (string)windowKey.GetValue(r, def.ToString(GCode.format));
            double val = def;
            double.TryParse(sval, NumberStyles.Float, GCode.format, out val);
            return val;
        }
        public static void SetDouble(string r, double val)
        {
            initKeys();
            windowKey.SetValue(r, val.ToString(GCode.format));
        }
        public static bool GetBool(string r, bool def)
        {
            initKeys();
            return (int)windowKey.GetValue(r, def?1:0)!=0;
        }
        public static void SetBool(string r, bool val)
        {
            initKeys();
            windowKey.SetValue(r, val?1:0);
        }
        public static string GetString(string r,string def)
        {
            initKeys();
            return (string)windowKey.GetValue(r, def);
        }
        public static void SetString(string r, string val)
        {
            initKeys();
            windowKey.SetValue(r, val);
        }
        public static Color GetColor(string r, Color def)
        {
            int v = GetInt(r, def.ToArgb());
            return Color.FromArgb(v);
        }
        public static void SetColor(string r, Color val)
        {
            SetInt(r, val.ToArgb());
        }
        public static string WindowPosToString(Form f,bool state)
        {
            return f.Location.X.ToString() + "|" +
                f.Location.Y.ToString()+(state?f.WindowState.ToString():"");
        }
        public static string WindowPosSizeToString(Form f, bool state)
        {
            return f.Location.X.ToString() + "|" +
                f.Location.Y.ToString() +"|"+
                f.Size.Width.ToString() + "|" +
                f.Size.Height.ToString() + "|" + 
                (state ? f.WindowState.ToString() : "");
        }
        private static bool GeometryIsBizarreSize(Size size)
        {
            return (size.Height <= Screen.PrimaryScreen.WorkingArea.Height &&
                size.Width <= Screen.PrimaryScreen.WorkingArea.Width);
        }
        private static bool GeometryIsBizarreLocation(Point loc, Size size)
        {
            bool locOkay;
            if (loc.X < 0 || loc.Y < 0)
            {
                locOkay = false;
            }
            else if (loc.X + size.Width > Screen.PrimaryScreen.WorkingArea.Width)
            {
                locOkay = false;
            }
            else if (loc.Y + size.Height > Screen.PrimaryScreen.WorkingArea.Height)
            {
                locOkay = false;
            }
            else
            {
                locOkay = true;
            }
            return locOkay;
        }
        public static void StringToWindowPos(Form f, string pos)
        {
            if (string.IsNullOrEmpty(pos)) return;
            string[] numbers = pos.Split('|');
            Point windowPoint = new Point(int.Parse(numbers[0]),
                int.Parse(numbers[1]));
            Size windowSize = f.Size;
            if (numbers.Length >= 4)
            {
                windowSize = new Size(int.Parse(numbers[2]),
                    int.Parse(numbers[3]));
            }
            string windowString = "Normal";
            if(numbers.Length==3 || numbers.Length==5)
            windowString = numbers[numbers.Length-1];
            if (windowString == "Normal")
            {

                bool locOkay = GeometryIsBizarreLocation(windowPoint, windowSize);
                bool sizeOkay = GeometryIsBizarreSize(windowSize);

                if (locOkay == true && sizeOkay == true)
                {
                    f.Location = windowPoint;
                    f.Size = windowSize;
                    f.StartPosition = FormStartPosition.Manual;
                    f.WindowState = FormWindowState.Normal;
                }
                else if (sizeOkay == true)
                {
                    f.Size = windowSize;
                }
            }
            else if (windowString == "Maximized")
            {
                f.Location = new Point(100, 100);
                f.StartPosition = FormStartPosition.Manual;
                f.WindowState = FormWindowState.Maximized;
            }
        }
        public static void StoreWindowPos(string name, Form f, bool storeSize, bool storeState)
        {
            string s = storeSize ? WindowPosSizeToString(f, storeState) : WindowPosToString(f, storeState);
            string s2 = GetString(name,"");
            if (s == s2) return;
            SetString(name, s);
        }
        public static void RestoreWindowPos(string name, Form f)
        {
            string s = GetString(name, "");
            if (s == "") return;
            StringToWindowPos(f, s);
        }
        public class HistoryFile
        {
            public string file;
            public HistoryFile(string fname)
            {
                file = fname;
            }
            public override string ToString()
            {
                int p = file.LastIndexOf(Path.DirectorySeparatorChar);
                if (p < 0) return file;
                return file.Substring(p + 1);
            }
        }
        public class FilesHistory
        {
            public LinkedList<HistoryFile> list = new LinkedList<HistoryFile>();
            string name;
            int maxLength;
            public FilesHistory(string id,int max)
            {
                name = id;
                maxLength = max;
                string l = RegMemory.GetString(name, "");
                foreach (string fn in l.Split('|'))
                {
                    if (fn.Length > 0 && File.Exists(fn))
                    {
                        list.AddLast(new HistoryFile(fn));
                        if (list.Count == max) break;
                    }
                }
            }
            public void Save(string fname)
            {
                if (list.Count > 0 && list.First.Value.file == fname) return;
                foreach (HistoryFile f in list)
                {
                    if (f.file == fname)
                    {
                        list.Remove(f);
                        break;
                    }
                }
                list.AddFirst(new HistoryFile(fname));
                while (list.Count > maxLength)
                    list.RemoveLast();
                // Build string
                string store = "";
                foreach (HistoryFile f in list)
                    store += "|" + f.file;
                RegMemory.SetString(name, store.Substring(1));
            }
        }
    }
}
