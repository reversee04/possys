using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using PosSystem.DTOs.EIS;
namespace PosSystem
{
    public partial class frmStockin : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        SqlDataReader dr;
        DBConnection dbcon = new DBConnection();
        string stitle = "PosSystem";

        public frmStockin()
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.MyConnection());
            LoadVendor();
        }

        private void frmStockin_Load(object sender, EventArgs e)
        {

        }



        public void LoadStockIn()
        {
            int i = 0;
            dataGridView2.Rows.Clear();
            cn.Open();
            cm = new SqlCommand("select * from vwStockin where refno like '" + txtRefNo.Text + "' and status like 'Pending'", cn);
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                dataGridView2.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString(), dr["vender"].ToString());
            }
            dr.Close();
            cn.Close();

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }




        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string colName = dataGridView2.Columns[e.ColumnIndex].Name;
            if (colName == "Delete")
            {
                if (MessageBox.Show("Remove this item?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SqlCommand("delete from tblStockin where id = '" + dataGridView2.Rows[e.RowIndex].Cells[1].Value.ToString() + "'", cn);
                    cm.ExecuteNonQuery();
                    cn.Close();
                    MessageBox.Show("Item has been successfully deleted", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadStockIn();
                }
            }
        }
        public void Clear()
        {
            txtBy.Clear();
            txtRefNo.Clear();
            dt1.Value = DateTime.Now;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmSearchProductStokin frm = new frmSearchProductStokin(this);
            frm.LoadProduct();
            frm.ShowDialog();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView2.Rows.Count > 0)
                {
                    if (MessageBox.Show("Are you sure you want to save this record?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        for (int i = 0; i < dataGridView2.Rows.Count; i++)
                        {
                            // update product qty
                            cn.Open();
                            cm = new SqlCommand("update  TblProduct1 set qty = qty + " + int.Parse(dataGridView2.Rows[i].Cells[5].Value.ToString()) + " where pcode like '" + dataGridView2.Rows[i].Cells[3].Value.ToString() + "'", cn);
                            cm.ExecuteNonQuery();
                            cn.Close();

                            // update stockin qty
                            cn.Open();
                            cm = new SqlCommand("update tblStockin set qty = qty +" + int.Parse(dataGridView2.Rows[i].Cells[5].Value.ToString()) + ",status = 'Done' where id like  '" + dataGridView2.Rows[i].Cells[1].Value.ToString() + "'", cn);
                            cm.ExecuteNonQuery();
                            cn.Close();
                        }
                        // Build inventory adjustment request and attempt submission
                        try
                        {
                            var svc = new PosSystem.Services.EIS.EISStockService();
                            var req = BuildInventoryAdjustmentRequest(txtRefNo.Text);
                            var task = svc.SubmitInventoryAdjustmentAsync(req);
                            task.Wait(10000);
                            var resp = task.IsCompleted ? task.Result : null;
                            if (resp != null && resp.StatusCode == 0)
                            {
                                // Optionally persist response to EISLogs or EISTransaction
                                try
                                {
                                    var db = new DBConnection();
                                    using (var cnn = new SqlConnection(db.MyConnection()))
                                    {
                                        cnn.Open();
                                        var cm2 = new SqlCommand("INSERT INTO EISLogs (operation, invoiceNumber, status, errorMessage) VALUES (@op, @inv, @st, @err)", cnn);
                                        cm2.Parameters.AddWithValue("@op", "SubmitInventoryAdjustment");
                                        cm2.Parameters.AddWithValue("@inv", txtRefNo.Text);
                                        cm2.Parameters.AddWithValue("@st", resp.Data?.Status ?? "");
                                        cm2.Parameters.AddWithValue("@err", resp.Remark ?? string.Empty);
                                        cm2.ExecuteNonQuery();
                                    }
                                }
                                catch { }
                            }
                            else
                            {
                                // on failure, create outbox entry for retry
                                try { InsertInventoryOutbox(txtRefNo.Text, req); } catch { }
                            }
                        }
                        catch { }
                        Clear();
                        LoadStockIn();
                    }
                }
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void LoadStockHistory()
        {
            int i = 0;
            dataGridView1.Rows.Clear();
            cn.Open();
            cm = new SqlCommand("select * from vwStockin where cast(sdate as date) between '" + date1.Value.ToShortDateString() + "' and '" + date2.Value.ToShortDateString() + "' and status like 'Done'", cn);
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                dataGridView1.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), DateTime.Parse(dr[5].ToString()).ToShortDateString(), dr[6].ToString(), dr["vender"].ToString());
            }
            dr.Close();
            cn.Close();

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void load_Click(object sender, EventArgs e)
        {
            LoadStockHistory();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void txtSearch_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        public void LoadVendor()
        {
            cbVendor.Items.Clear();
            cn.Open();
            cm = new SqlCommand("select * from tblVendor", cn);
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                cbVendor.Items.Add(dr["vender"].ToString());
            }
            dr.Close();
            cn.Close();

        }

        private InventoryAdjustmentRequest BuildInventoryAdjustmentRequest(string refno)
        {
            var req = new InventoryAdjustmentRequest();
            req.ReferenceNo = refno;
            req.Items = new System.Collections.Generic.List<InventoryItemDto>();

            using (var cn = new SqlConnection(dbcon.MyConnection()))
            {
                cn.Open();
                var cm = new SqlCommand("select pcode, pdesc, qty, price from tblStockin where refno = @refno", cn);
                cm.Parameters.AddWithValue("@refno", refno);
                using (var r = cm.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var item = new InventoryItemDto();
                        item.ProductCode = r.IsDBNull(0) ? "" : r.GetString(0);
                        item.Description = r.IsDBNull(1) ? "" : r.GetString(1);
                        item.Quantity = r.IsDBNull(2) ? 0 : r.GetDecimal(2);
                        item.UnitCost = r.IsDBNull(3) ? 0 : r.GetDecimal(3);
                        req.Items.Add(item);
                    }
                }
            }

            return req;
        }

        private void InsertInventoryOutbox(string refno, InventoryAdjustmentRequest req)
        {
            var payload = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(InventoryAdjustmentRequest));
            using (var ms = new System.IO.MemoryStream())
            {
                payload.WriteObject(ms, req);
                var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                using (var cn = new SqlConnection(dbcon.MyConnection()))
                {
                    cn.Open();
                    var cm = new SqlCommand("INSERT INTO EIS_Outbox (transno, payload) VALUES (@transno, @payload)", cn);
                    cm.Parameters.AddWithValue("@transno", refno);
                    cm.Parameters.AddWithValue("@payload", json);
                    cm.ExecuteNonQuery();
                }
            }
        }

        private void cbVendor_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void cbVendor_TextChanged(object sender, EventArgs e)
        {
            cn.Open();
            cm = new SqlCommand("select * from tblVendor where vender like '" + cbVendor.Text + "'", cn);
            dr = cm.ExecuteReader();
            dr.Read();
            if (dr.HasRows)
            {
                lblVendorID.Text = dr["id"].ToString();
                txtAddress.Text = dr["address"].ToString();
                txtPerson.Text = dr["contactperson"].ToString();
            }
            dr.Close();
            cn.Close();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Random rnd = new Random();
            txtRefNo.Clear();
            // int i = 0;
            //for(i = 0; i < 10; i++)
            //{
            txtRefNo.Text += rnd.Next();
            // }
        }

        private void cbVendor_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
