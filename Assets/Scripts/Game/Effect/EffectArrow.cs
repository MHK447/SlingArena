using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EffectArrow : MonoBehaviour
{
    [SerializeField] private Image ArrowTemplate;
    [SerializeField] private int ArrowCount = 15;
    [SerializeField] private float SpawnWidth = 400f;
    [SerializeField] private float MoveDistance = 900f;
    [SerializeField] private float MinDuration = 1.4f;
    [SerializeField] private float MaxDuration = 2.4f;
    [SerializeField] private float MinScale = 0.7f;
    [SerializeField] private float MaxScale = 1.1f;

    private readonly List<RectTransform> arrows = new List<RectTransform>();
    private readonly List<Image> arrowImages = new List<Image>();
    private readonly List<Sequence> sequences = new List<Sequence>();
    private bool spawned = false;

    private void Awake()
    {
        EnsureSpawned();
    }

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        StopAll();
    }

    public void Play()
    {
        EnsureSpawned();
        StopAll();
        ResetAll();

        for (int i = 0; i < arrows.Count; i++)
        {
            float startDelay = (i / (float)Mathf.Max(1, arrows.Count)) * MaxDuration;
            StartArrow(arrows[i], arrowImages[i], startDelay);
        }
    }

    private void EnsureSpawned()
    {
        if (spawned) return;
        if (ArrowTemplate == null) return;
        spawned = true;

        ArrowTemplate.gameObject.SetActive(false);

        for (int i = 0; i < ArrowCount; i++)
        {
            var go = Instantiate(ArrowTemplate.gameObject, transform);
            go.name = $"Arrow_{i}";
            go.SetActive(true);
            arrows.Add((RectTransform)go.transform);
            arrowImages.Add(go.GetComponent<Image>());
        }
    }

    private void StopAll()
    {
        for (int i = 0; i < sequences.Count; i++)
            sequences[i]?.Kill();
        sequences.Clear();
    }

    private void ResetAll()
    {
        for (int i = 0; i < arrows.Count; i++)
        {
            var rt = arrows[i];
            var img = arrowImages[i];
            rt.anchoredPosition = new Vector2(0f, -MoveDistance * 0.5f);
            rt.localScale = Vector3.one;
            if (img != null)
            {
                var c = img.color;
                c.a = 0f;
                img.color = c;
            }
        }
    }

    private void StartArrow(RectTransform rt, Image img, float initialDelay)
    {
        var seq = DOTween.Sequence();
        seq.SetDelay(initialDelay);
        seq.AppendCallback(() => RandomizeStart(rt, img));
        var dur = Random.Range(MinDuration, MaxDuration);
        seq.Append(rt.DOAnchorPosY(MoveDistance * 0.5f, dur).SetEase(Ease.OutCubic).SetRelative(false));
        seq.Join(img.DOFade(1f, dur * 0.25f));
        seq.Insert(initialDelay + dur * 0.65f, img.DOFade(0f, dur * 0.35f));
        seq.SetLoops(-1, LoopType.Restart);
        sequences.Add(seq);
    }

    private void RandomizeStart(RectTransform rt, Image img)
    {
        rt.anchoredPosition = new Vector2(
            Random.Range(-SpawnWidth * 0.5f, SpawnWidth * 0.5f),
            -MoveDistance * 0.5f);
        float s = Random.Range(MinScale, MaxScale);
        rt.localScale = new Vector3(s, s, 1f);
        var c = img.color;
        c.a = 0f;
        img.color = c;
    }
}
