using AppBase.Common;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

public sealed class TabPagePicture : TabPage, ISuccesfullTab
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image CloseImage { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image PinImage { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DatabaseTypeName { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsRunning { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsSuccess { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool FinishedInBackground { get; set; }
}
