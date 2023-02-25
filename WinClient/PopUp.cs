using System.Media;

namespace WinClient
{
    public partial class PopUp : Form
    {
        public PopUp()
        {
            InitializeComponent();
            this.Icon = SystemIcons.Error;
        }

        // Disable Close Button
        private const int WsSysMenu = 0x80000;
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style &= ~WsSysMenu;
                return cp;
            }
        }

        // Disable Movement
        protected override void WndProc(ref Message message)
        {
            const int wmSysCommand = 0x0112;
            const int scMove = 0xF010;

            switch (message.Msg)
            {
                case wmSysCommand:
                    var command = message.WParam.ToInt32() & 0xfff0;
                    if (command == scMove)
                        return;
                    break;
            }

            base.WndProc(ref message);
        }

        // Play Error Sound
        private void PopUp_Shown(object sender, EventArgs e)
        {
            SystemSounds.Hand.Play();
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}