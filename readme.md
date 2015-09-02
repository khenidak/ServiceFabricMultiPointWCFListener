#WCF Multi Point Listener for Service Fabric#

This repo contains the components necessary to enable Service Fabric to listen to (on a single servicer ) to multiple WCF endpoints (shared among a configurable number of WCF Hosts). 

Service Fabric out of the box uses WCF Listeners that support 1:1:1 mapping between Replica, Wcf Host & Wcf Endpoint and you should them. The components in this repo comes in play if this mapping does not work with you. For example services that uses multi-contracts, multi-endpoints or multi-hosts. 

## At a Glance: What can you do with this component ##
1- Map multiple Wcf endpoints/contracts/hosts to a single replica. 

2- Migrate Wcf Services as-is to Service Fabric based hosting. 

3- Migrate Wcf Services (and use Service Fabric State) in them. 

4- Control over how listening address are created.

5- control over how bindings are assigned. 

6- Control over Wcf Hosts, Wcf Endpoints as they are being created. to add behaviors, dispatchers etc..

7- Support for ICommunicationXXXX Service Fabric interfaces which recycles Wcf channels between callers. Maximum of 1 channel type per host/per endpoint is created at any single time.

8- The service fabric client implementation implements disposable pattern and will abort all open Wcf channels upon Dispose or GC.

##Typical Usage Pattern (Service Side): Wcf as-is (not using Service Fabric State)##

	// Override CreateCommunicationListener in your service
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

		
	// create a listener that will have net.tcp as base scheme and one Point Definition. You can add multiple point defs. Each point will result into creating a new Wcf Host
	var listener = new WcfMultiPointListener("net.tcp", def1);
	// the second param of Ctor is Param[] to add multiple defs

	 
	  listener.StateManager = this.StateManager; // this ensures that state manager used by this 
                                                       // replica instance is 		shared with wcf services instances.
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


##Typical Usage Pattern (Service Side): Wcf (Services that will be using Service Fabric State)##

Creating a listener for a Wcf Service that uses Service Fabric state does not change in terms listener initialization. You will just use the same approach outlined above.

The Wcf service implementation will have to be extended to use Service Fabric state as the following:

  	
	// sample Wcf Service implementing someWcfInterface (Service Contract)
	// additionally it implements (IServiceFabricWcfService) that allows it
	// to receive a reference to state manager (assigned by Wcf extentions) 
 
	public class WcfSvc_SF : IServiceFabricWcfService, ISomeWcfInterface
    {
        private readonly string m_QueueName = "HelloMessages";

        //IServiceFabricWcfService
        public IReliableStateManager StateManager { get; set; }

		// this a sample Operation Contarct method implementation that has internal implementation that uses Service Fabric Reliable State Manager

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

> Refer to Sample Wcf Services for the complete code. 

##Typical Usage Pattern (Client Side)##
Clients needs to know the following: 
1- Interface type (as per Wcf approach)
2- Endpoint address (typically from Service Fabric partition resolution). 
3- Host Title (that was used in Point Definition)

For clients that wants to use standard resolve (not using ICommunicationXX interfaces)

	


	FabricClient fc = new FabricClient(); //local cluster
    var resolvedPartitions = await fc.ServiceManager.ResolveServicePartitionAsync(new Uri(FabricServiceName), "P1");
    var ep = resolvedPartitions.Endpoints.SingleOrDefault((endpoint) => endpoint.Role == ServiceEndpointRole.StatefulPrimary);
    var uri = ep.Address;
            
	foreach (var host in hosts) // host array of whatever I export from the server
    {
		// SvcInterface1 is a Wcf Service Contract
    	Console.WriteLine(string.Format("working on {0} host", host));
        string result; 
        var channelFactory =
                            new ChannelFactory<SvcInterface1>(new NetTcpBinding(), new EndpointAddress(string.Concat(uri, host, "/" , typeof(SvcInterface1).ToString())));

		// because i created this channel manually i need to close it


		  var channel = channelFactory.CreateChannel();
                result = channel.SvcInterface1Op1(parameter);
                Console.WriteLine(string.Format("opeartion {0} on interface {1} called with {2} returned {3}", "SvcInterface1Op1", "SvcInterface1", parameter, result));

 
		
       ((IClientChannel)channel).Close(); // be kind rewind. 
	}


The component also contains support for ICommunicationXXX interfaces used by service fabric clients. You can use them as the following

	// on the client use the same PointDefinition class
 			
	var PointDef = new PointDefinition()
    {
        HostTitle = "host1",
        Binding = new NetTcpBinding(),    
        Interfaces = new List<Type>() { typeof(SvcInterface1), typeof(SvcInterface2) } // interfaces that i expect this host to operate as endpoints. 
    };

	// create the client factory
	 var factory = new WcfMultiPointCommunicationClientFactory(ServicePartitionResolver.GetDefault());
    
	// create the client 
	  ServicePartitionClient<WcfMultiPointCommunicationClient> partitionClient = new ServicePartitionClient<WcfMultiPointCommunicationClient>(factory, new Uri(FabricServiceName), "P1"); // P1 is a named partition
	
	// use invoke 
	await partitionClient.InvokeWithRetryAsync(client => {

	// inform the client with my Point Defs I expect it to connect to
       if (client.ConnectionStatus == ClientConnectionStatus.NotConnected)
                    client.PointDefinition = defs;
	// get the Wcf channel
	var Channel1= client.GetChannel<SvcInterface1>(def,  typeof(SvcInterface1));
    // use it                
    result = Channel1.SvcInterface1Op1(parameter);
    
	// i don't need to close it the client implement dispose pattern and will close it upon dispose or GC (although if you know you don't need it any more you should close it).        
         
	});

> refer to the Harness project for the complete source code.

## What is in the Package##
1- *ServiceFabric.WcfMultiPoint.Services* Wct Multi Point listener implementation. Your Service Fabric projects will have to reference that.
 
2- *ServiceFabric.WcfMultiPoint.Common* contains the Point Definition class to be referenced by your Service Fabric projects and clients (if they are using ICommunicationXX interfaces

3- *ServiceFabric.WcfMultiPoint.Services.Common* contains interfaces needed to be implemented by your Wcf Services if they expect to use IReliable State (the implementation uses simple DI to inject it in your services).

4- *ServiceFabric.WcfMultiPoint.Clients* contains ICommunicationXXX client interface implementation to be referenced by your service fabric clients.  

5- *WcfSvcLib_SF* Sample Wcf service library that is expected to use Service Fabric state manager .

6- *WcfServiceLib* Sample Wcf service library (an example for a Wcf Service migrated as is to Service Fabric hosting).

7- *svc01* & *svcApp01* sample service fabric service and service fabric application. 

8- *Harness* Sample service fabric client project. 