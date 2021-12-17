using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitterScript : MonoBehaviour
{
    Animator bodyAnim;
    Animator spitterAnim;
    PlayerController ply;
    Transform bulletSpawnPos;

    public float noticeDistance = 10;
    public GameObject gib;
    public Sprite[] goreBits;
    public GameObject projectile;

    int storedDir = -1;
    bool flipping;

    // Start is called before the first frame update
    void Start()
    {
        bodyAnim = GetComponent<Animator>();
        spitterAnim = transform.GetChild(0).GetComponent<Animator>();
        ply = FindObjectOfType<PlayerController>();
        bulletSpawnPos = spitterAnim.transform.GetChild(0);
        StartCoroutine(ShootCycle());
    }

    public void ShootProjectile()
    {
        Instantiate(projectile, bulletSpawnPos.position, Quaternion.identity).GetComponent<SpitterProjectileScript>().Initialize(storedDir);
    }
    public void FlipLeft()
    {
        storedDir = -1;
    }

    public void FlipRight()
    {
        storedDir = 1;
    }

    public void BreakApart()
    {
        Instantiate(gib, transform.position + Vector3.down*1.35f, Quaternion.identity).GetComponent<GibScript>().InitializeGib(goreBits[0], Random.Range(-50, 50));
        Instantiate(gib, transform.position + Vector3.down * 2.24f + Vector3.right*storedDir*0.7f, Quaternion.identity).GetComponent<GibScript>().InitializeGib(goreBits[1], Random.Range(-50, 50));
        Instantiate(gib, transform.position + Vector3.down * 1.85f + Vector3.right*storedDir*0.6f, Quaternion.identity).GetComponent<GibScript>().InitializeGib(goreBits[2], Random.Range(-50, 50));

        Destroy(this.gameObject);
    }

    // Update is called once per frame
    IEnumerator ShootCycle()
    {
        while (true)
        {
            if (ply.pResources.health > 0)
            {
                if (ply.transform.position.x < transform.position.x && storedDir != -1)
                {
                    CheckAndPlayClip(spitterAnim, "SpitterBody_TurnLeft");
                    yield return new WaitForSeconds(0.5f);
                }
                else if (ply.transform.position.x > transform.position.x && storedDir != 1)
                {
                    CheckAndPlayClip(spitterAnim, "SpitterBody_TurnRight");
                    yield return new WaitForSeconds(0.5f);
                }

                CheckAndPlayClip(bodyAnim, "Spitter_Shoot");
                if (storedDir == -1)
                {
                    CheckAndPlayClip(spitterAnim, "SpitterBody_ShootLeft");
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    CheckAndPlayClip(spitterAnim, "SpitterBody_ShootRight");
                    yield return new WaitForSeconds(0.5f);
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    public void CheckAndPlayClip(Animator anim, string clipName)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clipName))
            anim.Play(clipName, 0, 0);
    }
}
