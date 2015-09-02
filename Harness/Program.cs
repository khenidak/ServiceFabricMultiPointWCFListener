using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WcfServiceLib;

using ServiceFabric.WcfMultiPoint.Clients;
using ServiceFabric.WcfMultiPoint.Common;

using WcfSvcLib_SF;
using Microsoft.ServiceFabric.Services;

namespace Harness
{
    class Program
    {
        static readonly string[] hosts = new string[] { "host1" };
        static readonly string parameter = "Hello, World!";
        static readonly string FabricServiceName = "fabric:/svcApp01/svc01";


        static void Main(string[] args)
        {

            try
            { }
            finally
            {
                // using service fabric interfaces 
                var factory = new WcfMultiPointCommunicationClientFactory(ServicePartitionResolver.GetDefault());
                //if you are using singlton partition
                //ServicePartitionClient<WcfMultiPointCommunicationClient> partitionClient = new ServicePartitionClient<WcfMultiPointCommunicationClient>(factory, new Uri(FabricServiceName));
                // if you are using partitions (named (string param) range (long param))
                ServicePartitionClient<WcfMultiPointCommunicationClient> partitionClient = new ServicePartitionClient<WcfMultiPointCommunicationClient>(factory, new Uri(FabricServiceName), "P1");

                int MaxRounds = 100;
                int current = 1;
                    while (current <= MaxRounds)
                    {
                        try
                        {
                            //doStuffUsingStandardResolve().Wait();
                            //doStuffUsingICommInterfaces(partitionClient).Wait();
                            CallSvcThatUsesReliableCollection().Wait();
                            Console.WriteLine("*********************************************************");
                            Thread.Sleep(500);
                        }
                        catch(AggregateException ae)
                        {
                            Console.WriteLine(string.Format("call error {0}", ae.Flatten().Message));
                            Thread.Sleep(100);
                        }

                        current++;
                    }
              
            }


            Console.WriteLine("Done!");
            Console.Read();

        }

        static async Task CallSvcThatUsesReliableCollection()
        {
            var def = new PointDefinition()
            {
                HostTitle = "host2", // this points to host 2 where a different interfaces are exposed
                Binding = new NetTcpBinding(),
                Interfaces = new List<Type>() { typeof(ISomeWcfInterface) }
            };


            FabricClient fc = new FabricClient(); //local cluster

            var resolvedPartitions = await fc.ServiceManager.ResolveServicePartitionAsync(new Uri(FabricServiceName), "P1");
            var ep = resolvedPartitions.Endpoints.SingleOrDefault((endpoint) => endpoint.Role == ServiceEndpointRole.StatefulPrimary);
            var uri = ep.Address; 

            Console.WriteLine(string.Format("working on {0} host", def.HostTitle    ));
                string result;
                
                var channelFactory =
                            new ChannelFactory<ISomeWcfInterface>(new NetTcpBinding(), new EndpointAddress(string.Concat(uri, def.HostTitle, "/", typeof(ISomeWcfInterface).ToString())));

                // data interface 
                var channel = channelFactory.CreateChannel();
                result = channel.SayHello(parameter);
                Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SayHello", "ISomeWcfInterface", parameter, result));

                var Count = channel.GetHello();
                Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "GetHello", "ISomeWcfInterface", parameter, result));

                ((IClientChannel)channel).Close(); // be kind rewind. 

            }
            static async Task doStuffUsingICommInterfaces(ServicePartitionClient<WcfMultiPointCommunicationClient> partitionClient)
        {

            // the client needs those to know which interface is wired to wich host. 

            var PointDef = new PointDefinition()
            {
                HostTitle = "host1",
                Binding = new NetTcpBinding(),    
                Interfaces = new List<Type>() { typeof(SvcInterface1), typeof(SvcInterface2) }
            };


          
            var defs = new PointDefinition[1] { PointDef }; ;
        
            await partitionClient.InvokeWithRetryAsync(client => {

                if (client.ConnectionStatus == ClientConnectionStatus.NotConnected)
                    client.PointDefinition = defs;



                foreach (var def in defs) // hosts
                {
                    Console.WriteLine(string.Format("working on {0} host", def.HostTitle));
                    string result = string.Empty;
                    // our listner has wired the same 2 interfaces per host


                    var Channel1= client.GetChannel<SvcInterface1>(def,  typeof(SvcInterface1));
                    
                    result = Channel1.SvcInterface1Op1(parameter);
                    Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SvcInterface1Op1", "SvcInterface1", parameter, result));


                    result = Channel1.SvcInterface1Op2(parameter);
                    Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SvcInterface1Op2", "SvcInterface1", parameter, result));

                    var Channel2 = client.GetChannel<SvcInterface2>(def, typeof(SvcInterface2));


                    result = Channel2.SvcInterface2Op1(parameter);
                    Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SvcInterface2Op1", "SvcInterface2", parameter, result));

                    result = Channel2.SvcInterface2Op2(parameter);
                    Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SimSessionOp2", "SvcInterface2Op2", parameter, result));


                }
                return Task.Delay(0);
            });
            
        }


        // using manual resolution
        static async Task doStuffUsingStandardResolve()
        {
            
            FabricClient fc = new FabricClient(); //local cluster

            var resolvedPartitions = await fc.ServiceManager.ResolveServicePartitionAsync(new Uri(FabricServiceName), "P1");
            var ep = resolvedPartitions.Endpoints.SingleOrDefault((endpoint) => endpoint.Role == ServiceEndpointRole.StatefulPrimary);
            var uri = ep.Address;
            foreach (var host in hosts)
            {
                Console.WriteLine(string.Format("working on {0} host", host));
                string result; 
                var channelFactory =
                            new ChannelFactory<SvcInterface1>(new NetTcpBinding(), new EndpointAddress(string.Concat(uri, host, "/" , typeof(SvcInterface1).ToString())));

                // data interface 
                var channel = channelFactory.CreateChannel();
                result = channel.SvcInterface1Op1(parameter);
                Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SvcInterface1Op1", "SvcInterface1", parameter, result));

                result = channel.SvcInterface1Op2(parameter);
                Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SvcInterface1Op2", "SvcInterface1", parameter, result));

                ((IClientChannel)channel).Close(); // be kind rewind. 


                var channelFactory2 =
                            new ChannelFactory<SvcInterface2>(new NetTcpBinding(), new EndpointAddress(string.Concat(uri, host, "/", typeof(SvcInterface2).ToString())));

                var channel2 = channelFactory2.CreateChannel();

                result = channel2.SvcInterface2Op1(parameter);
                Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SvcInterface2Op1", "SvcInterface2", parameter, result));

                result = channel2.SvcInterface2Op2(parameter);
                Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SvcInterface2Op2", "SvcInterface2", parameter, result));


                ((IClientChannel)channel2).Close(); // be kind rewind. 
            }



        }


    }
}
