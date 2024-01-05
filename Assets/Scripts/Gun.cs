using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))] 

public class Gun : MonoBehaviour
{

    [SerializeField]
    private bool BulletSpread = true;
    [SerializeField]
    private Vector3 BulletSpreadVar = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField]
    private ParticleSystem ShotSystem;
    [SerializeField]
    private Transform BulletSpawn;
    [SerializeField]
    private ParticleSystem ImpactSystem;
    [SerializeField]
    private TrailRenderer BulletTrail;
    [SerializeField]
    private float ShotDelay = 0.5f;
    [SerializeField]
    private LayerMask Mask;

    

    private Animator animator;

    private float LastShotTime;


    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Shoot()
    {
        if(LastShotTime + ShotDelay < Time.time)
        {
            //animator.SetBool("IsShooting", true);
            ShotSystem.Play();
            Vector3 direction = GetDirection();

            if(Physics.Raycast(BulletSpawn.position, direction, out RaycastHit hit, 25, Mask))
            {
                TrailRenderer trail = Instantiate(BulletTrail, BulletSpawn.position, Quaternion.identity);

                LastShotTime = Time.time;

                StartCoroutine(SpawnTrail(trail, hit));

            }
        }

    }

    private Vector3 GetDirection()
    {
        Vector3 direction = transform.forward;

        if (BulletSpread)
        {
            direction += new Vector3(
                Random.Range(-BulletSpreadVar.x, BulletSpreadVar.x),
                Random.Range(-BulletSpreadVar.y, BulletSpreadVar.y),
                Random.Range(-BulletSpreadVar.z, BulletSpreadVar.z)
                );
            direction.Normalize();
        }
        return direction;
    }

    private IEnumerator SpawnTrail(TrailRenderer Trail, RaycastHit hit)
    {
        float time = 0;
        Vector3 StartPos = Trail.transform.position;
        while(time < 1)
        {
            Trail.transform.position = Vector3.Lerp(StartPos, hit.point, time);
            time += Time.deltaTime / Trail.time;
            yield return null;
        }
        animator.SetBool("IsShooting", false);
        Trail.transform.position = hit.point;
        Instantiate(ImpactSystem, hit.point, Quaternion.LookRotation(hit.normal));

        Destroy(Trail.gameObject, Trail.time);
    }


}
