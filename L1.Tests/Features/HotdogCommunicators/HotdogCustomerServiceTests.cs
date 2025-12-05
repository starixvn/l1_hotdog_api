using System.Threading.Tasks;
using AutoFixture.Xunit2;
using L1.Features.PartnerCommunicators;
using L1.Features.PartnerCommunicators.EndGame;
using L1.Features.PartnerCommunicators.EnterPortal;
using L1.Features.PartnerCommunicators.GetBalance;
using L1.Features.PartnerCommunicators.GetBetLimit;
using L1.Features.PartnerCommunicators.Log;
using L1.Features.PartnerCommunicators.PlaceBet;
using L1.Features.PartnerCommunicators.VoidGame;
using L1.Features.HotdogCommunicators;
using L1.Shared.Configurations;
using NSubstitute;
using Sentry;
using Xunit;

namespace L1.Tests.Features.HotdogCommunicators
{
    public class HotdogCustomerServiceTests
    {
        private readonly HotdogEndPoints endpoints = Substitute.For<HotdogEndPoints>();
        private readonly ILogService logService = Substitute.For<ILogService>();
        private readonly IHotdogService HotdogService = Substitute.For<IHotdogService>();
        private readonly ISentryClient sentryClient = Substitute.For<ISentryClient>();
        private readonly IAppSettings appSettings = Substitute.For<IAppSettings>();
        private readonly IPartnerEndGameHandler partnerEndGameHandler = Substitute.For<IPartnerEndGameHandler>();

        private readonly HotdogCustomerService service;

        public HotdogCustomerServiceTests()
        {
            appSettings.PartnerIds = new PartnerIds
            {
                OW = "OW",
                SBO = "SBO",
                SabaPlay_HD = "Hotdog"
            };
            service = new HotdogCustomerService(HotdogService, logService, endpoints, sentryClient, appSettings, partnerEndGameHandler);
            service.OperatorId = "Hotdog";
        }

        [Theory, AutoData]
        public async Task EnterPortal_ShouldPostWithEnterPortalEndpoint(PartnerEnterPortalCall call)
        {
            // Arrange

            // Act
            await service.EnterPortal(call);

            // Assert
            await HotdogService.Received().Post<PartnerEnterPortalResult>(endpoints.EnterPortal, call, appSettings.PartnerIds.SabaPlay_HD);
        }

        [Theory, AutoData]
        public async Task GetBalance_ShouldPostWithBalanceEndpoint(PartnerGetBalanceCall call)
        {
            // Arrange
            var response = new PartnerResponse<PartnerGetBalanceResult> { Result = new PartnerGetBalanceResult { Balance = 1000 } };
            HotdogService.Post<PartnerGetBalanceResult>(endpoints.GetBalance, call, appSettings.PartnerIds.SabaPlay_HD)
                .Returns(x => Task.FromResult(response));

            // Act
            await service.GetBalance(call);

            // Assert
            await HotdogService.Received().Post<PartnerGetBalanceResult>(endpoints.GetBalance, call, appSettings.PartnerIds.SabaPlay_HD);
        }

        [Theory, AutoData]
        public async Task GetBetLimit_ShouldPostWithBetLimitEndpoint(PartnerGetBetLimitCall call)
        {
            // Arrange

            // Act
            await service.GetBetLimit(call);

            // Assert
            await HotdogService.Received().Post<PartnerGetBetLimitResult>(endpoints.GetBetLimit, call, appSettings.PartnerIds.SabaPlay_HD);
        }

        [Theory, AutoData]
        public async Task PlaceBet_SuccessfulResponse_LogBet(PartnerPlaceBetCall call)
        {
            // Arrange
            var response = new PartnerResponse<PartnerPlaceBetResult> { Result = new PartnerPlaceBetResult { Balance = 2000 } };
            HotdogService.Post<PartnerPlaceBetResult>(endpoints.PlaceBet, call, appSettings.PartnerIds.SabaPlay_HD)
                .Returns(x => Task.FromResult(response));

            // Act
            await service.PlaceBet(call, 1);

            // Assert
            await logService.Received(1).LogBetAsync(appSettings.PartnerIds.SabaPlay_HD, response, 1);
        }

        [Theory, AutoData]
        public async Task EndGame_SuccessfulResponse_LogEndGame(PartnerEndGameCall call)
        {
            // Arrange
            var response = new PartnerResponse<PartnerEndGameResult> { Result = new PartnerEndGameResult() };
            HotdogService.Post<PartnerEndGameResult>(endpoints.EndGame, call, appSettings.PartnerIds.SabaPlay_HD)
                .Returns(x => Task.FromResult(response));

            // Act
            await service.EndGame(call);

            // Assert
            await HotdogService.Received(1).Post<PartnerEndGameResult>(endpoints.EndGame, call, appSettings.PartnerIds.SabaPlay_HD);
            await logService.Received(1).LogEndGameAsync(appSettings.PartnerIds.SabaPlay_HD, response);
        }

        [Theory, AutoData]
        public async Task EndGame_SuccessfulResponse_Returns(PartnerEndGameCall call)
        {
            // Arrange
            var response = new PartnerResponse<PartnerEndGameResult> { Result = new PartnerEndGameResult() };
            HotdogService.Post<PartnerEndGameResult>(endpoints.EndGame, call, appSettings.PartnerIds.SabaPlay_HD)
                .Returns(x => Task.FromResult(response));

            // Act
            var actualReponse = await service.EndGame(call);

            // Assert
            Assert.Equal(response, actualReponse);
        }

        [Theory, AutoData]
        public async Task VoidGame_SuccessfulResponse_LogVoidGame(PartnerVoidGameCall call)
        {
            // Arrange
            var response = new PartnerResponse<PartnerVoidGameResult> { Result = new PartnerVoidGameResult() };
            HotdogService.Post<PartnerVoidGameResult>(endpoints.VoidGame, call, appSettings.PartnerIds.SabaPlay_HD)
                .Returns(x => Task.FromResult(response));

            // Act
            await service.VoidGame(call);

            // Assert
            await HotdogService.Received(1).Post<PartnerVoidGameResult>(endpoints.VoidGame, call, appSettings.PartnerIds.SabaPlay_HD);
            await logService.Received(1).LogVoidGameAsync(appSettings.PartnerIds.SabaPlay_HD, response);
        }

        [Theory, AutoData]
        public async Task VoidGame_SuccessfulResponse_Returns(PartnerVoidGameCall call)
        {
            // Arrange
            var response = new PartnerResponse<PartnerVoidGameResult> { Result = new PartnerVoidGameResult() };
            HotdogService.Post<PartnerVoidGameResult>(endpoints.VoidGame, call, appSettings.PartnerIds.SabaPlay_HD)
                .Returns(x => Task.FromResult(response));

            // Act
            var actualReponse = await service.VoidGame(call);

            // Assert
            Assert.Equal(response, actualReponse);
        }
    }
}