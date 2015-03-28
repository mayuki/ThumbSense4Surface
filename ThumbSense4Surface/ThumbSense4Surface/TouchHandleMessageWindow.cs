using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThumbSense4SurfacePro
{
    class TouchHandleMessageWindow : Form
    {
        private Boolean _isFingerOnTouchPad;
        private Win32.HookHandle _handle;
        private Boolean _isMouseDown;

        public TouchHandleMessageWindow()
        {
            var handle = this.Handle; // Create Handle

            Win32.HookProcedure = KeyboardHookProc;
            _handle = Win32.SetWindowsHookEx(
                Win32.WH_KEYBOARD_LL,
                Win32.HookProcedure,
                Win32.GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName),
                0
            );
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Change to message only window
            Win32.SetParent(this.Handle, new IntPtr(-3) /*HWND_MESSAGE*/);
        }

        protected override void OnClosed(EventArgs e)
        {
            _handle.Dispose();
            base.OnClosed(e);
        }

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var keyEventStruct = (Win32.KeyboardHookEventStruct)Marshal.PtrToStructure(
                lParam,
                typeof(Win32.KeyboardHookEventStruct)
            );
            Debug.WriteLine(keyEventStruct);

            if (_isFingerOnTouchPad)
            {
                var handled = false;
                if ((Keys)keyEventStruct.wVk == Keys.J || (Keys)keyEventStruct.wVk == Keys.K)
                {
                    handled = true;

                    var isLeft = (Keys)keyEventStruct.wVk == Keys.J;
                    var isPressed = !((keyEventStruct.dwFlags & (0x1 << 7)) == (0x1 << 7));
                    Debug.WriteLine(isPressed);
                    if (isPressed)
                    {
                        if (!_isMouseDown)
                        {
                            Debug.WriteLine("Down");
                            Win32.mouse_event(
                                (uint)(isLeft ? Win32.MouseEventFlags.LEFTDOWN : Win32.MouseEventFlags.RIGHTDOWN),
                                0, 0, 0, UIntPtr.Zero);
                            _isMouseDown = true;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Up");
                        Win32.mouse_event(
                                (uint)(isLeft ? Win32.MouseEventFlags.LEFTUP : Win32.MouseEventFlags.RIGHTUP),
                                0, 0, 0, UIntPtr.Zero);
                        _isMouseDown = false;
                    }
                    // if (keyEventStruct.dwFlags)
                }

                if (handled)
                {
                    Debug.WriteLine("Handled");
                    return (IntPtr)1; // Key Hook Handled
                }
            }

            return Win32.CallNextHookEx(_handle, nCode, wParam, lParam);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)Win32.WM.CREATE)
            {
                var rawDevices = new[]
                {
                    new Win32.RAWINPUTDEVICE()
                    {
                        Flags = Win32.RawInputDeviceFlags.InputSink | Win32.RawInputDeviceFlags.PageOnly,
                        //UsagePage = Win32.HIDUsagePage.Generic,
                        //Usage = Win32.HIDUsage.Mouse,
                        UsagePage = (Win32.HIDUsagePage)0x0D,
                        Usage = (Win32.HIDUsage)0,
                        WindowHandle = m.HWnd,
                    }
                };
                Win32.RegisterRawInputDevices(rawDevices, rawDevices.Length, Marshal.SizeOf(rawDevices[0]));
            }

            if (m.Msg == (int)Win32.WM.INPUT)
            {
                Win32.RawInput rawInput;
                var size = Marshal.SizeOf(typeof(Win32.RawInput));
                var sizeHid = Marshal.SizeOf(typeof(Win32.RawHID));
                var headerSize = Marshal.SizeOf(typeof(Win32.RawInputHeader));

                // サイズをとる
                Win32.GetRawInputData(
                    m.LParam,
                    Win32.RawInputCommand.Input,
                    IntPtr.Zero,
                    ref size,
                    Marshal.SizeOf(typeof(Win32.RawInputHeader))
                );

                // サイズがきたいしてるのとちがうことがある(たぶんデータの形の都合)
                if (size == Marshal.SizeOf(typeof(Win32.RawInput)))
                {
                    // データを吸い出す
                    var outSize = Win32.GetRawInputData(
                        m.LParam,
                        Win32.RawInputCommand.Input,
                        out rawInput,
                        ref size,
                        Marshal.SizeOf(typeof(Win32.RawInputHeader))
                    );

                    if (outSize != -1)
                    {
                        var deviceInfo = new Win32.DeviceInfo();
                        var dataSize = (UInt32)Marshal.SizeOf(typeof(Win32.DeviceInfo));
                        Win32.GetRawInputDeviceInfo(rawInput.Header.Device, 0x2000000b, ref deviceInfo, ref dataSize);

                        if (deviceInfo.HIDInfo.VendorID == 0x45e && deviceInfo.HIDInfo.ProductID == 0x7dc)
                        {
                            //Debug.WriteLine("ReportID:{0}, wXData:{1}, bStatus:{2}",
                            //    rawInput.Data.HID.ReportID, rawInput.Data.HID.wXData, rawInput.Data.HID.bStatus);
                            _isFingerOnTouchPad = (rawInput.Data.HID.bStatus == 3);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Invalid Size");
                    }
                }

            }
            base.WndProc(ref m);
        }
    }
}
