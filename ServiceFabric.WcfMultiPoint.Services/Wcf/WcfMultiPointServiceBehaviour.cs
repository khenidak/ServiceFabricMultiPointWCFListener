using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace ServiceFabric.WcfMultiPoint.Services
{
    class WcfMultiPointServiceBehaviour : IServiceBehavior 
    {
        private IReliableStateManager m_StateManager;

        public WcfMultiPointServiceBehaviour(IReliableStateManager StateManager)
        {
            m_StateManager = StateManager;
        }

        public void AddBindingParameters(ServiceDescription serviceDescription,
                                        System.ServiceModel.ServiceHostBase serviceHostBase,
                                        System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints,
                                        System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            // no op
        }

        public void Validate(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase)
        { }
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase)
        {
            foreach (var cdb in serviceHostBase.ChannelDispatchers)
            {
                var cd = cdb as ChannelDispatcher;

                if (cd != null)
                {
                    foreach (var ed in cd.Endpoints)
                    {
                        ed.DispatchRuntime.InstanceProvider = new WcfMultiPointInstanceProvider(m_StateManager);
                    }
                }
            }
        }

    }
}
