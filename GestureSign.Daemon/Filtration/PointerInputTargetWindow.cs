using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Daemon.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GestureSign.Daemon.Filtration
{
    public class PointerInputTargetWindow : Form
    {
        private bool _isRegistered;
        private int _blockTouchInputThreshold;
        private Dictionary<int, int> _pointerIdList = new Dictionary<int, int>(10);
        private Queue<int> _idPool = new Queue<int>(10);
        private bool _isInitialized = false;
        private bool _tempDisable;
        private int _lastFrameID;

        /// <summary>
        /// Raised with the touch frames received through the pointer input target.
        /// Injected touch (e.g. remote-desktop or streaming hosts that use
        /// InjectSyntheticPointerInput) produces no HID raw input, so MessageWindow
        /// never sees it; when AppConfig.InterceptInjectedTouch is enabled these
        /// frames are forwarded to the recognition pipeline instead.
        /// </summary>
        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public PointerInputTargetWindow()
        {
            CreateHandle();
            ResetIdPool();
            if (AppConfig.InterceptInjectedTouch)
                IsRegistered = true;
        }

        public int BlockTouchInputThreshold
        {
            get { return _blockTouchInputThreshold; }
            set
            {
                if (IsDisposed || !IsHandleCreated) return;

                _blockTouchInputThreshold = value;

                bool flag = _blockTouchInputThreshold >= 2 || AppConfig.InterceptInjectedTouch;

                if (InvokeRequired)
                    Invoke(new Action(() => IsRegistered = flag));
                else
                    IsRegistered = flag;
            }
        }

        public bool IsRegistered
        {
            get { return _isRegistered; }
            private set
            {
                if (value)
                {
                    if (_isRegistered || !AppConfig.UiAccess) return;

                    if (!_isInitialized)
                    {
                        NativeMethods.InitializeTouchInjection(10, TOUCH_FEEDBACK.NONE);
                        _isInitialized = true;
                    }

                    if (NativeMethods.RegisterPointerInputTarget(Handle, POINTER_INPUT_TYPE.TOUCH))
                    {
                        _isRegistered = true;
                    }
                }
                else
                {
                    if (_isRegistered && NativeMethods.UnregisterPointerInputTarget(Handle, POINTER_INPUT_TYPE.TOUCH))
                    {
                        _isRegistered = false;
                    }
                }
            }
        }

        public void TemporarilyDisable()
        {
            _tempDisable = true;
        }

        protected sealed override void CreateHandle()
        {
            base.CreateHandle();
            ChangeToMessageOnlyWindow();
        }

        private void ChangeToMessageOnlyWindow()
        {
            IntPtr HWND_MESSAGE = new IntPtr(-3);
            NativeMethods.SetParent(this.Handle, HWND_MESSAGE);
        }

        private void ResetIdPool()
        {
            _idPool.Clear();
            for (int i = 0; i < 10; i++)
            {
                _idPool.Enqueue(i);
            }
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                //case NativeMethods.WM_POINTERENTER:
                //case NativeMethods.WM_POINTERLEAVE:
                //case NativeMethods.WM_POINTERCAPTURECHANGED:
                case NativeMethods.WM_POINTERDOWN:
                case NativeMethods.WM_POINTERUP:
                case NativeMethods.WM_POINTERUPDATE:
                    ProcessPointerMessage(message);
                    return;
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

        private void ProcessPointerMessage(Message message)
        {
            POINTER_INFO[] pointerInfos = GetPointerInfos(message);

            if (pointerInfos.Length == 0 || pointerInfos[0].FrameID == _lastFrameID) return;
            _lastFrameID = pointerInfos[0].FrameID;

            // Feed intercepted frames to the recognition pipeline. Only needed for
            // injected touch, which generates no usable HID raw input; physical
            // digitizers are handled by MessageWindow, so this stays off unless the
            // user opted in (MessageWindow drops the injected devices to avoid
            // double capture, see ValidateDevice).
            if (AppConfig.InterceptInjectedTouch && PointsIntercepted != null)
            {
                var rawData = new List<RawData>(pointerInfos.Length);
                foreach (var pi in pointerInfos)
                {
                    var state = pi.PointerFlags.HasFlag(POINTER_FLAGS.UP) ? DeviceStates.None : DeviceStates.Tip;
                    rawData.Add(new RawData(state, pi.PointerID, new System.Drawing.Point(pi.PtPixelLocation.X, pi.PtPixelLocation.Y)));
                }
                PointsIntercepted(this, new RawPointsDataMessageEventArgs(rawData, Devices.TouchScreen));
            }

            List<POINTER_TOUCH_INFO> ptis = GenerateInput(pointerInfos);

            if (pointerInfos.Length != ptis.Count) return;

            if (pointerInfos.Length < _blockTouchInputThreshold || _blockTouchInputThreshold < 2 ||
                Input.PointCapture.Instance.State == Common.Input.CaptureState.CapturingInvalid ||
                _tempDisable)
            {
                if (ptis.Count != 0)
                    NativeMethods.InjectTouchInput(ptis.Count, ptis.ToArray());
            }
        }

        private POINTER_INFO[] GetPointerInfos(Message message)
        {
            int pointerId = (int)(message.WParam.ToInt64() & 0xffff);
            int pCount = 0;
            if (!NativeMethods.GetPointerFrameInfo(pointerId, ref pCount, null))
            {
                CheckLastError();
            }
            POINTER_INFO[] pointerInfos = new POINTER_INFO[pCount];
            if (!NativeMethods.GetPointerFrameInfo(pointerId, ref pCount, pointerInfos))
            {
                CheckLastError();
            }
            return pointerInfos;
        }

        private List<POINTER_TOUCH_INFO> GenerateInput(POINTER_INFO[] pointerInfos)
        {
            List<POINTER_TOUCH_INFO> ptis = new List<POINTER_TOUCH_INFO>(pointerInfos.Length);
            int upFlagCount = 0;

            foreach (var currentPointerInfo in pointerInfos)
            {
                POINTER_INFO pointerInfo = new POINTER_INFO()
                {
                    pointerType = POINTER_INPUT_TYPE.TOUCH,
                    PtPixelLocation = currentPointerInfo.PtPixelLocation,
                };

                if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.UPDATE))
                {
                    pointerInfo.PointerFlags = POINTER_FLAGS.INCONTACT | POINTER_FLAGS.INRANGE | POINTER_FLAGS.UPDATE;

                    if (_pointerIdList.TryGetValue(currentPointerInfo.PointerID, out int id))
                    {
                        pointerInfo.PointerID = id;
                    }
                    else continue;
                }
                else if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.UP))
                {
                    pointerInfo.PointerFlags = POINTER_FLAGS.UP;

                    upFlagCount++;

                    if (_pointerIdList.TryGetValue(currentPointerInfo.PointerID, out int id))
                    {
                        pointerInfo.PointerID = id;
                        _idPool.Enqueue(id);
                        _pointerIdList.Remove(currentPointerInfo.PointerID);
                        if (_pointerIdList.Count == 0)
                        {
                            pointerInfo.PointerFlags |= POINTER_FLAGS.INRANGE;
                        }
                    }
                    else continue;
                }
                else if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.DOWN))
                {
                    pointerInfo.PointerFlags = POINTER_FLAGS.DOWN | POINTER_FLAGS.INRANGE | POINTER_FLAGS.INCONTACT;

                    if (_pointerIdList.ContainsKey(currentPointerInfo.PointerID)) continue;

                    if (_idPool.Count > 0)
                    {
                        pointerInfo.PointerID = _idPool.Dequeue();
                        _pointerIdList.Add(currentPointerInfo.PointerID, pointerInfo.PointerID);
                    }
                }
                else continue;

                POINTER_TOUCH_INFO pti = new POINTER_TOUCH_INFO()
                {
                    TouchFlags = TOUCH_FLAGS.NONE,
                    PointerInfo = pointerInfo,
                };
                ptis.Add(pti);
            }

            if (upFlagCount == pointerInfos.Length)
            {
                _pointerIdList.Clear();
                ResetIdPool();
                if (_tempDisable)
                {
                    _tempDisable = false;
                }
            }
            return ptis;
        }

        #endregion ProcessInput

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                CreateParams myParams = base.CreateParams;
                myParams.ExStyle = myParams.ExStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return myParams;
            }
        }
    }
}

