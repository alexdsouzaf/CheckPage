namespace CheckPage.Services;

public class ContentCheckerService
{
    public bool IsTextPresent(string html, string searchText)
    {
        return html.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}
