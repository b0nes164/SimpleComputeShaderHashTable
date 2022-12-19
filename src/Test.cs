using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Test : MonoBehaviour
{
    [SerializeField]
    private ComputeShader compute;
    [SerializeField]
    private int inputSize;
    [SerializeField]
    private bool validation;
    [SerializeField]
    private bool validationText;
    [Range(1, 1000)]
    public int batchSizeForTiming;

    private ComputeBuffer b_hash;
    private ComputeBuffer b_inputOutput;

    private int k_init;
    private int k_insert;
    private int k_lookup;
    private int k_delete;

    private static int THREAD_BLOCKS = 256;
    private uint[] inputOutputArray;
    private int hashBufferSize;
    private System.Random rand;

    void Start()
    {
        //initialize and fill test arrays based on the size of the desired input
        hashBufferSize = SizeToPow(inputSize);
        inputOutputArray = new uint[inputSize];

        compute.SetInt("e_hashBufferSize", hashBufferSize);
        compute.SetInt("e_inputSize", inputSize);

        k_init = compute.FindKernel("Initialize");
        k_insert = compute.FindKernel("Insert");
        k_lookup = compute.FindKernel("Lookup");
        k_delete = compute.FindKernel("Delete");

        b_hash = new ComputeBuffer(hashBufferSize, sizeof(uint) * 2);
        b_inputOutput = new ComputeBuffer(inputSize, sizeof(uint));

        //for simplicity we generate the random numbers on the CPU, then push to the GPU
        rand = new System.Random();
        for (int i = 0; i < inputSize; i++)
        {
            inputOutputArray[i] = (uint)rand.Next(0, int.MaxValue);
        }
        b_inputOutput.SetData(inputOutputArray);

        //assign the buffers to the kernels
        compute.SetBuffer(k_init, "b_hash", b_hash);

        compute.SetBuffer(k_insert, "b_hash", b_hash);
        compute.SetBuffer(k_insert, "b_inputOutput", b_inputOutput);

        compute.SetBuffer(k_lookup, "b_hash", b_hash);
        compute.SetBuffer(k_lookup, "b_inputOutput", b_inputOutput);

        compute.SetBuffer(k_delete, "b_hash", b_hash);
        compute.SetBuffer(k_delete, "b_inputOutput", b_inputOutput);

        //Dispatch init
        compute.Dispatch(k_init, THREAD_BLOCKS, 1, 1);
        Debug.Log("Initialization Complete, press space to begin test");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (validation)
            {
                HashValidation();
            }
            else
            {
                HashTest();
            }
        }
    }

    private void HashValidation()
    {
        uint[] validationArray = inputOutputArray;
        bool checks = true;

        compute.Dispatch(k_insert, THREAD_BLOCKS, 1, 1);
        compute.Dispatch(k_lookup, THREAD_BLOCKS, 1, 1);
        b_inputOutput.GetData(inputOutputArray);
        for (int i = 0; i < inputSize; i++)
        {
            if (inputOutputArray[i] != validationArray[i])
            {
                if (validationText)
                    Debug.LogError("EXPECTED THE SAME: " + inputOutputArray[i] + ", " + validationArray[i]);
                checks = false;
            }
        }

        compute.Dispatch(k_delete, THREAD_BLOCKS, 1, 1);
        compute.Dispatch(k_lookup, THREAD_BLOCKS, 1, 1);
        b_inputOutput.GetData(inputOutputArray);
        for (int i = 0; i < inputSize; i++)
        {
            if (inputOutputArray[i] != 0xffffffff)
            {
                if (validationText)
                    Debug.Log("EXPECTED UINT MAX - 1: " + inputOutputArray[i]);
                checks = false;
            }
            inputOutputArray[i] = (uint)rand.Next(0, int.MaxValue);
        }
        b_inputOutput.SetData(inputOutputArray);

        compute.Dispatch(k_insert, THREAD_BLOCKS, 1, 1);
        compute.Dispatch(k_lookup, THREAD_BLOCKS, 1, 1);
        b_inputOutput.GetData(inputOutputArray);
        for (int i = 0; i < inputSize; i++)
        {
            if (inputOutputArray[i] != validationArray[i])
            {
                if (validationText)
                    Debug.LogError("EXPECTED THE SAME: " + inputOutputArray[i] + ", " + validationArray[i]);
                checks = false;
            }
        }

        if (checks)
            Debug.Log("Complete, all tests passed");
        else
            Debug.LogError("Validation Error, further tests required");
    }

    private void HashTest()
    {
        Debug.Log("Beginning " + batchSizeForTiming + " iterations of " + inputSize + " elements at: " + (inputSize * 100.0f / hashBufferSize) + "% load factor");
        StartCoroutine(MultiTiming());
    }

    private IEnumerator MultiTiming()
    {
        float insertTotalTime = 0;
        float lookupTotalTime = 0;
        float deletionTotalTime = 0;

        for (int i = 0; i < batchSizeForTiming; i++)
        {
            float time = Time.realtimeSinceStartup;
            compute.Dispatch(k_insert, THREAD_BLOCKS, 1, 1);
            var request = AsyncGPUReadback.Request(b_hash);
            yield return new WaitUntil(() => request.done);
            insertTotalTime += Time.realtimeSinceStartup - time;

            time = Time.realtimeSinceStartup;
            compute.Dispatch(k_lookup, THREAD_BLOCKS, 1, 1);
            request = AsyncGPUReadback.Request(b_inputOutput);
            yield return new WaitUntil(() => request.done);
            lookupTotalTime += Time.realtimeSinceStartup - time;

            time = Time.realtimeSinceStartup;
            compute.Dispatch(k_delete, THREAD_BLOCKS, 1, 1);
            request = AsyncGPUReadback.Request(b_hash);
            yield return new WaitUntil(() => request.done);
            deletionTotalTime += Time.realtimeSinceStartup - time;

            if (i == (inputSize / batchSizeForTiming))
                Debug.Log("Running");
        }

        Debug.Log("Done");
        Debug.Log("Insertion average time:  " + inputSize * (batchSizeForTiming / insertTotalTime) + " elements/sec.");
        Debug.Log("Lookup average time:  " + inputSize * (batchSizeForTiming / lookupTotalTime) + " elements/sec.");
        Debug.Log("Insertion average time:  " + inputSize * (batchSizeForTiming / deletionTotalTime) + " elements/sec.");
    }

    private int SizeToPow(int size)
    {
        size--;
        int counter = 1;
        while (size > 0)
        {
            size >>= 1;
            counter <<= 1;
        }
        return counter;
    }
    private void OnDisable()
    {
        b_inputOutput.Release();
        b_hash.Release();
    }
}
