namespace FastColoredTextBoxNS.Helpers;

public interface IAutocompleteClass
{
    System.Threading.Tasks.Task AddAutocompleteForGeneral(int selectionStart, string _cleanSqlText);
    System.Threading.Tasks.Task AddAutocompleteForNZ(int selectionStart, string _cleanSqlText);
    int LastSelect(ref string innerString, bool doTrim = true);
    int FirstFrom(string afterSelect);
    int FirstWhereGroupLimit(string txt);
}
