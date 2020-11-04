using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MSXML2;
using System.Threading;
using System.Diagnostics;
using Microsoft.Win32;

namespace 采集结算工具
{
    public partial class Form1 : Form
    {
        //软件标题
        private string softTitle = "";
        //Ini文件工具
        private IniFile ini = null;
        //检测时间
        private int speed = 5000;
        //检测线程
        private Thread ThreadCheck = null;

        public Form1()
        {
            //加载嵌入资源
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            InitializeComponent();
        }
        #region 内存回收
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
        /// <summary>
        /// 释放内存
        /// </summary>
        public static void ClearMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                //Form1为我窗体的类名
                Form1.SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
        }
        #endregion
        /// <summary>
        /// 加载嵌入资源中的全部dll文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(GetType().Namespace + ".Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());
            byte[] bytes = (byte[])rm.GetObject(dllName);
            return System.Reflection.Assembly.Load(bytes);
        }
        /// <summary>
        /// 将本程序设为开启自启
        /// </summary>
        /// <param name="onOff">自启开关</param>
        /// <returns></returns>
        public static bool SetMeStart(bool onOff)
        {
            bool isOk = false;
            string appName = Process.GetCurrentProcess().MainModule.ModuleName;
            string appPath = Process.GetCurrentProcess().MainModule.FileName;
            isOk = SetAutoStart(onOff, appName, appPath);
            return isOk;
        }

        /// <summary>
        /// 将应用程序设为或不设为开机启动
        /// </summary>
        /// <param name="onOff">自启开关</param>
        /// <param name="appName">应用程序名</param>
        /// <param name="appPath">应用程序完全路径</param>
        public static bool SetAutoStart(bool onOff, string appName, string appPath)
        {
            bool isOk = true;
            //如果从没有设为开机启动设置到要设为开机启动
            if (!IsExistKey(appName) && onOff)
            {
                isOk = SelfRunning(onOff, appName, @appPath);
            }
            //如果从设为开机启动设置到不要设为开机启动
            else if (IsExistKey(appName) && !onOff)
            {
                isOk = SelfRunning(onOff, appName, @appPath);
            }
            return isOk;
        }

        /// <summary>
        /// 判断注册键值对是否存在，即是否处于开机启动状态
        /// </summary>
        /// <param name="keyName">键值名</param>
        /// <returns></returns>
        private static bool IsExistKey(string keyName)
        {
            try
            {
                bool _exist = false;
                RegistryKey local = Registry.LocalMachine;
                RegistryKey runs = local.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (runs == null)
                {
                    RegistryKey key2 = local.CreateSubKey("SOFTWARE");
                    RegistryKey key3 = key2.CreateSubKey("Microsoft");
                    RegistryKey key4 = key3.CreateSubKey("Windows");
                    RegistryKey key5 = key4.CreateSubKey("CurrentVersion");
                    RegistryKey key6 = key5.CreateSubKey("Run");
                    runs = key6;
                }
                string[] runsName = runs.GetValueNames();
                foreach (string strName in runsName)
                {
                    if (strName.ToUpper() == keyName.ToUpper())
                    {
                        _exist = true;
                        return _exist;
                    }
                }
                return _exist;

            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 写入或删除注册表键值对,即设为开机启动或开机不启动
        /// </summary>
        /// <param name="isStart">是否开机启动</param>
        /// <param name="exeName">应用程序名</param>
        /// <param name="path">应用程序路径带程序名</param>
        /// <returns></returns>
        private static bool SelfRunning(bool isStart, string exeName, string path)
        {
            try
            {
                RegistryKey local = Registry.LocalMachine;
                RegistryKey key = local.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null)
                {
                    local.CreateSubKey("SOFTWARE//Microsoft//Windows//CurrentVersion//Run");
                }
                //若开机自启动则添加键值对
                if (isStart)
                {
                    key.SetValue(exeName, path);
                    key.Close();
                }
                else//否则删除键值对
                {
                    string[] keyNames = key.GetValueNames();
                    foreach (string keyName in keyNames)
                    {
                        if (keyName.ToUpper() == exeName.ToUpper())
                        {
                            key.DeleteValue(exeName);
                            key.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string ss = ex.Message;
                return false;
                //throw;
            }

            return true;
        }

        int time = 1;
        private void timer1_Tick(object sender, EventArgs e)
        {
            //释放内存
            ClearMemory();
            this.Text = softTitle + "  当前已运行：" + parseTimeSeconds(time, 0);
            time++;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (this.btnStart.Text == "开始")
            {
                this.ini.WriteString("web", "url", this.txtUrl.Text.Trim());
                this.ini.WriteInteger("web", "time", Convert.ToInt32(this.numTime.Value));
                this.speed = Convert.ToInt32(this.numTime.Value) * 1000;
                //检测线程状态
                Control.CheckForIllegalCrossThreadCalls = false;
                this.ThreadCheck = new Thread(new ThreadStart(this.Check));
                this.ThreadCheck.Start();
                this.btnStart.Text = "停止";
                this.btnStart.BackColor = Color.Maroon;
                this.labStatus.ForeColor = Color.Green;
                this.labStatus.Text = "已启动";
            }
            else
            {
                //关闭线程
                this.ThreadCheck.Abort();
                this.btnStart.Text = "开始";
                this.btnStart.BackColor = Color.DeepSkyBlue;
                this.labStatus.ForeColor = Color.Red;
                this.labStatus.Text = "已停止";
            }
           
        }
        private void Check()
        {
            while (true)
            {
                try
                {
                    string strurl = this.txtUrl.Text.Trim();
                    if (strurl == string.Empty)
                    {
                        MessageBox.Show("采集地址不能为空", "提示：", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                       break;
                    }
                    string url = strurl + "?t=" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    XMLHTTPClass xmlhttp = new XMLHTTPClass();
                    xmlhttp.open("Get", url, false, null, null);
                    xmlhttp.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
                    xmlhttp.send(null);
                    if (xmlhttp.status == 200)
                   {
                        this.labStatus.Text = "最後一次執行時間 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        this.labStatus.ForeColor = Color.Red;
                        this.labStatus.Text = "URL执行错误 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                catch (Exception)
                {
                    this.labStatus.ForeColor = Color.Red;
                    this.labStatus.Text = "URL连接失敗 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                Thread.Sleep(this.speed);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //读取软件标题
            softTitle = this.Text;
            this.ini = new IniFile("config.ini");
            this.txtUrl.Text = this.ini.ReadString("web", "url", "");
            this.numTime.Value = this.ini.ReadInteger("web", "time", 60);
            bool boot = this.ini.ReadBool("web", "boot", true);
            if (boot)
            {
                //开机启动
                SetMeStart(this.cbBootUp.Checked);
                this.btnStart_Click(sender,e);
            }
            this.cbBootUp.Checked = Convert.ToBoolean(boot);
         
        }
        ///<summary>
        ///由秒数得到日期几天几小时。。。
        ///</summary
        ///<param name="t">秒数</param>
        ///<param name="type">0：转换后带秒，1:转换后不带秒</param>
        ///<returns>几天几小时几分几秒</returns>
        public static string parseTimeSeconds(int t, int type)
        {
            string r = "";
            int day, hour, minute, second;
            if (t >= 86400) //天,
            {
                day = Convert.ToInt16(t / 86400);
                hour = Convert.ToInt16((t % 86400) / 3600);
                minute = Convert.ToInt16((t % 86400 % 3600) / 60);
                second = Convert.ToInt16(t % 86400 % 3600 % 60);
                if (type == 0)
                    r = day + ("天") + hour + ("时") + minute + ("分") + second + ("秒");
                else
                    r = day + ("天") + hour + ("时") + minute + ("分");

            }
            else if (t >= 3600)//时,
            {
                hour = Convert.ToInt16(t / 3600);
                minute = Convert.ToInt16((t % 3600) / 60);
                second = Convert.ToInt16(t % 3600 % 60);
                if (type == 0)
                    r = hour + ("时") + minute + ("分") + second + ("秒");
                else
                    r = hour + ("时") + minute + ("分");
            }
            else if (t >= 60)//分
            {
                minute = Convert.ToInt16(t / 60);
                second = Convert.ToInt16(t % 60);
                r = minute + ("分") + second + ("秒");
            }
            else
            {
                second = Convert.ToInt16(t);
                r = second + ("秒");
            }
            return r;
        }

        private void cbBootUp_CheckedChanged(object sender, EventArgs e)
        {
            //开机启动
            SetMeStart(this.cbBootUp.Checked);
            this.ini.WriteBool("web", "boot", this.cbBootUp.Checked);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //点击窗体关闭按钮时，最小化到系统托盘里
            e.Cancel = true;
            this.Hide();               //隐藏窗体 
            this.ShowInTaskbar = false;//图标不显示在任务栏上
        }

        private void tsmiOpen_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;//图标显示在任务栏上           
            this.Show();//显示窗体
            this.WindowState = FormWindowState.Normal;//正常显示
        }

        private void tsmiClose_Click(object sender, EventArgs e)
        {
            //销毁线程           
            if (ThreadCheck != null)
            {
                ThreadCheck.Abort();
                ThreadCheck = null;
            }
            //关闭定时器
            this.timer1.Stop();
            //强制退出程序
            Environment.Exit(0);
        }

        private void labelUrl_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.zy13.net");
        }
    }
}
