using CursorPace.Models;

namespace CursorPace.Services;

public interface IPlanStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}
