using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Daemon.Native;

namespace GestureSign.Daemon
{
    public class PointerInputTargetWindow : Form
    {
        bool _isRegistered;
        private int _blockTouchInputThreshold;
        private Dictionary<int, int> pointerIdList = new Dictionary<int, int>(10);
        private Queue<int> idPool = new Queue<int>(10);

        public PointerInputTargetWindow()
        {
            ResetIdPool();
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

        public void InterceptTouchInput(object sender, int blockTouchInputThreshold)
        {
            _blockTouchInputThreshold = blockTouchInputThreshold;
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
            idPool.Clear();
            for (int i = 0; i < 10; i++)
            {
                idPool.Enqueue(i);
            }
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                //case WM_POINTERENTER:
                //case WM_POINTERLEAVE:
                //case WM_POINTERCAPTURECHANGED:
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

                if (pCount < _blockTouchInputThreshold)
                {
                    List<POINTER_TOUCH_INFO> ptis = new List<POINTER_TOUCH_INFO>(pCount);
                    int upFlagCount = 0;

                    for (int i = 0; i < pCount; i++)
                    {
                        var currentPointerInfo = pointerInfos[i];
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

                            if (pointerIdList.ContainsKey(currentPointerInfo.PointerID))
                            {
                                pti.PointerInfo.PointerID = pointerIdList[currentPointerInfo.PointerID];
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.UP))
                        {
                            pti.PointerInfo.PointerFlags = POINTER_FLAGS.UP;

                            upFlagCount++;

                            if (pointerIdList.ContainsKey(currentPointerInfo.PointerID))
                            {
                                int id = pointerIdList[currentPointerInfo.PointerID];
                                pti.PointerInfo.PointerID = id;
                                idPool.Enqueue(id);
                                pointerIdList.Remove(currentPointerInfo.PointerID);
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if (currentPointerInfo.PointerFlags.HasFlag(POINTER_FLAGS.DOWN))
                        {
                            pti.PointerInfo.PointerFlags = POINTER_FLAGS.DOWN | POINTER_FLAGS.INRANGE | POINTER_FLAGS.INCONTACT;

                            if (pointerIdList.ContainsKey(currentPointerInfo.PointerID)) return;

                            if (idPool.Count > 0)
                            {
                                pti.PointerInfo.PointerID = idPool.Dequeue();
                                pointerIdList.Add(currentPointerInfo.PointerID, pti.PointerInfo.PointerID);
                            }
                        }
                        else return;

                        ptis.Add(pti);
                    }

                    if (upFlagCount == pCount)
                    {
                        pointerIdList.Clear();
                        ResetIdPool();
                    }

                    if (ptis.Count > 0)
                    {
                        NativeMethods.InjectTouchInput(ptis.Count, ptis.ToArray());
                    }
                }
            }
            catch (Win32Exception) { }
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

