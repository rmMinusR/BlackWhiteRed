using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public static class ClientIDCache
{
    private static Dictionary<ulong, ulong[]> cache = new Dictionary<ulong, ulong[]>();

    /// <summary>
    /// Helper for sending RPCs to one client without constantly setting off GC
    /// </summary>
    /// <param name="clientID"></param>
    /// <returns></returns>
    public static ulong[] NarrowcastArr(ulong clientID)
    {
        ulong[] v;

        //Try to retrieve from cache
        if (cache.TryGetValue(clientID, out v)) return v;

        //Make new and store in cache
        v = new ulong[] { clientID };
        cache.Add(clientID, v);
        return v;
    }

    /// <summary>
    /// Convenience method for responding to a client in a server-side RPC
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    public static ClientRpcParams ReturnToSender(this ServerRpcParams src) => Narrowcast(src.Receive.SenderClientId);

    /// <summary>
    /// Convenience method for narrowcasting to a specific client
    /// </summary>
    /// <param name="clientID"></param>
    /// <returns></returns>
    public static ClientRpcParams Narrowcast(ulong clientID)
    {
        ClientRpcParams val = default;
        val.Send.TargetClientIds = NarrowcastArr(clientID);
        return val;
    }
}
