using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using PosSystem.DTOs.EIS;

namespace PosSystem.Services.EIS
{
    public class EISApiClient
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public EISApiClient()
        {
            // Prefer EIS.BaseUrl but fall back to EIS.ApiUrl for backwards compatibility
            _baseUrl = ConfigurationManager.AppSettings["EIS.BaseUrl"] ?? ConfigurationManager.AppSettings["EIS.ApiUrl"] ?? string.Empty;
            _apiKey = ConfigurationManager.AppSettings["EIS.ApiKey"] ?? string.Empty;
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string relativePath, TRequest payload)
        {
            var url = _baseUrl?.TrimEnd('/') + relativePath;
            var json = Serialize(payload);

            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(30);
                if (!string.IsNullOrEmpty(_apiKey))
                    http.DefaultRequestHeaders.Add("x-api-key", _apiKey);

                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var resp = await http.PostAsync(url, content))
                {
                    resp.EnsureSuccessStatusCode();
                    using (var stream = await resp.Content.ReadAsStreamAsync())
                    {
                        return Deserialize<TResponse>(stream);
                    }
                }
            }
        }

        private string Serialize<T>(T obj)
        {
            if (obj == null) return string.Empty;
            var ser = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private T Deserialize<T>(Stream stream)
        {
            if (stream == null) return default(T);
            var ser = new DataContractJsonSerializer(typeof(T));
            return (T)ser.ReadObject(stream);
        }

        // Convenience methods for common API calls (skeletons)
        public Task<ApiResponse<PongResponse>> PingAsync()
        {
            return PostAsync<object, ApiResponse<PongResponse>>("/api/v1/utilities/ping", null);
        }

        public Task<ApiResponse<SubmitSalesResponseData>> SubmitSalesTransactionAsync(SubmitSalesRequest request)
        {
            return PostAsync<SubmitSalesRequest, ApiResponse<SubmitSalesResponseData>>("/api/v1/sales/submit-sales-transaction", request);
        }
    }
}
