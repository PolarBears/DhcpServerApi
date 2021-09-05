using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dhcp
{
    public class DhcpServerClientCollection : IDhcpServerClientCollection
    {
        [JsonIgnore]
        public DhcpServer Server { get; }
        IDhcpServer IDhcpServerClientCollection.Server => Server;

        internal DhcpServerClientCollection(DhcpServer server)
        {
            Server = server;
        }

        public IEnumerator<IDhcpServerClient> GetEnumerator()
            => DhcpServerClient.GetClients(Server, scopeLookup: null).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void RemoveClient(IDhcpServerClient client)
            => client.Delete();
    }
}
