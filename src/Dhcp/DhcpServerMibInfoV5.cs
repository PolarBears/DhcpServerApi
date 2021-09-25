using System;
using System.Collections.Generic;
using Dhcp.Native;
using Newtonsoft.Json;

namespace Dhcp
{
    public class DhcpServerMibInfoV5 : IDhcpServerMibInfoV5
    {
        public int Discovers { get; }
        public int Offers { get; }
        public int Requests { get; }
        public int Acks { get; }
        public int Naks { get; }
        public int Declines { get; }
        public int Releases { get; }
        public DateTime ServerStartTime { get; }
        public int QtnNumLeases { get; }
        public int QtnPctQtnLeases { get; }
        public int QtnProbationLeases { get; }
        public int QtnNonQtnLeases { get; }
        public int QtnExemptLeases { get; }
        public int QtnCapableClients { get; }
        public int QtnIASErrors { get; }
        public int DelayedOffers { get; }
        public int ScopesWithDelayedOffers { get; }
        public int Scopes { get; }
        public int? NumAddressesInUse { get; }
        public int? NumAddressesFree { get; }
        public int? NumPendingOffers { get; }
        [JsonIgnore]
        public IDhcpServer Server { get; }
        private DhcpServerMibInfoV5(IDhcpServer server)
        {
            Server = server;
            var resumeHandle = IntPtr.Zero;
            var result = Api.DhcpGetMibInfoV5(server.Address, out resumeHandle);
            if (result != DhcpServerNativeErrors.SUCCESS)
            {
                throw new DhcpServerException(nameof(Api.DhcpGetMibInfoV5), result);
            }
            DHCP_MIB_INFO_V5 info = (DHCP_MIB_INFO_V5)resumeHandle.MarshalToStructure<DHCP_MIB_INFO_V5>();
            Discovers = info.Discovers;
            Offers = info.Offers;
            Requests = info.Requests;
            Acks = info.Acks;
            Naks = info.Naks;
            Declines = info.Declines;
            Releases = info.Releases;
            ServerStartTime = info.ServerStartTime;
            QtnNumLeases = info.QtnNumLeases;
            QtnPctQtnLeases = info.QtnPctQtnLeases;
            QtnProbationLeases = info.QtnProbationLeases;
            QtnNonQtnLeases = info.QtnNonQtnLeases;
            QtnExemptLeases = info.QtnExemptLeases;
            QtnCapableClients = info.QtnCapableClients;
            QtnIASErrors = info.QtnIASErrors;
            DelayedOffers = info.DelayedOffers;
            ScopesWithDelayedOffers = info.ScopesWithDelayedOffers;
            Scopes = info.Scopes;
            if (info.ScopeInfoPointer != IntPtr.Zero)
            {
                NumAddressesInUse = info.ScopeInfo.Value.NumAddressesInuse;
                NumAddressesFree = info.ScopeInfo.Value.NumAddressesFree;
                NumPendingOffers = info.ScopeInfo.Value.NumPendingOffers;
            }
            Api.FreePointer(resumeHandle);
        }

        internal static DhcpServerMibInfoV5 GetIPV4MibInfo(IDhcpServer server)
        {
            return new DhcpServerMibInfoV5(server);
        }
    }
}