using System;
using System.Runtime.InteropServices;

namespace Dhcp.Native
{
    /// <summary>
    /// The SCOPE_MIB_INFO structure defines information about an available scope for use within returned DHCP-specific SNMP Management Information Block (MIB) data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct SCOPE_MIB_INFO
    {
        /// <summary>
        /// DHCP_IP_ADDRESS value that specifies the subnet mask for this scope.
        /// </summary>
        public readonly DHCP_IP_ADDRESS Subnet;
        /// <summary>
        /// Contains the number of IP addresses currently in use for this scope.
        /// </summary>
        public readonly int NumAddressesInuse;
        /// <summary>
        /// Contains the number of IP addresses currently available for this scope.
        /// </summary>
        public readonly int NumAddressesFree;
        /// <summary>
        /// Contains the number of IP addresses currently in the offer state for this scope.
        /// </summary>
        public readonly int NumPendingOffers;
    }
}