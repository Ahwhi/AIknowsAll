using UnityEngine;
using Unity.InferenceEngine;
using System.Collections.Generic;

public class ModelInference : MonoBehaviour
{
    [Header("Model")]
    public ModelAsset modelAsset;

    [Header("Classes")]
    public string[] classes = {
        "cat","dog","fish","bird","rabbit",
        "horse","cow","pig","duck","frog",
        "lion","elephant","shark","snake","butterfly",
        "car","airplane","bicycle","bus","train",
        "helicopter","ambulance",
        "apple","banana","pizza","cake","ice cream",
        "hamburger","grapes","watermelon",
        "tree","sun","cloud","flower","mushroom","mountain",
        "house","star","umbrella","clock","guitar",
        "cup","book","pencil",
        "hand","eye","face","smiley face","crown"
    };

    [Header("References")]
    public UIManager      uiManager;
    public CNNVisualizer  cnnVisualizer;
    public DrawingCanvas  drawingCanvas;  // 결과 전달용

    private Worker _worker;
    private Model  _runtimeModel;



    // 결과 저장 해놓기
    private float _lastTopProb = 0f;
    private string _lastTopClass = "";

    void Start()
    {
        _runtimeModel = ModelLoader.Load(modelAsset);
        _worker       = new Worker(_runtimeModel, BackendType.GPUCompute);
    }

    public void Predict(float[] pixelData)
    {
        using var inputTensor = new Tensor<float>(new TensorShape(1, 1, 28, 28), pixelData);
        _worker.Schedule(inputTensor);

        var outputTensor = (_worker.PeekOutput("output") as Tensor<float>).ReadbackAndClone();
        float[] logits = new float[classes.Length];
        for (int i = 0; i < classes.Length; i++)
            logits[i] = outputTensor[i];
        outputTensor.Dispose();

        float[] probs = Softmax(logits);

        if (cnnVisualizer != null)
        {
            var feat1 = (_worker.PeekOutput("feat1") as Tensor<float>).ReadbackAndClone();
            var feat2 = (_worker.PeekOutput("feat2") as Tensor<float>).ReadbackAndClone();
            var feat3 = (_worker.PeekOutput("feat3") as Tensor<float>).ReadbackAndClone();
            cnnVisualizer.UpdateFeatureMaps(feat1, feat2, feat3, probs);
            feat1.Dispose();
            feat2.Dispose();
            feat3.Dispose();
        }

        var results = new List<(string label, float prob)>();
        for (int i = 0; i < classes.Length; i++)
            results.Add((classes[i], probs[i]));
        results.Sort((a, b) => b.prob.CompareTo(a.prob));
        _lastTopProb = results[0].prob;
        _lastTopClass = results[0].label;
        uiManager.ShowResult(results.GetRange(0, 3));

        // GameManager에 결과 전달
        if (drawingCanvas != null)
            drawingCanvas.NotifyResult(results[0].prob, results[0].label);
    }

    public void ClearResult()
    {
        uiManager.ClearResult();
        if (cnnVisualizer != null) cnnVisualizer.ClearAll();
    }

    private float[] Softmax(float[] logits)
    {
        float max = float.MinValue;
        foreach (var v in logits) if (v > max) max = v;
        float sum = 0f;
        float[] exp = new float[logits.Length];
        for (int i = 0; i < logits.Length; i++) { exp[i] = Mathf.Exp(logits[i] - max); sum += exp[i]; }
        for (int i = 0; i < exp.Length; i++) exp[i] /= sum;
        return exp;
    }

    public void SubmitCurrentResult() {
        GameManager.Instance?.SubmitCurrentResult(_lastTopProb, _lastTopClass);
    }

    void OnDestroy() { _worker?.Dispose(); }
}
