using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int id;
    public string username;
    public float health;
    public float maxHealth;
    public MeshRenderer model;
    public MeshRenderer gunModel;

    [SerializeField]
    Interpolator interpolator;

    [SerializeField]
    Gun gun;


    private void Update()
    {
        if (this.GetComponent<PlayerManager>().tag == "LocalPlayer")
        {
            if (Input.GetMouseButtonDown(0))
            {
                gun.Shoot();
            }
        }
    }

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;
    }

    public void SetHealth(float _health)
    {
        health = _health;

        if(health <= 0f)
        {
            Die();
        }
    }

    public void Die()
    {
        model.enabled = false;
    }

    public void Respawn()
    {
        model.enabled = true;

        SetHealth(maxHealth);

    }

    public void Move(int _tick, Vector3 _position)
    {

        interpolator.newUpdate(_tick, _position);

    }

    public void Shoot()
    {
        gun.Shoot();
    }
}
