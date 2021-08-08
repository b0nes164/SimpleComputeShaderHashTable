using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

public class Test : MonoBehaviour
{
    [SerializeField]
    private ComputeShader compute;

    [SerializeField]
    private bool validation;
    [SerializeField]
    private int bufferSize;

    private ComputeBuffer valuesBuffer;
    private ComputeBuffer hashBuffer;
    private int hashBufferSize;

    private int initKernel;
    private int insertKernel;
    private int lookupKernel;
    private int deleteKernel;

    System.Random rand = new System.Random();
    private float time;
    private GraphicsFence fence;

    private uint[] test;
    private uint[] test2;
    private uint[] test3;

    private void Start()
    {
        //Find the smallest power of 2 equal to or larger than input buffer size, because the size of the hashBuffer must always be a power of two.
        hashBufferSize = SizeToPow(bufferSize);

        //Initialize the bufferSize fields 
        compute.SetInt("hashBufferSize", hashBufferSize);
        compute.SetInt("bufferSize", bufferSize);

        //Get the int associated with each kernel.
        initKernel = compute.FindKernel("Initialize");
        insertKernel = compute.FindKernel("HashInsertKern");
        lookupKernel = compute.FindKernel("HashLookupKern");
        deleteKernel = compute.FindKernel("HashDeleteKern");

        //Allocate the memory for the buffers.
        hashBuffer = new ComputeBuffer(hashBufferSize, sizeof(uint) * 2);
        valuesBuffer = new ComputeBuffer(bufferSize, sizeof(uint) * 2);

        //Just in case, for debugging purposes
        fence = Graphics.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
        HashTest();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HashTest();
        }
    }

    //Dispatches the kernels for initialization, insertion, lookup, delete, in that order.
    private void HashTest()
    {
        //set the random seeds
        compute.SetInt("random", rand.Next(0, 2000000000));
        compute.SetInt("randomTwo", rand.Next(1, 2000000000));

        //dispatch the Initialize kernel, which sets all indexes on the HashBuffer to the empty state, and fills the ValueBuffer with random uints.
        compute.SetBuffer(initKernel, "HashBuffer", hashBuffer);
        compute.SetBuffer(initKernel, "ValuesBuffer", valuesBuffer);
        compute.Dispatch(initKernel, Mathf.CeilToInt(hashBufferSize / 1024f), 1, 1);

        //Grab the data from the ValuesBuffer before insertion + lookup for validation.
        if (validation)
        {
            test = new uint[bufferSize * 2];
            valuesBuffer.GetData(test);
        }

        //Dispatch the insertion kernel.
        Graphics.WaitOnAsyncGraphicsFence(fence);
        time = Time.realtimeSinceStartup;
        compute.SetBuffer(insertKernel, "HashBuffer", hashBuffer);
        compute.SetBuffer(insertKernel, "ValuesBuffer", valuesBuffer);
        compute.Dispatch(insertKernel, Mathf.CeilToInt(bufferSize / 1024f), 1, 1);
        StartCoroutine(Timing(0));

        //Dispatch the lookup kernel.
        Graphics.WaitOnAsyncGraphicsFence(fence);
        time = Time.realtimeSinceStartup;
        compute.SetBuffer(lookupKernel, "HashBuffer", hashBuffer);
        compute.SetBuffer(lookupKernel, "ValuesBuffer", valuesBuffer);
        compute.Dispatch(lookupKernel, Mathf.CeilToInt(bufferSize / 1024f), 1, 1);
        StartCoroutine(Timing(1));

        //Grab the data after the lookup for validation
        if (validation)
        {
            test2 = new uint[bufferSize * 2];
            valuesBuffer.GetData(test2);

            //Because test[] and test2[] are arrays of type uint and not KeyValue, we effectively have a stride of 2. Thus to check the value part of KeyValue, we start at index 1 and iterate up by 2.
            //If the insertion and lookup were performed properly, test[] and test2[] should be identical. 
            for (int i = 1; i < test.Length; i +=2)
            {
                if (test[i] != test2[i])
                {
                    Debug.Log("Insertion/Lookup error at :" + i);
                }
            }
        }

        //Dispatch the delete kernel
        Graphics.WaitOnAsyncGraphicsFence(fence);
        time = Time.realtimeSinceStartup;
        compute.SetBuffer(deleteKernel, "HashBuffer", hashBuffer);
        compute.SetBuffer(deleteKernel, "ValuesBuffer", valuesBuffer);
        compute.Dispatch(deleteKernel, Mathf.CeilToInt(bufferSize / 1024f), 1, 1);
        StartCoroutine(Timing(2));

        //Grab the hashbuffer after deletion for validation.
        if (validation)
        {
            StartCoroutine(debug()); 
            //StartCoroutine(asyncDebug());
        }
    }

    //Release memory for buffers when done.
    private void OnDisable()
    {
        valuesBuffer.Release();
        hashBuffer.Release();
    }

    //Return the smallest power of 2 equal to or larger than the input integer.
    private int SizeToPow(int size)
    {
        size--;

        int counter = 0;
        while (size > 0)
        {
            size >>= 1;
            counter++;
        }

        return (int)Mathf.Pow(2, counter);
    }

    IEnumerator debug()
    {
        GetDebugData();

        yield return new WaitForSeconds(1);

        GetDebugData();

        yield return new WaitForSeconds(1);

        GetDebugData();
    }

    private void GetDebugData()
    {
        Debug.Log("running!");

        test3 = new uint[hashBufferSize * 2];
        hashBuffer.GetData(test3);


        //If deletion was performed properly, test3[] should always equal the sentinel value, 0xffffffff
        //there appears to be a bug
        for (int i = 1; i < test3.Length; i += 2)
        {
            if (test3[i] != 0xffffffff)
            {
                Debug.Log("Deletion error at " + i);
            }
        }
    }

    IEnumerator AsyncDebug()
    {
        NativeArray<uint> test4 = new NativeArray<uint>(hashBufferSize * 2, Allocator.Persistent);

        var request = AsyncGPUReadback.Request(hashBuffer);
        yield return new WaitUntil(() => request.done);

        test4 = request.GetData<uint>();

        for (int i = 1; i < test4.Length; i+=2)
        {
            if (test4[i] != 0xffffffff)
            {
                Debug.Log("Deletion error at " + i);
            }
        }

        test4.Dispose();
    }

    IEnumerator Timing(int timingCase)
    {
        if (timingCase == 0)
        {
            var request = AsyncGPUReadback.Request(valuesBuffer);
            yield return new WaitUntil(() => request.done);
            Debug.Log("Insertion time for " + bufferSize + " random numbers: " + (Time.realtimeSinceStartup - time) + ".      " + ((1f / (Time.realtimeSinceStartup - time)) * bufferSize) + " keys/sec");
        }

        if (timingCase == 1)
        {
            var request = AsyncGPUReadback.Request(valuesBuffer);
            yield return new WaitUntil(() => request.done);
            Debug.Log("Lookup time for " + bufferSize + " random numbers: " + (Time.realtimeSinceStartup - time) + ".      " + ((1f / (Time.realtimeSinceStartup - time)) * bufferSize) + " keys/sec");
        }

        if (timingCase == 2)
        {
            var request = AsyncGPUReadback.Request(hashBuffer);
            yield return new WaitUntil(() => request.done);
            Debug.Log("Deletion time for " + bufferSize + " random numbers: " + (Time.realtimeSinceStartup - time) + ".      " + ((1f / (Time.realtimeSinceStartup - time)) * bufferSize) + " keys/sec");
        }

    }
}
