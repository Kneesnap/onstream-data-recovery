using ModToolFramework.Utils.Data;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ModToolFramework.Utils {
    /// <summary>
    /// A configuration file which can be used for storing data.
    /// We created our own configuration utility here with these things in mind:
    /// 1. The data I imagine will be put in this config is arbitrary text, from scripting data to user settings to information about different game engine versions. The syntax of all of the major config formats aren't this flexible / powerful.
    /// 2. If we have our own format, it's easier to save it in our own binary format.
    /// 3. I've never seen any configuration formats which have both inheritance and extension support like this one does.
    /// 4. Nothing stops the user from using another library for configuration, instead of this.
    ///
    /// The ModToolFramework configuration format is heavily inspired by ini in terms of syntax, but has some key differences that set it apart.
    /// It's a tree structure, starting with a root "Config" node. (The term "Config" will be used interchangeably with "Config node" and "Section".)
    /// Each node can contain these things.
    ///
    /// 1. A name.
    /// 2. Key value pairs.
    /// 3. Raw text lines.
    /// 4. Child config nodes.
    ///
    /// Here is an example of a valid configuration.
    /// [SectionName] # This declares the section. It would result in a section literally named "SectionName".
    /// key1=value1 # Example key values. These values are of type string, but can be parsed to be basically anything. It's a breeze if you use static extension methods to allow for your own shared parsing logic.
    /// key2=value2
    /// This is an example line of text which would be considered a 'raw text line'.
    ///
    /// [[ChildSection]] # This is a new section, a child to the previous section. Note how there are two square brackets used here. This is to indicate that it is attached to the previous node, and not a node which is attached to the root.
    /// # If you wanted to attach a child here, you'd create a section with three square brackets prefixing the section name.
    /// ``` # Three backticks, as seen here will result in an empty line of text. If you just include an empty line of text normally, it will be ignored. You can also technically put a '#' on the line (Single-Line comment), as comments can exist on otherwise empty lines.
    ///
    /// By default, any text is interpreted as being in the implicit root node. In other words, there is a default node which values will go into until a section is defined.
    ///
    /// Additionally, it is possible to "extend" a section the same way you can "extend" a class in C#.
    /// When defining a new section, after the section name include a colon ":", followed by the name of the section you wish to extend.
    /// NOTE: If there are multiple sections with that name, an error will be thrown.
    /// Now, the new section inherits all of the values and data from the "super config". (Think super-class/base-class.)
    /// You can then add any additional values, or override values in the parent.
    ///
    /// How does text inheritance work?
    /// This was a hard decision, but any text in your child-config will override the text in the parent config.
    /// There is no way in the config syntax to merge the two. However, they can be merged together using code, as it is possible to access the super config.
    /// Using the 'FullText' property will do this for you.
    ///
    /// Everything after '#' on a line of text will be ignored, as a comment. If you need to put '#' into a line of text or a value, escape it by typing '\#' instead.
    /// </summary>
    public class Config : IBinarySerializable {
        private readonly Dictionary<string, ConfigValueNode> _keyValuePairs = new Dictionary<string, ConfigValueNode>();
        private string _temporarySuperConfigName;

        /// <summary>
        /// The name of this section. Should NOT contain ':', '[', or ']'.
        /// </summary>
        public string SectionName = "Root";

        /// <summary>
        /// The comment (if there is one) which is included on the section definition.
        /// </summary>
        public string SectionComment;

        /// <summary>
        /// This is the config which holds this config as a child config, if there is one.
        /// </summary>
        public Config ParentConfig { get; protected set; }

        /// <summary>
        /// This is the config which this config extends, if this extends a config.
        /// Extending a config is much like a class extending another class. It will inherit the properties of this config, but override them if specified.
        /// This is safe to access and change.
        /// </summary>
        public Config SuperConfig; // This is NOT the config which owns this config, but rather it is the config which this one extends, if it extends one.

        /// <summary>
        /// A list of all child configs, in order, which can safely be modified.
        /// This gives the children attached to THIS config, and ONLY this config.
        /// </summary>
        public readonly List<Config> InternalChildConfigs = new List<Config>(); // This is intentionally not a Dictionary, because we want to support multiple sections with the same name. However, you can create a dictionary with CreateChildDictionary().

        /// <summary>
        /// This contains all child configs accessible in this config, or super configs. Overridden properties are omitted.
        /// </summary>
        public ImmutableList<Config> ChildConfigs {
            get {
                List<Config> allChildren = new List<Config>();
                HashSet<string> seenNames = new HashSet<string>();
                
                Config node = this;
                while (node != null) {
                    allChildren.InsertRange(0, node.InternalChildConfigs.Where(childConfig => !seenNames.Contains(childConfig.SectionName)));
                    node.InternalChildConfigs.ForEach(childConfig => seenNames.Add(childConfig.SectionName));
                    node = node.SuperConfig;
                }

                return allChildren.ToImmutableList();
            }
        }
        
        /// <summary>
        /// All of the key value pairs.
        /// To modify the contents of this, use the functions for doing so. Iterating through this will only give properties for this config, not any parent configs.
        /// </summary>
        public ImmutableDictionary<string, ConfigValueNode> KeyValuePairs { get => this._keyValuePairs.ToImmutableDictionary(); }
        
        /// <summary>
        /// Gets the internal text tracking, which can be safely modified.
        /// This gives the text specific to THIS config, and ONLY this config.
        /// </summary>
        public List<ConfigValueNode> InternalText { get; } = new List<ConfigValueNode>();

        /// <summary>
        /// This contains the section text data. If this is empty and this node extends another node, it will return the text of said node.
        /// </summary>
        public ImmutableList<ConfigValueNode> Text {
            get => this.InternalText.Count == 0 && this.SuperConfig != null ? this.SuperConfig.Text : this.InternalText.ToImmutableList(); }

        /// <summary>
        /// Gets the text for this node, and any of its predecessors.
        /// </summary>
        public ImmutableList<ConfigValueNode> FullText {
            get {
                List<ConfigValueNode> fullText = new List<ConfigValueNode>();

                Config node = this;
                while (node != null) {
                    fullText.InsertRange(0, node.InternalText);
                    node = node.SuperConfig;
                }

                return fullText.ToImmutableList();
            }
        }

        /// <summary>
        /// Is this Config a root config? (A root config is the configuration which parents any child configs. There is no parent to the root config.)
        /// </summary>
        public bool IsRoot {
            get => this.ParentConfig == null;
        }

        private const byte BinaryFormatVersion = 1;

        public Config(Config parentConfig = null) {
            this.ParentConfig = parentConfig;
        }

        /// <inheritdoc cref="IBinarySerializable"/>
        public void LoadFromReader(DataReader reader, DataSettings settings = null) {
            this.LoadFromReader(reader, null, settings);
        }

        private void LoadFromReader(DataReader reader, Config parentConfig, DataSettings settings = null) {
            ConfigSettings loadSettings = settings as ConfigSettings ?? new ConfigSettings();
            if (loadSettings.NeedsHeader) {
                reader.VerifyStringBytes("CFG");
                byte version = reader.ReadByte();
                if (version != BinaryFormatVersion)
                    throw new InvalidDataException("Unsupported binary config version " + version + ".");

                loadSettings.HasComments = reader.ReadBoolean();
                loadSettings.NeedsHeader = false;
            }

            this.SectionName = reader.ReadString();
            this._temporarySuperConfigName = (parentConfig != null) ? reader.ReadString() : null; // If it is the root config, it can't extend anything.
            if (loadSettings.HasComments)
                this.SectionComment = reader.ReadString();

            // Read key value pairs.
            int keyValueCount = reader.ReadInt32();
            for (int i = 0;
                i < keyValueCount;
                i++) {
                string key = reader.ReadString();
                ConfigValueNode node = new ConfigValueNode(string.Empty, string.Empty);
                node.LoadFromReader(reader, loadSettings);
                this._keyValuePairs[key] = node;
            }

            // Read Text.
            int textEntries = reader.ReadInt32();
            for (int i = 0; i < textEntries; i++) {
                ConfigValueNode node = new ConfigValueNode(string.Empty, string.Empty);
                node.LoadFromReader(reader, loadSettings);
                this.InternalText.Add(node);
            }

            // Read child configs.
            int childCount = reader.ReadInt32();
            for (int i = 0; i < childCount; i++) {
                Config childConfig = new Config(this);
                childConfig.LoadFromReader(reader, this, loadSettings);
                this.InternalChildConfigs.Add(childConfig);
            }

            // Handle config extensions recursively. This is the absolute last thing to run, so configs can reference each other regardless of order.
            if (parentConfig == null)
                this.LoadSuperConfigs(GetRoot(this));
        }

        private void LoadSuperConfigs(Config root) {
            foreach (Config config in this.InternalChildConfigs) {
                if (!string.IsNullOrEmpty(config._temporarySuperConfigName)) {
                    Config superConfig = FindConfigByName(root, config._temporarySuperConfigName);
                    if (superConfig == null)
                        throw new KeyNotFoundException($"The config '{config.SectionName}' tried to extend '{config._temporarySuperConfigName}', which could not be found.");
                    config.SuperConfig = superConfig;
                    config._temporarySuperConfigName = null;
                }

                config.LoadSuperConfigs(root);
            }
        }

        /// <inheritdoc cref="IBinarySerializable"/>
        public void SaveToWriter(DataWriter writer, DataSettings settings = null) {
            ConfigSettings saveSettings = settings as ConfigSettings ?? new ConfigSettings();

            bool writeRoot = saveSettings.NeedsHeader;
            if (saveSettings.NeedsHeader) {
                writer.WriteStringBytes("CFG");
                writer.Write(BinaryFormatVersion);
                writer.Write(saveSettings.HasComments);
                saveSettings.NeedsHeader = false;
            }

            writer.Write(this.SectionName);

            // Write the super config identifier.
            if (!writeRoot) { // If this is the root (of all of the configs being written), then we can't have a super config.
                Config otherConfig = null;
                if (this.SuperConfig != null) {
                    try {
                        otherConfig = FindConfigByName(GetRoot(this), this.SuperConfig?.SectionName);
                    } catch (Exception) {
                        if (saveSettings.ErrorIfBadExtension)
                            throw;
                    }

                    if (saveSettings.ErrorIfBadExtension && otherConfig == null)
                        throw new KeyNotFoundException($"The super-class for '{this.SectionName}' ({this.SuperConfig.SectionName}) is not found on the config tree. To ignore the extension, disable ConfigSettings.ErrorIfBadExtension.");
                }

                writer.Write(otherConfig?.SectionName ?? string.Empty);
            }

            if (saveSettings.HasComments)
                writer.Write(this.SectionComment ?? string.Empty);

            // Write key value pairs.
            writer.Write(this._keyValuePairs.Count);
            foreach ((string key, ConfigValueNode node) in this._keyValuePairs) {
                writer.Write(key);
                node.SaveToWriter(writer, saveSettings);
            }

            // Write Text.
            writer.Write(this.InternalText.Count);
            foreach (ConfigValueNode node in this.InternalText)
                node.SaveToWriter(writer, saveSettings);

            // Write child configs.
            writer.Write(this.InternalChildConfigs.Count);
            foreach (Config childConfig in this.InternalChildConfigs)
                childConfig.SaveToWriter(writer, saveSettings);
        }

        /// <summary>
        /// Removes a key value pair with the given key name.
        /// </summary>
        /// <param name="keyName">The name of the key to remove.</param>
        /// <returns>removeSuccess</returns>
        public bool RemoveValue(string keyName) {
            return this._keyValuePairs.Remove(keyName);
        }

        /// <summary>
        /// Creates or updates the value in a key value pair.
        /// Applies the value to this config, not a super config with the value.
        /// </summary>
        /// <param name="keyName">The name of the key.</param>
        /// <param name="node">The value.</param>
        /// <exception cref="InvalidDataException">Thrown if an invalid valid is supplied.</exception>
        public void SetValue(string keyName, ConfigValueNode node) {
            if (node == null) {
                this.RemoveValue(keyName);
                return;
            }

            if (node.IsEscapedNewLine)
                throw new InvalidDataException($"Supplied config value '{node.Value}' had IsEscapedNewLine set to true. This cannot be used in key-value pairs!");
            
            if (this._keyValuePairs.TryGetValue(keyName, out ConfigValueNode existingNode)) { // Get existing node.
                existingNode.Value = node.Value;
                if (!string.IsNullOrWhiteSpace(node.Comment) && string.IsNullOrWhiteSpace(existingNode.Comment))
                    existingNode.Comment = node.Comment;
            } else { // Create new node.
                this._keyValuePairs[keyName] = node.Clone();
            }
        }

        /// <summary>
        /// Gets the config value node for a given key. Creates it if it does not exist.
        /// Applies the value to this config, not a super config with the value.
        /// The main purpose of this method is to be used for settings. Ie: GetOrCreateValue("testValue").setValue().
        /// </summary>
        /// <param name="keyName">The name of the key to get the value for.</param>
        /// <returns>configValueNode</returns>
        public ConfigValueNode GetOrCreateValue(string keyName) {
            if (this._keyValuePairs.TryGetValue(keyName, out ConfigValueNode valueNode))
                return valueNode;

            ConfigValueNode newNode = new ConfigValueNode("$NO_VALUE$", string.Empty);
            this._keyValuePairs[keyName] = newNode;
            return newNode;
        }

        /// <summary>
        /// Check if this config contains a key value pair with a given key.
        /// </summary>
        /// <param name="keyName">The key to look for.</param>
        /// <returns>hasKey</returns>
        public bool HasKey(string keyName) {
            return this._keyValuePairs.ContainsKey(keyName) || (this.SuperConfig?.HasKey(keyName) ?? false);
        }

        /// <summary>
        /// Gets a value from a key value pair in the config. Throws an error if the value is not found.
        /// </summary>
        /// <param name="keyName">The name of the key. (Case-sensitive)</param>
        /// <returns>valueNode</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the key is not found, and errorIfNotFound is true.</exception>
        public ConfigValueNode GetValueOrError(string keyName) {
            if (!this._keyValuePairs.ContainsKey(keyName)) {
                return this.SuperConfig?.GetValueOrError(keyName)
                    ?? throw new KeyNotFoundException($"'{keyName}' was not found in the config.");
            }

            return this._keyValuePairs[keyName];
        }

        /// <summary>
        /// Gets a value from a key value pair in the config, returning null if the value is not found.
        /// </summary>
        /// <param name="keyName">The name of the key. (Case-sensitive)</param>
        /// <returns>valueNode</returns>
        public ConfigValueNode GetValue(string keyName) {
            return this._keyValuePairs.TryGetValue(keyName, out ConfigValueNode value) ? value : this.SuperConfig?.GetValue(keyName);
        }

        /// <summary>
        /// Gets a child config with the supplied name, case-insensitive.
        /// Returns null if there is not child with that name.
        /// </summary>
        /// <param name="name">The name of the child section.</param>
        /// <returns>childConfig</returns>
        public Config GetChildConfigByName(string name) { 
            return this.InternalChildConfigs.Find(config => config.SectionName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                ?? this.SuperConfig?.GetChildConfigByName(name);
        }

        /// <summary>
        /// Gets all of the child configs with the supplied name, case-insensitive.
        /// </summary>
        /// <param name="name">The child config name to look for.</param>
        /// <returns>childConfigsWithName</returns>
        public IEnumerable<Config> GetAllChildConfigsByName(string name) {
            List<Config> configList = this.InternalChildConfigs.Where(config => config.SectionName.Equals(name, StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (this.SuperConfig != null) 
                configList.InsertRange(0, this.SuperConfig.GetAllChildConfigsByName(name));
            return configList;
        }

        /// <summary>
        /// Creates a dictionary of child configs based on their name.
        /// If there are duplicate names, the first section with the name will be used.
        /// </summary>
        /// <param name="includeSuper">Whether or not super-config children should be included. (Overriden children would still be omitted.)</param>
        /// <returns>childDictionary</returns>
        public Dictionary<string, Config> CreateChildDictionary(bool includeSuper = false) {
            Dictionary<string, Config> dictionary = new Dictionary<string, Config>();
            foreach (Config config in (includeSuper ? this.ChildConfigs.AsEnumerable() : this.InternalChildConfigs))
                if (!dictionary.ContainsKey(config.SectionName))
                    dictionary[config.SectionName] = config;

            return dictionary;
        }

        /// <inheritdoc/>
        public override string ToString() {
            using StringWriter writer = new StringWriter();
            if (!this.IsRoot)
                WriteSectionHeader(writer, "[", "]", this);
            ToString(writer, "[", "]");
            return writer.ToString();
        }

        private void ToString(TextWriter writer, string sectionStart, string sectionEnd) {
            // Write key value pairs.
            foreach ((string key, ConfigValueNode value) in this._keyValuePairs) {
                writer.Write(EscapeKey(key));
                writer.Write("=");
                writer.WriteLine(value);
            }

            // Write Raw text.
            foreach (ConfigValueNode line in this.InternalText)
                writer.WriteLine(line);

            // Empty line between sections.
            writer.WriteLine();

            // Write child sections in order.
            if (this.InternalChildConfigs.Count > 0) {
                string newSectionStart = sectionStart + "[";
                string newSectionEnd = sectionEnd + "]";

                foreach (Config child in this.InternalChildConfigs) {
                    WriteSectionHeader(writer, sectionStart, sectionEnd, child);
                    child.ToString(writer, newSectionStart, newSectionEnd);
                }
            }
        }

        private static void WriteSectionHeader(TextWriter writer, string sectionStart, string sectionEnd, Config node) {
            writer.Write(sectionStart);
            writer.Write(EscapeString(node.SectionName));
            if (node.SuperConfig != null) {
                writer.Write(" : ");
                writer.Write(EscapeString(node.SuperConfig.SectionName));
            }

            writer.Write(sectionEnd);
            if (!string.IsNullOrWhiteSpace(node.SectionComment)) {
                writer.Write(" # ");
                writer.Write(node.SectionComment);
            }

            writer.WriteLine();
        }

        /// <summary>
        /// General escaping applied to various parts of a config.
        /// </summary>
        /// <param name="value">The value to escape.</param>
        /// <returns>escapedValue</returns>
        public static string EscapeString(string value) {
            return value.Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\\", "\\\\")
                .Replace("#", "\\#");
        }

        /// <summary>
        /// Unescape the general escaping applied to various parts of a config.
        /// </summary>
        /// <param name="value">The value to unescape.</param>
        /// <returns>unescapedValue</returns>
        public static string UnescapeString(string value) {
            return value.Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\#", "#")
                .Replace("\\\\", "\\");
        }

        /// <summary>
        /// Escapes a key string.
        /// </summary>
        /// <param name="key">The key to escape</param>
        /// <returns>escapedKey</returns>
        public static string EscapeKey(string key) {
            return EscapeString(key).Replace(" ", "\\ ").Replace("=", "\\=");
        }

        /// <summary>
        /// Unescapes a key string.
        /// </summary>
        /// <param name="key">The key to unescape</param>
        /// <returns>unescapedKey</returns>
        public static string UnescapeKey(string key) {
            return UnescapeString(key).Replace("\\ ", " ").Replace("\\=", "=");
        }

        /// <summary>
        /// Gets the root configuration node.
        /// </summary>
        /// <param name="config">The node whose root we want to find.</param>
        /// <returns>rootConfigNode</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
        public static Config GetRoot(Config config) {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Could not get the root config for null!");

            Config temp = config;
            while (temp.ParentConfig != null)
                temp = temp.ParentConfig;
            return temp;
        }

        /// <summary>
        /// Finds a Config searching all children recursively of the provided config.
        /// If there is more than one match with the given name, 
        /// </summary>
        /// <param name="config">The config to search.</param>
        /// <param name="configName">The name to search for. Case-sensitive.</param>
        /// <returns>foundConfig, or null if there is none by that name.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the passed config is null.</exception>
        /// <exception cref="DuplicateNameException">Thrown if there are multiple configs with the specified name.</exception>
        public static Config FindConfigByName(Config config, string configName) {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(configName))
                return null;

            Queue<Config> configCheckQueue = new Queue<Config>();
            configCheckQueue.Enqueue(config);

            Config foundMatch = null;
            while (configCheckQueue.Count > 0) {
                Config checkConfig = configCheckQueue.Dequeue();
                foreach (Config childConfig in checkConfig.InternalChildConfigs) {
                    if (configName.Equals(childConfig.SectionName)) {
                        if (foundMatch != null) {
                            throw new DuplicateNameException($"Found multiple config entries with the name '{configName}'.");
                        } else {
                            foundMatch = childConfig;
                        }
                    }

                    configCheckQueue.Enqueue(childConfig);
                }
            }

            return foundMatch;
        }

        /// <summary>
        /// Reads a configuration from a string.
        /// </summary>
        /// <param name="configString">The string to read the configuration from</param>
        /// <returns>readConfig</returns>
        /// <exception cref="SyntaxErrorException">Thrown if invalid configuration syntax is loaded.</exception>
        public static Config LoadConfigFromString(string configString) {
            using BadStringReader reader = new BadStringReader(new StringReader(configString));
            Config loadedConfig = LoadConfigFromString(null, reader, 0, "Root", null);
            loadedConfig.LoadSuperConfigs(loadedConfig);
            return loadedConfig;
        }
        
        /// <summary>
        /// Reads a configuration from a file.
        /// </summary>
        /// <param name="cfgFilePath">The file to read the configuration from</param>
        /// <returns>readConfig</returns>
        /// <exception cref="SyntaxErrorException">Thrown if invalid configuration syntax is loaded.</exception>
        public static Config LoadConfigFromFile(string cfgFilePath) {
            Config config = LoadConfigFromString(File.ReadAllText(cfgFilePath));
            config.SectionName = Path.GetFileNameWithoutExtension(cfgFilePath);
            return config;
        }

        private static Config LoadConfigFromString(Config parentConfig, BadStringReader stringReader, int layer, string sectionName, string sectionComment) {
            Config config = new Config(parentConfig) {SectionName = sectionName, SectionComment = sectionComment};

            while (true) {
                string line = stringReader.ReadLine();
                if (line == null)
                    return config; // Reached end of file?

                if (string.IsNullOrWhiteSpace(line))
                    continue; // Empty lines are skipped.

                string trimmedLine = line.Trim();

                if (trimmedLine[0] == '[') {
                    string comment = string.Empty;
                    string sectionLine = trimmedLine;

                    // Read the comment, if there is one.
                    int commentIndex = sectionLine.IndexOf("#", StringComparison.InvariantCulture);
                    if (commentIndex != -1) {
                        comment = sectionLine.Substring(commentIndex + 2).TrimStart();
                        sectionLine = sectionLine.Substring(0, commentIndex).TrimEnd();
                    }

                    // This is the start of a new section.
                    if (sectionLine[^1] != ']')
                        throw new SyntaxErrorException($"Invalid section identifier. (Line: '{sectionLine}')");

                    int leftLayer = 0;
                    foreach (char t in sectionLine) {
                        if (t == '[') {
                            leftLayer++;
                        } else {
                            break;
                        }
                    }

                    int rightLayer = 0;
                    for (int i = sectionLine.Length - 1; i >= 0; i--) {
                        if (sectionLine[i] == ']') {
                            rightLayer++;
                        } else {
                            break;
                        }
                    }

                    // Verify layer.
                    if (leftLayer != rightLayer)
                        throw new SyntaxErrorException($"Section identifier had mismatched tags! (Line: '{sectionLine}', Left: {leftLayer}, Right: {rightLayer})");

                    int sectionLayer = leftLayer;
                    if (sectionLayer > layer + 1)
                        throw new SyntaxErrorException($"Section identifier has too many brackets to connect to its parent! (Line: '{sectionLine}', Parent: {layer}, New Layer: {sectionLayer})");

                    int nameLength = sectionLine.Length - rightLayer - leftLayer;
                    if (nameLength == 0)
                        throw new SyntaxErrorException($"Section identifier had no name! (Line: '{sectionLine}')");

                    // Determine super-section and section name.
                    string newSectionName = UnescapeString(sectionLine.Substring(leftLayer, nameLength));
                    string extensionClass = null;
                    if (newSectionName.Contains(":", StringComparison.InvariantCulture)) {
                        string[] split = newSectionName.Split(":");
                        newSectionName = split[0].TrimEnd();
                        extensionClass = split[1].TrimStart();
                    }

                    // Create child config.
                    if (sectionLayer == layer + 1) {
                        // Read for this config.
                        Config loadedChildConfig = LoadConfigFromString(config, stringReader, sectionLayer, newSectionName, comment);
                        loadedChildConfig._temporarySuperConfigName = extensionClass;
                        config.InternalChildConfigs.Add(loadedChildConfig);
                    } else {
                        // Some other config earlier in the hierarchy (earlier layer) owns this.
                        stringReader.NextRead = line; // A parent reader will need to access the current line.
                        return config;
                    }

                    continue;
                }

                // Read the comment, if there is one.
                string text = trimmedLine;

                string commentText = string.Empty;
                int commentAt = -1;
                for (int i = 0; i < text.Length; i++) {
                    char current = text[i];

                    if (current == '\\') {
                        i++; // Skip the next character, it's escaped.
                    } else if (current == '#') {
                        commentAt = i;
                        break;
                    }
                }

                if (commentAt != -1) {
                    commentText = text.Substring(commentAt + 1).TrimStart();
                    text = text.Substring(0, commentAt).TrimEnd();
                }

                if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(commentText))
                    continue; // If there is no text, and no comment on a line, skip it.

                // Determine if this is a key-value pair.
                int splitAt = -1;
                for (int i = 0; i < text.Length; i++) {
                    char current = text[i];

                    if (current == '\\') {
                        i++; // Skip the next character, it's escaped.
                    } else if (current == '=') {
                        splitAt = i;
                        break;
                    } else if (current == ' ') {
                        break; // It's not a key value pair if we see a space before =.
                    }
                }

                // Parse/store the values.
                if (splitAt != -1) { // It's a key-value pair.
                    string key = UnescapeKey(text.Substring(0, splitAt));
                    string value = UnescapeString(text.Substring(splitAt + 1));
                    config._keyValuePairs[key] = new ConfigValueNode(value, commentText);
                } else { // It's raw text.
                    bool isEmpty = text.Equals("```", StringComparison.InvariantCulture);
                    config.InternalText.Add(isEmpty ? new ConfigValueNode(string.Empty, commentText) : new ConfigValueNode(text, commentText));
                }
            }
        }
    }

    /// <summary>
    /// Settings/data used when loading a config.
    /// This is an example of passing settings / data among IBinarySerializable can work.
    /// </summary>
    public class ConfigSettings : DataSettings {
        public bool HasComments = true;
        public bool NeedsHeader = true;
        public bool ErrorIfBadExtension = true; // Setting this to false will ignore the error, and skip any extensions that can't be saved. Useful if only extensions and not the base configs are to be saved.
    }

    /// <summary>
    /// A node which contains some kind of value, and potentially a comment.
    /// </summary>
    public class ConfigValueNode : IBinarySerializable {
        [NotNull] public string Comment;
        public bool IsEscapedNewLine { get; private set; }
        [NotNull] public string Value; // Can be empty, but not null.

        public ConfigValueNode(string value, string comment) {
            this.Value = value ?? string.Empty;
            this.Comment = comment;
            this.IsEscapedNewLine = string.IsNullOrEmpty(value);
        }

        /// <inheritdoc cref="IBinarySerializable"/>
        public void LoadFromReader(DataReader reader, DataSettings settings = null) {
            ConfigSettings configSettings = settings as ConfigSettings ?? new ConfigSettings();
            this.Value = reader.ReadString();
            this.IsEscapedNewLine = reader.ReadBoolean();
            if (configSettings.HasComments)
                this.Comment = reader.ReadString();
        }

        /// <inheritdoc cref="IBinarySerializable"/>
        public void SaveToWriter(DataWriter writer, DataSettings settings = null) {
            ConfigSettings configSettings = settings as ConfigSettings ?? new ConfigSettings();
            writer.Write(this.Value);
            writer.Write(this.IsEscapedNewLine);
            if (configSettings.HasComments)
                writer.Write(this.Comment);
        }

        /// <summary>
        /// Creates a copy of this object.
        /// </summary>
        /// <returns>valueNodeCopy</returns>
        public ConfigValueNode Clone() {
            ConfigValueNode newNode = new ConfigValueNode(this.Value, this.Comment);
            newNode.IsEscapedNewLine = this.IsEscapedNewLine;
            return newNode;
        }

        /// <summary>
        /// Gets the node value as a string.
        /// </summary>
        /// <returns>stringValue</returns>
        public string GetAsString() {
            return this.Value;
        }

        /// <summary>
        /// Sets the node value to a string.
        /// </summary>
        /// <param name="newValue">The new string value</param>
        public void SetAsString(string newValue) {
            this.Value = newValue;
        }

        /// <summary>
        /// Gets the node value as a boolean.
        /// </summary>
        /// <returns>boolValue</returns>
        /// <exception cref="SyntaxErrorException">Thrown if the node data is not a valid boolean.</exception>
        public bool GetAsBoolean() {
            if ("true".Equals(this.Value, StringComparison.InvariantCultureIgnoreCase)
                || "yes".Equals(this.Value, StringComparison.InvariantCultureIgnoreCase)
                || "1".Equals(this.Value, StringComparison.InvariantCulture))
                return true;

            if ("false".Equals(this.Value, StringComparison.InvariantCultureIgnoreCase)
                || "no".Equals(this.Value, StringComparison.InvariantCultureIgnoreCase)
                || "0".Equals(this.Value, StringComparison.InvariantCulture))
                return false;

            throw new SyntaxErrorException($"Don't know how to interpret '{Value}' as a boolean.");
        }

        /// <summary>
        /// Sets the node value to a boolean.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        public void SetAsBoolean(bool newValue) {
            this.Value = newValue ? "true" : "false";
        }

        /// <summary>
        /// Gets the node value as an integer.
        /// </summary>
        /// <returns>intValue</returns>
        /// <exception cref="SyntaxErrorException">Thrown if the value is not formatted as either an integer or a hex integer.</exception>
        public int GetAsInteger() {
            if (NumberUtils.TryParseInteger(this.Value, out int resultValue, out NumberParseFailureReason failReason))
                return resultValue;

            throw new SyntaxErrorException($"Invalid integer: '{this.Value}'. ({failReason})");
        }

        /// <summary>
        /// Sets the node value to an integer.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        public void SetAsInteger(int newValue) {
            Value = newValue.ToString();
        }

        /// <summary>
        /// Gets the node value as an double.
        /// </summary>
        /// <returns>doubleValue</returns>
        /// <exception cref="SyntaxErrorException">Thrown if the value is not formatted as a valid number.</exception>
        public double GetAsDouble() {
            if (NumberUtils.IsValidNumber(this.Value))
                return Double.Parse(Value, CultureInfo.InvariantCulture);
            throw new SyntaxErrorException($"Value '{this.Value}' is not a valid number.");
        }

        /// <summary>
        /// Sets the node value to a double.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        public void SetAsDouble(double newValue) {
            this.Value = ((decimal)newValue).ToString(CultureInfo.InvariantCulture); // The purpose of converting to decimal is that it means it won't use scientific notation. (Ie: "5e-9")
        }

        /// <summary>
        /// Gets the value as an enum.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>enumValue</returns>
        /// <exception cref="SyntaxErrorException">Thrown if the value was not a valid enum or a valid enum index.</exception>
        public TEnum GetAsEnum<TEnum>() where TEnum : Enum {
            try {
                if (NumberUtils.TryParseInteger(this.Value, out int numericValue)) {
                    // An enum can't start with - or a digit, so it's a safe assumption that if it's a number, we should use it as an index.
                    return (TEnum)Enum.Parse(typeof(TEnum), Enum.GetName(typeof(TEnum), numericValue));
                }

                return (TEnum)Enum.Parse(typeof(TEnum), Value, true);
            } catch (Exception e) {
                throw new SyntaxErrorException($"The value '${this.Value}' could not be interpreted as an enum value from {typeof(TEnum).Name}.", e);
            }
        }

        /// <summary>
        /// Sets the node value to an enum.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        public void SetAsEnum<TEnum>(TEnum newValue) where TEnum : Enum {
            this.Value = newValue.ToString();
        }

        // Internal stuff.

        /// <inheritdoc/>
        public override string ToString() {
            if (this.IsEscapedNewLine)
                return "```" + (string.IsNullOrWhiteSpace(this.Comment) ? "" : " # " + this.Comment);
            if (string.IsNullOrWhiteSpace(this.Comment))
                return Config.EscapeString(this.Value ?? string.Empty);
            return Config.EscapeString(this.Value ?? string.Empty) + (string.IsNullOrWhiteSpace(this.Value) ? "# " : " # ") + this.Comment;
        }
    }

    // This is scuffed, but it works.
    internal class BadStringReader : IDisposable {
        private readonly TextReader _internalReader;
        public string NextRead;

        public BadStringReader(TextReader reader) {
            this._internalReader = reader;
        }

        public void Dispose() {
            this._internalReader?.Dispose();
            GC.SuppressFinalize(this);
        }

        public string ReadLine() {
            if (this.NextRead != null) {
                string value = this.NextRead;
                this.NextRead = null;
                return value;
            }

            return this._internalReader.ReadLine();
        }
    }

    public static class ConfigListExtensions {
        /// <summary>
        /// Gets the individual lines of text from a list of config value nodes.
        /// </summary>
        /// <param name="nodeList">The list to get the text from</param>
        /// <returns>textStringList</returns>
        public static List<string> GetText(this List<ConfigValueNode> nodeList) {
            return nodeList.ConvertAll(node => node.Value).Where(str => !string.IsNullOrWhiteSpace(str)).ToList();
        }
    }
}