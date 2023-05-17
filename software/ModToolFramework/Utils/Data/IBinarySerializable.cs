namespace ModToolFramework.Utils.Data {
    /// <summary>
    /// An interface which can be saved / read from a DataReader/DataWriter.
    /// </summary>
    public interface IBinarySerializable {
        /// <summary>
        /// Reads the object's data from a DataReader.
        /// </summary>
        /// <param name="reader">The reader to read data from.</param>
        /// <param name="settings">Any extra data you wish to pass to the function. Usually null.</param>
        public void LoadFromReader(DataReader reader, DataSettings settings = null);


        /// <summary>
        /// Writes the object's data to a DataWriter.
        /// </summary>
        /// <param name="writer">The writer to save data to.</param>
        /// <param name="settings">Any extra data you wish to pass to the function. Usually null.</param>
        public void SaveToWriter(DataWriter writer, DataSettings settings = null);
    }
}