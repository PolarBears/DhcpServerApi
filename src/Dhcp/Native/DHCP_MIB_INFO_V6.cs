using System;
using System.Runtime.InteropServices;

namespace Dhcp.Native
{
    /// <summary>
    /// The DHCP_MIB_INFO_V6 structure contains statistics for the DHCPv6 server.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DHCP_MIB_INFO_V6 : IDisposable
    {
        /// <summary>
        /// Integer value that specifies the number of DHCPSOLICIT messages received by the DHCPv6 server from DHCPv6 clients.
        /// </summary>
        public readonly int Solicits;
        /// <summary>
        /// Integer value that specifies the number of DHCPADVERTISE messages sent by DHCPv6 server to DHCPv6 clients.
        /// </summary>
        public readonly int Advertises;
        /// <summary>
        /// Integer value that specifies the number of DHCPREQUEST messages sent by DHCPv6 server to DHCPv6 clients.
        /// </summary>
        public readonly int Requests;
        /// <summary>
        /// Integer value that specifies the number of DHCPRENEW messages sent by DHCPv6 server to DHCPv6 clients.
        /// </summary>
        public readonly int Renews;
        /// <summary>
        /// Integer value that specifies the number of DHCPREBIND messages sent by DHCPv6 server to DHCPv6 clients.
        /// </summary>
        public readonly int Rebinds;
        /// <summary>
        /// Integer value that specifies the number of DHCPREPLY messages sent by DHCPv6 server to DHCPv6 clients.
        /// </summary>
        public readonly int Replies;
        /// <summary>
        /// Integer value that specifies the number of DHCPCONFIRM messages sent by DHCPv6 server to DHCPv6 clients.
        /// </summary>
        public readonly int Confirms;
        /// <summary>
        /// Integer value that specifies the number of DHCPDECLINE messages sent by DHCPv6 server to DHCPv6 clients.
        /// </summary>
        public readonly int Declines;
        /// <summary>
        /// Integer value that specifies the number of DHCPRELEASE messages sent by DHCPv6 server to DHCPv6 clients.
        /// </summary>
        public readonly int Releases;
        /// <summary>
        /// Integer value that specifies the number of DHCPINFORM messages sent by DHCPv6 server to DHCPv6 clients.
        /// </summary>
        public readonly int Informs;
        /// <summary>
        /// DATE_TIME value that specifies the time the DHCPv6 server was started.
        /// </summary>
        public readonly DATE_TIME ServerStartTime;
        /// <summary>
        /// Integer value that contains the number of IPv6 scopes configured on the current DHCPv6 server. This member defines the number of DHCPv6 scopes in the subsequent member ScopeInfo.
        /// </summary>
        public readonly int Scopes;

        /// <summary>
        /// Pointer to a SCOPE_MIB_INFO structure that contains specific information about the scopes configured on the DHCP server.
        /// </summary>
        public readonly IntPtr ScopeInfoPointer;
        public readonly SCOPE_MIB_INFO? ScopeInfo => ScopeInfoPointer == IntPtr.Zero ? null : ScopeInfoPointer.MarshalToStructure<SCOPE_MIB_INFO>();

        public void Dispose()
        {
            Api.FreePointer(ScopeInfoPointer);
        }
    }
}
