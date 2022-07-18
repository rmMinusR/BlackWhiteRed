using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

/*
 * 
 * Makes a property read-only in inspector
 * Taken from https://answers.unity.com/questions/489942/how-to-make-a-readonly-property-in-inspector.html
 * 
 */

public class InspectorReadOnlyAttribute : PropertyAttribute
{
    public AccessMode editing;
    public AccessMode playing;

    public InspectorReadOnlyAttribute(AccessMode editing = AccessMode.ReadOnly, AccessMode playing = AccessMode.ReadOnly)
    {
        this.editing = editing;
        this.playing = playing;
    }
}

[MovedFrom("InspectorReadOnlyAttribute.Mode")]
public enum AccessMode
{
    ReadOnly,
    ReadWrite
}