using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceFabric.WcfMultiPoint.Services.Common
{
    // WCF Services that needs state will implement 
    // this interface so it can reach state managers assigned to stateful services

     // implement this interface is optional to allow existing services
     // to migrate as is.
    public interface IServiceFabricWcfService
    {
         IReliableStateManager StateManager { get; set; }
    }
}
