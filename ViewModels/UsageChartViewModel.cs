using CursorUsageProgress.Models;

namespace CursorUsageProgress.ViewModels;

public sealed class UsageChartViewModel : ViewModelBase
{
    private UsageChartDocument? _document;

    public UsageChartDocument? Document
    {
        get => _document;
        private set => SetProperty(ref _document, value);
    }

    public void Replace(UsageChartDocument? document) => Document = document;
}
