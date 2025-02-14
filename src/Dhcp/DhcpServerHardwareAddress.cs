﻿using System;
using Dhcp.Native;
using Newtonsoft.Json;

namespace Dhcp
{
    [Serializable]
    public struct DhcpServerHardwareAddress : IEquatable<DhcpServerHardwareAddress>
    {
        public const int MaximumLength = 16;

#pragma warning disable IDE0032 // Use auto property
        private readonly DhcpServerHardwareType hwAddrType;
        private readonly int hwAddrLen;
        private readonly ulong hwAddr1;
        private readonly ulong hwAddr2;
#pragma warning restore IDE0032 // Use auto property
        public string String => this.ToString();

        /// <summary>
        /// Type of hardware address (see RFC 1700 Assigned Numbers - Number Hardware Type (hrd))
        /// </summary>
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DhcpServerHardwareType Type => hwAddrType;

        /// <summary>
        /// Length of hardware address in bytes
        /// </summary>
        public int Length => hwAddrLen;

        /// <summary>
        /// True unless the source of the hardware address is invalid (beyond the 16 byte storage limit)
        /// </summary>
        public bool IsValid => hwAddrLen <= MaximumLength;

        /// <summary>
        /// Native hardware address as a byte array
        /// </summary>
        public byte[] Native
        {
            get
            {
                EnsureValid();

                var addr = new byte[hwAddrLen];
                ToNative(addr, 0, hwAddrLen);
                return addr;
            }
        }

        internal DhcpServerHardwareAddress(DhcpServerHardwareType type, int length, ulong addr1, ulong addr2)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            hwAddrType = type;
            hwAddrLen = length;
            hwAddr1 = addr1;
            hwAddr2 = addr2;
        }

        public void ToNative(byte[] buffer, int offset, int length)
        {
            if (length < hwAddrLen)
                throw new ArgumentOutOfRangeException(nameof(length));

            EnsureValid();

            for (var i = 0; i < length; i++)
            {
                if (i < 8)
                    buffer[offset + i] = (byte)(hwAddr1 >> ((7 - i) * 8));
                else
                    buffer[offset + i] = (byte)(hwAddr2 >> ((15 - i) * 8));
            }
        }

        internal DHCP_BINARY_DATA_Managed ToNativeBinaryData()
        {
            EnsureValid();

            return new DHCP_BINARY_DATA_Managed(hwAddr1, hwAddr2, hwAddrLen);
        }
        internal DHCP_CLIENT_UID_Managed ToNativeClientUid()
        {
            EnsureValid();

            return new DHCP_CLIENT_UID_Managed(hwAddr1, hwAddr2, hwAddrLen);
        }

        public static DhcpServerHardwareAddress FromNativeEthernet(ulong nativeHardwareAddress)
            => new DhcpServerHardwareAddress(DhcpServerHardwareType.Ethernet, 6, nativeHardwareAddress, 0UL);

        public static DhcpServerHardwareAddress FromNativeEthernet(byte[] hardwareAddress)
            => FromNative(DhcpServerHardwareType.Ethernet, hardwareAddress);

        public static DhcpServerHardwareAddress FromEthernetString(string hardwareAddress)
        {
            if (string.IsNullOrWhiteSpace(hardwareAddress))
                throw new ArgumentNullException(nameof(hardwareAddress));

            var hwAddr1 = 0UL;
            var hwAddr2 = 0UL;
            int hwLength;

            if (hardwareAddress.Length < 3 || (hardwareAddress[2] != ':' && hardwareAddress[2] != '-'))
            {
                // no separator
                hwLength = hardwareAddress.Length / 2;

                if (hardwareAddress.Length % 2 != 0 || hwLength > MaximumLength)
                    throw new ArgumentOutOfRangeException(nameof(hardwareAddress), "Invalid hardware address format");

                for (var i = 0; i < hwLength; i++)
                {
                    if (!BitHelper.TryParseByteFromHexSubstring(hardwareAddress, i * 2, 2, out var octet))
                        throw new ArgumentOutOfRangeException(nameof(hardwareAddress), "Invalid hardware address format");

                    if (i < 8)
                        hwAddr1 |= ((ulong)octet) << (7 - i) * 8;
                    else
                        hwAddr2 |= ((ulong)octet) << (15 - i) * 8;
                }
            }
            else
            {
                // separator between octets
                hwLength = (hardwareAddress.Length + 1) / 3;

                if (hardwareAddress.Length % 3 != 2 || hwLength > MaximumLength)
                    throw new ArgumentOutOfRangeException(nameof(hardwareAddress), "Invalid hardware address format");

                for (var i = 0; i < hwLength; i++)
                {
                    if (!BitHelper.TryParseByteFromHexSubstring(hardwareAddress, i * 3, 2, out var octet))
                        throw new ArgumentOutOfRangeException(nameof(hardwareAddress), "Invalid hardware address format");

                    if (i < 8)
                        hwAddr1 |= ((ulong)octet) << (7 - i) * 8;
                    else
                        hwAddr2 |= ((ulong)octet) << (15 - i) * 8;
                }
            }

            return new DhcpServerHardwareAddress(DhcpServerHardwareType.Ethernet, hwLength, hwAddr1, hwAddr2);
        }

