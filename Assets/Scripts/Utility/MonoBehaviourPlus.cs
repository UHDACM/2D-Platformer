using UnityEngine;


public abstract class MonoBehaviourPlus : MonoBehaviour
{
    // these values show up in the black inspector block
    public abstract string prettyName { get; }
    public abstract string description { get; }
}
