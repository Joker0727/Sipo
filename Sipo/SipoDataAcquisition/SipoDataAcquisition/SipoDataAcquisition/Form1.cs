using DotNet.Framework.Common.Algorithm;
using Sipo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Linq;
using System.Text;
using System.Threading;

namespace SipoDataAcquisition
{
    public partial class Form1 : Form
    {
        Thread th1 = null;
        Thread th2 = null;
        UpdataTask u;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            u = new UpdataTask(textBox2);
            comboBox1.SelectedIndex = 22;

            DateTime start = Convert.ToDateTime("2018-3-18");
            DateTime end = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));//获取当前日期
            TimeSpan ts = end.Subtract(start);
            if (ts.Days <= 3)
            {
                int qwe = ts.Days;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "开  始")
            {
                button1.Text = "停  止";

                th1 = new Thread(ToDoTask);
                th1.IsBackground = true;
                th1.Name = "任务线程";
                th1.Start();

                th2 = new Thread(IsToDoTask);
                th2.IsBackground = true;
                th2.Name = "定时线程";
                th2.Start();
            }
            else
            {
                button1.Text = "开  始";
                th1.Abort();
                th2.Abort();
            }
        }
        /// <summary>
        /// 判断是否运行任务
        /// </summary>
        public void IsToDoTask()
        {

            int index = 0;
            if (this.comboBox1.InvokeRequired)
            {
                // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                Action<int> actionDelegate = (x) => { index = this.comboBox1.SelectedIndex; };
                // 或者
                // Action<string> actionDelegate = delegate(string txt) { this.label2.Text = txt; };
                this.comboBox1.Invoke(actionDelegate, index);
            }
            else
                index = this.comboBox1.SelectedIndex;

            while (true)
            {
                DateTime dt = DateTime.Now;
                if (dt.Hour == index)
                {
                    ToDoTask();
                }
                Thread.Sleep(1000 * 60 * 10);
            }
        }
        /// <summary>
        /// 执行任务
        /// </summary>
        public void ToDoTask()
        {
            string textbox3 = textBox3.Text;
            string textbox4 = textBox4.Text;
            string textbox7 = textBox7.Text;

            if (string.IsNullOrEmpty(textbox3))
            {
                MessageBox.Show("更新频率不能为空！", "提示");
                return;
            }
            if (string.IsNullOrEmpty(textbox4))
            {
                MessageBox.Show("更新数据条数不能为空！", "提示");
                return;
            }
            if (string.IsNullOrEmpty(textbox7))
            {
                MessageBox.Show("过滤天数不能为空！", "提示");
                return;
            }
            if (u.IsNumeric(textbox3) && u.IsNumeric(textbox4) && u.IsNumeric(textbox7))
            {
                UpdataTask ut = new UpdataTask(int.Parse(textbox3), int.Parse(textbox4), int.Parse(textbox7), textBox1, textBox2, textBox5, textBox6);
                ut.StartTask();
            }
            else
            {
                MessageBox.Show("请输入不小于0的数字！", "提示");
                return;
            }
        }
    }
}

