﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dhcp.Native;
using Newtonsoft.Json;

namespace Dhcp
{
    public class DhcpServerClient : IDhcpServerClient
    {
        [JsonIgnore]
        public DhcpServer Server { get; }
        IDhcpServer IDhcpServerClient.Server => Server;
        [JsonIgnore]
        public DhcpServerScope Scope { get; }
        IDhcpServerScope IDhcpServerClient.Scope => Scope;

        public DhcpServerIpAddress IpAddress { get; }
        internal ClientInfo Info { get; private set; }

        public DhcpServerIpMask SubnetMask => Info.SubnetMask;
        public DhcpServerHardwareAddress HardwareAddress
        {
            get => Info.HardwareAddress;
            set => SetHardwareAddress(value);
        }

        public string Name
        {
            get => Info.Name;
            set => SetName(value);
        }
        public string Comment
        {
            get => Info.Comment;
            set => SetComment(value);
        }

        [Obsolete("Caused confusion. Use " + nameof(LeaseExpiresUtc) + " or " + nameof(LeaseExpiresLocal) + " instead.")]
        public DateTime LeaseExpires => Info.LeaseExpiresUtc;
        public DateTime LeaseExpiresUtc => Info.LeaseExpiresUtc;
        public DateTime LeaseExpiresLocal => Info.LeaseExpiresUtc.ToLocalTime();

        public bool LeaseExpired => DateTime.UtcNow > Info.LeaseExpiresUtc;
        public bool LeaseHasExpiry => Info.LeaseExpiresUtc != DateTime.MaxValue;

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DhcpServerClientTypes Type => Info.Type;

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DhcpServerClientAddressStates AddressState => Info.AddressState;
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DhcpServerClientNameProtectionStates NameProtectionState => Info.NameProtectionState;
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DhcpServerClientDnsStates DnsState => Info.DnsState;

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DhcpServerClientQuarantineStatuses QuarantineStatus => Info.QuarantineStatus;

        public DateTime ProbationEnds => Info.ProbationEnds;

        public bool QuarantineCapable => Info.QuarantineCapable;

        private DhcpServerClient(DhcpServer server, DhcpServerScope scope, DhcpServerIpAddress ipAddress, ClientInfo info)
        {
            Server = server;
            Scope = scope;
            IpAddress = ipAddress;
            Info = info;
        }

        public void Delete()
            => Delete(Server, IpAddress);

        public IDhcpServerScopeReservation ConvertToReservation()
        {
            if (Scope == null)
                throw new NullReferenceException("This client is not associated with a DHCP scope");

            return Scope.Reservations.AddReservation(this);
        }

        private void SetName(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = string.Empty;

            if (!name.Equals(Info.Name, StringComparison.Ordinal))
            {
                var proposedInfo = Info.UpdateName(name);
                SetInfo(proposedInfo);
            }
        }

        private void SetComment(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                comment = string.Empty;

            if (!comment.Equals(Info.Comment, StringComparison.Ordinal))
            {
                var proposedInfo = Info.UpdateComment(comment);
                SetInfo(proposedInfo);
            }
        }

        private void SetHardwareAddress(DhcpServerHardwareAddress hardwareAddress)
        {
            if (!hardwareAddress.Equals(Info.HardwareAddress))
            {
                var proposedInfo = Info.UpdateHardwareAddress(hardwareAddress);
                SetInfo(proposedInfo);
            }
        }

        internal static DhcpServerClient GetClient(DhcpServer server, DhcpServerScope scope, DhcpServerIpAddress ipAddress)
        {
            var searchInfo = new DHCP_SEARCH_INFO_Managed_IpAddress(ipAddress.ToNativeAsNetwork());

            using (var searchInfoPtr = BitHelper.StructureToPtr(searchInfo))
            {
                if (server.IsCompatible(DhcpServerVersions.Windows2008R2))
                    return GetClientVQ(server, scope, searchInfoPtr);
                else if (server.IsCompatible(DhcpServerVersions.Windows2000))
                    return GetClientV0(server, scope, searchInfoPtr);
                else
                    throw new PlatformNotSupportedException($"DHCP Server v{server.VersionMajor}.{server.VersionMinor} does not support this feature");
            }
        }

