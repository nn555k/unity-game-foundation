using System;
using GameFoundation.Core;
using GameFoundation.Save;
using UnityEngine;

namespace GameFoundation.Samples.Save
{
    [Serializable]
    public sealed class ProfileSaveData
    {
        public string DisplayName = string.Empty;
        public bool SoundEnabled = true;
    }

    [Serializable]
    internal sealed class ProfileSaveDataV1
    {
        public string DisplayName = string.Empty;
    }

    public sealed class ProfileSaveV1ToV2Migration : ISaveMigration
    {
        public int FromVersion => 1;
        public int ToVersion => 2;

        /// <summary>
        /// Adds the version-two preference while retaining version-one profile data.
        /// </summary>
        public string Migrate(string payload)
        {
            var previous = JsonUtility.FromJson<ProfileSaveDataV1>(payload) ?? new ProfileSaveDataV1();
            var current = new ProfileSaveData
            {
                DisplayName = previous.DisplayName,
                SoundEnabled = true
            };
            return JsonUtility.ToJson(current);
        }
    }

    public static class ProfileSaveComposition
    {
        /// <summary>
        /// Creates a version-two profile service while leaving the storage choice to the project.
        /// </summary>
        public static VersionedSaveService<ProfileSaveData> Create(
            ISaveStore store,
            IFoundationClock clock,
            IFoundationLogger logger = null)
        {
            return new VersionedSaveService<ProfileSaveData>(
                "profile",
                2,
                store,
                new JsonUtilitySaveSerializer(),
                clock,
                logger,
                new ISaveMigration[] { new ProfileSaveV1ToV2Migration() });
        }
    }
}
