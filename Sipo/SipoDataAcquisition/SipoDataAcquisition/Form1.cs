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

namespace SipoDataAcquisition
{
    public partial class Form1 : Form
    {
        public string MainUrl = "http://cpquery.sipo.gov.cn/txnQueryOrdinaryPatents.do";//首页链接
        public string codeUrl = "http://cpquery.sipo.gov.cn/freeze.main?txn-code=createImgServlet&freshStept=1";//验证码链接
        public string LastUrl = "http://cpquery.sipo.gov.cn/txnQueryBibliographicData.do";//案件状态页面链接
        public string path = AppDomain.CurrentDomain.BaseDirectory;//程序运行根目录
        public CookieContainer cookie = new CookieContainer();
        VerifyCode vfc;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            HttpHelper hh = new HttpHelper();
            hh.GetHtmlData(MainUrl, cookie);//获取cookie
            hh.DowloadCheckImg(codeUrl, cookie, path);//下载验证码

            if (File.Exists(path + "yzm.jpg"))
            {
                Bitmap bmp = new Bitmap(path + "yzm.jpg");

                vfc = new VerifyCode(bmp);
                vfc.ClearPicBorder(3);//去图形边框
                vfc.GrayByPixels();//灰度处理
                vfc.ClearNoise(128, 1);//清除噪点
                vfc.GrayByPixels();//灰度处理
                vfc.BitmapTo1Bpp(1);//二值化
                vfc.GetPicValidByValue(128, 4);//得到有效空间
                Bitmap[] pics = vfc.GetSplitPics(4, 1);//分割           
                int verycode = Calculate(pics);//计算验证码的结果

                long longTime = ConvertDateTimeToInt(DateTime.Now);//当前时间戳
                string shenqingh = "2017302262484";
                string data = "select-key:shenqingh=" + shenqingh + "&select-key:zhuanlimc=&select-key:shenqingrxm=&select-key:zhuanlilx=&select-key:shenqingr_from=&select-key:shenqingr_to=&verycode=" + verycode + "&inner-flag:open-type=window&inner-flag:flowno="+ longTime + "";

                string retString = hh.SendDataByGET(MainUrl, data, ref cookie);
                if (retString.Contains("申请信息"))
                {
                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(retString);
                    HtmlNode node = doc.DocumentNode.SelectSingleNode(("//input[@id='sq_token']"));
                    string token = node.GetAttributeValue("value", "");
                    longTime = ConvertDateTimeToInt(DateTime.Now);//当前时间戳
                    string backPage = LastUrl + "?" + data;

                    string lastData = "select-key:shenqingh="+ shenqingh + "&select-key:backPage="+ backPage + "&token="+ token + "&inner-flag:open-type=window&inner-flag:flowno = "+ longTime + "";
                    string qwe = LastUrl + "?"+ lastData;
                    string lastResult = hh.SendDataByGET(LastUrl, lastData,ref cookie);
                    if (lastResult.Contains("案件状态"))
                    {
                        HtmlAgilityPack.HtmlDocument lastDoc = new HtmlAgilityPack.HtmlDocument();
                        doc.LoadHtml(lastResult);
                        HtmlNode lastNode = doc.DocumentNode.SelectSingleNode(("//span[@name='record_zlx:anjianywzt']/span"));
                        string statusCase = lastNode.InnerHtml;
                        MessageBox.Show(statusCase);
                    }
                }
            }
            else
            {
                MessageBox.Show("验证码不存在根目录!");
            }
        }
        /// <summary>
        /// 根据样本识别并计算结果
        /// </summary>
        /// <param name="pics"></param>
        public int Calculate(Bitmap[] pics)
        {
            List<code> codes = new List<code>();
            LevenshteinDistance ld = new LevenshteinDistance();

            string result = string.Empty;
            int a = 0;
            int b = 0;
            int total = 0;
            string option = string.Empty;

            codes.Clear();
            using (StreamReader sr = new StreamReader(path + @"\DigitalDotMatrix.ini"))
            {
                while (!sr.EndOfStream)
                {
                    string[] temp = sr.ReadLine().Split(':');
                    code c = new code();
                    c.Key = temp[1];
                    c.Value = temp[0];
                    codes.Add(c);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                string code = vfc.GetSingleBmpCode(pics[i], 128);//得到代码串

                decimal max = 0.0M;
                string value = "";

                for (int m = 0; m < codes.Count; m++)
                {
                    code c = codes[m];
                    decimal parent = ld.LevenshteinDistancePercent(code, c.Key);
                    if (parent > max)
                    {
                        max = parent;
                        value = c.Value;
                    }
                }
                if (IsNumeric(value) && i == 0)
                    a = int.Parse(value);
                else if (IsNumeric(value) && i == 2)
                    b = int.Parse(value);
                else
                    option = value;
                result = result + value;
            }
            switch (option)
            {
                case "-":
                    {
                        total = a - b;
                        break;
                    }
                case "+":
                    {
                        total = a + b;
                        break;
                    }
                default:
                    break;
            }
            return total;
        }
        /// <summary>
        /// 判断字符串是不是数字类型的 true是数字
        /// </summary>
        /// <param name="value">需要检测的字符串</param>
        /// <returns></returns>
        public bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^\d(\.\d+)?|[1-9]\d+(\.\d+)?$");
        }
        /// <summary>  
        /// 将c# DateTime时间格式转换为Unix时间戳格式  
        /// </summary>  
        /// <param name="time">时间</param>  
        /// <returns>long</returns>  
        public long ConvertDateTimeToInt(DateTime time)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (time.Ticks - startTime.Ticks) / 10000;   //除10000调整为13位      
            return t;
        }

    }
}
