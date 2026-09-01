using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PosSystem.DTOs.EIS;

namespace PosSystem.Services.EIS
{
    public class EISStockService
    {
        private readonly EISApiClient _client;

        public EISStockService()
        {
            _client = new EISApiClient();
        }

        public async Task<ApiResponse<InventoryResponseData>> SubmitInventoryAdjustmentAsync(InventoryAdjustmentRequest req)
        {
            return await _client.PostAsync<InventoryAdjustmentRequest, ApiResponse<InventoryResponseData>>("/api/v1/stock/submit-adjustment", req);
        }
    }
}
