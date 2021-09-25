using System;

namespace Dhcp
{
    public interface IDhcpServerMibInfoV6
    {
        int Solicits { get; }
        int Advertises { get; }
        int Requests { get; }
        int Renews { get; }
        int Rebinds { get; }
        int Replies { get; }
        int Confirms { get; }
        int Declines { get; }
        int Releases { get; }
        int Informs { get; }
        DateTime ServerStartTime { get; }
        int Scopes { get; }
        int? NumAddressesInUse { get; }
        int? NumAddressesFree { get; }
        int? NumPendingOffers { get; }
        IDhcpServer Server { get; }
    }
}