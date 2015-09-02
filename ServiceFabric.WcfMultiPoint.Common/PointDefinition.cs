using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace ServiceFabric.WcfMultiPoint.Common
{
    public class PointDefinition
    {
        public string HostTitle = Guid.NewGuid().ToString();
        public List<Type> Interfaces = new List<Type>(); 
        public Binding Binding = null;
        public Type ImplementationType;


        public void verifyAndThrow(bool verifybinding = false, bool verifyImplementationType = false)
        {
            if (null == HostTitle || string.Empty == HostTitle)
                throw new InvalidOperationException("host title is null or empty"); // we allow empty string.



            if (null == Interfaces)
                throw new InvalidOperationException("interfaces list is null"); // we allow empty string.

            if (0 == Interfaces.Count())
                throw new InvalidOperationException("interfaces list is empty");

            foreach (var t in Interfaces)
                if (!t.IsInterface)
                    throw new InvalidOperationException("interface list contain a type that is not an interface");

            if (verifybinding && (null == Binding))
                throw new InvalidOperationException("binding is null");

            if(verifyImplementationType && (null == ImplementationType))
                throw new InvalidOperationException("implementation type is null");


            //todo: verify ImplementationType is a class and has a new() constructor 
            // and implement the interfaces in question.
            // <<OR>> leave it to WCF host to bubble up the error during start.
        }
    }
}
