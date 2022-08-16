using System.Collections;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

public static class ComponentNetworkSerializationUtility
{
    #region Explicit definitions

    public static void ReadValueSafe(this FastBufferReader buf, out PlayerController pc) => DecodePlayerBehaviour(buf, out pc);
    public static void WriteValueSafe(this FastBufferWriter buf, in PlayerController pc) => EncodePlayerBehaviour(buf, in pc);

    #endregion

    #region Helpers

    private const ulong NULL_OBJECT_ID = ulong.MaxValue;
    private const ushort NULL_COMPONENT_ID = ushort.MaxValue;
    
    private static void DecodePlayerBehaviour<T>(FastBufferReader buf, out T pc) where T : NetworkBehaviour
    {
        buf.ReadValueSafe(out ulong ownerID);
        if (ownerID != NULL_OBJECT_ID) pc = NetHeartbeat.Of(ownerID).GetComponent<T>();
        else pc = null;
    }

    private static void EncodePlayerBehaviour<T>(FastBufferWriter buf, in T pc) where T : NetworkBehaviour
    {
        buf.WriteValueSafe(pc!=null ? pc.OwnerClientId : NULL_OBJECT_ID);
    }


    private static MethodInfo NetworkBehaviour_GetNetworkBehaviour; //TODO this is awful, can we speed it up at all?
    private static MethodInfo NetworkBehaviour_GetNetworkObject;
    private static NetworkBehaviour NetworkBehaviour_InvocationTarget; //Also awful
    private static void EnsureGoodReflectionTargets()
    {
        if (NetworkBehaviour_GetNetworkBehaviour == null) NetworkBehaviour_GetNetworkBehaviour = typeof(NetworkBehaviour).GetMethod("GetNetworkBehaviour", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (NetworkBehaviour_GetNetworkObject    == null) NetworkBehaviour_GetNetworkBehaviour = typeof(NetworkBehaviour).GetMethod("GetNetworkObject"   , BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (NetworkBehaviour_InvocationTarget    == null) NetworkBehaviour_InvocationTarget    = Component.FindObjectOfType<NetworkBehaviour>();
    }

    private static void DecodeNetworkBehaviour<T>(FastBufferReader buf, out T c) where T : NetworkBehaviour
    {
        buf.ReadValueSafe(out ushort componentID);

        EnsureGoodReflectionTargets();

        c = (T) NetworkBehaviour_GetNetworkBehaviour.Invoke(NetworkBehaviour_InvocationTarget, new object[] { componentID });
    }

    private static void EncodeNetworkBehaviour<T>(FastBufferWriter buf, in T c) where T : NetworkBehaviour
    {
        buf.WriteValueSafe(c!=null ? c.NetworkBehaviourId : NULL_COMPONENT_ID);
    }

    #endregion
}
