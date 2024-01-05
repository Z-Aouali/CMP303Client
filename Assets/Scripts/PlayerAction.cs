using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    [SerializeField]
    private Gun gun;

    public void OnShoot() {
        gun.Shoot();

    }

}