        private static DhcpServerClient GetClientV0(DhcpServer server, DhcpServerScope scope, IntPtr searchInfo)
        {
            var result = Api.DhcpGetClientInfo(server.Address, searchInfo, out var clientPtr);

            if (result == DhcpServerNativeErrors.JET_ERROR)
                return null;

            if (result != DhcpServerNativeErrors.SUCCESS)
                throw new DhcpServerException(nameof(Api.DhcpGetClientInfo), result);

            try
            {
                using (var client = clientPtr.MarshalToStructure<DHCP_CLIENT_INFO>())
                {
                    return FromNative(server, scope, in client);
                }
            }
            finally
            {
                Api.FreePointer(clientPtr);
            }
        }

        private static DhcpServerClient GetClientVQ(DhcpServer server, DhcpServerScope scope, IntPtr searchInfo)
        {
            var result = Api.DhcpGetClientInfoVQ(server.Address, searchInfo, out var clientPtr);

            if (result == DhcpServerNativeErrors.JET_ERROR)
                return null;

            if (result != DhcpServerNativeErrors.SUCCESS)
                throw new DhcpServerException(nameof(Api.DhcpGetClientInfoVQ), result);

            try
            {
                using (var client = clientPtr.MarshalToStructure<DHCP_CLIENT_INFO_VQ>())
                {
                    return FromNative(server, scope, in client);
                }
            }
            finally
            {
                Api.FreePointer(clientPtr);
            }
        }

        internal static IEnumerable<DhcpServerClient> GetClients(DhcpServer server, Dictionary<DhcpServerIpAddress, DhcpServerScope> scopeLookup)
        {
            if (server.IsCompatible(DhcpServerVersions.Windows2008R2))
                return GetClientsVQ(server, scopeLookup);
            else if (server.IsCompatible(DhcpServerVersions.Windows2000))
                return GetClientsV0(server, scopeLookup);
            else
                throw new PlatformNotSupportedException($"DHCP Server v{server.VersionMajor}.{server.VersionMinor} does not support this feature");
        }

        private static IEnumerable<DhcpServerClient> GetClientsV0(DhcpServer server, Dictionary<DhcpServerIpAddress, DhcpServerScope> scopeLookup)
        {
            if (scopeLookup == null)
                scopeLookup = DhcpServerScope.GetScopes(server, preloadClients: false, preloadFailoverRelationships: false).ToDictionary(s => s.Address);

            var resultHandle = IntPtr.Zero;
            var result = Api.DhcpEnumSubnetClients(ServerIpAddress: server.Address,
                                                   SubnetAddress: (DHCP_IP_ADDRESS)0,
                                                   ResumeHandle: ref resultHandle,
                                                   PreferredMaximum: 0x10000,
                                                   ClientInfo: out var clientsPtr,
                                                   ClientsRead: out _,
                                                   ClientsTotal: out _);

            // shortcut if no subnet clients are returned
            if (result == DhcpServerNativeErrors.ERROR_NO_MORE_ITEMS)
                yield break;

            if (result != DhcpServerNativeErrors.SUCCESS && result != DhcpServerNativeErrors.ERROR_MORE_DATA)
                throw new DhcpServerException(nameof(Api.DhcpEnumSubnetClients), result);

            try
            {
                while (result == DhcpServerNativeErrors.SUCCESS || result == DhcpServerNativeErrors.ERROR_MORE_DATA)
                {
                    using (var clients = clientsPtr.MarshalToStructure<DHCP_CLIENT_INFO_ARRAY>())
                    {
                        foreach (var client in clients.Clients)
                        {
                            var clientValue = client.Value;
                            var scope = scopeLookup.TryGetValue(clientValue.SubnetAddress, out var s) ? s : null;
                            yield return FromNative(server, scope, in clientValue);
                        }
                    }

                    if (result == DhcpServerNativeErrors.SUCCESS)
                        yield break; // Last results

                    Api.FreePointer(clientsPtr);
                    result = Api.DhcpEnumSubnetClients(ServerIpAddress: server.Address,
                                                       SubnetAddress: (DHCP_IP_ADDRESS)0,
                                                       ResumeHandle: ref resultHandle,
                                                       PreferredMaximum: 0x10000,
                                                       ClientInfo: out clientsPtr,
                                                       ClientsRead: out _,
                                                       ClientsTotal: out _);

                    if (result != DhcpServerNativeErrors.SUCCESS && result != DhcpServerNativeErrors.ERROR_MORE_DATA && result != DhcpServerNativeErrors.ERROR_NO_MORE_ITEMS)
                        throw new DhcpServerException(nameof(Api.DhcpEnumSubnetClients), result);
                }
            }
            finally
            {
                Api.FreePointer(clientsPtr);
            }
        }

