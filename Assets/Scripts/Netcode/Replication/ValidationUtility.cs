using System;
using UnityEngine;

public static class ValidationUtility
{
    public static bool Bound(in float @in, out float @out, float expected, float allowedError)
    {
        float newVal = Mathf.Clamp(@in, expected-allowedError, expected+allowedError);
        bool anyChange = (@in != newVal);
        @out = newVal;
        return anyChange;
    }

    public static bool Bound(in Vector2 @in, out Vector2 @out, Vector2 expected, float allowedError)
    {
        Vector2 newVal = Vector2.MoveTowards(expected, @in, allowedError);
        bool anyChange = (@in != newVal);
        @out = newVal;
        return anyChange;
    }

    public static bool Bound(in Vector3 @in, out Vector3 @out, Vector3 expected, float allowedError)
    {
        Vector3 newVal = Vector3.MoveTowards(expected, @in, allowedError);
        bool anyChange = (@in != newVal);
        @out = newVal;
        return anyChange;
    }

    public static bool Bound(in Vector3 @in, out Vector3 @out, Vector3 expected, float allowedError, Func<Vector3, Vector3, float> measureDist)
    {
        //FIXME Not sure if this is working correctly

        float curLength = measureDist(@in, expected);

        if (curLength > allowedError)
        {
            Vector3 d = @in-expected;
            float amtAccepted = 1 / Mathf.Max(curLength, allowedError);
            d *= amtAccepted; //Readjust length

            @out = expected + d;

            return true;
        }
        else
        {
            @out = @in;
            return false;
        }
    }
}
