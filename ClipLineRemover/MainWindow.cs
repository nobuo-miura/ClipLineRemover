using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipLineRemover
{
    public partial class MainWindow : Form
    {
        private bool _running = true;
        private IntPtr nextClipboardViewer;

        public MainWindow()
        {
            InitializeComponent();
            nextClipboardViewer = SetClipboardViewer(Handle);
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            if (_running)
            {
                _running = false;
                LabelStatus.Text = "Stopped";
                RunButton.Text = "Run";
            }
            else
            {
                LabelStatus.Text = "Running";
                RunButton.Text = "Stop";
                _running = true;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        protected override void WndProc(ref Message m)
        {
            const int WM_DRAWCLIPBOARD = 0x0308;
            const int WM_CHANGECBCHAIN = 0x030D;

            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    ProcessClipboard();
                    SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipboardViewer)
                    {
                        nextClipboardViewer = m.LParam;
                    }
                    else
                    {
                        SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    }

                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void ProcessClipboard()
        {
            try
            {
                if (!_running)
                {
                    return;
                }

                if (!Clipboard.ContainsText())
                {
                    return;
                }

                var text = Clipboard.GetText();
                if (!text.Contains("\n"))
                {
                    return;
                }

                text = text.Replace("\n", "").Replace("\r", "");
                if (!string.IsNullOrEmpty(text))
                {
                    Clipboard.SetText(text);
                }
            }
            catch
            {
                // ignored
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ChangeClipboardChain(Handle, nextClipboardViewer);
            base.OnFormClosed(e);
        }
    }
}
