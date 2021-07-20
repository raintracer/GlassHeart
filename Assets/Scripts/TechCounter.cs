using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TechCounter : MonoBehaviour
{

    const float TECH_COUNTER_LIFETIME = 0.75f;
    public GameObject GO;
    TextMeshPro CounterTextMesh;
    SpriteRenderer SR_Background;
    public enum TechType { Combo, Chain };

    private void Awake()
    {
        GO = gameObject;
        CounterTextMesh = GO.transform.Find("CounterText").GetComponent<TextMeshPro>();
        SR_Background = GO.transform.Find("TechCounterBackObject").GetComponent<SpriteRenderer>();
    }

    public void StartEffect(TechType _Type, int _Level, Vector2 _WorldPosition)
    {

        transform.position = _WorldPosition;
        
        if(_Type == TechType.Chain)
        {
            CounterTextMesh.text = "X" + _Level.ToString();
            SR_Background.color = Color.green;
        } else if (_Type == TechType.Combo){
            CounterTextMesh.text = _Level.ToString();
            SR_Background.color = Color.red;
        }

        StartCoroutine("DisplayCountdown");

    }

    IEnumerator DisplayCountdown()
    {
        yield return new WaitForSeconds(TECH_COUNTER_LIFETIME);
        Destroy(gameObject);
    }
}
