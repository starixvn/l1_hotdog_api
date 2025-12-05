using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using L1.Features.PartnerCommunicators;
using L1.Features.PartnerCommunicators.Award;
using L1.Features.PartnerCommunicators.EndGame;
using L1.Features.PartnerCommunicators.EnterPortal;
using L1.Features.PartnerCommunicators.GetBalance;
using L1.Features.PartnerCommunicators.GetBetLimit;
using L1.Features.PartnerCommunicators.HitJackpot;
using L1.Features.PartnerCommunicators.Log;
using L1.Features.PartnerCommunicators.PlaceBet;
using L1.Features.PartnerCommunicators.VoidGame;
using L1.Shared.Configurations;
using L1.Shared.Constants;
using Polly;
using Sentry;

namespace L1.Features.HotdogCommunicators
{
    public class HotdogCustomerService : IPartnerCustomerServices
    {
        private readonly IHotdogService HotdogService;
        private readonly ILogService logService;
        private readonly HotdogEndPoints endpoints;
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private readonly IPartnerEndGameHandler partnerEndGameHandler;

        public HotdogCustomerService(
            IHotdogService HotdogService,
            ILogService logService,
            HotdogEndPoints endpoints,
            ISentryClient sentryClient,
            IAppSettings appSettings,
            IPartnerEndGameHandler partnerEndGameHandler)
        {
            this.HotdogService = HotdogService;
            this.logService = logService;
            this.endpoints = endpoints;
            this.sentryClient = sentryClient;
            this.appSettings = appSettings;
            this.partnerEndGameHandler = partnerEndGameHandler;
        }

        public string OperatorId { get; set; }

        public Task<PartnerResponse<PartnerEnterPortalResult>> EnterPortal(PartnerEnterPortalCall call)
        => HotdogService.Post<PartnerEnterPortalResult>(endpoints.EnterPortal, call, OperatorId);

        public async Task<PartnerResponse<PartnerGetBalanceResult>> GetBalance(PartnerGetBalanceCall call)
        {
            var response = await HotdogService.Post<PartnerGetBalanceResult>(endpoints.GetBalance, call, OperatorId);
            sentryClient.CaptureMessage($"GetBalance Response From Hotdog: {JsonSerializer.Serialize(response)}");

            if (response.Result.IsSuccessful())
            {
                sentryClient.CaptureMessage($"Balance After Get Balance From Hotdog: {response.Result.Balance}");
            }

            return response;
        }

        public Task<PartnerResponse<PartnerGetBetLimitResult>> GetBetLimit(PartnerGetBetLimitCall call)
        => HotdogService.Post<PartnerGetBetLimitResult>(endpoints.GetBetLimit, call, OperatorId);

        public async Task<PartnerResponse<PartnerPlaceBetResult>> PlaceBet(PartnerPlaceBetCall call, byte system = AppTypes.SabaClub, Dictionary<string, string> metadata = null)
        {
            var response = await HotdogService.Post<PartnerPlaceBetResult>(endpoints.PlaceBet, call, OperatorId);

            if (response.Result.IsSuccessful())
            {
                await logService.LogBetAsync(appSettings.PartnerIds.SabaPlay_HD, response, system);
            }

            return response;
        }

        public async Task<PartnerResponse<PartnerEndGameResult>> EndGame(PartnerEndGameCall call)
        {
            var response = await HotdogService.Post<PartnerEndGameResult>(endpoints.EndGame, call, OperatorId);

            if (response.Result.IsSuccessful())
            {
                await logService.LogEndGameAsync(OperatorId, response);
            } else
            {
                await partnerEndGameHandler.HandleFailedEndGame(call, response, OperatorId);
            }

            return response;
        }

        public async Task<PartnerResponse<PartnerVoidGameResult>> VoidGame(PartnerVoidGameCall call)
        {
            var response = await HotdogService.Post<PartnerVoidGameResult>(endpoints.VoidGame, call, OperatorId);

            if (response.Result.IsSuccessful())
            {
                await logService.LogVoidGameAsync(appSettings.PartnerIds.SabaPlay_HD, response);
            }

            return response;
        }

        public async Task<PartnerResponse<HitJackpotResult>> HitJackpot(HitJackpotCall call)
        {
            const double retryDelayInSeconds = 30;
            const int numberOfRetries = 6;

            var response = await Policy
                .HandleResult<PartnerResponse<HitJackpotResult>>(response => response.Result.IsFailed())
                .WaitAndRetryAsync(numberOfRetries, retryAttempt => TimeSpan.FromSeconds(retryDelayInSeconds))
                .ExecuteAsync(() => HotdogService.Post<HitJackpotResult>(endpoints.HitJackpot, call, OperatorId));

            if (response.Result.IsSuccessful())
            {
                await logService.LogHitJackpotAsync(appSettings.PartnerIds.SabaPlay_HD, response);
            }
            else
            {
                sentryClient.CaptureMessage($"Call HitJackpot failed for Partner {appSettings.PartnerIds.SabaPlay_HD} - CustId {call.PlayerId}", SentryLevel.Fatal);
            }

            return response;
        }

        public async Task<PartnerResponse<PartnerAwardResult>> Award(PartnerAwardCall call)
        {
            var response = await HotdogService.Post<PartnerAwardResult>(endpoints.Award, call, OperatorId);

            if (response.Result.IsSuccessful())
            {
                await logService.LogAwardAsync(OperatorId, response);
            }
            else
            {
                sentryClient.CaptureMessage($"Call Award failed for Partner {appSettings.PartnerIds.SabaPlay_HD} - CustId {call.PlayerId}", SentryLevel.Fatal);
            }

            return response;
        }
    }
}