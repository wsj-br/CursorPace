using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public interface IUsageSampleStore
{
    UsageSampleDocument Load();
    void Save(UsageSampleDocument document);
}
