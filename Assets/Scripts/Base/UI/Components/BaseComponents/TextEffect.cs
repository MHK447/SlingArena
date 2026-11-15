using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

[EffectPath("Effect/TextEffect", false, true)]
public class TextEffect : Effect
{
    [SerializeField]
    private Animator DoubleSpawnAnim;

    [SerializeField]
    private  ParticleSystem Particle;


    [SerializeField]
    private TextMeshProUGUI DescText;




    public void Set(string desc , Color color )
    {
        DescText.text = desc;
        DescText.color = color;

        var main = Particle.main;
        main.startColor = color;

        DoubleSpawnAnim.Play("MultiShot", 0, 0f);
    }

    public void Set(string desc, Color color, Vector3 offset)
    {
        Set(desc, color);
        transform.localPosition += offset;
    }
}

