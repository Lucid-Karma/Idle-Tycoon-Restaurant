using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionImages : MonoBehaviour
{
    public GameObject Crying;
    public GameObject Satisfied;
    public GameObject Happy;

    void OnEnable()
    {
        ScoreManager.OnTotalScoreBad.AddListener(Cry);
        ScoreManager.OnTotalScoreNotBad.AddListener(Satisfy);
        ScoreManager.OnTotalScoreGood.AddListener(MakeHappy);
    }
    void OnDisable()
    {
        try
        {
            ScoreManager.OnTotalScoreBad.RemoveListener(Cry);
            ScoreManager.OnTotalScoreNotBad.RemoveListener(Satisfy);
            ScoreManager.OnTotalScoreGood.RemoveListener(MakeHappy);
        }
        catch(NullReferenceException ex)
        {
            Debug.LogError("ScoreManager may be null " + ex.ToString());
        }
    }

    void Start()
    {
        Satisfied.SetActive(false);
        Happy.SetActive(false);
    }

    void Cry()
    {
        Happy.SetActive(false);
        Satisfied.SetActive(false);
        Crying.SetActive(true);
    }

    void Satisfy()
    {
        Crying.SetActive(false);
        Happy.SetActive(false);
        Satisfied.SetActive(true);
    }

    void MakeHappy()
    {
        Crying.SetActive(false);
        Satisfied.SetActive(false);
        Happy.SetActive(true);
    }
}
