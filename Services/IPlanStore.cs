using CursorQuotaProgress.Models;

namespace CursorQuotaProgress.Services;

public interface IPlanStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}
