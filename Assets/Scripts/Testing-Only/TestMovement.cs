using UnityEngine;
using UnityEngine.InputSystem;
public class TestMovement : MonoBehaviourPlus
{
    public override string prettyName => "Test Movement";
    public override string description => "you can delete this later";
    
    Rigidbody2D rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Keyboard.current.aKey.wasPressedThisFrame)
            rb.AddForceX(-5f, ForceMode2D.Impulse);
        if (Keyboard.current.dKey.wasPressedThisFrame)
            rb.AddForceX(5f, ForceMode2D.Impulse);
        if (Keyboard.current.wKey.wasPressedThisFrame)
            rb.AddForceY(10f, ForceMode2D.Impulse);
        if (Keyboard.current.dKey.wasPressedThisFrame)
            rb.AddForceY(-3f, ForceMode2D.Impulse);
    }
    
}
