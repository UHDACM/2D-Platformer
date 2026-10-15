using UnityEngine;

public abstract class ScriptableObjectPlus : ScriptableObject
{
    // these values show up in the black inspector block
    public abstract string prettyName { get; }
    public abstract string description { get; }
}
