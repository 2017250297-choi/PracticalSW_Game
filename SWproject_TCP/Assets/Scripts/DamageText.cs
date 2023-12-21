using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    private float moveSpeed;
    private float alphaSpeed;
    private float destroyTime;

    TextMeshPro text;
    Color alpha;
    public short cases;
    public short damage;

    // Start is called before the first frame update
    void Start()
    {
        moveSpeed = 2.0f;
        alphaSpeed = 2.0f;
        destroyTime = 2.0f;

        text = GetComponent<TextMeshPro>();
        alpha = text.color;
        switch (cases)
        {
            case 0:
                if (damage <= 100)
                    text.text = damage.ToString();
                else
                    text.text = "DEAD!";
                break;
            case 1:
                text.text = "DODGE";
                break;
            case 2:
                text.text = "STUN!";
                break;
        }
        
            
        Invoke("DestroyObject", destroyTime);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(new Vector2(0, moveSpeed * Time.deltaTime));
        alpha.a = Mathf.Lerp(alpha.a, 0, Time.deltaTime * alphaSpeed);
        text.color = alpha;
    }

    private void DestroyObject()
    {
        Destroy(gameObject);
    }
}
