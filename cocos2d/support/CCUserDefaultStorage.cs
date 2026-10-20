using System.IO;
#if !(WINDOWS || MACOS || LINUX)
using System.IO.IsolatedStorage;
#endif

namespace Cocos2D;

/// <summary>
/// Storage backend used by <see cref="CCUserDefault"/> to persist its XML settings file.
/// Assign <see cref="CCUserDefault.Storage"/> BEFORE the first
/// <see cref="CCUserDefault.SharedUserDefault"/> access to substitute a platform-specific
/// store (for example, console save-data on platforms without a writable file system).
/// </summary>
public interface ICCUserDefaultStorage
{
    /// <summary>Whether the settings file already exists in the store.</summary>
    bool Exists();

    /// <summary>Opens the settings file for reading.</summary>
    Stream OpenRead();

    /// <summary>Creates (or truncates) the settings file and opens it for writing.</summary>
    Stream OpenWrite();
}

#if WINDOWS || MACOS || LINUX
/// <summary>
/// Desktop backend: a plain file next to the executable (the historical behavior of the
/// WINDOWS/MACOS/LINUX builds).
/// </summary>
internal sealed class CCFileUserDefaultStorage : ICCUserDefaultStorage
{
    private readonly string path;

    public CCFileUserDefaultStorage(string path)
    {
        this.path = path;
    }

    public bool Exists()
    {
        return new FileInfo(path).Exists;
    }

    public Stream OpenRead()
    {
        return new FileInfo(path).OpenRead();
    }

    public Stream OpenWrite()
    {
        // FileShare.Read matches the StreamWriter(path) the pre-seam code used
        // (its FileStream allows concurrent readers during a write).
        return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
    }
}
#else
/// <summary>
/// Isolated-storage backend (the historical behavior of the mobile builds).
/// </summary>
internal sealed class CCIsolatedStorageUserDefaultStorage : ICCUserDefaultStorage
{
    private readonly string fileName;
    private readonly IsolatedStorageFile store;

    public CCIsolatedStorageUserDefaultStorage(string fileName)
    {
        this.fileName = fileName;
        store = IsolatedStorageFile.GetUserStoreForApplication();
    }

    public bool Exists()
    {
        return store.FileExists(fileName);
    }

    public Stream OpenRead()
    {
        return store.OpenFile(fileName, FileMode.Open, FileAccess.Read);
    }

    public Stream OpenWrite()
    {
        return new IsolatedStorageFileStream(fileName, FileMode.Create, FileAccess.Write, store);
    }
}
#endif
