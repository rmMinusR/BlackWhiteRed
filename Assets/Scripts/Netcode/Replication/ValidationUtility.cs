using System;
using UnityEngine;

public static class ValidationUtility
{
    public static void Bound(out bool anyChange, ref float val, float expected, float allowedError)
    {
        float newVal = Mathf.Clamp(val, expected-allowedError, expected+allowedError);
        anyChange = (val != newVal);
        val = newVal;
    }

    public static void Bound(out bool anyChange, ref Vector3 val, Vector3 expected, float allowedError) => Bound(out anyChange, ref val, expected, allowedError, Vector3.Distance);
    public static void Bound(out bool anyChange, ref Vector3 val, Vector3 expected, float allowedError, Func<Vector3, Vector3, float> measureDist)
    {
        float curLength = measureDist(val, expected);

        if(anyChange = curLength > allowedError)
        {
            Vector3 d = val-expected;
            float amtAccepted = 1 / Mathf.Max(curLength, allowedError);
            d *= amtAccepted; //Readjust length

            val = expected + d;
        }
    }
}
