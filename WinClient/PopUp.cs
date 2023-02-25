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