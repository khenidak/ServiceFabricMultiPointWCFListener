using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Fabric;
using System.Threading;
using System.ServiceModel.Channels;
using System.Collections.ObjectModel;
using System.Globalization;
using System.ServiceModel;
using System.Collections.Concurrent;
using System.Diagnostics;
using ServiceFabric.WcfMultiPoint.Common;
using Microsoft.ServiceFabric.Data;

namespace ServiceFabric.WcfMultiPoint.Services
{ 
    public class WcfMultiPointListener : ICommunicationListener
    {
        
        private string m_Scheme = string.Empty;
        private Binding m_OverrideBinding = null;
        private ListenerStatus m_ListenerStatus = ListenerStatus.Stopped;

        private List<PointDefinition> m_listeningDefinitions = null; 
        
        public Func<WcfMultiPointListener,string> OnCreateListeningAddress = null;
        public Func<WcfMultiPointListener,string> OnCreatePublishingAddress = null;

        private ServiceInitializationParameters m_serviceInitializationParameters = null;

        private string m_listeningAddress = string.Empty;
        private string m_publishingAddress = string.Empty;


        private ConcurrentDictionary<string, WcfMultiPointServiceHost> m_hosts = new ConcurrentDictionary<string, WcfMultiPointServiceHost>();

        public Action<WcfMultiPointListener, PointDefinition,WcfMultiPointServiceHost>
            OnWcfHostCreated = null;

        public Action<WcfMultiPointListener, 
                      PointDefinition, 
                      WcfMultiPointServiceHost, 
                      System.ServiceModel.Description.ServiceEndpoint,
                      Type>
            OnWcfEndpointAdded = null;


        public IReliableStateManager StateManager
        { get; set; }

        public ListenerStatus Status
        {
            get { return m_ListenerStatus; }
        }

        public string ListeningAddress
        {
            get { return m_listeningAddress; }
        }

        public string PublishingAddres
        {
            get { return m_publishingAddress; }
        }


        public string Scheme
        {
            get { return m_Scheme; }
            set {
                verifyScheme(value);
                m_Scheme = value;
            }
        }
        public List<PointDefinition> ListeningDefinitions
        {

            get { return m_listeningDefinitions; }
            set {
                verifyDefs(value);
                m_listeningDefinitions = value;
            }
        }

        
        public WcfMultiPointListener(string scheme)
        {
            verifyScheme(scheme);
            m_Scheme = scheme;
        }

        public WcfMultiPointListener(params PointDefinition[] defs)
        {
            verifyDefs(defs);
            m_listeningDefinitions = new List<PointDefinition>(defs);
        }

        public WcfMultiPointListener(string scheme, params PointDefinition[] defs) :
            this(scheme)
        {

            verifyDefs(defs);
            m_listeningDefinitions = new List<PointDefinition>(defs);
        }


        public WcfMultiPointListener(PointDefinition[] defs, string Scheme, Binding overrideBinding)
            : this(Scheme, defs)
        {
            if (null == overrideBinding)
                throw new NullReferenceException("overrideBinding");

            m_OverrideBinding = overrideBinding;
        }

        private void verifyScheme(string scheme)
        {
            if (m_ListenerStatus != ListenerStatus.Stopped)
                throw new InvalidOperationException("can not change schema while listener is not in stopped state");

            if (null == scheme)
                throw new InvalidOperationException("scheme can not be null");

            if(string.Empty == scheme)
                throw new InvalidOperationException("scheme can not be empty");


        }


        private void verifyDefs(List<PointDefinition> defs)
        {
            if (null == defs)
                throw new InvalidOperationException("listeing definition is null");

            

            verifyDefs(defs.ToArray());
        }
        private void verifyDefs(PointDefinition[] defs)
        {
            if (null == defs)
                throw new InvalidOperationException("listeing definition is null");

            if (0 == defs.Count())
                throw new Exception("Listening definition(s) is empty");

            // are they unique?
            foreach (var def in defs)
                if (1 != defs.Where(current => current.HostTitle == def.HostTitle).Count())
                    throw new InvalidOperationException("Host titles has to be unique");

            var bVerifyBinding = (null == m_OverrideBinding);

            foreach (var def in defs)
                def.verifyAndThrow( bVerifyBinding);

        }


        private void EnsureDelegates()
        {
            if (null == OnWcfHostCreated)
                OnWcfHostCreated = (listener,pointDef ,host) => { };

            if (null == OnWcfEndpointAdded)
                OnWcfEndpointAdded = (listener, pointDef, host, ep, type) => { };


            // default create functions use the init params. 
            // if the user override them he will have to use init params in the service class base.Initparam..
            if (null == OnCreateListeningAddress)
                OnCreateListeningAddress = (thisInstance) =>
                {
                    StatefulServiceInitializationParameters statefulInitParam;
                    
                    var bIsStateful = (null != (statefulInitParam = thisInstance.m_serviceInitializationParameters as StatefulServiceInitializationParameters));                    
                    var port = thisInstance.m_serviceInitializationParameters.CodePackageActivationContext.GetEndpoint("ServiceEndPoint").Port;

                    // net.tcp listner doesn't like netssh + because it is build on top of the port sharing 
                    // which does not like it. we are directly using the host name
                    // in a cluser scenario we will use the public facing load balancer Uri
                    // driven from a configurable variable

                    if (bIsStateful)
                        return String.Format(
                                    CultureInfo.InvariantCulture,
                                    "{0}://{1}:{2}/{3}/{4}/",
                                    thisInstance.m_Scheme,
                                    FabricRuntime.GetNodeContext().IPAddressOrFQDN, 
                                    port,
                                    statefulInitParam.PartitionId,
                                    statefulInitParam.ReplicaId);
                    else
                        return String.Format(
                                CultureInfo.InvariantCulture,
                                "{0}://{1}:{2}/",
                                thisInstance.m_Scheme,
                                FabricRuntime.GetNodeContext().IPAddressOrFQDN,
                                port);
                };




            if (null == OnCreatePublishingAddress)
                OnCreatePublishingAddress = (thisInstance) =>
                {
                    // net.tcp listner doesn't like netssh + because it is build on top of the port sharing 
                    // which does not like it
                    return thisInstance.m_listeningAddress;
                    //return this.m_listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);
                };

        }




