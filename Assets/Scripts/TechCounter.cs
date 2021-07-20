using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TechCounter : MonoBehaviour
{

    const float TECH_COUNTER_LIFETIME = 1.5f;
    public GameObject GO;
    public enum TechType { Combo, Chain };

    private void Awake()
    {
        GO = gameObject;    
    }

    public void StartEffect(TechType _Type, int _Level, Vector2 _WorldPosition)
    {

        transform.position = _WorldPosition;
        TextMeshPro CounterTextMesh = GO.transform.Find("CounterText").GetComponent<TextMeshPro>();
        if(_Type == TechType.Chain)
        {
            CounterTextMesh.text = "X" + _Level.ToString();
        } else if (_Type == TechType.Combo){
            CounterTextMesh.text = _Level.ToString();
        }

        StartCoroutine("DisplayCountdown");

    }

    IEnumerator DisplayCountdown()
    {
        yield return new WaitForSeconds(TECH_COUNTER_LIFETIME);
        Destroy(gameObject);
    }
}
