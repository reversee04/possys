using System;
using System.Threading.Tasks;
using PosSystem.DTOs.EIS;

namespace PosSystem.Services.EIS
{
    public class TerminalActivationService
    {
        private readonly EISApiClient _client;

        public TerminalActivationService()
        {
            _client = new EISApiClient();
        }

        public async Task<ApiResponse<ActivationResponse>> ActivateTerminalAsync(ActivationRequest req)
        {
            return await _client.PostAsync<ActivationRequest, ApiResponse<ActivationResponse>>("/api/v1/onboarding/activate-terminal", req);
        }

        public async Task<ApiResponse<object>> ConfirmActivationAsync(ActivationConfirmationRequest req)
        {
            return await _client.PostAsync<ActivationConfirmationRequest, ApiResponse<object>>("/api/v1/onboarding/terminal-activated-confirmation", req);
        }
    }
}
