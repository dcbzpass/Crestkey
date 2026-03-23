using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Crestkey.Core
{
    public class IdleLock
    {
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        private readonly Timer _timer;
        private readonly int _timeoutMs;
        private readonly Action _onLock;

        public bool Enabled
        {
            get => _timer.Enabled;
            set => _timer.Enabled = value;
        }

        public IdleLock(int timeoutSeconds, Action onLock)
        {
            _timeoutMs = timeoutSeconds * 1000;
            _onLock = onLock;
            _timer = new Timer { Interval = 15000 };
            _timer.Tick += Check;
        }

        private void Check(object sender, EventArgs e)
        {
            var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            if (!GetLastInputInfo(ref info)) return;

            int idle = (int)(Environment.TickCount - info.dwTime);
            if (idle >= _timeoutMs)
            {
                _timer.Stop();
                _onLock();
            }
        }

        public void Reset() => _timer.Start();
        public void Stop() => _timer.Stop();
    }
}