        private static IEnumerable<DhcpServerClient> GetClientsVQ(DhcpServer server, Dictionary<DhcpServerIpAddress, DhcpServerScope> scopeLookup)
        {
            if (scopeLookup == null)
                scopeLookup = DhcpServerScope.GetScopes(server, preloadClients: false, preloadFailoverRelationships: false).ToDictionary(s => s.Address);

            var resultHandle = IntPtr.Zero;
            var result = Api.DhcpEnumSubnetClientsVQ(ServerIpAddress: server.Address,
                                                     SubnetAddress: (DHCP_IP_ADDRESS)0,
                                                     ResumeHandle: ref resultHandle,
                                                     PreferredMaximum: 0x10000,
                                                     ClientInfo: out var clientsPtr,
                                                     ClientsRead: out _,
                                                     ClientsTotal: out _);

            // shortcut if no subnet clients are returned
            if (result == DhcpServerNativeErrors.ERROR_NO_MORE_ITEMS)
                yield break;

            if (result != DhcpServerNativeErrors.SUCCESS && result != DhcpServerNativeErrors.ERROR_MORE_DATA)
                throw new DhcpServerException(nameof(Api.DhcpEnumSubnetClientsVQ), result);

            try
            {
                while (result == DhcpServerNativeErrors.SUCCESS || result == DhcpServerNativeErrors.ERROR_MORE_DATA)
                {
                    using (var clients = clientsPtr.MarshalToStructure<DHCP_CLIENT_INFO_ARRAY_VQ>())
                    {
                        foreach (var client in clients.Clients)
                        {
                            var clientValue = client.Value;
                            var scope = scopeLookup.TryGetValue(clientValue.SubnetAddress, out var s) ? s : null;
                            yield return FromNative(server, scope, in clientValue);
                        }
                    }

                    if (result == DhcpServerNativeErrors.SUCCESS)
                        yield break; // Last results

                    Api.FreePointer(clientsPtr);
                    result = Api.DhcpEnumSubnetClientsVQ(ServerIpAddress: server.Address,
                                                         SubnetAddress: (DHCP_IP_ADDRESS)0,
                                                         ResumeHandle: ref resultHandle,
                                                         PreferredMaximum: 0x10000,
                                                         ClientInfo: out clientsPtr,
                                                         ClientsRead: out _,
                                                         ClientsTotal: out _);

                    if (result != DhcpServerNativeErrors.SUCCESS && result != DhcpServerNativeErrors.ERROR_MORE_DATA)
                        throw new DhcpServerException(nameof(Api.DhcpEnumSubnetClientsVQ), result);
                }
            }
            finally
            {
                Api.FreePointer(clientsPtr);
            }
        }

        internal static IEnumerable<DhcpServerClient> GetScopeClients(DhcpServerScope scope)
        {
            if (scope.Server.IsCompatible(DhcpServerVersions.Windows2008R2))
                return GetScopeClientsVQ(scope);
            else if (scope.Server.IsCompatible(DhcpServerVersions.Windows2000))
                return GetScopeClientsV0(scope);
            else
                throw new PlatformNotSupportedException($"DHCP Server v{scope.Server.VersionMajor}.{scope.Server.VersionMinor} does not support this feature");
        }

