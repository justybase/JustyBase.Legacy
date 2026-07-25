namespace AppBase.Common;

public interface IImportProgressForm
{
    int AddRow(string text, int style = -1);
    void SetProgressBarValue(int value, int style = -1);
    void SetColor(int rowNum, Color color);
    void SetFirstDisplayedScrollingRowIndex(int rowNum);
    void CompleteForNetezza(string randName, string configDirecotry, string[] headers, bool importToExisting, string? qualifiedTableName = null);
    void CompleteForGeneral(string randName, bool top = false);
    void CompleteForGeneral(List<string> randNames, bool top = false);

    void Close();
}
