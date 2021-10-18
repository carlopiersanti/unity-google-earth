using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Atomic<T> where T : struct
{
    T value;
    public T Value { get { lock (this) { return value; } } set { lock (this) { this.value = value; }  } }

    public Atomic(T value)
    {
        Value = value;
    }

    public void Operation(Func<T, T> operation)
    {
        lock (this)
        {
            value = operation(value);
        }
    }
}