        private static IEnumerable<DhcpServerClient> GetScopeClientsV0(DhcpServerScope scope)
        {
            var resultHandle = IntPtr.Zero;
            var result = Api.DhcpEnumSubnetClients(ServerIpAddress: scope.Server.Address,
                                                   SubnetAddress: scope.Address.ToNativeAsNetwork(),
                                                   ResumeHandle: ref resultHandle,
                                                   PreferredMaximum: 0x10000,
                                                   ClientInfo: out var clientsPtr,
                                                   ClientsRead: out _,
                                                   ClientsTotal: out _);

            // shortcut if no subnet clients are returned
            if (result == DhcpServerNativeErrors.ERROR_NO_MORE_ITEMS)
                yield break;

            if (result != DhcpServerNativeErrors.SUCCESS && result != DhcpServerNativeErrors.ERROR_MORE_DATA)
                throw new DhcpServerException(nameof(Api.DhcpEnumSubnetClients), result);

            try
            {
                while (result == DhcpServerNativeErrors.SUCCESS || result == DhcpServerNativeErrors.ERROR_MORE_DATA)
                {
                    using (var clients = clientsPtr.MarshalToStructure<DHCP_CLIENT_INFO_ARRAY>())
                    {
                        foreach (var client in clients.Clients)
                            yield return FromNative(scope.Server, scope, in client.Value);
                    }

                    if (result == DhcpServerNativeErrors.SUCCESS)
                        yield break; // Last results

                    Api.FreePointer(clientsPtr);
                    result = Api.DhcpEnumSubnetClients(ServerIpAddress: scope.Server.Address,
                                                       SubnetAddress: scope.Address.ToNativeAsNetwork(),
                                                       ResumeHandle: ref resultHandle,
                                                       PreferredMaximum: 0x10000,
                                                       ClientInfo: out clientsPtr,
                                                       ClientsRead: out _,
                                                       ClientsTotal: out _);

                    if (result != DhcpServerNativeErrors.SUCCESS && result != DhcpServerNativeErrors.ERROR_MORE_DATA && result != DhcpServerNativeErrors.ERROR_NO_MORE_ITEMS)
                        throw new DhcpServerException(nameof(Api.DhcpEnumSubnetClients), result);
                }
            }
            finally
            {
                Api.FreePointer(clientsPtr);
            }
        }

        private static IEnumerable<DhcpServerClient> GetScopeClientsVQ(DhcpServerScope scope)
        {
            var resultHandle = IntPtr.Zero;
            var result = Api.DhcpEnumSubnetClientsVQ(ServerIpAddress: scope.Server.Address,
                                                     SubnetAddress: scope.Address.ToNativeAsNetwork(),
                                                     ResumeHandle: ref resultHandle,
                                                     PreferredMaximum: 0x10000,
                                                     ClientInfo: out var clientsPtr,
                                                     ClientsRead: out _,
                                                     ClientsTotal: out _);

            // shortcut if no subnet clients are returned
            if (result == DhcpServerNativeErrors.ERROR_NO_MORE_ITEMS)
                yield break;

            if (result != DhcpServerNativeErrors.SUCCESS && result != DhcpServerNativeErrors.ERROR_MORE_DATA)
                throw new DhcpServerException(nameof(Api.DhcpEnumSubnetClientsVQ), result);

            try
            {
                while (result == DhcpServerNativeErrors.SUCCESS || result == DhcpServerNativeErrors.ERROR_MORE_DATA)
                {
                    using (var clients = clientsPtr.MarshalToStructure<DHCP_CLIENT_INFO_ARRAY_VQ>())
                    {
                        foreach (var client in clients.Clients)
                            yield return FromNative(scope.Server, scope, in client.Value);
                    }

                    if (result == DhcpServerNativeErrors.SUCCESS)
                        yield break; // Last results

                    Api.FreePointer(clientsPtr);
                    result = Api.DhcpEnumSubnetClientsVQ(ServerIpAddress: scope.Server.Address,
                                                         SubnetAddress: scope.Address.ToNativeAsNetwork(),
                                                         ResumeHandle: ref resultHandle,
                                                         PreferredMaximum: 0x10000,
                                                         ClientInfo: out clientsPtr,
                                                         ClientsRead: out _,
                                                         ClientsTotal: out _);

                    if (result != DhcpServerNativeErrors.SUCCESS && result != DhcpServerNativeErrors.ERROR_MORE_DATA)
                        throw new DhcpServerException(nameof(Api.DhcpEnumSubnetClientsVQ), result);
                }
            }
            finally
            {
                Api.FreePointer(clientsPtr);
            }
        }

        internal static DhcpServerClient CreateClient(DhcpServerScope scope, DhcpServerIpAddress address, DhcpServerHardwareAddress hardwareAddress)
            => CreateClientV0(scope, address, hardwareAddress, name: null, comment: null, leaseExpires: DateTime.MaxValue, ownerHost: null);

        internal static DhcpServerClient CreateClient(DhcpServerScope scope, DhcpServerIpAddress address, DhcpServerHardwareAddress hardwareAddress, string name)
            => CreateClientV0(scope, address, hardwareAddress, name, comment: null, leaseExpires: DateTime.MaxValue, ownerHost: null);

