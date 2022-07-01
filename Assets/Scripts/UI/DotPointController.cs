using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DotPointController : MonoBehaviour
{
    [SerializeField]
    float animationTime;
    [Space]
    [SerializeField]
    Image fill;
    [SerializeField]
    Gradient colors;

    bool marked;
    float timer;

    void Start()
    {
        marked = false;
        timer = 0;
        fill.color = colors.Evaluate(0);
    }

    // Update is called once per frame
    void Update()
    {
        if(timer > 0)
        {
            timer -= Time.deltaTime;
            if(timer > 0)
            {
                fill.color = colors.Evaluate(1 - timer / animationTime);
            }
            else
            {
                fill.color = colors.Evaluate(1);
            }
        }
    }

    public void MarkOff()
    {
        if (!marked)
        {
            marked = true;
            timer = animationTime;
        }
    }
}
