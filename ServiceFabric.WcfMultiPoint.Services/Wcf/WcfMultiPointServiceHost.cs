using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ServiceFabric.WcfMultiPoint.Services
{
    public class WcfMultiPointServiceHost : ServiceHost
    {
        private IReliableStateManager m_StateManager;
        public WcfMultiPointServiceHost(IReliableStateManager StateManager)
        {
            m_StateManager = StateManager;
        }

        public WcfMultiPointServiceHost(IReliableStateManager StateManager, 
                                        Type serviceType, params Uri[] baseAddresses) 
                                        : base(serviceType, baseAddresses)
        {
            m_StateManager = StateManager;
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            Description.Behaviors.Add(new WcfMultiPointServiceBehaviour(m_StateManager));
            base.OnOpen(timeout);
        }


    }

}
