using System;
using System.Runtime.InteropServices;

namespace Dhcp.Native
{
    /// <summary>
    /// The DHCP_MIB_INFO_V5 structure contains statistical information about a DHCP server.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DHCP_MIB_INFO_V5 : IDisposable
    {
        /// <summary>
        /// The number of DISCOVER messages received by the DHCP server.
        /// </summary>
        public readonly int Discovers;
        /// <summary>
        /// The number of OFFER messages sent to DHCP clients.
        /// </summary>
        public readonly int Offers;
        /// <summary>
        /// The number of REQUEST messages received by DHCP clients.
        /// </summary>
        public readonly int Requests;
        /// <summary>
        /// The number of ACK messages sent by the DHCP server.
        /// </summary>
        public readonly int Acks;
        /// <summary>
        /// The number of NACK messages sent by the DHCP server.
        /// </summary>
        public readonly int Naks;
        /// <summary>
        /// The number of DECLINE messages sent by DHCP clients.
        /// </summary>
        public readonly int Declines;
        /// <summary>
        /// The number of RELEASE messages received by the DHCP server.
        /// </summary>
        public readonly int Releases;
        /// <summary>
        /// DATE_TIME structure that contains the most recent time the DHCP server was started.
        /// </summary>
        public readonly DATE_TIME ServerStartTime;

        /// <summary>
        /// This member is not currently used. Please set this to value 0x00000000.
        /// </summary>
        public readonly int QtnNumLeases;
        /// <summary>
        /// This member is not currently used. Please set this to value 0x00000000.
        /// </summary>
        public readonly int QtnPctQtnLeases;
        /// <summary>
        /// This member is not currently used. Please set this to value 0x00000000.
        /// </summary>
        public readonly int QtnProbationLeases;
        /// <summary>
        /// This member is not currently used. Please set this to value 0x00000000.
        /// </summary>
        public readonly int QtnNonQtnLeases;
        /// <summary>
        /// This member is not currently used. Please set this to value 0x00000000.
        /// </summary>
        public readonly int QtnExemptLeases;
        /// <summary>
        /// This member is not currently used. Please set this to value 0x00000000.
        /// </summary>
        public readonly int QtnCapableClients;
        /// <summary>
        /// This member is not currently used. Please set this to value 0x00000000.
        /// </summary>
        public readonly int QtnIASErrors;
        /// <summary>
        /// The number of OFFER messages sent with a specific delay by the DHCP server.
        /// </summary>
        public readonly int DelayedOffers;
        /// <summary>
        /// The number of scopes with a delay value greater than 0.
        /// </summary>
        public readonly int ScopesWithDelayedOffers;
        /// <summary>
        /// The total number of scopes configured on the DHCP server
        /// </summary>
        public readonly int Scopes;
        /// <summary>
        /// Pointer to a SCOPE_MIB_INFO_V5 structure that contains specific information about the scopes configured on the DHCP server.
        /// </summary>
        public readonly IntPtr ScopeInfoPointer;
        public readonly SCOPE_MIB_INFO_V5? ScopeInfo => ScopeInfoPointer == IntPtr.Zero ? null : ScopeInfoPointer.MarshalToStructure<SCOPE_MIB_INFO_V5>();

        public void Dispose()
        {
            Api.FreePointer(ScopeInfoPointer);
        }
    }
}
