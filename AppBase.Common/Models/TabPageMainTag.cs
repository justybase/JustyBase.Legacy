namespace AppBase.Common;

public sealed class TabPageMainTag
{
    public string? Filename { get; set; }
    public bool IsSaved { get; set; }
    public bool NotFirstTime { get; set; }
    public TabPageMainTag()
    {
        IsSaved = true;
        NotFirstTime = false;
    }
}

