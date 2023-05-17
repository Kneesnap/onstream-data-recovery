using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ModToolFramework.Utils
{
    /// <summary>
    /// GameVersionConfig is a data structure which allows developers to represent a version history of the game they're working on.
    /// For instance, if a game has v1.0, v1.1, etc. Sometimes this can get complicated with the slight variations of each version.
    /// So, this solution has been created.
    /// Each version is represented by a config entry.
    /// Each version will specify its version, along with any information that pertains to that version.
    /// By extending the GameVersion class, you will be able to read this data when a version config is read.
    /// </summary>
    public class GameVersionConfig<TVersion> where TVersion: GameVersion<TVersion>, new()
    {
        private readonly Dictionary<string, TVersion> _versionsByName = new Dictionary<string, TVersion>();
        private readonly Dictionary<string, TVersion> _versionsById = new Dictionary<string, TVersion>();
        private readonly List<TVersion> _rootVersions = new List<TVersion>();

        /// <summary>
        /// Get the versions with no parents.
        /// </summary>
        public ImmutableList<TVersion> RootVersions => this._rootVersions.ToImmutableList();

        /// <summary>
        /// Gets a version by its name.
        /// </summary>
        /// <param name="versionName">The version name.</param>
        /// <param name="errorIfNotFound">If an error should be thrown if the result is not found.</param>
        /// <returns>version</returns>
        public TVersion GetVersionByName(string versionName, bool errorIfNotFound = true) {
            if (errorIfNotFound && !this._versionsByName.ContainsKey(versionName))
                throw new Exception($"'{versionName}' is not a known version name.");
            this._versionsByName.TryGetValue(versionName, out TVersion version);
            return version;
        }
        
        /// <summary>
        /// Gets a version by its id.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        /// <param name="errorIfNotFound">If an error should be thrown if the result is not found.</param>
        /// <returns>version</returns>
        public TVersion GetVersionById(string versionId, bool errorIfNotFound = true) {
            if (errorIfNotFound && !this._versionsById.ContainsKey(versionId))
                throw new Exception($"'{versionId}' is not a known version id.");
            this._versionsById.TryGetValue(versionId, out TVersion version);
            return version;
        }

        /// <summary>
        /// Loads a GameVersionConfig from a Config.
        /// </summary>
        /// <param name="config">The config to load data from.</param>
        /// <returns>loadedVersionConfig</returns>
        public static GameVersionConfig<TVersion> LoadFromConfig<TVer>(Config config) where TVer: GameVersion<TVer> {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            GameVersionConfig<TVersion> loaded = new GameVersionConfig<TVersion>();
            
            // First, read the versions which exist, so it's possible to reference entries that haven't been read yet.
            foreach (Config childConfig in config.ChildConfigs)
            {
                if (loaded._versionsByName.ContainsKey(childConfig.SectionName))
                    throw new ArgumentException(childConfig.SectionName + " is defined twice in the version config!");
                loaded._versionsByName[childConfig.SectionName] = new TVersion();
            }
            
            // Read the version data.
            foreach (Config childConfig in config.ChildConfigs) {
                TVersion version = loaded._versionsByName[childConfig.SectionName];

                string parentName = childConfig.SuperConfig?.SectionName;
                if (parentName != null) {
                    TVersion previousVersion = loaded._versionsByName[parentName];
                    if (previousVersion == null)
                        throw new ArgumentException($"Parent version id '{parentName}' was not found!");
                    version.PreviousVersion = previousVersion;
                } else {
                    loaded._rootVersions.Add(version);
                }
                
                version.LoadFromConfig(childConfig);
                if (loaded._versionsById.ContainsKey(version.Id))
                    throw new ArgumentException($"Version ID {version.Id} is defined twice in the version config!");
                loaded._versionsById[version.Id] = version;
            }

            return loaded;
        }
    }

    /// <summary>
    /// Represents a single version in a game's history.
    /// Designed to be overwritten by another class.
    /// </summary>
    public class GameVersion<T> where T: GameVersion<T>
    {
        public T PreviousVersion; // The previous version was of the same type. Can be null.
        public string DisplayName { get; private set; }
        public string Id { get; private set; }
        
        /// <summary>
        /// Loads version data from the configuration.
        /// </summary>
        /// <param name="config"></param>
        public virtual void LoadFromConfig(Config config) {
            this.DisplayName = config.SectionName;
            this.Id = config.GetValue("version")?.GetAsString() ?? "No Version ID Specified";
        }
        
        /// <summary>
        /// Check if this version is preceded by/at least another version.
        /// If the version does not parent this one, false will be returned.
        /// </summary>
        /// <param name="other">The version to test against.</param>
        /// <returns>isAtLeast</returns>
        /// <exception cref="ArgumentNullException">If other is null.</exception>
        public virtual bool IsAtLeast(T other) {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            
            return (this == other) || (this.PreviousVersion != null && this.PreviousVersion.IsAtLeast(other));
        }
    }
}