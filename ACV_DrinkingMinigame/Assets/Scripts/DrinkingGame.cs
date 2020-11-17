using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Experimental;
using UnityEditor.Scripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class DrinkingGame : MonoBehaviour
{
    [SerializeField] private float newMugDelay = 1f;
    [SerializeField] private float failedChugDelay = 0.3f;
    [SerializeField, Range(0f, 0.3f)] private float cutoffMultiplier = 0.12f;
    [SerializeField] private int chugsPerMug = 10;
    [SerializeField, Range(0, 1f)] private float drinkTimingLimit = 0.6f;
    [SerializeField] private GameObject scalingCircle = null; //for later visual feedback
    [SerializeField] private float timeLimit = 1f;
    [SerializeField] private float wobbleTime = 0.5f;
    [SerializeField, Min(0.5f)] private float wobbleDelayMin = 1f;
    [SerializeField] private float wobbleDelayMax = 4f;
    [SerializeField] private GameObject right = null;
    [SerializeField] private GameObject left = null;
    [SerializeField] private int chugsBeforeWobble = 14;
    [SerializeField] private float wobbleDelay = 1.5f;

    private int chugs = 0;
    private int mugsFinished = 0;
    private float currentTimeLimit = 0f;
    private float timeCounter = 0f;
    private float wobbleTimeCounter = 0f;
    private bool drinking = false;
    private bool gameOngoing = false;
    private bool staggered = false;
    

    private Material objectMaterial = null;
    
    
    void Start()
    {
        currentTimeLimit = timeLimit;
        gameOngoing = true;
        objectMaterial = scalingCircle.GetComponent<MeshRenderer>().material;
        StartCoroutine(DrinkingLoop(0));
    }
    
    private bool Chug()
    {
        return timeCounter > currentTimeLimit * drinkTimingLimit;
    }

    private void ResetTimer()
    {
        currentTimeLimit = timeLimit;
    }

    private void LowerTimeLimit()
    {
        currentTimeLimit = currentTimeLimit * (1 - cutoffMultiplier);
    }

    private void EndGame()
    {
        scalingCircle.SetActive(false);
        Debug.Log("you win");
    }

    private IEnumerator DrinkingLoop(int incrementor)
    {
        incrementor++;
        if (incrementor > 20)
        {
            throw new NullReferenceException();
        }
        yield return new WaitForSeconds(0.2f);
        scalingCircle.SetActive(true);
        timeCounter = 0f;
        drinking = true;
        staggered = false;
        bool success = false;
        while (timeCounter < currentTimeLimit && staggered == false)
        {
            scalingCircle.transform.localScale = Vector3.one * (1- timeCounter/currentTimeLimit);
            if (timeCounter > currentTimeLimit * drinkTimingLimit)
            {
                objectMaterial.color = Color.green;
            }
            else
            {
                objectMaterial.color = Color.red;
            }
            timeCounter += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (Chug())
                {
                    chugs++;
                    success = true;
                    if (chugs + mugsFinished * chugsPerMug == chugsBeforeWobble)
                    {
                        StartCoroutine(RandomWobble());
                    }
                }
                break;
            }
            yield return null;
        }

        scalingCircle.SetActive(false);
        if ( chugs / chugsPerMug == 1)
        {
            drinking = false;
            Debug.Log("new mug");
            ResetTimer();
            StartCoroutine(NewMugDelay(incrementor));
        }
        else if (staggered)
        {
            drinking = false;
            ResetTimer();

            StartCoroutine(Delay(wobbleDelay, incrementor));
        }
        else if (success)
        {
            LowerTimeLimit();
            StartCoroutine(DrinkingLoop(incrementor));
        }
        else
        {
            drinking = false;
            ResetTimer();
            StartCoroutine(Delay(failedChugDelay, incrementor));
        }
        
    }

    private IEnumerator NewMugDelay(int incrementor)
    {
        mugsFinished++;
        if (mugsFinished == 3)
        {
            gameOngoing = false;
            EndGame();
        }
        else
        {
            chugs = 0;
            yield return new WaitForSeconds(newMugDelay);
             StartCoroutine(DrinkingLoop(incrementor));
        }
    }

    private IEnumerator Delay(float delay, int incrementor)
    {
        yield return new WaitForSeconds(delay);

        StartCoroutine(DrinkingLoop(incrementor));
    }

    private IEnumerator RandomWobble()
    {
        right.SetActive(false);
        left.SetActive(false);
        if (gameOngoing)
        {
            right.SetActive(false);
            left.SetActive(false);
            while (drinking == false)
            {
                yield return null;
            }
            yield return new WaitForSeconds(GetRandomValue());

            int type = Random.Range(1, 3);
            if (type == 1)
            {
                StartCoroutine(WobbleTimer(KeyCode.A, KeyCode.D, left));
            }
            else
            {
                StartCoroutine(WobbleTimer(KeyCode.D, KeyCode.A, right));
            }

        }
    }

    private IEnumerator WobbleTimer(KeyCode correctKey, KeyCode incorrectKey, GameObject button)
    {
        bool success = false;
        button.SetActive(true);
        Vector3 originalScale = button.transform.localScale;
        while (wobbleTimeCounter <= wobbleTime && gameOngoing == true)
        {
            button.transform.localScale *= wobbleTimeCounter < wobbleTime / 2 ? (1 + 1.05f * Time.deltaTime) : (1 - 0.95f * Time.deltaTime);
            
            if (drinking == false)
            {
                success = true;
                break;
            }
            if (Input.GetKeyDown(correctKey))
            {
                success = true;
                break;
            }
            else if (Input.GetKeyDown(incorrectKey))
            {
                break;
            }
            
            wobbleTimeCounter += Time.deltaTime;
            yield return null;
        }

        wobbleTimeCounter = 0f;

        if (success == false)
        {
            staggered = true;
        }
        button.transform.localScale = originalScale;
        StartCoroutine(RandomWobble());
    }

    private float GetRandomValue()
    {
        return Random.Range(wobbleDelayMin, wobbleDelayMax);
    }
}
