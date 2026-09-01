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
using System.Configuration;
using System.Text;
using Microsoft.Reporting.WinForms;
using PosSystem.DTOs.EIS;

namespace PosSystem
{
    public partial class frmSettel : Form
    {

        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        SqlDataReader dr;
        DBConnection dbcon = new DBConnection();
        frmPOS fpos;
        frmStoreSetting fstore;
        public frmSettel(frmPOS fp)
        {
            InitializeComponent();
            fpos = fp;

            cn = new SqlConnection(dbcon.MyConnection());
            this.KeyPreview = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void txtCash_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double sale = double.Parse(txtSale.Text);

                double cash = double.Parse(txtCash.Text.ToString());
                double change = cash - sale;

                txtChange.Text = change.ToString("#,##0.00");





            }
            catch (Exception ex)
            {

                txtChange.Text = "0.00";
            }
        }

        private void btn7_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn7.Text;
        }

        private void btn8_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn8.Text;
        }

        private void btn9_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn9.Text;
        }

        private void btnC_Click(object sender, EventArgs e)
        {
            txtCash.Clear();
            txtCash.Focus();
        }

        private void btn4_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn4.Text;
        }

        private void btn5_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn5.Text;
        }

        private void btn6_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn6.Text;
        }

        private void btn0_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn0.Text;
        }

        private void btn1_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn1.Text;
        }

        private void btn2_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn2.Text;
        }

        private void btn3_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn3.Text;
        }

        private void btn00_Click(object sender, EventArgs e)
        {
            txtCash.Text += btn00.Text;
        }

        private void btnEnter_Click(object sender, EventArgs e)
        {
            try
            {
                if ((double.Parse(txtChange.Text) < 0) || (txtChange.Text == String.Empty))
                {
                    MessageBox.Show("Insufficient amount.Please enter the corrent amount!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    for (int i = 0; i < fpos.dataGridView1.Rows.Count; i++)
                    {
                        cn.Open();
                        cm = new SqlCommand("update TblProduct1 set qty = qty - " + int.Parse(fpos.dataGridView1.Rows[i].Cells[5].Value.ToString()) + " where pcode = '" + fpos.dataGridView1.Rows[i].Cells[2].Value.ToString() + "'", cn);
                        cm.ExecuteNonQuery();
                        cn.Close();

                        cn.Open();
                        cm = new SqlCommand("update tblCart1 set status = 'Sold' where id = '" + fpos.dataGridView1.Rows[i].Cells[1].Value.ToString() + "'", cn);
                        cm.ExecuteNonQuery();
                        cn.Close();
                    }
                    try
                    {
                        // Attempt online submission first, fallback to outbox on failure
                        var salesService = new PosSystem.Services.EIS.EISSalesService();
                        var req = BuildSubmitSalesRequest(fpos.lblTransno.Text);
                        var respTask = salesService.SubmitSaleAndPersistAsync(req);
                        respTask.Wait(10000); // wait up to 10s for sync attempt
                        var resp = respTask.IsCompleted ? respTask.Result : null;
                        if (resp == null || resp.StatusCode != 0)
                        {
                            InsertOutbox(fpos.lblTransno.Text);
                        }
                    }
                    catch
                    {
                        // Do not block checkout if outbox insert fails.
                    }
                    frmResipt frm = new frmResipt(fpos);
                    frm.LoadReport(txtCash.Text, txtChange.Text);
                    frm.ShowDialog();


                    MessageBox.Show("Payment successfully saved!", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    fpos.GetTransNo();
                    fpos.LoadCart();
                    this.Dispose();

                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Insufficient amount.Please enter the correct amount!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void frmSettel_Load(object sender, EventArgs e)
        {

        }

        private void frmSettel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Dispose();

            }
            else if (e.KeyCode == Keys.Enter)
            {
                btnEnter_Click(sender, e);
            }
        }

        private void txtSale_TextChanged(object sender, EventArgs e)
        {

        }

        private void InsertOutbox(string transno)
        {
            var enabled = ConfigurationManager.AppSettings["EIS.Enabled"] ?? "false";
            if (!enabled.Equals("true", StringComparison.OrdinalIgnoreCase)) return;

            // Build a simple JSON payload from tblCart1 rows for the transno
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"invoice_number\":\"{0}\",", transno);
            sb.Append("\"lines\":[");

            using (var cn = new SqlConnection(dbcon.MyConnection()))
            {
                cn.Open();
                var cm = new SqlCommand("select c.pcode, p.pdesc, c.qty, c.price, c.total from tblCart1 c join TblProduct1 p on c.pcode = p.pcode where c.transno = @transno", cn);
                cm.Parameters.AddWithValue("@transno", transno);
                using (var r = cm.ExecuteReader())
                {
                    bool first = true;
                    while (r.Read())
                    {
                        if (!first) sb.Append(",");
                        first = false;
                        var pcode = (r[0] == DBNull.Value) ? "" : r.GetString(0).Replace("\"", "'");
                        var pdesc = (r[1] == DBNull.Value) ? "" : r.GetString(1).Replace("\"", "'");
                        var qty = (r[2] == DBNull.Value) ? 0 : r.GetInt32(2);
                        var price = (r[3] == DBNull.Value) ? 0.0m : r.GetDecimal(3);
                        var total = (r[4] == DBNull.Value) ? 0.0m : r.GetDecimal(4);
                        sb.AppendFormat("{{\"pcode\":\"{0}\",\"desc\":\"{1}\",\"qty\":{2},\"price\":{3},\"total\":{4}}}", pcode, pdesc, qty, price, total);
                    }
                }
            }

            sb.Append("]}");

            // Insert to EIS_Outbox
            using (var cn = new SqlConnection(dbcon.MyConnection()))
            {
                cn.Open();
                var cm = new SqlCommand("INSERT INTO EIS_Outbox (transno, payload) VALUES (@transno, @payload)", cn);
                cm.Parameters.AddWithValue("@transno", transno);
                cm.Parameters.AddWithValue("@payload", sb.ToString());
                cm.ExecuteNonQuery();
            }
        }

        private SubmitSalesRequest BuildSubmitSalesRequest(string transno)
        {
            var req = new SubmitSalesRequest();
            req.InvoiceNumber = transno;
            req.TransactionDate = DateTime.Now;
            req.Tin = "";
            req.Items = new System.Collections.Generic.List<SaleItemDto>();

            using (var cn = new SqlConnection(dbcon.MyConnection()))
            {
                cn.Open();
                var cm = new SqlCommand("select c.pcode, p.pdesc, c.qty, c.price, c.total from tblCart1 c join TblProduct1 p on c.pcode = p.pcode where c.transno = @transno", cn);
                cm.Parameters.AddWithValue("@transno", transno);
                using (var r = cm.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var item = new SaleItemDto();
                        item.ProductCode = r.IsDBNull(0) ? "" : r.GetString(0);
                        item.Description = r.IsDBNull(1) ? "" : r.GetString(1);
                        item.Quantity = r.IsDBNull(2) ? 0 : r.GetDecimal(2);
                        item.UnitPrice = r.IsDBNull(3) ? 0 : r.GetDecimal(3);
                        item.TaxAmount = 0;
                        req.Items.Add(item);
                    }
                }
            }

            return req;
        }


    }
}
