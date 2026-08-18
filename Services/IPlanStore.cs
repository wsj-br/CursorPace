using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public interface IPlanStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}
