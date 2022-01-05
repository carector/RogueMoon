using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sunfish : Fish
{
    public Transform[] damagePoints;
    public Sprite[] damagedSunfishOverlaySprites;
    public GameObject[] sunfishGibs;
    bool[] destroyedAreas;
    SpriteRenderer[] destroyedOverlays;
    ParticleSystem[] bloodParticles;

    // Start is called before the first frame update
    void Start()
    {
        GetReferences();
        destroyedAreas = new bool[3];
        destroyedOverlays = new SpriteRenderer[3];
        bloodParticles = new ParticleSystem[3];
        for (int i = 0; i < 3; i++)
        {
            destroyedOverlays[i] = transform.GetChild(i).GetComponent<SpriteRenderer>();
            bloodParticles[i] = destroyedOverlays[i].transform.GetChild(0).GetComponent<ParticleSystem>();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 0.1f);
    }

    public override void BreakApart()
    {
        StartCoroutine(DamagePartOfSunfish());
    }

    protected override IEnumerator DamageFlashCoroutine()
    {
        for (int i = 0; i < 3; i++)
        {
            foreach (SpriteRenderer spr2 in destroyedOverlays)
                spr2.color = Color.red;
            spr.color = Color.red;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            foreach (SpriteRenderer spr2 in destroyedOverlays)
                spr2.color = Color.white;
            spr.color = Color.white;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator DamagePartOfSunfish()
    {
        // Wait a frame so harpoon can potentially attach
        yield return new WaitForFixedUpdate();

        // Check the closest damage point to the player / harpoon's location
        Vector3 pos;
        if (ply.pAbilities.beingPulledTowardsHarpoon)
            pos = ply.harpoonEndpoint.transform.position;
        else
            pos = ply.transform.position;

        int closestPointIndex = 0;
        for (int i = 0; i < damagePoints.Length; i++)
        {
            if (Vector2.Distance(pos, damagePoints[i].position) < Vector2.Distance(pos, damagePoints[closestPointIndex].position) && !destroyedAreas[i])
                closestPointIndex = i;
        }

        destroyedAreas[closestPointIndex] = true;
        destroyedOverlays[closestPointIndex].sprite = damagedSunfishOverlaySprites[closestPointIndex];
        bloodParticles[closestPointIndex].Play();

        // Complicated gib spawning procedure
        // Spawn generic gore
        for (int i = 0; i < 6; i++)
            Instantiate(sunfishGibs[0], damagePoints[closestPointIndex].transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)), Quaternion.identity);

        // Point-specific gibs
        switch (closestPointIndex)
        {
            // Top: Spawn a couple tubes
            case (0):
                for (int i = 0; i < 2; i++)
                    Instantiate(sunfishGibs[1], damagePoints[closestPointIndex].transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)), Quaternion.identity);
                break;
            // Bottom: Spawn lots of tubes + ribs
            case (1):
                for (int i = 0; i < 4; i++)
                {
                    Instantiate(sunfishGibs[1], damagePoints[closestPointIndex].transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)), Quaternion.identity);
                    Instantiate(sunfishGibs[2], damagePoints[closestPointIndex].transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)), Quaternion.identity);
                }
                break;
            // Middle: Spawn brain
            case (2):
                Instantiate(sunfishGibs[3], damagePoints[closestPointIndex].transform.position, Quaternion.identity).GetComponent<GibScript>().InitializeGib(goreBits[1], Random.Range(-50, 50));
                break;
        }

        if(destroyedAreas[0] && destroyedAreas[1] && destroyedAreas[2])
        {
            GameObject g = Instantiate(sunfishGibs[4], transform.position, Quaternion.identity);
            g.GetComponent<SunfishGib>().InitializeGib(spr.flipX);
            foreach (ParticleSystem p in bloodParticles)
                p.transform.parent = g.transform;

            Destroy(this.gameObject);
        }
    }
}
