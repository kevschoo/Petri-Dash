using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// enums
using EnumHolder;
using TMPro;
using UnityEngine.UI;

public class PlayerScript : BaseOrganism
{

    Vector2 m_velocity;

    [SerializeField]
    List<Offspring> _children;

    //True means independent thinking, false means flock mode
    [SerializeField]
    bool m_toggleOffspringBehaviour;

    [SerializeField]
    int _foodRequired;

    [SerializeField]
    GameObject _offspringPrefab;

    [SerializeField]
    bool m_controlsEnabled = true;

    public GameObject scienceMan;

    const float FOOD_DECAY_TIME = 3.0f;
    const int FOOD_DECAY_AMOUNT = 1;

    float alpha;
    bool imageShown = false;

    [SerializeField] bool isDashing = false;
    [SerializeField] float _dashSpeed = 35f;
    [SerializeField] float _dashCooldown = 2.5f;
    [SerializeField] float _dashLength = 2.5f;
    [SerializeField] bool isBeingPushed = false;
    [SerializeField] float _pushSpeed = 25f;
    [SerializeField] Vector2 pushDirection;

    new void Awake()
    {
        base.Awake();
        alpha = scienceMan.GetComponent<Image>().color.a;
        _stats.Food = _foodRequired - 1;

        StartCoroutine(DecayFood());
    }


    private IEnumerator DecayFood()
    {
        while (_stats.IsAlive)
        {
            yield return new WaitForSeconds(FOOD_DECAY_TIME);

            if (_stats.Food > 0)
            {
                _stats.Food -= FOOD_DECAY_AMOUNT;
                if (_stats.Food < 0) 
                    _stats.Food = 0;
            }
            else// (_stats.Food <= 0)
            {
                _stats.Health--;
            }
            if (_stats.Health <= 0)
                    StartCoroutine(Die());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (m_controlsEnabled)
        {
            m_velocity = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f);
            _rigidbody2D.velocity = m_velocity * _stats.Speed;
            if (Input.GetKeyUp(KeyCode.Space) && (this.Stats.Food > 2) && (isDashing == false))
            {
                // && this.Stats.Food > 0 && (isDashing = false)
                
                StartCoroutine(Dash());
            }
        }

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Application.Quit();
        }
        //transform.position += m_velocity * m_speed * Time.deltaTime;

        if (Input.GetKeyUp(KeyCode.X))
        {
            m_toggleOffspringBehaviour = !m_toggleOffspringBehaviour;
        }

