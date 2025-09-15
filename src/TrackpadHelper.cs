namespace tfd
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;

    //below code & structs referenced from https://github.com/emoacht/RawInput.Touchpad
    public struct TrackpadContact
    {
        public int Id { get; }
        public int X { get; }
        public int Y { get; }

        public TrackpadContact(int id, int x, int y) => (Id, X, Y) = (id, x, y);
    }

    public class TrackpadContactCreator
    {
        public int? Id { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }

        public bool TryCreate(out TrackpadContact contact)
        {
            if (Id.HasValue && X.HasValue && Y.HasValue)
            {
                contact = new TrackpadContact(Id.Value, X.Value, Y.Value);
                return true;
            }
            contact = default;
            return false;
        }

        public void Clear()
        {
            Id = null;
            X = null;
            Y = null;
        }
    }

    public static class TrackpadHelper
    {
        public static bool Exists()
        {
            uint deviceListCount = 0;
            uint rawInputDeviceListSize = (uint)Marshal.SizeOf<win32.RAWINPUTDEVICELIST>();
            if (win32.GetRawInputDeviceList(
                null,
                ref deviceListCount,
                rawInputDeviceListSize) != 0)
            {
                return false;
            }

            var devices = new win32.RAWINPUTDEVICELIST[deviceListCount];
            if (win32.GetRawInputDeviceList(
                devices,
                ref deviceListCount,
                rawInputDeviceListSize) != deviceListCount)
            {
                return false;
            }

            foreach (var device in devices.Where(x => x.dwType == win32.RIM_TYPEHID))
            {
                uint deviceInfoSize = 0;
                if (win32.GetRawInputDeviceInfo(
                    device.hDevice,
                    win32.RIDI_DEVICEINFO,
                    IntPtr.Zero,
                    ref deviceInfoSize) != 0)
                {
                    continue;
                }

                var deviceInfo = new win32.RID_DEVICE_INFO { cbSize = deviceInfoSize };

                if (win32.GetRawInputDeviceInfo(
                    device.hDevice,
                    win32.RIDI_DEVICEINFO,
                    ref deviceInfo,
                    ref deviceInfoSize) == unchecked((uint)-1))
                {
                    continue;
                }

                if (deviceInfo.hid.usUsagePage == 0x0D && deviceInfo.hid.usUsage == 0x05)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool RegisterTrackpad(IntPtr hWnd)
        {
            //Precision Touchpad (PTP) in HID Clients Supported in Windows
            //https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/hid-architecture#hid-clients-supported-in-windows
            win32.RAWINPUTDEVICE trackpadDevice = new win32.RAWINPUTDEVICE
            {
                usUsagePage = 0x0D,
                usUsage = 0x05,
                dwFlags = 0x100, // WM_INPUT messages come when the window is in background as well
                hWndTarget = hWnd
            };

            return win32.RegisterRawInputDevices(new[] { trackpadDevice }, 1, (uint)Marshal.SizeOf<win32.RAWINPUTDEVICE>());
        }

        public static TrackpadContact[] ParseInput(IntPtr lParam)
        {
            uint rawInputSize = 0;
            uint rawInputHeaderSize = (uint)Marshal.SizeOf<win32.RAWINPUTHEADER>();

            if (win32.GetRawInputData(
                lParam,
                win32.RID_INPUT,
                IntPtr.Zero,
                ref rawInputSize,
                rawInputHeaderSize) != 0)
            {
                return null;
            }

            win32.RAWINPUT rawInput;
            byte[] rawHidRawData;

            IntPtr rawInputPointer = IntPtr.Zero;
            try
            {
                rawInputPointer = Marshal.AllocHGlobal((int)rawInputSize);
                if (win32.GetRawInputData(
                    lParam,
                    win32.RID_INPUT,
                    rawInputPointer,
                    ref rawInputSize,
                    rawInputHeaderSize) != rawInputSize)
                {
                    return null;
                }

                var rawInputData = new byte[rawInputSize];
                Marshal.Copy(rawInputPointer, rawInputData, 0, rawInputData.Length);

                rawInput = Marshal.PtrToStructure<win32.RAWINPUT>(rawInputPointer);
                rawHidRawData = new byte[rawInput.Hid.dwSizeHid * rawInput.Hid.dwCount];

                int rawInputOffset = (int)rawInputSize - rawHidRawData.Length;
                Buffer.BlockCopy(rawInputData, rawInputOffset, rawHidRawData, 0, rawHidRawData.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(rawInputPointer);
            }


            IntPtr rawHidRawDataPointer = Marshal.AllocHGlobal(rawHidRawData.Length);
            Marshal.Copy(rawHidRawData, 0, rawHidRawDataPointer, rawHidRawData.Length);
            IntPtr preparsedDataPointer = IntPtr.Zero;
            try
            {
                uint preparsedDataSize = 0;
                if (win32.GetRawInputDeviceInfo(
                    rawInput.Header.hDevice,
                    win32.RIDI_PREPARSEDDATA,
                    IntPtr.Zero,
                    ref preparsedDataSize) != 0)
                {
                    return null;
                }

                preparsedDataPointer = Marshal.AllocHGlobal((int)preparsedDataSize);

                if (win32.GetRawInputDeviceInfo(
                    rawInput.Header.hDevice,
                    win32.RIDI_PREPARSEDDATA,
                    preparsedDataPointer,
                    ref preparsedDataSize) != preparsedDataSize)
                {
                    return null;
                }

                if (win32.HidP_GetCaps(
                    preparsedDataPointer,
                    out win32.HIDP_CAPS caps) != win32.HIDP_STATUS_SUCCESS)
                {
                    return null;
                }

                ushort valueCapsLength = caps.NumberInputValueCaps;
                win32.HIDP_VALUE_CAPS[] valueCaps = new win32.HIDP_VALUE_CAPS[valueCapsLength];

                if (win32.HidP_GetValueCaps(
                    win32.HIDP_REPORT_TYPE.HidP_Input,
                    valueCaps,
                    ref valueCapsLength,
                    preparsedDataPointer) != win32.HIDP_STATUS_SUCCESS)
                {
                    return null;
                }

                //Usage Page and ID in Windows Precision Touchpad input reports
                //https://docs.microsoft.com/en-us/windows-hardware/design/component-guidelines/windows-precision-touchpad-required-hid-top-level-collections#windows-precision-touchpad-input-reports
                //https://learn.microsoft.com/en-us/windows-hardware/design/component-guidelines/touchpad-windows-precision-touchpad-collection
                uint hidStatus = win32.HidP_GetUsageValue(
                        win32.HIDP_REPORT_TYPE.HidP_Input,
                        0x0D,
                        0,
                        0x54,
                        out uint numContacts,
                        preparsedDataPointer,
                        rawHidRawDataPointer,
                        (uint)rawHidRawData.Length);

                if (hidStatus != win32.HIDP_STATUS_SUCCESS || numContacts <= 0) return null;

                TrackpadContact[] contactsArray = new TrackpadContact[numContacts];
                TrackpadContactCreator creator = new TrackpadContactCreator();

                for (int i = 0, contactIdx = 0; i < valueCaps.Length; ++i)
                {
                    win32.HIDP_VALUE_CAPS valueCap = valueCaps[i];
                    if (valueCap.LinkCollection == 0) continue;

                    if (win32.HidP_GetUsageValue(
                        win32.HIDP_REPORT_TYPE.HidP_Input,
                        valueCap.UsagePage,
                        valueCap.LinkCollection,
                        valueCap.Usage,
                        out uint value,
                        preparsedDataPointer,
                        rawHidRawDataPointer,
                        (uint)rawHidRawData.Length) != win32.HIDP_STATUS_SUCCESS)
                    {
                        continue;
                    }

                    if (valueCap.UsagePage == 0x0D && valueCap.Usage == 0x51)
                    {
                        creator.Id = (int)value;
                    }
                    else if (valueCap.UsagePage == 0x01 && valueCap.Usage == 0x30)
                    {
                        creator.X = (int)value;
                    }
                    else if (valueCap.UsagePage == 0x01 && valueCap.Usage == 0x31)
                    {
                        creator.Y = (int)value;
                    }

                    if (creator.TryCreate(out TrackpadContact contact))
                    {
                        contactsArray[contactIdx++] = contact;
                        if (contactIdx >= numContacts) break;
                        creator.Clear();
                    }
                }

                return contactsArray;
            }
            finally
            {
                Marshal.FreeHGlobal(rawHidRawDataPointer);
                Marshal.FreeHGlobal(preparsedDataPointer);
            }
        }
    }
}