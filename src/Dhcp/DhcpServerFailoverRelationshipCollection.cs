﻿using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dhcp
{
    public class DhcpServerFailoverRelationshipCollection : IDhcpServerFailoverRelationshipCollection
    {
        [JsonIgnore]
        public DhcpServer Server { get; }
        IDhcpServer IDhcpServerFailoverRelationshipCollection.Server => Server;

        internal DhcpServerFailoverRelationshipCollection(DhcpServer server)
        {
            Server = server;
        }

        public IDhcpServerFailoverRelationship GetRelationship(string relationshipName)
            => DhcpServerFailoverRelationship.GetFailoverRelationship(Server, relationshipName);

        public void RemoveRelationship(IDhcpServerFailoverRelationship relationship)
            => relationship.Delete();

        public IEnumerator<IDhcpServerFailoverRelationship> GetEnumerator()
            => DhcpServerFailoverRelationship.GetFailoverRelationships(Server).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