        internal static DhcpServerClient CreateClient(DhcpServerScope scope, DhcpServerIpAddress address, DhcpServerHardwareAddress hardwareAddress, string name, string comment)
            => CreateClientV0(scope, address, hardwareAddress, name, comment, leaseExpires: DateTime.MaxValue, ownerHost: null);

        internal static DhcpServerClient CreateClient(DhcpServerScope scope, DhcpServerIpAddress address, DhcpServerHardwareAddress hardwareAddress, string name, string comment, DateTime leaseExpires)
            => CreateClientV0(scope, address, hardwareAddress, name, comment, leaseExpires, ownerHost: null);

        internal static DhcpServerClient CreateClient(DhcpServerScope scope, DhcpServerIpAddress address, DhcpServerHardwareAddress hardwareAddress, string name, string comment, DateTime leaseExpires, DhcpServerHost ownerHost)
            => CreateClientV0(scope, address, hardwareAddress, name, comment, leaseExpires, ownerHost);

        internal static DhcpServerClient CreateClient(DhcpServerScope scope, DhcpServerIpAddress address, DhcpServerHardwareAddress hardwareAddress, string name, string comment, DateTime leaseExpires, DhcpServerHost ownerHost, DhcpServerClientTypes clientType, DhcpServerClientAddressStates addressState, DhcpServerClientQuarantineStatuses quarantineStatus, DateTime probationEnds, bool quarantineCapable)
        {
            if (scope.Server.IsCompatible(DhcpServerVersions.Windows2008R2))
                return CreateClientVQ(scope, address, hardwareAddress, name, comment, leaseExpires, ownerHost, clientType, addressState, quarantineStatus, probationEnds, quarantineCapable);
            else
                return CreateClientV0(scope, address, hardwareAddress, name, comment, leaseExpires, ownerHost);
        }

        private static DhcpServerClient CreateClientV0(DhcpServerScope scope, DhcpServerIpAddress address, DhcpServerHardwareAddress hardwareAddress, string name, string comment, DateTime leaseExpires, DhcpServerHost ownerHost)
        {
            if (!scope.IpRange.Contains(address))
                throw new ArgumentOutOfRangeException(nameof(address), "The address is not within the IP range of the scope");

            using (var clientInfo = new DHCP_CLIENT_INFO_Managed(clientIpAddress: address.ToNativeAsNetwork(),
                                                                 subnetMask: scope.Mask.ToNativeAsNetwork(),
                                                                 clientHardwareAddress: hardwareAddress.ToNativeBinaryData(),
                                                                 clientName: name,
                                                                 clientComment: comment,
                                                                 clientLeaseExpires: leaseExpires,
                                                                 ownerHost: (ownerHost ?? DhcpServerHost.Empty).ToNative()))
            {
                using (var clientInfoPtr = BitHelper.StructureToPtr(clientInfo))
                {
                    var result = Api.DhcpCreateClientInfo(ServerIpAddress: scope.Server.Address, ClientInfo: clientInfoPtr);

                    if (result != DhcpServerNativeErrors.SUCCESS)
                        throw new DhcpServerException(nameof(Api.DhcpCreateClientInfo), result);
                }
            }

            return GetClient(scope.Server, scope, address);
        }

        private static DhcpServerClient CreateClientVQ(DhcpServerScope scope, DhcpServerIpAddress address, DhcpServerHardwareAddress hardwareAddress, string name, string comment, DateTime leaseExpires, DhcpServerHost ownerHost, DhcpServerClientTypes clientType, DhcpServerClientAddressStates addressState, DhcpServerClientQuarantineStatuses quarantineStatus, DateTime probationEnds, bool quarantineCapable)
        {
            if (!scope.IpRange.Contains(address))
                throw new ArgumentOutOfRangeException(nameof(address), "The address is not within the IP range of the scope");

            using (var clientInfo = new DHCP_CLIENT_INFO_VQ_Managed(clientIpAddress: address.ToNativeAsNetwork(),
                                                                    subnetMask: scope.Mask.ToNativeAsNetwork(),
                                                                    clientHardwareAddress: hardwareAddress.ToNativeBinaryData(),
                                                                    clientName: name,
                                                                    clientComment: comment,
                                                                    clientLeaseExpires: leaseExpires,
                                                                    ownerHost: (ownerHost ?? DhcpServerHost.Empty).ToNative(),
                                                                    (DHCP_CLIENT_TYPE)clientType,
                                                                    (byte)addressState,
                                                                    (QuarantineStatus)quarantineStatus,
                                                                    probationEnds,
                                                                    quarantineCapable))
            {
                using (var clientInfoPtr = BitHelper.StructureToPtr(clientInfo))
                {
                    var result = Api.DhcpCreateClientInfo(ServerIpAddress: scope.Server.Address, ClientInfo: clientInfoPtr);

                    if (result != DhcpServerNativeErrors.SUCCESS)
                        throw new DhcpServerException(nameof(Api.DhcpCreateClientInfo), result);
                }
            }

            return GetClient(scope.Server, scope, address);
        }

