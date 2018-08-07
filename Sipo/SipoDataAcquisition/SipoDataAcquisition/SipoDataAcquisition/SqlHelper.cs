using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SipoDataAcquisition
{
    public class SqlHelper
    {
        public readonly string conStr = ConfigurationManager.ConnectionStrings["ConnectionStrJXC"].ToString();

        /// <summary>
        /// 查出专利
        /// </summary>
        /// <returns></returns>
        public List<Patents> GetPatents(int day)
        {
            string sqlStr = string.Empty;
            sqlStr = "select [专利号],[法律状态],LastCollectionDate from Basic_Patent";
            List<Patents> patentList = new List<Patents>();

            SqlDataAdapter adapter = new SqlDataAdapter(sqlStr, conStr);
            DataTable ds = new DataTable();
            adapter.Fill(ds);
            foreach (DataRow dr in ds.Rows)
            {
                //string number = dr["专利编号"].ToString();
                string id = dr["专利号"].ToString();
                //string name = dr["专利名称"].ToString();
                //string instructions = dr["说明书全文"].ToString();
                //string field = dr["专利领域"].ToString();
                //string group = dr["专利分组"].ToString();
                //string certificate = dr["已有证书"].ToString();
                //string type = dr["持有类型"].ToString();
                string lawStatus = dr["法律状态"].ToString();
                //string salesStatus = dr["销售状态"].ToString();
                //string applican = dr["当前申请人"].ToString();
                //string inventor = dr["当前发明人"].ToString();
                //string monitor = dr["是否监控"].ToString();
                string lastCollectionDate = dr["LastCollectionDate"].ToString();
                if (!string.IsNullOrEmpty(lastCollectionDate))
                {
                    DateTime start = Convert.ToDateTime(lastCollectionDate);
                    DateTime end = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));//获取当前日期
                    TimeSpan ts = end.Subtract(start);
                    if (ts.Days <= day)
                        continue;
                }
                patentList.Add(new Patents()
                {
                    P_Id = id,
                    P_LawStatus = lawStatus,
                    P_LastCollectionDate = lastCollectionDate
                });
            }
            return patentList;
        }
        /// <summary>
        /// 更新专利状态
        /// </summary>
        /// <param name="status"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool UpDataStatus(string newStatus, string id,string lastupdatetime)
        {
            try
            {
                string sqlStr = "update Basic_Patent set [法律状态] = '" + newStatus + "', LastCollectionDate = '" + lastupdatetime + "' where [专利号] = '" + id + "'";
                using (SqlConnection con = new SqlConnection(conStr))
                {
                    using (SqlCommand cmd = new SqlCommand(sqlStr, con))
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    public class Patents
    {
        //public string P_Number;//专利编号;
        public string P_Id;//专利号;
        //public string P_Name;//专利名称;
        //public string P_Instructions;//说明书全文;
        //public string P_Field;//专利领域;
        //public string P_Group;//专利分组;
        //public bool P_Certificate;//已有证书;
        //public string P_Type;//持有类型;
        public string P_LawStatus;//法律状态;
        //public string P_SalesStatus;//销售状态;
        //public string P_Applicant;//当前申请人;
        //public string P_Inventor;//当前发明人;
        //public string P_Monitor;//是否监控;
        public string P_LastCollectionDate;//法律状态;

    }
}
