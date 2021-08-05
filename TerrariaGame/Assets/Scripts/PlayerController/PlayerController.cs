using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public TileClass selectedTile;
    public int playerRange;
    public Vector2Int mousePos;

    public float moveSpeed;
    public float jumpForce;
    public bool onGround;

    private Rigidbody2D rb;
    private Animator anim;

    private float horizontal;
    public bool hit;
    public bool place;

    [HideInInspector]
    public Vector2 spawnPos;

    public TerrainGeneration terrainGenerator;

    public void Spawn()
    {
        GetComponent<Transform>().position = spawnPos;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Ground"))
            onGround = true;
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Ground"))
            onGround = false;
    }

    private void FixedUpdate()
    {
        horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        float jump = Input.GetAxis("Jump");

        Vector2 movement = new Vector2(horizontal * moveSpeed, rb.velocity.y);

        hit = Input.GetMouseButton(0);
        place = Input.GetMouseButton(1);

        if (Vector2.Distance(transform.position, mousePos) <= playerRange &&
            Vector2.Distance(transform.position, mousePos) > 1f)
        {
            if (hit)
                terrainGenerator.RemoveTile(mousePos.x, mousePos.y);            
            else if (place)
                terrainGenerator.CheckTile(selectedTile, mousePos.x, mousePos.y, false);
        }        

        if (horizontal > 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else if (horizontal < 0)
            transform.localScale = new Vector3(1, 1, 1);

        if (vertical > 0.1f || jump > 0.1f)
        {
            if (onGround)
                movement.y = jumpForce;
        }

        rb.velocity = movement;
    }

    private void Update()
    {
        mousePos.x = Mathf.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition).x - 0.5f);
        mousePos.y = Mathf.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition).y - 0.5f);

        anim.SetFloat("horizontal", horizontal);
        anim.SetBool("hit", hit || place);
    }
}