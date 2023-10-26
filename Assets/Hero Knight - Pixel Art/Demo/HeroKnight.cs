using UnityEngine;
using System.Collections;

public class HeroKnight : MonoBehaviour
{

    [SerializeField] float m_speed = 4.0f;
    [SerializeField] float m_jumpForce = 7.5f;
    [SerializeField] float m_rollForce = 6.0f;
    [SerializeField] bool m_noBlood = false;
    [SerializeField] GameObject m_slideDust;

    [SerializeField] GameObject my_healthObj;
    [SerializeField] GameObject enemy;
    public string attack_key;
    public string dodge_key;
    private HeroKnight enemyController;
    private HealthSystem healthSys;

    private Animator m_animator;
    private Rigidbody2D m_body2d;
    private Sensor_HeroKnight m_groundSensor;
    private Sensor_HeroKnight m_wallSensorR1;
    private Sensor_HeroKnight m_wallSensorR2;
    private Sensor_HeroKnight m_wallSensorL1;
    private Sensor_HeroKnight m_wallSensorL2;
    private bool m_isWallSliding = false;
    private bool m_grounded = false;
    private bool m_rolling = false;
    private int m_facingDirection = 1;
    private int m_currentAttack = 0;
    private float m_timeSinceAttack = 0.0f;
    private float m_delayToIdle = 0.0f;
    private float m_rollDuration = 0.04f;
    private float m_rollCurrentTime;
    private float globalCoolDown = 0.0f;
    private float originPos;
    private float enemyPos;

    enum State
    {
        attack,
        dodge,
        idle,
        reload,
    }
    private State nowState = State.idle;
    private bool isHitalbe = true;
    private float attackDelay = 2.0f;
    public float attackSpeed = 2.0f;
    public float reloadSpeed = 2.0f;
    public float dodgeSpeed = 2.0f;
    public float atk = 10.0f;
    public float def = 2.0f;


    private bool hitted = false;
    private bool actable = true;

    public void GetHitted(float attackDmg)
    {
        healthSys = my_healthObj.GetComponent<HealthSystem>();
        if (isHitalbe)
        {
            float dmg = CalculateDamage(attackDmg);
            healthSys.TakeDamage(dmg);
        }
    }
    float CalculateDamage(float attackDmg)
    {
        return (attackDmg - def);
    }
    IEnumerator Attack()
    {
        nowState = State.attack;
        float temp = 0.0f;
        globalCoolDown = 3.0f;
        Debug.Log("attackStart");


        Vector3 origin = transform.position;
        Vector3 newPos = origin;
        while (temp < attackDelay)
        {
            temp += Time.deltaTime * attackSpeed;
            m_body2d.velocity = new Vector2((enemyPos - transform.position.x) * 1 * m_speed, m_body2d.velocity.y);
            yield return null;
        }
        isHitalbe = false;
        Debug.Log("attack!");
        enemyController.GetHitted(atk);
        m_currentAttack++;

        if (m_currentAttack > 3)
            m_currentAttack = 1;
        // Call one of three attack animations "Attack1", "Attack2", "Attack3"
        m_animator.SetTrigger("Attack" + m_currentAttack);
        yield return new WaitForSeconds(0.06f);
        isHitalbe = true;
        temp = 0.0f;
        nowState = State.reload;
        while (globalCoolDown > temp)
        {
            temp += Time.deltaTime * reloadSpeed;
            yield return null;
        }
        globalCoolDown = 0.0f;
        Debug.Log("attackend");
        nowState = State.idle;
    }
    IEnumerator Dodge()
    {
        nowState = State.dodge;
        isHitalbe = false;
        float invincibleTime = 0.04f;
        globalCoolDown = 1.0f;
        float temp = 0.0f;

        m_rolling = true;
        m_rollCurrentTime = 0.0f;
        m_animator.SetTrigger("Roll");
        Vector3 origin = transform.position;
        Vector3 newPos = origin;
        Debug.Log("dodge start");
        while (temp < invincibleTime)
        {
            temp += Time.deltaTime;
            if (m_rolling)
            {
                m_body2d.velocity = new Vector2(-16f * m_speed * m_facingDirection, m_body2d.velocity.y);
            }
            yield return null;
        }
        temp = 0.0f;
        isHitalbe = true;
        nowState = State.reload;
        while (globalCoolDown > temp)
        {

            temp += Time.deltaTime * reloadSpeed;
            yield return null;
        }
        Debug.Log("dodge end");
        nowState = State.idle;
        globalCoolDown = 0.0f;
    }


