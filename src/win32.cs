namespace tfd
{
    using System;
    using System.Runtime.InteropServices;

    public partial class win32
    {
        //https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-mouse_event#:~:text=button%20is%20up.-,MOUSEEVENTF_MOVE,-0x0001
        public const int MOUSEEVENTF_MOVE = 0x0001;
        public const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const int MOUSEEVENTF_LEFTUP = 0x0004;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const int MOUSEEVENTF_RIGHTUP = 0x0010;
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        [DllImport("user32", SetLastError = true)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        //https://stackoverflow.com/questions/2416748/how-do-you-simulate-mouse-click-in-c
        [StructLayout(LayoutKind.Sequential)]
        public struct MousePoint
        {
            public int X;
            public int Y;
        }
        [DllImport("user32", SetLastError = true)]
        public static extern bool GetCursorPos(out MousePoint lpMousePoint);

        public static int SM_CXSCREEN = 0;
        public static int SM_CYSCREEN = 1;
        [DllImport("user32", SetLastError = true)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32", SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        public delegate int WindowHookDelegate(int code, int wParam, IntPtr lParam);

        [DllImport("user32", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, WindowHookDelegate callback, IntPtr hInstance, uint threadId);

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32", SetLastError = true)]
        public static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);
    }

    public partial class win32
    {
        public const int WS_EX_TOOLWINDOW = 0x80;
        public const int WH_MOUSE_LL = 14;
        public const int WM_INPUT = 0x00FF;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int RIM_INPUT = 0;
        public const int RIM_INPUTSINK = 1;

        [DllImport("user32", SetLastError = true)]
        public static extern uint GetRawInputDeviceList(
            [Out] RAWINPUTDEVICELIST[] pRawInputDeviceList,
            ref uint puiNumDevices,
            uint cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType; // RIM_TYPEMOUSE or RIM_TYPEKEYBOARD or RIM_TYPEHID
        }

        public const uint RIM_TYPEMOUSE = 0;
        public const uint RIM_TYPEKEYBOARD = 1;
        public const uint RIM_TYPEHID = 2;

        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterRawInputDevices(
            RAWINPUTDEVICE[] pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags; // RIDEV_INPUTSINK
            public IntPtr hWndTarget;
        }

        public const uint RIDEV_INPUTSINK = 0x00000100;

        [DllImport("user32", SetLastError = true)]
        public static extern uint GetRawInputData(
            IntPtr hRawInput, // lParam in WM_INPUT
            uint uiCommand, // RID_HEADER
            IntPtr pData,
            ref uint pcbSize,
            uint cbSizeHeader);

        public const uint RID_INPUT = 0x10000003;

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUT
        {
            public RAWINPUTHEADER Header;
            public RAWHID Hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType; // RIM_TYPEMOUSE or RIM_TYPEKEYBOARD or RIM_TYPEHID
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam; // wParam in WM_INPUT
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWHID
        {
            public uint dwSizeHid;
            public uint dwCount;
            public IntPtr bRawData; // This is not for use.
        }

        [DllImport("user32", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice, // hDevice by RAWINPUTHEADER
            uint uiCommand, // RIDI_PREPARSEDDATA
            IntPtr pData,
            ref uint pcbSize);

        [DllImport("user32", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice, // hDevice by RAWINPUTDEVICELIST
            uint uiCommand, // RIDI_DEVICEINFO
            ref RID_DEVICE_INFO pData,
            ref uint pcbSize);

        public const uint RIDI_PREPARSEDDATA = 0x20000005;
        public const uint RIDI_DEVICEINFO = 0x2000000b;

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO
        {
            public uint cbSize; // This is determined to accommodate RID_DEVICE_INFO_KEYBOARD.
            public uint dwType;
            public RID_DEVICE_INFO_HID hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_HID
        {
            public uint dwVendorId;
            public uint dwProductId;
            public uint dwVersionNumber;
            public ushort usUsagePage;
            public ushort usUsage;
        }

        [DllImport("hid", SetLastError = true)]
        public static extern uint HidP_GetCaps(
            IntPtr PreparsedData,
            out HIDP_CAPS Capabilities);

        public const uint HIDP_STATUS_SUCCESS = 0x00110000;

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;

            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [DllImport("hid", CharSet = CharSet.Auto)]
        public static extern uint HidP_GetValueCaps(
            HIDP_REPORT_TYPE ReportType,
            [Out] HIDP_VALUE_CAPS[] ValueCaps,
            ref ushort ValueCapsLength,
            IntPtr PreparsedData);

        public enum HIDP_REPORT_TYPE
        {
            HidP_Input,
            HidP_Output,
            HidP_Feature
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_VALUE_CAPS
        {
            public ushort UsagePage;
            public byte ReportID;

            [MarshalAs(UnmanagedType.U1)]
            public bool IsAlias;

            public ushort BitField;
            public ushort LinkCollection;
            public ushort LinkUsage;
            public ushort LinkUsagePage;

            [MarshalAs(UnmanagedType.U1)]
            public bool IsRange;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsStringRange;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsDesignatorRange;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsAbsolute;
            [MarshalAs(UnmanagedType.U1)]
            public bool HasNull;

            public byte Reserved;
            public ushort BitSize;
            public ushort ReportCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public ushort[] Reserved2;

            public uint UnitsExp;
            public uint Units;
            public int LogicalMin;
            public int LogicalMax;
            public int PhysicalMin;
            public int PhysicalMax;

            // Range
            public ushort UsageMin;
            public ushort UsageMax;
            public ushort StringMin;
            public ushort StringMax;
            public ushort DesignatorMin;
            public ushort DesignatorMax;
            public ushort DataIndexMin;
            public ushort DataIndexMax;

            // NotRange
            public ushort Usage => UsageMin;
            // ushort Reserved1;
            public ushort StringIndex => StringMin;
            // ushort Reserved2;
            public ushort DesignatorIndex => DesignatorMin;
            // ushort Reserved3;
            public ushort DataIndex => DataIndexMin;
            // ushort Reserved4;
        }

        [DllImport("hid", CharSet = CharSet.Auto)]
        public static extern uint HidP_GetUsageValue(
            HIDP_REPORT_TYPE ReportType,
            ushort UsagePage,
            ushort LinkCollection,
            ushort Usage,
            out uint UsageValue,
            IntPtr PreparsedData,
            IntPtr Report,
            uint ReportLength);
    }
}
