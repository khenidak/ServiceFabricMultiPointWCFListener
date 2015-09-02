
using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Fabric;
using System.Threading;
using ServiceFabric.WcfMultiPoint.Common;

namespace ServiceFabric.WcfMultiPoint.Clients
{
    public class WcfMultiPointCommunicationClientFactory : CommunicationClientFactoryBase<WcfMultiPointCommunicationClient>
    {
        public WcfMultiPointCommunicationClientFactory(ServicePartitionResolver resolver) : base(resolver, null, null)
        {

        }
        protected override void AbortClient(WcfMultiPointCommunicationClient client)
        {
            client.CloseAllChannels();
        }

        protected override  Task<WcfMultiPointCommunicationClient> CreateClientAsync(ResolvedServiceEndpoint endpoint, CancellationToken cancellationToken)
        {
            return Task.FromResult<WcfMultiPointCommunicationClient>(new WcfMultiPointCommunicationClient() { BaseAddress = endpoint.Address });
        }


    
        protected override bool ValidateClient(WcfMultiPointCommunicationClient clientChannel)
        {
            return (clientChannel != null);
            // todo: possible check the status of the channels.
        }

        protected override bool ValidateClient(ResolvedServiceEndpoint endpoint, WcfMultiPointCommunicationClient client)
        {
            return (client.BaseAddress == endpoint.Address);
        }
    }
}
