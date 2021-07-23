using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ParticleController : MonoBehaviour
{

    float LifeTime;
    public GameObject ParticleObject;

    [Server]
    public void StartParticle(string ParticleName, Vector2 ParticlePosition, float LifeTime)
    {
        this.LifeTime = LifeTime;
        ParticleObject = Instantiate(Resources.Load<GameObject>(ParticleName), ParticlePosition, Quaternion.identity);
        if(ParticleObject == null)
        {
            Debug.LogError("Particle prefab not found in resources: " + ParticleName);
        }

        ParticleObject.GetComponent<ParticleSystem>().Play();
        StartCoroutine("ParticleCountdown");

    }

    [Server]
    IEnumerator ParticleCountdown()
    {
        NetworkServer.Spawn(gameObject);
        yield return new WaitForSeconds(LifeTime);
        Destroy(ParticleObject);
        Destroy(gameObject);
    }

}
