using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spell")]
public class Spell : ScriptableObject
{

    public enum Element { Air, Water, Fire, Earth }

    public string SpellName;
    public float BoostTime;
    public float BoostIntensity;
    public float StopTime;
    public float FloatTime;

    public bool HasRowBurst;


}
