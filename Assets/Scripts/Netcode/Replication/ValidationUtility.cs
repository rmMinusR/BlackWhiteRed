using System;
using UnityEngine;

public static class ValidationUtility
{
    public static float Bound(out bool anyChange, float val, float expected, float allowedError)
    {
        float newVal = Mathf.Clamp(val, expected-allowedError, expected+allowedError);
        anyChange = (val != newVal);
        return newVal;
    }

    public static Vector3 Bound(out bool anyChange, Vector3 val, Vector3 expected, float allowedError)
    {
        Vector3 newVal = Vector3.MoveTowards(expected, val, allowedError);
        anyChange = (newVal != val);
        return newVal;
    }

    public static Vector3 Bound(out bool anyChange, Vector3 val, Vector3 expected, float allowedError, Func<Vector3, Vector3, float> measureDist)
    {
        //FIXME Not sure if this is working correctly

        float curLength = measureDist(val, expected);

        if(curLength > allowedError)
        {
            anyChange = true;

            Vector3 d = val-expected;
            float amtAccepted = 1 / Mathf.Max(curLength, allowedError);
            d *= amtAccepted; //Readjust length

            val = expected + d;
        } else anyChange = false;

        return val;
    }
}
