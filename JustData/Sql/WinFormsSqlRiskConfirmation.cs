using AppBase.Common;
using JustData.Application.Sql;
using JustyBase.Core.Risk;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Sql;

internal sealed class WinFormsSqlRiskConfirmation(IWin32Window owner, ILogger logger) : ISqlRiskConfirmation
{
    private readonly IWin32Window _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public bool Confirm(SqlRisk risk)
    {
        string caption = risk.Kind switch
        {
            SqlRiskKind.UnsafeUpdateDelete => "Update/delete warning",
            SqlRiskKind.MissingDistribute => "Create table warning",
            SqlRiskKind.SelectInto => "SELECT INTO warning",
            _ => "SQL risk warning"
        };

        DialogResult result = _logger.MessageBox_Show(
            _owner,
            risk.Message,
            caption,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);
        return result == DialogResult.OK;
    }
}
