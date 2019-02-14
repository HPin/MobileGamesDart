using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * Used for background animation (of a gif file)
 */
public class BgAnimationScript : MonoBehaviour
{
    public Sprite[] animatedImages;
    public Image animatedImageObj;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        animatedImageObj.sprite = animatedImages[(int)(Time.time * 10) % animatedImages.Length];
    }
}
