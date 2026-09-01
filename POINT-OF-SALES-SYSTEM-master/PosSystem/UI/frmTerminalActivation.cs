using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using PosSystem.Services.EIS;
using PosSystem.DTOs.EIS;

namespace PosSystem.UI
{
    public partial class frmTerminalActivation : Form
    {
        private readonly TerminalActivationService _svc;

        public frmTerminalActivation()
        {
            InitializeComponent();
            _svc = new TerminalActivationService();
        }

        private async void btnActivate_Click(object sender, EventArgs e)
        {
            var tac = txtTAC.Text.Trim();
            var req = new ActivationRequest { Tac = tac, TerminalId = "POS-001" };
            try
            {
                var resp = await _svc.ActivateTerminalAsync(req);
                if (resp != null && resp.StatusCode == 0)
                {
                    MessageBox.Show("Activation response received. Save configuration and confirm.", "Activated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Persist configuration then confirm activation
                }
                else
                {
                    MessageBox.Show("Activation failed: " + resp?.Remark, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Activation error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}