namespace DatabaseDataGridView.WinForms.Extensions;

public static class StringExtensions
{
    
    /// <summary>
    /// Generates a random name with timestamp and random letters
    /// </summary>
    /// <param name="startName">The prefix for the name (default: "export_")</param>
    /// <param name="randomLength">The length of random letters to append (default: 10)</param>
    /// <returns>A unique random name</returns>
    public static string RandomName(string startName = "export_", int randomLength = 10)
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        if (string.IsNullOrEmpty(startName))
        {
            startName = "ABCDE_";
        }

        return startName + DateTime.Now.ToString("yyMMdd_HHmm") + new string(Enumerable.Repeat(letters, randomLength).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}