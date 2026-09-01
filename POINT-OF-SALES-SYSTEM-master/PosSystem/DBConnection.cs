using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace PosSystem
{
    public class DBConnection
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        SqlDataReader dr;
        private double dailySales;
        private int productline;
        private int Stockonhand;
        private int craticalitems;
        private string con;

        public string MyConnection()
        {
            // Update this connection string to match your SQL Server instance.
            return @"Server=localhost\SQLEXPRESS; Database=PosDB; Trusted_Connection=True;";
        }

        public double DailySales()
        {
            using (SqlConnection cn = new SqlConnection(MyConnection()))
            {
                cn.Open();
                using (SqlCommand cm = new SqlCommand("select isnull(sum(total), 0) from tblCart1 where status like 'Sold' and cast(sdate as date) = cast(getdate() as date)", cn))
                {
                    return Convert.ToDouble(cm.ExecuteScalar());
                }
            }
        }

        public int ProductLine()
        {
            using (SqlConnection cn = new SqlConnection(MyConnection()))
            {
                cn.Open();
                using (SqlCommand cm = new SqlCommand("select count(*) from TblProduct1", cn))
                {
                    return Convert.ToInt32(cm.ExecuteScalar());
                }
            }
        }

        public int StockOnHand()
        {
            using (SqlConnection cn = new SqlConnection(MyConnection()))
            {
                cn.Open();
                using (SqlCommand cm = new SqlCommand("select isnull(sum(qty), 0) from TblProduct1", cn))
                {
                    return Convert.ToInt32(cm.ExecuteScalar());
                }
            }
        }

        public int CraticalItems()
        {
            using (SqlConnection cn = new SqlConnection(MyConnection()))
            {
                cn.Open();
                using (SqlCommand cm = new SqlCommand("select count(*) from vwCriticalItems", cn))
                {
                    return Convert.ToInt32(cm.ExecuteScalar());
                }
            }
        }

        public double GetVal()
        {
            // Return the VAT rate used by POS calculations.
            // Update this value if your store uses a different rate or if the rate is stored in the database.
            return 0.12;
        }
    }
}