    // Use this for initialization
    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR1 = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR2 = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL1 = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL2 = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();
        m_facingDirection = (GetComponent<SpriteRenderer>().flipX) ? -1 : 1;

        enemyController = enemy.GetComponent<HeroKnight>();

        originPos = transform.position.x;
        enemyPos = enemy.transform.position.x;

    }

    // Update is called once per frame
    void Update()
    {
        if (enemyPos - transform.position.x > 0)
        {
            m_facingDirection = 1;
            GetComponent<SpriteRenderer>().flipX = false;
        }
        else
        {
            m_facingDirection = -1;
            GetComponent<SpriteRenderer>().flipX = true;
        }
        // Increase timer that controls attack combo
        m_timeSinceAttack += Time.deltaTime;

        // Increase timer that checks roll duration
        if (m_rolling)
            m_rollCurrentTime += Time.deltaTime;

        // Disable rolling if timer extends duration
        if (m_rollCurrentTime > m_rollDuration)
            m_rolling = false;

        //Check if character just landed on the ground
        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
        }

        //Check if character just started falling
        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        // -- Handle input and movement --
        float inputX = Input.GetAxis("Horizontal");

        // Swap direction of sprite depending on walk direction


        // Move
        if (!m_rolling)
            m_body2d.velocity = new Vector2(inputX * m_speed, m_body2d.velocity.y);

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        // -- Handle Animations --
        //Wall Slide
        m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State());
        m_animator.SetBool("WallSlide", m_isWallSliding);

        //Death
        if (Input.GetKeyDown("e") && !m_rolling)
        {
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");
        }

        //Hurt
        else if (Input.GetKeyDown("q") && !m_rolling)
            m_animator.SetTrigger("Hurt");

        //Attack
        else if (Input.GetMouseButtonDown(0) && m_timeSinceAttack > 0.25f && !m_rolling)
        {
            m_currentAttack++;

            // Loop back to one after third attack
            if (m_currentAttack > 3)
                m_currentAttack = 1;

            // Reset Attack combo if time since last attack is too large
            if (m_timeSinceAttack > 1.0f)
                m_currentAttack = 1;

            // Call one of three attack animations "Attack1", "Attack2", "Attack3"
            m_animator.SetTrigger("Attack" + m_currentAttack);

            // Reset timer
            m_timeSinceAttack = 0.0f;
        }

        else if (Input.GetKeyDown(attack_key) && nowState == State.idle)
        {

            StartCoroutine(Attack());

        }
        else if (Input.GetKeyDown(dodge_key) && nowState == State.idle)
        {
            StartCoroutine(Dodge());
        }

        // Block
        else if (Input.GetMouseButtonDown(1) && !m_rolling)
        {
            m_animator.SetTrigger("Block");
            m_animator.SetBool("IdleBlock", true);
        }

        else if (Input.GetMouseButtonUp(1))
            m_animator.SetBool("IdleBlock", false);

        // Roll
        else if (Input.GetKeyDown("left shift") && !m_rolling && !m_isWallSliding)
        {
            m_rolling = true;
            m_animator.SetTrigger("Roll");
            m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);
        }


        //Jump
        else if (Input.GetKeyDown("space") && m_grounded && !m_rolling)
        {
            m_animator.SetTrigger("Jump");
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);
        }

        //Run
        else if (Mathf.Abs(inputX) > Mathf.Epsilon)
        {
            // Reset timer
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
        }

        //Idle
        if (nowState == State.reload || nowState == State.idle)
        {
            // Prevents flickering transitions to idle
            m_delayToIdle -= Time.deltaTime;
            m_body2d.velocity = new Vector2((originPos - transform.position.x) * reloadSpeed, m_body2d.velocity.y);
            if (m_delayToIdle < 0)
                m_animator.SetInteger("AnimState", 0);
        }
    }

    // Animation Events
    // Called in slide animation.
    void AE_SlideDust()
    {
        Vector3 spawnPosition;

        if (m_facingDirection == 1)
            spawnPosition = m_wallSensorR2.transform.position;
        else
            spawnPosition = m_wallSensorL2.transform.position;

        if (m_slideDust != null)
        {
            // Set correct arrow spawn position
            GameObject dust = Instantiate(m_slideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
            // Turn arrow in correct direction
            dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
        }
    }
}
