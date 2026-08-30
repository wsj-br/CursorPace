using CursorPace.Models;

namespace CursorPace.Services;

public interface IUsageSampleStore
{
    UsageSampleDocument Load();
    void Save(UsageSampleDocument document);
}
