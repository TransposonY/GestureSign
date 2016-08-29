using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Daemon.Native;

namespace GestureSign.Daemon.Filtration
{
    public class PointerInputTargetWindow : Form
    {
        private bool _isRegistered;
        private int _blockTouchInputThreshold;
        private Dictionary<int, int> _pointerIdList = new Dictionary<int, int>(10);
        private Queue<int> _idPool = new Queue<int>(10);
        private Dictionary<int, POINTER_TOUCH_INFO> _pointerCache = new Dictionary<int, POINTER_TOUCH_INFO>();
        private bool _pointMoved;
        private int _screenWidth;
        private int _screenHeight;

        public PointerInputTargetWindow()
        {
            ResetIdPool();
            NativeMethods.InitializeTouchInjection(10, TOUCH_FEEDBACK.NONE);
        }

        public int BlockTouchInputThreshold
        {
            get { return _blockTouchInputThreshold; }
            set
            {
                _blockTouchInputThreshold = value;

                bool flag = _blockTouchInputThreshold >= 2;

                if (!IsHandleCreated)
                {
                    if (!IsDisposed)
                        CreateHandle();
                }

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
                    if (_isRegistered) return;
                    if (NativeMethods.RegisterPointerInputTarget(Handle, POINTER_INPUT_TYPE.TOUCH))
                    {
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
            List<POINTER_TOUCH_INFO> ptis = GenerateInput(pointerInfos);

            if (pointerInfos.Length != ptis.Count) return;

            if (!_pointMoved)
            {
                GetDisplayResolution();
                foreach (var pointerTouchInfo in ptis)
                {
                    var currentPointerInfo = pointerTouchInfo.PointerInfo;
                    if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.UPDATE))
                    {
                        _pointMoved |= IsPointMoved(_pointerCache[currentPointerInfo.PointerID].PointerInfo.PtPixelLocation, currentPointerInfo.PtPixelLocation);
                    }
                    else if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.DOWN))
                    {
                        if (!_pointerCache.ContainsKey(currentPointerInfo.PointerID))
                            _pointerCache.Add(currentPointerInfo.PointerID, pointerTouchInfo);
                    }
                    else
                    {
                        if (_pointerCache.ContainsKey(currentPointerInfo.PointerID))
                            _pointerCache.Remove(currentPointerInfo.PointerID);
                        if (ptis.Count == 1)
                            SimulateClick(ptis.ToArray());
                    }
                }
            }

            if (_pointMoved && pointerInfos.Length < _blockTouchInputThreshold)
            {
                if (_pointerCache.Count != 0)
                {
                    NativeMethods.InjectTouchInput(_pointerCache.Count, _pointerCache.Values.ToArray());
                    _pointerCache.Clear();
                    Thread.Sleep(1);
                }
                if (ptis.Count != 0)
                    NativeMethods.InjectTouchInput(ptis.Count, ptis.ToArray());
            }

            if (_pointerIdList.Count == 0)
            {
                _pointerCache.Clear();
                _pointMoved = false;
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
            }
            return ptis;
        }

        private void SimulateClick(POINTER_TOUCH_INFO[] pointerTouchInfos)
        {
            pointerTouchInfos[0].PointerInfo.PointerFlags = POINTER_FLAGS.DOWN | POINTER_FLAGS.INRANGE | POINTER_FLAGS.INCONTACT;
            NativeMethods.InjectTouchInput(1, pointerTouchInfos);
            Thread.Sleep(5);
            pointerTouchInfos[0].PointerInfo.PointerFlags = POINTER_FLAGS.UP;
            NativeMethods.InjectTouchInput(1, pointerTouchInfos);
        }

        private bool IsPointMoved(POINT point1, POINT point2)
        {
            return Math.Abs(point1.X - point2.X) > _screenWidth / 150 || Math.Abs(point1.Y - point2.Y) > _screenHeight / 150;
        }

        private void GetDisplayResolution()
        {
            _screenWidth = Screen.PrimaryScreen.Bounds.Width;
            _screenHeight = Screen.PrimaryScreen.Bounds.Height;
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

