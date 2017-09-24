using GestureSign.Common.Configuration;
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
        private readonly float _doubleTapRadius;
        private readonly int _doubleTapTime;
        private readonly List<string> _doubleTapList = new List<string> { "MMCMainFrame", "CabinetWClass", "ExploreWClass", "Shell_TrayWnd", "Progman", "WorkerW" };
        private bool _isRegistered;
        private int _blockTouchInputThreshold;
        private Dictionary<int, int> _pointerIdList = new Dictionary<int, int>(10);
        private Queue<int> _idPool = new Queue<int>(10);
        private POINT? _doubleTapPoint;
        private int _lastTapTime;
        private bool _blockTap;
        private bool _isInitialized = false;
        private bool _tempDisable;
        private int _lastFrameID;

        public PointerInputTargetWindow()
        {
            CreateHandle();
            ResetIdPool();
            _doubleTapRadius = 3 * SystemInformation.DoubleClickSize.Width * NativeMethods.GetScreenDpi() / 96f;
            _doubleTapTime = SystemInformation.DoubleClickTime;
        }

        public int BlockTouchInputThreshold
        {
            get { return _blockTouchInputThreshold; }
            set
            {
                if (IsDisposed || !IsHandleCreated) return;

                _blockTouchInputThreshold = value;

                bool flag = _blockTouchInputThreshold >= 2;

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

            List<POINTER_TOUCH_INFO> ptis = GenerateInput(pointerInfos);

            if (pointerInfos.Length != ptis.Count) return;

            if (SimulateDoubleTap(ptis) || _blockTap) return;

            if (pointerInfos.Length < _blockTouchInputThreshold ||
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
                POINTER_TOUCH_INFO pti = new POINTER_TOUCH_INFO
                {
                    TouchFlags = TOUCH_FLAGS.NONE,
                    PointerInfo = new POINTER_INFO
                    {
                        pointerType = POINTER_INPUT_TYPE.TOUCH,
                        PtPixelLocation = currentPointerInfo.PtPixelLocation,
                    }
                };

                if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.UPDATE))
                {
                    pti.PointerInfo.PointerFlags = POINTER_FLAGS.INCONTACT | POINTER_FLAGS.INRANGE | POINTER_FLAGS.UPDATE;

                    if (_pointerIdList.ContainsKey(currentPointerInfo.PointerID))
                    {
                        pti.PointerInfo.PointerID = _pointerIdList[currentPointerInfo.PointerID];
                    }
                    else continue;
                }
                else if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.UP))
                {
                    pti.PointerInfo.PointerFlags = POINTER_FLAGS.UP;

                    upFlagCount++;

                    if (_pointerIdList.ContainsKey(currentPointerInfo.PointerID))
                    {
                        int id = _pointerIdList[currentPointerInfo.PointerID];
                        pti.PointerInfo.PointerID = id;
                        _idPool.Enqueue(id);
                        _pointerIdList.Remove(currentPointerInfo.PointerID);
                    }
                    else continue;
                }
                else if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.DOWN))
                {
                    pti.PointerInfo.PointerFlags = POINTER_FLAGS.DOWN | POINTER_FLAGS.INRANGE | POINTER_FLAGS.INCONTACT;

                    if (_pointerIdList.ContainsKey(currentPointerInfo.PointerID)) continue;

                    if (_idPool.Count > 0)
                    {
                        pti.PointerInfo.PointerID = _idPool.Dequeue();
                        _pointerIdList.Add(currentPointerInfo.PointerID, pti.PointerInfo.PointerID);
                    }
                }
                else continue;

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

        private bool SimulateDoubleTap(List<POINTER_TOUCH_INFO> ptis)
        {
            if (ptis.Count == 1)
            {
                if ((ptis[0].PointerInfo.PointerFlags & POINTER_FLAGS.DOWN) == POINTER_FLAGS.DOWN)
                {
                    string className;
                    try
                    {
                        className = ManagedWinapi.Windows.SystemWindow.FromPointEx(ptis[0].PointerInfo.PtPixelLocation.X, ptis[0].PointerInfo.PtPixelLocation.Y, true, true).ClassName;
                    }
                    catch
                    {
                        return false;
                    }
                    if (_doubleTapPoint != null && _doubleTapList.Contains(className))
                    {
                        var distance = Math.Sqrt(Math.Pow(ptis[0].PointerInfo.PtPixelLocation.X - _doubleTapPoint.Value.X, 2) +
                            Math.Pow(ptis[0].PointerInfo.PtPixelLocation.Y - _doubleTapPoint.Value.Y, 2));
                        var deltaTime = Environment.TickCount - _lastTapTime;
                        if (distance < _doubleTapRadius && deltaTime < _doubleTapTime)
                        {
                            _blockTap = true;
                            Task.Delay(100).ContinueWith((t) =>
                            {
                                var infoArray = ptis.ToArray();
                                infoArray[0].PointerInfo.PointerFlags = POINTER_FLAGS.DOWN | POINTER_FLAGS.INRANGE | POINTER_FLAGS.INCONTACT;
                                NativeMethods.InjectTouchInput(1, infoArray);
                                Thread.Sleep(2);
                                infoArray[0].PointerInfo.PointerFlags = POINTER_FLAGS.UP;
                                NativeMethods.InjectTouchInput(1, infoArray);
                                Thread.Sleep(2);
                                infoArray[0].PointerInfo.PointerFlags = POINTER_FLAGS.DOWN | POINTER_FLAGS.INRANGE | POINTER_FLAGS.INCONTACT;
                                NativeMethods.InjectTouchInput(1, infoArray);
                                Thread.Sleep(2);
                                infoArray[0].PointerInfo.PointerFlags = POINTER_FLAGS.UP;
                                NativeMethods.InjectTouchInput(1, infoArray);
                            });

                            _doubleTapPoint = null;
                            return true;
                        }
                    }
                    _doubleTapPoint = ptis[0].PointerInfo.PtPixelLocation;
                    _lastTapTime = Environment.TickCount;
                }
                else if (_blockTap && (ptis[0].PointerInfo.PointerFlags & POINTER_FLAGS.UP) == POINTER_FLAGS.UP)
                {
                    _blockTap = false;
                    return true;
                }
            }
            else _doubleTapPoint = null;
            return false;
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

