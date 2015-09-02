using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WcfSvcLib_SF
{
    [ServiceContract]
    public interface ISomeWcfInterface
    {
        [OperationContract]
        string SayHello(string Message);
        [OperationContract]
        int GetHello();

    }
}
