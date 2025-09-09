using System;
using System.Threading.Tasks;
using L1.Shared.Configurations;
using L1.Shared.Json;
using NATS.Client;
using NATS.Client.JetStream;
using Sentry;

namespace L1.Features.PartnerCommunicators
{
    public interface INatsService
    {
        Task PublishAsync(string subject, object message);
    }

    public class NatsService : INatsService
    {
        private readonly IJetStream jetStream;
        private readonly string subjectPrefix;
        private readonly bool enable;
        private readonly int publishTimeoutMs;
        private readonly ISentryClient sentryClient;
        private readonly IJsonService jsonService;

        public NatsService(
            IAppSettings appSettings,
            ISentryClient sentryClient,
            IJsonService jsonService)
        {
            this.subjectPrefix = appSettings.StreamSettings.SubjectPrefix;
            this.enable = appSettings.StreamSettings.Enable;
            this.publishTimeoutMs = appSettings.StreamSettings.PublishTimeoutMs;
            this.sentryClient = sentryClient;
            this.jsonService = jsonService;
        }

        public async Task PublishAsync(string subject, object message)
        {
            if (!enable)
            {
                return;
            }

            var formattedSubject = SubjectFormatter.FormatSubject(subjectPrefix, subject);
            var jsonMessage = jsonService.Serialize(message);
            var msg = new Msg(formattedSubject, (MsgHeader)null, System.Text.Encoding.UTF8.GetBytes(jsonMessage));

            try
            {
                var publishOpts = PublishOptions.Builder()
                    .WithTimeout(publishTimeoutMs)
                    .WithMessageId(Guid.NewGuid().ToString())
                    .Build();

                await jetStream.PublishAsync(msg, publishOpts);
            }
            catch (NATSTimeoutException ex)
            {
                sentryClient.CaptureException(ex);
            }
            catch (NATSException ex)
            {
                sentryClient.CaptureException(ex);
            }
        }
    }
}