using System;

public struct RelayHostData
{
    public string mJoinCode;
    public string mIPv4Address;
    public int mPort;
    public Guid mAllocationID;
    public byte[] mAllocationIDBytes;
    public byte[] mConnectionData;
    public byte[] mKey;
}
