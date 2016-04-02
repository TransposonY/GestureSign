using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Daemon.Native;

namespace GestureSign.Daemon
{
    public class PointerInputTargetWindow : Form
    {
        bool _isRegistered;
        bool _isPointerMove;
        POINT _lastPoint;

        public bool IsRegistered
        {
            get { return _isRegistered; }
            private set
            {
                if (value)
                {
                    if (_isRegistered) return;
                    if (NativeMethods.RegisterPointerInputTarget(Handle, POINTER_INPUT_TYPE.TOUCH))
                    {
                        NativeMethods.InitializeTouchInjection(10, TOUCH_FEEDBACK.NONE);

                        NativeMethods.AccSetRunningUtilityState(Handle, NativeMethods.ANRUS_TOUCH_MODIFICATION_ACTIVE, NativeMethods.ANRUS_TOUCH_MODIFICATION_ACTIVE);
                        _isRegistered = true;
                    }
                }
                else
                {
                    if (_isRegistered && NativeMethods.UnregisterPointerInputTarget(Handle, POINTER_INPUT_TYPE.TOUCH))
                    {
                        NativeMethods.AccSetRunningUtilityState(Handle, 0, 0);
                        _isRegistered = false;
                    }
                }
            }
        }

        public int NumberOfTouchscreens { get; set; }

        public void InterceptTouchInput(bool intercept)
        {
            if (!IsHandleCreated)
            {
                if (!IsDisposed)
                    CreateHandle();
            }

            if (InvokeRequired)
                Invoke(new Action(() => IsRegistered = intercept));
            else
                IsRegistered = intercept;

        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ChangeToMessageOnlyWindow();
        }

        private void ChangeToMessageOnlyWindow()
        {
            IntPtr HWND_MESSAGE = new IntPtr(-3);
            NativeMethods.SetParent(this.Handle, HWND_MESSAGE);
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                //case WM_POINTERENTER:
                //case WM_POINTERLEAVE:
                //case WM_POINTERCAPTURECHANGED:
                case NativeMethods.WM_POINTERDOWN:
                    _isPointerMove = false;
                    _lastPoint.X = _lastPoint.Y = 0;
                    if (ProcessPointerMessage(message)) return;
                    break;
                case NativeMethods.WM_POINTERUP:
                case NativeMethods.WM_POINTERUPDATE:
                    _isPointerMove |= ProcessPointerMessage(message);
                    if (_isPointerMove) return;
                    else break;

            }
            base.WndProc(ref message);
        }

        private void CheckLastError()
        {
            int errCode = Marshal.GetLastWin32Error();
            if (errCode != 0)
            {
                throw new Win32Exception(errCode);
            }
        }

        #region ProcessInput

        private bool ProcessPointerMessage(Message message)
        {
            bool pointChanged = false;
            int pointerId = (int)(message.WParam.ToInt64() & 0xffff);
            int pCount = 0;
            try
            {
                if (!NativeMethods.GetPointerFrameInfo(pointerId, ref pCount, null))
                {
                    CheckLastError();
                }
                POINTER_INFO[] pointerInfos = new POINTER_INFO[pCount];
                if (!NativeMethods.GetPointerFrameInfo(pointerId, ref pCount, pointerInfos))
                {
                    CheckLastError();
                }

                if (pCount == 1)
                {
                    if (_lastPoint.X == 0 && _lastPoint.Y == 0) _lastPoint = pointerInfos[0].PtPixelLocation;
                    else
                    {
                        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                        pointChanged = Math.Abs(pointerInfos[0].PtPixelLocation.X - _lastPoint.X) >
                                       screenWidth / 100 ||
                                       Math.Abs(pointerInfos[0].PtPixelLocation.Y - _lastPoint.Y) >
                                       screenHeight / 100;

                        _lastPoint = pointerInfos[0].PtPixelLocation;
                    }

                    //Allow single-finger slide
                    POINTER_TOUCH_INFO[] ptis = new POINTER_TOUCH_INFO[pCount];
                    for (int i = 0; i < ptis.Length; i++)
                    {
                        ptis[i].TouchFlags = TOUCH_FLAGS.NONE;
                        ptis[i].PointerInfo = new POINTER_INFO
                        {
                            pointerType = POINTER_INPUT_TYPE.TOUCH,
                            PointerFlags = pointerInfos[i].PointerFlags.HasFlag(POINTER_FLAGS.UPDATE) ?
                            POINTER_FLAGS.INCONTACT | POINTER_FLAGS.INRANGE | POINTER_FLAGS.UPDATE : pointerInfos[i].PointerFlags.HasFlag(POINTER_FLAGS.UP) ?
                            POINTER_FLAGS.UP : POINTER_FLAGS.DOWN | POINTER_FLAGS.INRANGE | POINTER_FLAGS.INCONTACT,
                            PtPixelLocation = pointerInfos[i].PtPixelLocation,
                        };
                    }
                    NativeMethods.InjectTouchInput(1, ptis);
                }
                else pointChanged = true;
            }
            catch (Win32Exception) { }
            return pointChanged;
        }

        #endregion ProcessInput

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                CreateParams myParams = base.CreateParams;
                myParams.ExStyle = myParams.ExStyle | WS_EX_NOACTIVATE;
                return myParams;
            }
        }
    }
}

