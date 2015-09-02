using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceFabric.Data;

using Microsoft.ServiceFabric.Data.Collections;
using ServiceFabric.WcfMultiPoint.Services.Common;

namespace WcfSvcLib_SF
{

    // this is a sample service that while we are migrating to Service Fabric hosting
    // we decided to use Service Fabric Reliable State interfaces. 

    // in this case the service implementation (in addition to traditional wcf contracts)
    // needs to IServiceFabricWcfService which where the state manager will be commnuicated


    public class WcfSvc_SF : IServiceFabricWcfService, ISomeWcfInterface
    {
        private readonly string m_QueueName = "HelloMessages";

        //IServiceFabricWcfService
        public IReliableStateManager StateManager { get; set; }


        // ISomeWcfInterface - implementation uses Service Fabric reliable state manager
        public int GetHello()
        {
            var queue = StateManager.GetOrAddAsync<IReliableQueue<string>>(m_QueueName).Result;

            return queue.Count(); // this is a dirty read 

            
        }

        public string SayHello(string Message)
        {
            var queue = StateManager.GetOrAddAsync<IReliableQueue<string>>(m_QueueName).Result;

            using (var tx = this.StateManager.CreateTransaction())
            {
                queue.EnqueueAsync(tx, Message);
                
                tx.CommitAsync().Wait();
            }

            return string.Concat("Hello ", Message);
        }
    }
}
