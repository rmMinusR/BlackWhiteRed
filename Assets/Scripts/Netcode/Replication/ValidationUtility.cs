using System;
using UnityEngine;

public static class ValidationUtility
{
    public static bool Bound(ref float val, float expected, float allowedError)
    {
        float newVal = Mathf.Clamp(val, expected-allowedError, expected+allowedError);
        bool anyChange = (val != newVal);
        val = newVal;
        return anyChange;
    }

    public static bool Bound(ref Vector3 val, Vector3 expected, float allowedError)
    {
        Vector3 newVal = Vector3.MoveTowards(expected, val, allowedError);
        bool anyChange = (val != newVal);
        val = newVal;
        return anyChange;
    }

    public static bool Bound(ref Vector3 val, Vector3 expected, float allowedError, Func<Vector3, Vector3, float> measureDist)
    {
        //FIXME Not sure if this is working correctly

        float curLength = measureDist(val, expected);

        if(curLength > allowedError)
        {
            Vector3 d = val-expected;
            float amtAccepted = 1 / Mathf.Max(curLength, allowedError);
            d *= amtAccepted; //Readjust length

            val = expected + d;

            return true;
        }
        else return false;
    }
}
