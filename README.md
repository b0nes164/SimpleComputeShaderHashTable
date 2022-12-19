# SimpleComputeShaderHashTable

![hash](https://user-images.githubusercontent.com/68340554/128619452-65042a29-9174-4a14-a0ba-efc0abb0f598.PNG)

# About This Project
This project is a Unity Compute Shader implementation of a [simple GPU hash table written by David Farell](https://github.com/nosferalatu/SimpleGPUHashTable) which in turn is based on [Cliff Click's hash table](https://preshing.com/20130605/the-worlds-simplest-lock-free-hash-table/). It uses the [MurmurHash3 function by Austin Appleby](https://github.com/aappleby/smhasher). All of the above works are in the public domain, and free to use, as is this project. 

This code implements a lock free hash table using linear probing, and achieves thread safety using an atomic function, `InterlockedCompareExchange()`, to insert key/values into the table. Because it uses linear probing, the table is cache-effecient, but performance quickly degrades as the load factor increases.

# Important notes
* The table uses 32bit keys and 32bit values.
* Because we use bitwise AND to cycle through the table when probing, the size of the table must be a power of 2.
* It reserves 0xffffffff as an empty sentinel value for both keys and values.
* I have not included a resizing function, but it would operate exactly like you would expect. A general outline would be something like: cycle through the values of the hashbuffer, and then rehash any non-empty value into the new table. 

# To use this project
To use this project, simply add `Hash.compute` and `Test.cs` to an existing Unity project, attach `Test.cs` to a gameobject in the editor, and attach `Hash.compute` to `Test.cs`.
* If you want to verify that the HashTable is working properly, tick `Validation` on the gameobject.
* If you want to get a text output if there are validation errors, tick `Validation Text` though be careful because this will cause a lot of lag on large inputs (1 mil +).
* If you want to test the speed of the HashTable, untick `Validation.` The validation and speed testing is mutually exclusive in this demo.
* These scripts were written in a Unity project version 2021.1.5f1, but can probably be used in older versions so long as it supports compute shaders.    

# To Learn More
If you want learn more about this hash table and how it was designed I would highly encourage reading [David Farell's blog post](https://nosferalatu.com/SimpleGPUHashTable.html). If you want to learn more about GPU powered hash tables in general see this scholarly article [WarpCore: A Library for fast Hash Tables on GPUs](https://arxiv.org/pdf/2009.07914.pdf).
