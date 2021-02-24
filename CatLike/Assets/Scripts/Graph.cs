using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Graph : MonoBehaviour
{
    public enum TransitionMode { Cycle, Random }
    
    [SerializeField]
    private Transform pointPrefab;
    
    [SerializeField, Range(10, 200)]
    private int resolution = 10;
    
    [SerializeField]
    private FunctionLibrary.FunctionName functionName = default;

    [SerializeField]
    TransitionMode transitionMode = TransitionMode.Cycle;
    
    [SerializeField, Min(0f)]
    private float functionDuration = 1f, transitionDuration = 1f;
    
    private Transform[] points;
    private float duration;
    bool transitioning;
    FunctionLibrary.FunctionName transitionFunction;
    
    // private void Awake ()
    // {
    //     float step = 2f / resolution;
    //     var scale = Vector3.one * step;
    //     points = new Transform[resolution * resolution];
    //
    //     for (int i = 0; i < points.Length; i++)
    //     {
    //         Transform point = Instantiate(pointPrefab, transform, false);
    //         point.localScale = scale;
    //         points[i] = point;
    //     }
    // }

    private void Update()
    {
        duration += Time.deltaTime;

        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            }
        }
        else if (duration >= functionDuration)
        {
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = functionName;
            PickNextFunction();
        }
        
        if (transitioning)
            UpdateFunctionTransition();
        else
            UpdateFunction();
    }
    
    private void PickNextFunction () {
        functionName = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(functionName) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(functionName);
    }

    private void UpdateFunction()
    {
        FunctionLibrary.Function function = FunctionLibrary.GetFunction(functionName);
        float time = Time.time;
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;

        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }

            float u = (x + 0.5f) * step - 1f;
            points[i].localPosition = function(u, v, time);
        }
    }
    
    private void UpdateFunctionTransition ()
    {
        FunctionLibrary.Function
            from = FunctionLibrary.GetFunction(transitionFunction),
            to = FunctionLibrary.GetFunction(functionName);
        float progress = duration / transitionDuration;
        float time = Time.time;
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;

        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }

            float u = (x + 0.5f) * step - 1f;
            points[i].localPosition = FunctionLibrary.Morph(u, v, time, from, to, progress);
        }
    }
}
