using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float MovementSpeed = 1;

    public float JumpForce = 1;

    private Rigidbody2D rb;

    public DialogueManager dialogueManager;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if(dialogueManager.dialogueIsPlaying == true)
        {
            return;
        }

        BasicMovement();
    }

    public void BasicMovement()
    {
        var movement = Input.GetAxis("Horizontal");

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            transform.position += new Vector3(movement, 0, 0) * Time.deltaTime * MovementSpeed;
        }

        if (Input.GetKeyDown(KeyCode.Space) && Mathf.Abs(rb.velocity.y) < 0.001f)
        {
            rb.AddForce(new Vector2(0, JumpForce), ForceMode2D.Impulse);
        }
    }
}