        private void SetInfo(ClientInfo info)
        {
            info.SetInfo(Server);

            // update cache
            Info = info;
        }

        private static void Delete(DhcpServer server, DhcpServerIpAddress address)
        {
            var searchInfo = new DHCP_SEARCH_INFO_Managed_IpAddress(address.ToNativeAsNetwork());

            using (var searchInfoPtr = BitHelper.StructureToPtr(searchInfo))
            {
                var result = Api.DhcpDeleteClientInfo(server.Address, searchInfoPtr);

                if (result != DhcpServerNativeErrors.SUCCESS)
                    throw new DhcpServerException(nameof(Api.DhcpDeleteClientInfo), result);
            }
        }

        private static DhcpServerClient FromNative(DhcpServer server, DhcpServerScope scope, in DHCP_CLIENT_INFO native)
        {
            var info = ClientInfo.FromNative(in native);

            return new DhcpServerClient(server: server,
                                        scope: scope,
                                        ipAddress: native.ClientIpAddress.AsNetworkToIpAddress(),
                                        info: info);
        }

        private static DhcpServerClient FromNative(DhcpServer server, DhcpServerScope scope, in DHCP_CLIENT_INFO_VQ native)
        {
            var info = ClientInfo.FromNative(in native);

            return new DhcpServerClient(server: server,
                                        scope: scope,
                                        ipAddress: native.ClientIpAddress.AsNetworkToIpAddress(),
                                        info: info);
        }

        public override string ToString()
        {
            return $"[{HardwareAddress}] {AddressState} {IpAddress}/{SubnetMask.SignificantBits} ({(LeaseHasExpiry ? (LeaseExpired ? "expired" : LeaseExpiresLocal.ToString()) : "no expiry")}): {Name}{(string.IsNullOrEmpty(Comment) ? null : $" ({Comment})")}";
        }

        internal class ClientInfo
        {
            public readonly DHCP_IP_ADDRESS Address;
            public readonly DhcpServerIpMask SubnetMask;
            public readonly DhcpServerHardwareAddress HardwareAddress;
            public readonly string Name;
            public readonly string Comment;
            public readonly DateTime LeaseExpiresUtc;
            public readonly DhcpServerHost OwnerHost;
            public readonly DhcpServerClientTypes Type;
            public readonly DhcpServerClientAddressStates AddressState;
            public readonly DhcpServerClientNameProtectionStates NameProtectionState;
            public readonly DhcpServerClientDnsStates DnsState;
            public readonly DhcpServerClientQuarantineStatuses QuarantineStatus;
            public readonly DateTime ProbationEnds;
            public readonly bool QuarantineCapable;

            public ClientInfo(DHCP_IP_ADDRESS address, DhcpServerIpMask subnetMask, DhcpServerHardwareAddress hardwareAddress, string name, string comment,
                DateTime leaseExpiresUtc, DhcpServerHost ownerHost, DhcpServerClientTypes type, DhcpServerClientAddressStates addressState,
                DhcpServerClientNameProtectionStates nameProtectionState, DhcpServerClientDnsStates dnsState, DhcpServerClientQuarantineStatuses quarantineStatus,
                DateTime probationEnds, bool quarantineCapable)
            {
                Address = address;
                SubnetMask = subnetMask;
                HardwareAddress = hardwareAddress;
                Name = name;
                Comment = comment;
                LeaseExpiresUtc = leaseExpiresUtc;
                OwnerHost = ownerHost;
                Type = type;
                AddressState = addressState;
                NameProtectionState = nameProtectionState;
                DnsState = dnsState;
                QuarantineStatus = quarantineStatus;
                ProbationEnds = probationEnds;
                QuarantineCapable = quarantineCapable;
            }

