using System;

namespace Dhcp
{
    public interface IDhcpServerMibInfoV5
    {
        int Discovers { get; }
        int Offers { get; }
        int Requests { get; }
        int Acks { get; }
        int Naks { get; }
        int Declines { get; }
        int Releases { get; }
        DateTime ServerStartTime { get; }
        int QtnNumLeases { get; }
        int QtnPctQtnLeases { get; }
        int QtnProbationLeases { get; }
        int QtnNonQtnLeases { get; }
        int QtnExemptLeases { get; }
        int QtnCapableClients { get; }
        int QtnIASErrors { get; }
        int DelayedOffers { get; }
        int ScopesWithDelayedOffers { get; }
        int Scopes { get; }
        int? NumAddressesInUse { get; }
        int? NumAddressesFree { get; }
        int? NumPendingOffers { get; }
        IDhcpServer Server { get; }
    }
}