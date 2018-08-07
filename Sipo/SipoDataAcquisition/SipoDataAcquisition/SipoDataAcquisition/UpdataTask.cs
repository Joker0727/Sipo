using DotNet.Framework.Common.Algorithm;
using HtmlAgilityPack;
using Sipo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SipoDataAcquisition
{
    public class UpdataTask
    {
        public string MainUrl = "http://cpquery.sipo.gov.cn/txnPantentInfoList.do?inner-flag:open-type=window&inner-flag:flowno=1530613684694";//首页链接
        public string queryurl = "http://cpquery.sipo.gov.cn/txnQueryOrdinaryPatents.do";
        public string codeUrl = "http://cpquery.sipo.gov.cn/freeze.main?txn-code=createImgServlet&freshStept=1";//验证码链接
        public string LastUrl = "http://cpquery.sipo.gov.cn/txnQueryBibliographicData.do";//案件状态页面链接
        public string path = AppDomain.CurrentDomain.BaseDirectory;//程序运行根目录
        public CookieContainer cookie = new CookieContainer();
        public VerifyCode vfc;
        public TextBox textBox1;//日志文本框
        public TextBox textBox2;//当前ip显示文本框
        public TextBox textBox5;
        public TextBox textBox6;
        public int time;//多少秒更新一条数据
        public int number;
        public int day;
        int search = 0;//成功查询条数
        int updata = 0;//成功更新条数
        int searchTotal = 0;//查询总条数
        int unUpdata = 0;//已经是最新状态,无需更新条数
        int failedUpdata = 0;//更新失败条数

        public UpdataTask(TextBox textBox2)
        {
            this.textBox2 = textBox2;
            GetIP();//检测当前IP
        }
        public UpdataTask(int time, int number, int day, TextBox textBox1, TextBox textBox2, TextBox textBox5, TextBox textBox6)
        {
            this.time = time;
            this.number = number;
            this.day = day;
            this.textBox1 = textBox1;
            this.textBox2 = textBox2;
            this.textBox5 = textBox5;
            this.textBox6 = textBox6;
        }
        /// <summary>
        /// 开启任务
        /// </summary>
        public void StartTask()
        {
            try
            {
                SqlHelper sh = new SqlHelper();
                List<Patents> patents = sh.GetPatents(day);
                if (patents.Count <= 0)
                {
                    WriteLog("暂时没有数据可更新");
                    return;
                }
                foreach (var patent in patents)
                {
                    searchTotal++;
                    GetIP();//检测当前IP              
                    string thisState = DoTask(patent.P_Id.Trim());
                    string lastUpDateTime = DateTime.Now.ToString("yyyy-MM-dd");
                    if (!string.IsNullOrEmpty(thisState))
                    {
                        if (thisState == patent.P_LawStatus)
                        {
                            WriteLog("专利号[" + patent.P_Id + ":" + patent.P_LawStatus + "] 状态已是最新，无需更改.");
                            sh.UpDataStatus(thisState, patent.P_Id, lastUpDateTime);
                            search++;
                            unUpdata++;
                        }
                        else if (thisState != "请求超时！")
                        {
                            search++;
                            if (sh.UpDataStatus(thisState, patent.P_Id, lastUpDateTime))
                            {
                                WriteLog("专利号[" + patent.P_Id + ":" + patent.P_LawStatus + "] 状态成功更新为[" + thisState + "].");
                                updata++;
                            }
                            else
                            {
                                WriteLog("专利号[" + patent.P_Id + ":" + patent.P_LawStatus + "] 状态更新失败.");
                                failedUpdata++;
                            }
                        }
                        else
                        {
                            WriteLog("专利号[" + patent.P_Id + ":" + patent.P_LawStatus + "] " + thisState + "");
                        }
                    }
                    else
                    {
                        WriteLog("专利号[" + patent.P_Id + ":" + patent.P_LawStatus + "] 查询到的状态与提供的模板不符，无法匹配到正确的案件状态！");
                    }
                    UpDataNumber();
                    Thread.Sleep(time * 1000);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
            }
        }
        /// <summary>
        /// 执行任务
        /// </summary>
        public string DoTask(string shenqingh)
        {
            string state = "请求超时！";
            int verycode = 0;
            int f = 0;

            try
            {
                while (true)
                {
                    Bitmap bmp = null;
                    HttpHelper hh = new HttpHelper("183.30.204.174", 9999);
                    hh.GetHtmlData(MainUrl, cookie);//获取cookie
                    hh.DowloadCheckImg(codeUrl, cookie, path);//下载验证码

                    if (File.Exists(path + "yzm.jpg"))
                    {
                        try
                        {
                            bmp = new Bitmap(path + "yzm.jpg");

                            vfc = new VerifyCode(bmp);
                            vfc.ClearPicBorder(3);//去图形边框
                            vfc.GrayByPixels();//灰度处理
                            vfc.ClearNoise(128, 1);//清除噪点
                            vfc.GrayByPixels();//灰度处理
                            vfc.BitmapTo1Bpp(1);//二值化
                            vfc.GetPicValidByValue(128, 4);//得到有效空间
                            Bitmap[] pics = vfc.GetSplitPics(4, 1);//分割           
                            verycode = Calculate(pics);//计算验证码的结果 
                        }
                        catch (Exception ex)
                        {
                            WriteLog("网络异常，验证码请求失败！");
                        }
                        finally
                        {
                            vfc.bmpobj.Dispose();
                            bmp.Dispose();
                        }
                        string data = "select-key:shenqingh=" + shenqingh + "&select-key:zhuanlimc=&select-key:shenqingrxm=&select-key:zhuanlilx=&select-key:shenqingr_from=&select-key:shenqingr_to=&verycode=" + verycode + "&inner-flag:open-type=window&inner-flag:flowno=" + ConvertDateTimeToInt() + "";

                        string retString = hh.SendDataByGET(queryurl, data, ref cookie);
                        if (retString.Contains("申请信息"))
                        {
                            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                            doc.LoadHtml(retString);
                            HtmlNode node = doc.DocumentNode.SelectSingleNode(("//input[@id='sq_token']"));
                            string token = node.GetAttributeValue("value", "");
                            string backPage = LastUrl + "?" + data;

                            string lastData = "select-key:shenqingh=" + shenqingh + "&select-key:backPage=" + backPage + "&token=" + token + "&inner-flag:open-type=window&inner-flag:flowno = " + ConvertDateTimeToInt() + "";
                            string qwe = LastUrl + "?" + lastData;
                            string lastResult = hh.SendDataByGET(LastUrl, lastData, ref cookie);
                            if (lastResult.Contains("案件状态"))
                            {
                                HtmlAgilityPack.HtmlDocument lastDoc = new HtmlAgilityPack.HtmlDocument();
                                doc.LoadHtml(lastResult);
                                HtmlNodeCollection spanNodes = doc.DocumentNode.SelectNodes("//span[@name='record_zlx:anjianywzt']/span[@class='nlkfqirnlfjerldfgzxcyiuro']");
                                int length = spanNodes.Count;
                                List<string> spanlist = new List<string>();
                                foreach (var sn in spanNodes)
                                {
                                    spanlist.Add(sn.InnerHtml);
                                }
                                List<string> newList = spanlist.Distinct().ToList();
                                string confusedState = string.Empty;
                                foreach (var item in newList)
                                {
                                    confusedState += item.ToString();
                                }
                                state = StateMatching(confusedState);
                            }
                        }
                    }
                    else
                    {
                        WriteLog("验证码不存在根目录!");
                    }
                    if (state == "请求超时！" && f < 5)
                    {
                        WriteLog("专利号[" + shenqingh + "] 请求失败，6秒后重新请求.");
                        Thread.Sleep(1000 * 6);
                        f++;
                        continue;
                    }
                    else
                    {
                        return state;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("网络异常，请求失败！");
            }
            return state;
        }
        /// <summary>
        /// 根据样本识别并计算结果
        /// </summary>
        /// <param name="pics"></param>
        public int Calculate(Bitmap[] pics)
        {
            int total = 0;
            try
            {
                List<code> codes = new List<code>();
                LevenshteinDistance ld = new LevenshteinDistance();

                string result = string.Empty;
                int a = 0;
                int b = 0;
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
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
            }
            return total;
        }
        /// <summary>
        /// 判断字符串是不是数字类型的 true是数字
        /// </summary>
        /// <param name="value">需要检测的字符串</param>
        /// <returns>true是数字</returns>
        public bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^\d(\.\d+)?|[1-9]\d+(\.\d+)?$");
        }
        /// <summary>  
        /// 将c# DateTime时间格式转换为Unix时间戳格式  
        /// </summary>  
        /// <param name="time">时间</param>  
        /// <returns>long</returns>  
        public long ConvertDateTimeToInt()
        {
            DateTime time = DateTime.Now;
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (time.Ticks - startTime.Ticks) / 10000;   //除10000调整为13位      
            return t;
        }
        /// <summary>
        /// 打印日志
        /// </summary>
        /// <param name="log"></param>
        public void WriteLog(string log)
        {
            try
            {
                string logPath = path + "log\\";//日志文件夹
                DirectoryInfo dir = new DirectoryInfo(logPath);
                if (!dir.Exists)//判断文件夹是否存在
                    dir.Create();//不存在则创建

                FileInfo[] subFiles = dir.GetFiles();//获取该文件夹下的所有文件
                foreach (FileInfo f in subFiles)
                {
                    string fname = Path.GetFileNameWithoutExtension(f.FullName); //获取文件名，没有后缀
                    DateTime start = Convert.ToDateTime(fname);//文件名转换成时间
                    DateTime end = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));//获取当前日期
                    TimeSpan sp = end.Subtract(start);//计算时间差
                    if (sp.Days > 15)//大于15天删除
                        f.Delete();
                }

                string logName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";//日志文件名称，按照当天的日期命名
                string fullPath = logPath + logName;//日志文件的完整路径
                string contents = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " -> " + log + "\r\n";//日志内容

                textBox1.Invoke(new Action(() =>
                {
                    textBox1.AppendText(contents);
                }));

                File.AppendAllText(fullPath, contents, Encoding.UTF8);//追加日志
            }
            catch (Exception ex) { }
        }
        /// <summary>
        /// 匹配正确的案件状态
        /// </summary>
        /// <param name="confusedState"></param>
        /// <returns></returns>
        public string StateMatching(string confusedState)
        {
            string rightState = string.Empty;

            char[] charArr = confusedState.ToCharArray();
            string[] strArr = ReadPatentStatus();

            for (int i = 0; i < strArr.Length; i++)
            {
                int flag = 0;
                for (int j = 0; j < charArr.Length; j++)
                {
                    if (strArr[i].Contains(charArr[j]))
                        flag++;
                }
                if (flag == charArr.Length)
                {
                    rightState = strArr[i];
                    break;
                }
            }
            return rightState;
        }
        /// <summary>
        /// 获取本机的公网IP
        /// </summary>
        public void GetIP()
        {
            string ipMsg = string.Empty;
            try
            {
                //WebProxy proxyObject = new WebProxy(IP, PORT);// port为端口号 整数型     
                WebRequest wr = WebRequest.Create("http://2018.ip138.com/ic.asp");
                // wr.Proxy = proxyObject; //设置代理
                using (Stream s = wr.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(s, Encoding.Default))
                    {
                        string all = sr.ReadToEnd(); //读取网站的数据
                        HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                        doc.LoadHtml(all);
                        HtmlNode node = doc.DocumentNode.SelectSingleNode(("//center"));
                        ipMsg = node.InnerText.Replace("您的IP是：[", "");
                        ipMsg = ipMsg.Replace("]", "");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("获取当前IP异常 " + ex.ToString());
            }
            textBox2.Invoke(new Action(() =>
            {
                textBox2.Text = ipMsg;
            }));
        }
        /// <summary>
        /// 读取本地专利状态模板
        /// </summary>
        public string[] ReadPatentStatus()
        {
            string statusPath = path + "PatentStatus.ini";
            string[] statusArr = { };

            try
            {
                statusArr = File.ReadAllLines(statusPath, Encoding.Default);
            }
            catch (Exception ex)
            {
                WriteLog("专利状态配置文件异常 " + ex.ToString());
            }
            return statusArr;
        }
        /// <summary>
        /// 更新查询的条数和更新的条数
        /// </summary>
        /// <param name="num"></param>       
        public void UpDataNumber()
        {
            try
            {
                textBox5.Invoke(new Action(() =>
                {
                    textBox5.Text = search.ToString();//成功查询总数
                }));

                textBox6.Invoke(new Action(() =>
                {
                    textBox6.Text = updata.ToString();//成功更新总数
                }));
                RecordData();
            }
            catch (Exception ex) { }
        }
        /// <summary>
        /// 记录操作数据
        /// </summary>
        public void RecordData()
        {
            try
            {
                string recordPath = path + "record\\";//记录文件夹
                DirectoryInfo dir = new DirectoryInfo(recordPath);
                if (!dir.Exists)//判断文件夹是否存在
                    dir.Create();//不存在则创建

                FileInfo[] subFiles = dir.GetFiles();//获取该文件夹下的所有文件
                foreach (FileInfo f in subFiles)
                {
                    string fname = Path.GetFileNameWithoutExtension(f.FullName); //获取文件名，没有后缀
                    DateTime start = Convert.ToDateTime(fname);//文件名转换成时间
                    DateTime end = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));//获取当前日期
                    TimeSpan sp = end.Subtract(start);//计算时间差
                    if (sp.Days > 15)//大于15天删除
                        f.Delete();
                }
                string fullPath = recordPath + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
                string contents = @"总共查询 " + searchTotal + " 次 \r\n查询成功 " + search + " 次 \r\n成功更新 " + updata + " 条 \r\n无需更新 " + unUpdata + " 条 \r\n更新失败 " + failedUpdata + " 条 \r\n";
                File.WriteAllText(fullPath, contents, Encoding.UTF8);
            }
            catch (Exception ex) { }
        }
    }
}