            public static ClientInfo FromNative(in DHCP_CLIENT_INFO native)
            {
                return new ClientInfo(address: native.ClientIpAddress,
                                      subnetMask: native.SubnetMask.AsNetworkToIpMask(),
                                      hardwareAddress: native.ClientHardwareAddress.DataAsHardwareAddress,
                                      name: native.ClientName,
                                      comment: native.ClientComment,
                                      leaseExpiresUtc: ((DateTime)native.ClientLeaseExpires).ToUniversalTime(),
                                      ownerHost: DhcpServerHost.FromNative(native.OwnerHost),
                                      type: DhcpServerClientTypes.Unspecified,
                                      addressState: DhcpServerClientAddressStates.Unknown,
                                      nameProtectionState: DhcpServerClientNameProtectionStates.Unknown,
                                      dnsState: DhcpServerClientDnsStates.Unknown,
                                      quarantineStatus: DhcpServerClientQuarantineStatuses.NoQuarantineInformation,
                                      probationEnds: DateTime.MaxValue,
                                      quarantineCapable: false);
            }

            public static ClientInfo FromNative(in DHCP_CLIENT_INFO_VQ native)
            {
                return new ClientInfo(address: native.ClientIpAddress,
                                      subnetMask: native.SubnetMask.AsNetworkToIpMask(),
                                      hardwareAddress: native.ClientHardwareAddress.DataAsHardwareAddress,
                                      name: native.ClientName,
                                      comment: native.ClientComment,
                                      leaseExpiresUtc: ((DateTime)native.ClientLeaseExpires).ToUniversalTime(),
                                      ownerHost: DhcpServerHost.FromNative(native.OwnerHost),
                                      type: (DhcpServerClientTypes)native.ClientType,
                                      addressState: (DhcpServerClientAddressStates)(native.AddressState & 0x03), // bits 0 & 1
                                      nameProtectionState: (DhcpServerClientNameProtectionStates)((native.AddressState >> 2) & 0x03), // bits 2 & 3
                                      dnsState: (DhcpServerClientDnsStates)((native.AddressState >> 4) & 0x0F), // bits 4-7
                                      quarantineStatus: (DhcpServerClientQuarantineStatuses)native.Status,
                                      probationEnds: native.ProbationEnds,
                                      quarantineCapable: native.QuarantineCapable);
            }

            public ClientInfo UpdateName(string name)
                => new ClientInfo(Address, SubnetMask, HardwareAddress, name, Comment, LeaseExpiresUtc, OwnerHost, Type, AddressState, NameProtectionState, DnsState, QuarantineStatus, ProbationEnds, QuarantineCapable);
            public ClientInfo UpdateComment(string comment)
                => new ClientInfo(Address, SubnetMask, HardwareAddress, Name, comment, LeaseExpiresUtc, OwnerHost, Type, AddressState, NameProtectionState, DnsState, QuarantineStatus, ProbationEnds, QuarantineCapable);
            public ClientInfo UpdateHardwareAddress(DhcpServerHardwareAddress hardwareAddress)
                => new ClientInfo(Address, SubnetMask, hardwareAddress, Name, Comment, LeaseExpiresUtc, OwnerHost, Type, AddressState, NameProtectionState, DnsState, QuarantineStatus, ProbationEnds, QuarantineCapable);

            public void SetInfo(DhcpServer server)
            {
                // only supporting v0 at this stage
                using (var infoNative = ToNativeV0())
                {
                    var result = Api.DhcpSetClientInfo(ServerIpAddress: server.Address,
                                                       ClientInfo: in infoNative);

                    if (result != DhcpServerNativeErrors.SUCCESS)
                        throw new DhcpServerException(nameof(Api.DhcpSetClientInfo), result);
                }
            }

            public DHCP_CLIENT_INFO_Managed ToNativeV0()
                => new DHCP_CLIENT_INFO_Managed(Address, SubnetMask.ToNativeAsNetwork(), HardwareAddress.ToNativeBinaryData(), Name, Comment, LeaseExpiresUtc, OwnerHost.ToNative());
        }
    }
}
