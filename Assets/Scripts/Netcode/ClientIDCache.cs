using System;
using System.Collections.Generic;
using System.Linq;

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
}
