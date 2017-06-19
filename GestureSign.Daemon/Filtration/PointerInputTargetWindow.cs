using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common.Configuration;
using GestureSign.Daemon.Native;

namespace GestureSign.Daemon.Filtration
{
    public class PointerInputTargetWindow : Form
    {
        private bool _isRegistered;
        private int _blockTouchInputThreshold;
        private Dictionary<int, int> _pointerIdList = new Dictionary<int, int>(10);
        private Queue<int> _idPool = new Queue<int>(10);
        private bool _isInitialized = false;

        public PointerInputTargetWindow()
        {
            CreateHandle();
            ResetIdPool();
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
            List<POINTER_TOUCH_INFO> ptis = GenerateInput(pointerInfos);

            if (pointerInfos.Length != ptis.Count) return;

            if (pointerInfos.Length < _blockTouchInputThreshold)
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

