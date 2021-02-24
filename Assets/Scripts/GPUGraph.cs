using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    public enum TransitionMode { Cycle, Random }
    
    private static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        timeId = Shader.PropertyToID("_Time"),
        transitionProgressId = Shader.PropertyToID("_TransitionProgress");
    
    private const int maxResolution = 2000;

    
    [SerializeField]
    private Material material = default;

    [SerializeField]
    private Mesh mesh = default;
    
    [SerializeField]
    private ComputeShader computeShader = default;
    

    [SerializeField, Range(10, maxResolution)]
    private int resolution = 10;
    
    [SerializeField]
    private FunctionLibrary.FunctionName functionName = default;

    [SerializeField]
    TransitionMode transitionMode = TransitionMode.Cycle;
    
    [SerializeField, Min(0f)]
    private float functionDuration = 1f, transitionDuration = 1f;
    
    private float duration;
    bool transitioning;
    FunctionLibrary.FunctionName transitionFunction;
    
    ComputeBuffer positionsBuffer;

    void OnEnable ()
    {
        positionsBuffer = new ComputeBuffer(resolution * resolution, 3 * 4);
    }

    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

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
        UpdateFunctionOnGPU();
    }
    
    private void PickNextFunction ()
    {
        functionName = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(functionName) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(functionName);
    }
    
    void UpdateFunctionOnGPU ()
    {
        float step = 2f / resolution;
        
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);
        if (transitioning) {
            computeShader.SetFloat(
                transitionProgressId,
                Mathf.SmoothStep(0f, 1f, duration / transitionDuration)
            );
        }
        
        var kernelIndex = (int)functionName + (int)(transitioning ? transitionFunction : functionName) * FunctionLibrary.FunctionCount;
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);
        
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);
        
        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);
        
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        
        Graphics.DrawMeshInstancedProcedural(
            mesh, 0, material, bounds, resolution * resolution
        );
    }
}
