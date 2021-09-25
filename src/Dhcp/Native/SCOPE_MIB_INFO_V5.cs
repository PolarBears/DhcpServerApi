using System;
using System.Runtime.InteropServices;

namespace Dhcp.Native
{
    /// <summary>
    /// The SCOPE_MIB_INFO_V5 structure contains information about a specific DHCP scope.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct SCOPE_MIB_INFO_V5
    {
        /// <summary>
        /// DHCP_IP_ADDRESS value that contains the IP address of the subnet gateway that defines the scope.
        /// </summary>
        public readonly DHCP_IP_ADDRESS Subnet;
        /// <summary>
        /// The number of IP addresses in the scope that are currently assigned to DHCP clients.
        /// </summary>
        public readonly int NumAddressesInuse;
        /// <summary>
        /// The number of IP addresses in the scope that are not currently assigned to DHCP clients.
        /// </summary>
        public readonly int NumAddressesFree;
        /// <summary>
        /// The number of IP addresses in the scope that have been offered to DHCP clients but have not yet received REQUEST messages.
        /// </summary>
        public readonly int NumPendingOffers;
    }
}