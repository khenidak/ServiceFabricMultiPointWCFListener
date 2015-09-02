using Microsoft.ServiceFabric.Data;
using ServiceFabric.WcfMultiPoint.Common;
using ServiceFabric.WcfMultiPoint.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace ServiceFabric.WcfMultiPoint.Services
{
    public class WcfMultiPointInstanceProvider : IInstanceProvider
    {
        private IReliableStateManager m_StateManager;
        public WcfMultiPointInstanceProvider(IReliableStateManager StateManager)
        {
            m_StateManager = StateManager;
        }
        public object GetInstance(InstanceContext instanceContext)
        {
            return GetInstance(instanceContext, null);
        }

        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            // What we are trying to do is to find 
            // if the service implements a known interface (to assign statemanager to);


            //get the type assigned to host
            var type = instanceContext.Host.Description.ServiceType;

            var instance = Activator.CreateInstance(type);

            IServiceFabricWcfService StateSvc;
            var bImplementsStateMgmt = (null != (StateSvc = instance as IServiceFabricWcfService));

            // if so pass the state management as we have it from the host

            if (bImplementsStateMgmt)
                StateSvc.StateManager = m_StateManager;


            return instance;

        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            // no op
        }
    }
}