        if (Input.GetKeyUp(KeyCode.C))
        {
            ProduceOffspring();
        }
        //Edited Code ---------------------------------------------------------------------------------------------------
        //TO DO: Add Splitting or dashing
        
        

    }
    void FixedUpdate()
    {
        if(isBeingPushed)
        {
            this._rigidbody2D.velocity += pushDirection * _pushSpeed * Time.fixedDeltaTime;
        }
        if(isDashing) //I thought about just changing the push variables instead of making additional dash variables but i decided it would be less clunky like this
        {
            this._rigidbody2D.velocity += pushDirection * _dashSpeed * Time.fixedDeltaTime;
        }
    }
    IEnumerator Dash()
    {
        this.Stats.Food -= 2;
        isDashing = true;
        Vector2 DashVector = _rigidbody2D.velocity.normalized;
        StartCoroutine(HaltControls(_dashLength));
        this.pushDirection = DashVector;
        //_rigidbody2D.MovePosition(_rigidbody2D.position + DashVector);
        yield return new WaitForSeconds(_dashCooldown);
        isDashing = false;
    }
    IEnumerator PotatoScientistImageFadeIn()
    {
        imageShown = true;
        while (alpha < 1)
        {
            alpha += 0.09f;
            scienceMan.GetComponent<Image>().color = new Color(scienceMan.GetComponent<Image>().color.r, scienceMan.GetComponent<Image>().color.g, scienceMan.GetComponent<Image>().color.b,alpha);
            yield return new WaitForSeconds(0.1f);

            if (alpha >= 1)
            {
                new WaitForSeconds(3f);
                StartCoroutine(PotatoScientistImageFadeOut());
                yield break;
            }
        }
    }

    IEnumerator PotatoScientistImageFadeOut()
    {
        while (alpha > 0)
        {
            alpha -= 0.09f;
            scienceMan.GetComponent<Image>().color = new Color(scienceMan.GetComponent<Image>().color.r, scienceMan.GetComponent<Image>().color.g, scienceMan.GetComponent<Image>().color.b, alpha);
            yield return new WaitForSeconds(0.1f);

            if (alpha <= 0)
            {
                imageShown = false;
                yield break;
            }
        }
    }


    private void ProduceOffspring()
    {
        if (_stats.Food >= _foodRequired)
        {
            _stats.Food -= (int)(_foodRequired * 0.75f);
            ScaleWithFood();

            // instantiate the offspring
            GameObject clone = Instantiate(_offspringPrefab,
                                            transform.position,
                                            transform.rotation);
            Offspring offspring = clone.GetComponent<Offspring>();
            offspring.AssignParent(this, _spriteRenderer.sprite,
                                         _spriteRenderer.color);

            foreach (Offspring child in _children)
            {
                child.AssignSibling(offspring);
            }

            _children.Add(offspring);

            AudioManager.Instance().PlaySoundEffect(AudioManager.SoundEffect.Spawning);
        }
    }
    void OnTriggerEnter2D(Collider2D collision)
    {

        
        if (collision.CompareTag("Superfood"))
        {
            Trait trait = Utility.PickTrait(_stats.Luck);
            _traits.Add(trait);

            if (!imageShown)
            {
                StartCoroutine(PotatoScientistImageFadeIn());
            }

            Destroy(collision.gameObject);
            AudioManager.Instance().PlaySoundEffect(AudioManager.SoundEffect.Bite);
        }
        else if (collision.CompareTag("Food"))
        {
            _stats.HarvestFood(collision.gameObject.GetComponent<FoodScript>().GetFood(false));
            ScaleWithFood();
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.AddForce(Utility.BounceBack(transform.position, collision.transform.position));
            StartCoroutine(HaltControls(.1f));

            AudioManager.Instance().PlaySoundEffect(AudioManager.SoundEffect.Bite);
        }
        else if (collision.CompareTag("SingleCelledOrganism"))
        {
            SingleCelledOrganism organism = collision.GetComponent<SingleCelledOrganism>();
            if (organism.CanAttack)
            {
                print("Player was attacked by a Single Cell");
                _stats.TakeDamage(organism.Stats.Damage);
                organism.Stats.Food += _stats.StealFood(organism.Stats.Damage);
            }

            if (CanAttack && _stats.IsAlive)
            {
                print("Player is attacking a Single Cell");
                organism.Stats.TakeDamage(_stats.Damage);
                _stats.Food += organism.Stats.StealFood(_stats.Damage);
            }

            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.AddForce(Utility.BounceBack(transform.position, collision.transform.position));
            StartCoroutine(HaltControls(.1f));

            AudioManager.Instance().PlaySoundEffect(AudioManager.SoundEffect.Bite);
        }
        else if (collision.CompareTag("Offspring"))
        {
            Offspring offspring = collision.GetComponent<Offspring>();
            if (_children.Contains(offspring))
                return;

            if (offspring.CanAttack)
            {
                print("Player was attacked by an offspring");
                _stats.TakeDamage(offspring.Stats.Damage);
                offspring.Stats.Food += _stats.StealFood(offspring.Stats.Damage);
            }

            if (CanAttack && _stats.IsAlive)
            {
                print("Player is attacking an offspring");
                offspring.Stats.TakeDamage(_stats.Damage);
                _stats.Food += offspring.Stats.StealFood(_stats.Damage);

            }
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.AddForce(Utility.BounceBack(transform.position, collision.transform.position));
            StartCoroutine(HaltControls(.1f));
            AudioManager.Instance().PlaySoundEffect(AudioManager.SoundEffect.Bite);
        }
        //Edited Code ---------------------------------------------------------------------------------------------------
        else if (collision.CompareTag("EnvironmentalHazard"))
        {
            _stats.TakeDamage(5);
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.AddForce(Utility.BounceBack(transform.position, collision.transform.position));
            StartCoroutine(HaltControls(.25f));

            AudioManager.Instance().PlaySoundEffect(AudioManager.SoundEffect.Bite);
        }
        else if (collision.CompareTag("PushableHazard"))
        {
            Hazard_Block BHaz = collision.GetComponent<Hazard_Block>();
            if(BHaz.isDangerous == true)
            {
            Debug.Log("Damaged by moving block");
            _stats.TakeDamage(5);
            StartCoroutine(HaltControls(2));
            isBeingPushed = true;
            _rigidbody2D.AddForce(Utility.BounceBack(transform.position, collision.transform.position));
            }
        }
        else if (collision.CompareTag("MovementHazard"))
        {
            Debug.Log("MovementHazard entered");
            StartCoroutine(HaltControls(2));
            isBeingPushed = true;
            Hazard_Movement MHaz = collision.GetComponent<Hazard_Movement>();
            this.pushDirection = MHaz.pushDirection;
            StartCoroutine(StopPush(2));

        }
        
    }
   
    private IEnumerator StopPush(float duration)
    {
        yield return new WaitForSeconds(duration);
        isBeingPushed = false;
    }

    IEnumerator HaltControls(float HaltTime)
    {
        m_controlsEnabled = false;
        yield return new WaitForSeconds(HaltTime);
        m_controlsEnabled = true;
    }


    private IEnumerator Die()
    {
        for (int i = 0; i < _children.Count; i++)
        {
            Destroy(_children[i]);

            yield return new WaitForEndOfFrame();
        }

        m_controlsEnabled = false;
        GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>().GameOver();
    }



    public int ChildCount { get { return _children.Count; } }
    public float FoodPercentage { get => Stats.Food / (float)_foodRequired; }
}
