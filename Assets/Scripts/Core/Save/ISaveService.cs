using System;
using FantasyDefense.Metagame;

namespace FantasyDefense.Save
{
    /// <summary>
    /// Kontrak interface untuk layanan penyimpanan lokal (Local Storage Service).
    /// Berjalan secara mandiri di penyimpanan persisten perangkat (Android/iOS/PC).
    /// </summary>
    public interface ISaveService
    {
        void Save(CitadelAccountData data, Action<bool> onComplete = null);
        void Load(Action<CitadelAccountData> onLoaded);
        bool HasSaveData();
        void DeleteSave();
    }

    /// <summary>
    /// Kontrak interface untuk penyedia sinkronisasi online / cloud save (Cloud Storage Provider).
    /// Siap diintegrasikan ke Google Play Games (Android), Game Center/iCloud (iOS), atau Firebase/UGS.
    /// </summary>
    public interface ICloudSaveProvider
    {
        bool IsCloudAvailable { get; }
        string CloudUserId { get; }

        void UploadCloudSave(CitadelAccountData data, Action<bool, string> onComplete);
        void DownloadCloudSave(Action<bool, CitadelAccountData, string> onComplete);
        CitadelAccountData ResolveConflict(CitadelAccountData localData, CitadelAccountData cloudData);
    }
}
