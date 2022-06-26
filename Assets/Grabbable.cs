using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Grabbable : MonoBehaviour
{
    public abstract void GetPulledByHarpoon();
    public abstract void GetReleasedByHarpoon(Vector2 velocity);
}
