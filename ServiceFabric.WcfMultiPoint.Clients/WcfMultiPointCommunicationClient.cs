

using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Fabric;
using ServiceFabric.WcfMultiPoint.Common;
using System.ServiceModel;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ServiceFabric.WcfMultiPoint.Clients
{
    public class WcfMultiPointCommunicationClient : ICommunicationClient, IDisposable
    {
        private PointDefinition[] m_defs = new PointDefinition[0];
        private ClientConnectionStatus m_ConnectionStatus = ClientConnectionStatus.NotConnected; 

        private ConcurrentDictionary<string, ChannelFactory> m_ChannelFactories =
                new  ConcurrentDictionary<string, ChannelFactory>();

        private ConcurrentDictionary<string, IClientChannel> m_Channels =
                new ConcurrentDictionary<string, IClientChannel>();


        private void verifyDefAndThrow(PointDefinition[] Points)
        {
            if (ClientConnectionStatus.NotConnected != m_ConnectionStatus)
                throw new InvalidOperationException("can not change types if the client is already connected");

            if (null == Points)
                throw new ArgumentNullException("types can not be null!");

            // are they unique? //todo: move this to an extention method
            foreach (var Point in Points)
                if (1 != Points.Where(current => current.HostTitle == Point.HostTitle).Count())
                    throw new InvalidOperationException("Host titles has to be unique");


            foreach (var point in Points)
                point.verifyAndThrow(true, false); // verify each and ensure binding is set.
        }

        protected T GetAddChannel<T>(PointDefinition Point, Type channelInterface)
        {

            m_ConnectionStatus = ClientConnectionStatus.Connected;

            var Key = string.Concat(Point.HostTitle, "-", channelInterface.ToString());

            //todo provide an external delegate methods to allow creation of 
            //factories that allow manpulating WCF bindings, settings etc (if passing the binding is not enough).

            //todo: the client as is assumes that base address scheme will match the binding. 

            
            
            var clientChannel = m_Channels.GetOrAdd(Key, channelKey =>
                                        {

                                            // get or crete a channel factory
                                            var factory =(ChannelFactory<T>) m_ChannelFactories.GetOrAdd(Key, factorykey => 
                                                {
                                                    Trace.WriteLine(string.Format("Channel Factory {0} Created", Key), "info");
                                                    return new ChannelFactory<T>(Point.Binding, new EndpointAddress(string.Concat(BaseAddress, Point.HostTitle, "/", channelInterface.ToString())));
                                                });
                                            
                                            Trace.WriteLine(string.Format("Channel {0} Created", Key), "info");
                                            // return the channel
                                            return (IClientChannel) factory.CreateChannel();
                                        });


              return (T)clientChannel;          
        }

        public PointDefinition[] PointDefinition
        {
            get { return (PointDefinition[]) m_defs.Clone(); } 
            set {
                verifyDefAndThrow(value);
                m_defs = value;
            }
        }

        public string BaseAddress { get; set; }
        public ResolvedServicePartition ResolvedServicePartition{get;set;}
        public ClientConnectionStatus ConnectionStatus { get { return m_ConnectionStatus; } protected set { m_ConnectionStatus = value; } }
        public WcfMultiPointCommunicationClient(PointDefinition[] defs)
        {
            verifyDefAndThrow(defs);
            m_defs = defs;
        }


        public WcfMultiPointCommunicationClient()
        {

        }

        public void CloseAllChannels()
        {
            Trace.WriteLine("Wcf MultiPoint is closing all channels");
            m_ConnectionStatus = ClientConnectionStatus.NotConnected;
            foreach (var clientChannel in m_Channels.Values)
                if (CommunicationState.Opened == clientChannel.State)
                    clientChannel.Close();


            foreach (var factory in m_ChannelFactories.Values)
                if (CommunicationState.Opened == factory.State)
                    factory.Close();
        }
        public T GetChannel<T>(string hostTitle, string channelInterfaceName)
        {
            var Point = m_defs.SingleOrDefault(p => p.HostTitle == hostTitle);
            if (null == Point)
                throw new InvalidOperationException(string.Format("can not find a point with [{0}] host title", hostTitle));

            var channelInterface = Point.Interfaces.FirstOrDefault(t => t.ToString() == channelInterfaceName);
            if(null == channelInterface)
                throw new InvalidOperationException(string.Format("can not find a interface  host title [{0}] and interacface named [{1}]", Point.HostTitle, channelInterfaceName));

            var bValidatePoint = false;
            var bValidateChanel = false;
            return GetChannel<T>(Point, channelInterface, bValidatePoint, bValidateChanel);
        }

        public T GetChannel<T>(string hostTitle, Type channelInterface)
        {
            var Point = m_defs.SingleOrDefault(p => p.HostTitle == hostTitle);
            if (null == Point)
                throw new InvalidOperationException(string.Format("can not find a point with [{0}] host title", hostTitle));

            var bValidatePoint = false;
            var bValidateChanel = true;
            return GetChannel<T>(Point, channelInterface, bValidatePoint, bValidateChanel);
        }
        public T GetChannel<T>(PointDefinition Point, Type channelInterface) 
        {

            var bValidatePoint = true;
            var bValidateChanel = true;
            return GetChannel<T>(Point, channelInterface, bValidatePoint, bValidateChanel);
        }

        
        private T GetChannel<T> (PointDefinition Point, Type channelInterface, bool validatedPoint, bool validatedchannelInterface)
        {
            if (null == Point)  throw new ArgumentNullException("Point");
            if (null == channelInterface) throw new ArgumentNullException("channelInterface");


            PointDefinition foundPoint = null;
            Type foundType = null;
            if (validatedPoint)
            {
                foundPoint = m_defs.SingleOrDefault(p => p.HostTitle == Point.HostTitle);
                
                if (null == foundPoint)
                    throw new InvalidOperationException(string.Format("can not find a point with [{0}] host title", Point.HostTitle));
            }

            if (validatedchannelInterface)
            {
                foundType = foundPoint.Interfaces.SingleOrDefault(t => t == channelInterface);
                if (null == foundPoint)
                    throw new InvalidOperationException(string.Format("can not find a interface  host title [{0}] and interacface named [{1}]", Point.HostTitle, channelInterface.ToString()));
            }

            // we are cool 
            return GetAddChannel<T>(foundPoint, foundType);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CloseAllChannels();
                    m_Channels.Clear();
                    m_ChannelFactories.Clear();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
