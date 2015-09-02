using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;
using ServiceFabric.WcfMultiPoint.Services;

using System.ServiceModel;
using WcfServiceLib;
using ServiceFabric.WcfMultiPoint.Common;
using WcfSvcLib_SF;

namespace svc01
{
    public class svc01 : StatefulService
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {


            // a point def defines a host and a set of interfaces that will be
            // exposed as an endpoints along with Binding

            //each point defi will be exposed as the following:
            // <listeningaddress>/<host title>/interface type name
            // unless you override using OnXXX delegates


            var def1 = new PointDefinition()
            {
                HostTitle = "host1",
                Binding = new NetTcpBinding(),
                ImplementationType = typeof(WcfService),
                Interfaces = new List<Type>() { typeof(SvcInterface1), typeof(SvcInterface2) }
            };



            // this one however uses a state manager
            // from a listener point of view nothing is different
            // the listner along with WCF extention make sure that each
            // service implementation instance gets a refernce to state manager
            var def2 = new PointDefinition()
            {
                HostTitle = "host2",
                Binding = new NetTcpBinding(),
                ImplementationType = typeof(WcfSvc_SF),
                Interfaces = new List<Type>() { typeof(ISomeWcfInterface),}
            };

            

            var listener = new WcfMultiPointListener("net.tcp", def1, def2);
            listener.StateManager = this.StateManager; // this ensures that state manager used by this 
                                                       // replica instance is shared with wcf services instances.
                                                       // created by the WcfHost (irrespective of instance model) 


            // if you want to control how listening and publishing addresses are created
            //listener.OnCreateListeningAddress = (listenerInstance) => { /* return address ere */ };
            //listener.OnCreatePublishingAddress = (listenerInstance) => { /* return address ere */ };

            // if you want access to host before it opens (i.e add custom behviours, credentials etc)
            //listener.OnWcfHostCreated = (listenerInstance, pointDef, host) => { /* magic goes here*/};
            
            // same apply for WcfEndPoint
            //listener.OnWcfHostCreated = (listenerInstance, pointDef, host, endPoint, interfaceType) => { /* magic goes here*/};
            // in both cases the host and the PointDef that caused the creation is passed to you


            return listener;




            
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(0);
        }
    }
}
