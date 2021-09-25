using System;
using System.Collections.Generic;
using Dhcp.Native;
using Newtonsoft.Json;

namespace Dhcp
{
    public class DhcpServerMibInfoV6 : IDhcpServerMibInfoV6
    {
        public int Solicits { get; }
        public int Advertises { get; }
        public int Requests { get; }
        public int Renews { get; }
        public int Rebinds { get; }
        public int Replies { get; }
        public int Confirms { get; }
        public int Declines { get; }
        public int Releases { get; }
        public int Informs { get; }
        public DateTime ServerStartTime { get; }
        public int Scopes { get; }
        public int? NumAddressesInUse { get; }
        public int? NumAddressesFree { get; }
        public int? NumPendingOffers { get; }
        [JsonIgnore]
        public IDhcpServer Server { get; }
        private DhcpServerMibInfoV6(IDhcpServer server)
        {
            Server = server;
            var resumeHandle = IntPtr.Zero;
            var result = Api.DhcpGetMibInfoV6(server.Address, out resumeHandle);
            if (result != DhcpServerNativeErrors.SUCCESS)
            {
                throw new DhcpServerException(nameof(Api.DhcpGetMibInfoV6), result);
            }
            DHCP_MIB_INFO_V6 info = (DHCP_MIB_INFO_V6)resumeHandle.MarshalToStructure<DHCP_MIB_INFO_V6>();
            Solicits = info.Solicits;
            Advertises = info.Advertises;
            Requests = info.Requests;
            Renews = info.Renews;
            Rebinds = info.Rebinds;
            Replies = info.Replies;
            Confirms = info.Confirms;
            Declines = info.Declines;
            Releases = info.Releases;
            ServerStartTime = info.ServerStartTime;
            Scopes = info.Scopes;
            if (info.ScopeInfoPointer != IntPtr.Zero)
            {
                NumAddressesInUse = info.ScopeInfo.Value.NumAddressesInuse;
                NumAddressesFree = info.ScopeInfo.Value.NumAddressesFree;
                NumPendingOffers = info.ScopeInfo.Value.NumPendingOffers;
            }
            Api.FreePointer(resumeHandle);
        }

        internal static DhcpServerMibInfoV6 GetIPV6MibInfoo(IDhcpServer server)
        {
            return new DhcpServerMibInfoV6(server);
        }
    }
}