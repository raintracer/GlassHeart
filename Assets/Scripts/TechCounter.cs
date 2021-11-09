using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TechCounter : MonoBehaviour
{

    const int TECH_COUNTER_LIFETIME_FRAMES = 45;
    float ScrollSpeed = .25f;

    private GameObject GO;
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
            SR_Background.sprite = GameAssets.Sprite.TechBox;
        } else if (_Type == TechType.Combo){
            CounterTextMesh.text = _Level.ToString();
            SR_Background.sprite = GameAssets.Sprite.TechBoxChain;
        }

        StartCoroutine("DisplayCountdown");

    }

    IEnumerator DisplayCountdown()
    {
        for (int i = 1; i <= TECH_COUNTER_LIFETIME_FRAMES; i++)
        {
            transform.position += Vector3.up * (ScrollSpeed / (float) i);
            yield return new WaitForFixedUpdate();
        }
        Destroy(gameObject);
    }
}
