# SimpleComputeShaderHashTable

![hash](https://user-images.githubusercontent.com/68340554/128312010-7b256383-3c02-4dae-83fb-014a025cc708.PNG)
# About This Project
This project is a Unity Compute Shader implementation of a [simple GPU hash table written by David Farell](https://github.com/nosferalatu/SimpleGPUHashTable) which in turn is based on [Cliff Click's hash table](https://preshing.com/20130605/the-worlds-simplest-lock-free-hash-table/). It uses the MurmurHash3 function by Austin Appleby. 

By leveraging the massive parellel processing power of GPUs the table is able to achieve an incredible rate of insertions, lookups, and deletions. On my RTX 2080 Super I was able to get an average rate of insertions, lookups and deletions of about 5 * 10^12 per second at a load factor of about 95% (though I am almost positive this is incorrect and due to the fact that I am unaware of any way to determine when a dispatch has been complete). 

This code implements a lock free hash table using linear probing, and achieves thread safety using an atomic function, `InterlockedCompareExchange()`, to insert key/values into the table. Because it uses linear probing, the table is cache-effecient, but performance quickly degrades as the load factor increases.

The compute shader portion of this code is written in HLSL, but since of the unique way Unity interfaces with the compute shader this is not a complete HLSL solution. This hash table was designed to work on 32bit keys and 32bit values. The hash table is not resizeable, and its size must be a power of 2. It reserves 0xffffffff as an empty sentinel value for both keys and values.

# To use this project
To use this project, simply add `Hash.compute` and `Test.cs` to an existing Unity project, attach `Test.cs` to a gameobject in the editor, and attach `Hash.compute` to `Test.cs`. These scripts were written in a Unity project version 2021.1.5f1, but can probably be used in older versions so long as it supports compute shaders.    

# To Learn More
If you want learn more about this hash table and how it was designed I would highly encourage reading [David Farell's blog post](https://nosferalatu.com/SimpleGPUHashTable.html). If you want to learn more about GPU powered hash tables in general see this article [here](https://arxiv.org/pdf/2009.07914.pdf).

