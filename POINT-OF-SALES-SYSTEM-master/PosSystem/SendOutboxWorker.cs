using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace PosSystem
{
    public class SendOutboxWorker
    {
        private readonly string _apiUrl;
        private readonly string _apiKey;

        public SendOutboxWorker()
        {
            _apiUrl = ConfigurationManager.AppSettings["EIS.ApiUrl"] ?? string.Empty;
            _apiKey = ConfigurationManager.AppSettings["EIS.ApiKey"] ?? string.Empty;
        }

        public async Task ProcessOutboxAsync(int maxRows = 20)
        {
            var db = new DBConnection();
            using (var cn = new SqlConnection(db.MyConnection()))
            {
                await cn.OpenAsync();
                var cmd = new SqlCommand("SELECT TOP (@top) id, transno, payload, attempts FROM EIS_Outbox WHERE status = 'Pending' ORDER BY created_at", cn);
                cmd.Parameters.AddWithValue("@top", maxRows);
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        int id = r.GetInt32(0);
                        string payload = r.IsDBNull(2) ? string.Empty : r.GetString(2);
                        await SendAndMarkAsync(id, payload, db.MyConnection());
                    }
                }
            }
        }

        private async Task SendAndMarkAsync(int id, string payload, string connString)
        {
            using (var http = new HttpClient())
            {
                if (!string.IsNullOrEmpty(_apiKey))
                    http.DefaultRequestHeaders.Add("x-api-key", _apiKey);

                var content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json");
                string respText = string.Empty;
                bool success = false;
                try
                {
                    var resp = await http.PostAsync(_apiUrl, content);
                    respText = await resp.Content.ReadAsStringAsync();
                    success = resp.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    respText = ex.ToString();
                    success = false;
                }

                using (var cn = new SqlConnection(connString))
                {
                    await cn.OpenAsync();
                    var update = new SqlCommand("UPDATE EIS_Outbox SET attempts = attempts + 1, last_attempt = GETDATE(), status = @status, response = @resp WHERE id = @id", cn);
                    update.Parameters.AddWithValue("@id", id);
                    update.Parameters.AddWithValue("@resp", (object)respText ?? string.Empty);
                    update.Parameters.AddWithValue("@status", success ? "Sent" : "Failed");
                    await update.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
