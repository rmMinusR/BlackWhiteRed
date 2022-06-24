using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public static class ClientIDCache
{
    private static Dictionary<ulong, ulong[]> cache = new Dictionary<ulong, ulong[]>();

    public static ulong[] Narrowcast(ulong clientID)
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
    public static ClientRpcParams ReturnToSender(this ServerRpcParams src)
    {
        ClientRpcParams val = new ClientRpcParams();
        val.Send.TargetClientIds = Narrowcast(src.Receive.SenderClientId);
        return val;
    }
}
