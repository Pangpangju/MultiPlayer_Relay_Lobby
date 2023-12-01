using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        if (Input.GetAxisRaw("Horizontal") != 0  || Input.GetAxisRaw("Vertical") != 0)
        {
            transform.Translate(Input.GetAxisRaw("Horizontal")*speed * Time.deltaTime, 0, Input.GetAxisRaw("Vertical") * speed * Time.deltaTime);
        }
    }
}
