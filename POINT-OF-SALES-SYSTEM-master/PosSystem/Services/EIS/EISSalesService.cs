using System;
using System.Threading.Tasks;
using PosSystem.DTOs.EIS;

namespace PosSystem.Services.EIS
{
    public class EISSalesService
    {
        private readonly EISApiClient _client;

        public EISSalesService()
        {
            _client = new EISApiClient();
        }

        public async Task<ApiResponse<SubmitSalesResponseData>> SubmitSaleAsync(SubmitSalesRequest request)
        {
            return await _client.SubmitSalesTransactionAsync(request);
        }

        public async Task<ApiResponse<SubmitSalesResponseData>> SubmitSaleAndPersistAsync(SubmitSalesRequest request)
        {
            // Call API
            var resp = await SubmitSaleAsync(request);

            // persist EISTransaction record into DB using DBConnection
            try
            {
                var db = new DBConnection();
                using (var cn = new System.Data.SqlClient.SqlConnection(db.MyConnection()))
                {
                    cn.Open();
                    var cm = new System.Data.SqlClient.SqlCommand("INSERT INTO EISTransaction (transno, submitted_at, status, mra_reference, validation_data, qr_data, error_code, error_message) VALUES (@transno, GETDATE(), @status, @mra, @validation, @qr, @errcode, @err)", cn);
                    cm.Parameters.AddWithValue("@transno", request.InvoiceNumber ?? string.Empty);
                    cm.Parameters.AddWithValue("@status", resp?.StatusCode == 0 ? "Submitted" : "Failed");
                    cm.Parameters.AddWithValue("@mra", (object)resp?.Data?.MraReference ?? string.Empty);
                    cm.Parameters.AddWithValue("@validation", (object)resp?.Data?.ValidationData ?? string.Empty);
                    cm.Parameters.AddWithValue("@qr", (object)resp?.Data?.QrData ?? string.Empty);
                    cm.Parameters.AddWithValue("@errcode", (object)null ?? string.Empty);
                    cm.Parameters.AddWithValue("@err", (object)resp?.Remark ?? string.Empty);
                    cm.ExecuteNonQuery();
                }
            }
            catch
            {
                // swallow; logging table will catch detailed errors
            }

            return resp;
        }
    }
}