        internal static DhcpServerHardwareAddress FromNative(DhcpServerHardwareType type, int hardwareAddressLength, ulong hardwareAddress1, ulong hardwareAddress2)
        {
            return new DhcpServerHardwareAddress(type, hardwareAddressLength, hardwareAddress1, hardwareAddress2);
        }

        public static DhcpServerHardwareAddress FromNative(DhcpServerHardwareType type, byte[] hardwareAddress)
        {
            if (hardwareAddress.Length > MaximumLength)
                throw new ArgumentOutOfRangeException(nameof(hardwareAddress));

            return new DhcpServerHardwareAddress(
                type,
                hardwareAddress.Length,
                Marshal(hardwareAddress, 0),
                hardwareAddress.Length > 8 ? Marshal(hardwareAddress, 8) : 0UL);
        }

        public static DhcpServerHardwareAddress FromNative(DhcpServerHardwareType type, byte[] buffer, int hardwareAddressOffset, int hardwareAddressLength)
        {
            if (hardwareAddressLength < 0 || hardwareAddressLength > MaximumLength)
                throw new ArgumentOutOfRangeException(nameof(hardwareAddressLength));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < hardwareAddressOffset + hardwareAddressLength)
                throw new ArgumentOutOfRangeException(nameof(hardwareAddressOffset));

            return new DhcpServerHardwareAddress(
                type,
                hardwareAddressLength,
                Marshal(buffer, hardwareAddressOffset, hardwareAddressLength),
                hardwareAddressLength > 8 ? Marshal(buffer, hardwareAddressOffset + 8, hardwareAddressLength - 8) : 0UL);
        }

        public static DhcpServerHardwareAddress FromNative(DhcpServerHardwareType type, IntPtr pointer, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (pointer == IntPtr.Zero)
                throw new ArgumentNullException(nameof(pointer));

            var hwAddr1 = 0UL;
            var hwAddr2 = 0UL;

            if (length == 0)
                return new DhcpServerHardwareAddress(type, length, hwAddr1, hwAddr2);

            hwAddr1 = (ulong)BitHelper.ReadIntVar(pointer, 0, length > 7 ? 8 : length);

            if (length > 8)
                hwAddr2 = (ulong)BitHelper.ReadIntVar(pointer, 8, Math.Min(length - 8, 8));

            return new DhcpServerHardwareAddress(type, length, hwAddr1, hwAddr2);
        }

        private static ulong Marshal(byte[] address, int offset)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (offset < 0 || address.Length == 0 || offset >= address.Length)
                return 0UL;

            var length = address.Length < offset + 8 ? address.Length - offset : 8;

            return Marshal(address, offset, length);
        }

        private static ulong Marshal(byte[] address, int offset, int length)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (offset < 0 || address.Length == 0 || offset >= address.Length)
                return 0UL;

            if (length > 8)
                length = 8;

            var result = default(ulong);

            for (var i = 0; i < length; i++)
                result |= (ulong)address[offset + i] << ((7 - i) * 8);

            return result;
        }


        private void EnsureValid()
        {
            if (!IsValid)
            {
                throw new InvalidCastException($"Invalid hardware address, the maximum length ({MaximumLength}) is exceeded ({hwAddrLen}).");
            }
        }

        public override int GetHashCode()
        {
            return
                (int)(hwAddr1 >> 32) ^
                (int)(hwAddr1 & 0xFFFFFFFF) ^
                (int)(hwAddr2 >> 32) ^
                ((int)hwAddrType << 24) ^
                (hwAddrLen);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is DhcpServerHardwareAddress ha)
                return Equals(ha);

            return false;
        }

        public bool Equals(DhcpServerHardwareAddress other)
        {
            return
                hwAddrType == other.hwAddrType &&
                hwAddrLen == other.hwAddrLen &&
                hwAddr1 == other.hwAddr1 &&
                hwAddr2 == other.hwAddr2;
        }

        public static bool operator ==(DhcpServerHardwareAddress lhs, DhcpServerHardwareAddress rhs) => lhs.Equals(rhs);
        public static bool operator !=(DhcpServerHardwareAddress lhs, DhcpServerHardwareAddress rhs) => !lhs.Equals(rhs);

        public static implicit operator string(DhcpServerHardwareAddress address) => address.ToString();
        public static implicit operator DhcpServerHardwareAddress(string address) => FromEthernetString(address);

        public override string ToString()
        {
            var hexString = BitHelper.ReadHexString(hwAddr1, hwAddr2, 0, Math.Min(hwAddrLen, MaximumLength), ':');

            if (hwAddrLen > MaximumLength)
            {
                return hexString + "...";
            }

            return hexString;
        }
    }
}
