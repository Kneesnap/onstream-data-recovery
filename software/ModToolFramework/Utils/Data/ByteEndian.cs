namespace ModToolFramework.Utils.Data
{
    public enum ByteEndian
    {
        LittleEndian, // an int32 with the value of 510 would be encoded as "FE 01 00 00".
        BigEndian // an int32 with the value 510 would be encoded as "00 00 01 FE".
    }
}