        public void Abort()
        {
            // force stop
            foreach (var def in m_listeningDefinitions)
            {
                StopHost(def);
                Trace.WriteLine(string.Format("Host {0} has been aborted", def.HostTitle));
            }

            m_ListenerStatus = ListenerStatus.Stopped;

        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                List<Task> stopTasks = new List<Task>(m_listeningDefinitions.Count());

                foreach (var def in m_listeningDefinitions)
                    stopTasks.Add(Task.Run(() => StopHost(def), cancellationToken));


                await Task.WhenAll(stopTasks);

                m_ListenerStatus = ListenerStatus.Stopped;

            }
            catch (TaskCanceledException tce)
            {
                // start canceled 
                Trace.WriteLine(string.Format("Multipoint listener cancled while trying to stop, trying to abort(error:{0})", tce.Message));
                Abort();
            }
            catch (AggregateException ae)
            {
                Abort();
                // something is wrong
                Trace.WriteLine(string.Format("Multi point listener failed to start with error {0} stack:{1}", ae.Message, ae.StackTrace));
                throw new AggregateException(ae);
            }

            catch (Exception E)
            {
                Abort();
                // something is wrong
                Trace.WriteLine(string.Format("Multi point listener failed to start with error {0} stack:{1}", E.Message, E.StackTrace));
                throw new AggregateException(E);
            }

            m_ListenerStatus = ListenerStatus.Stopped;
        }

        public void Initialize(ServiceInitializationParameters serviceInitializationParameters)
        {
            verifyDefs(m_listeningDefinitions);
            m_serviceInitializationParameters = serviceInitializationParameters;

            EnsureDelegates();

            m_listeningAddress = OnCreateListeningAddress(this);
            m_publishingAddress = OnCreatePublishingAddress(this);
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            if (m_ListenerStatus != ListenerStatus.Stopped)
                throw new InvalidOperationException("listener is not in stopped state");



            m_ListenerStatus = ListenerStatus.Starting;
            try
            {
                List<Task> startTasks = new List<Task>(m_listeningDefinitions.Count());

                foreach (var def in m_listeningDefinitions)
                    startTasks.Add(Task.Run(() => AddStartHost(def), cancellationToken));


                await Task.WhenAll(startTasks);

            }
            catch (TaskCanceledException tce)
            {
                Trace.WriteLine(string.Format("Multipoint listener cancled while trying to start, trying to abort (error:{0})", tce.Message));
                Abort();
            }
            catch (AggregateException ae)
            {
                Abort();
                // something is wrong
                Trace.WriteLine(string.Format("Multi point listener failed to start with error {0} stack:{1}", ae.Message, ae.StackTrace));
                throw new AggregateException(ae);
            }

            catch (Exception E)
            {
                Abort();
                // something is wrong
                Trace.WriteLine(string.Format("Multi point listener failed to start with error {0} stack:{1}", E.Message, E.StackTrace));
                throw new AggregateException(E);
            }
            m_ListenerStatus = ListenerStatus.Started;
            return m_publishingAddress;
        }

        private void AddStartHost(PointDefinition def)
        {
            var implementationName = def.ImplementationType.ToString();

            var baseAddress = string.Concat(m_listeningAddress, def.HostTitle, "/");

            
            var newHost = new WcfMultiPointServiceHost(StateManager, 
                                                       def.ImplementationType, 
                                                       new Uri(baseAddress));


            OnWcfHostCreated(this, def, newHost );

            newHost = m_hosts.AddOrUpdate(
                                def.HostTitle,
                                newHost,
                                (s, Current) => { return Current; }
                            );

            foreach (var i in def.Interfaces)
            {
                var interfaceName = i.ToString();
                var binding = (null != m_OverrideBinding) ? m_OverrideBinding : def.Binding;
                var EndPointAddress = string.Concat(baseAddress, interfaceName);
                var ep = newHost.AddServiceEndpoint(i,
                                         binding, // configure binding here
                                         EndPointAddress);
                OnWcfEndpointAdded(this, def, newHost,ep, i);

                Trace.WriteLine(string.Format("Host {0} with address:{1} added a new EP:{2}", def.HostTitle, baseAddress, EndPointAddress));
            }

            
            newHost.Open();
        }

        private void StopHost(PointDefinition def)
        {
            

            var host = m_hosts[def.HostTitle];
            if (null == host)
                return;

            if (CommunicationState.Opened == host.State )
                host.Close();

            Trace.WriteLine(string.Format("Host {0} stopped", def.HostTitle));

            WcfMultiPointServiceHost temp;
            m_hosts.TryRemove(def.HostTitle, out temp);



        }


 

    }
}
