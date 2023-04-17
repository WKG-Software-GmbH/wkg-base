using System;

namespace Wkg.Collections;

/// <summary>
/// A first-in, first-out collection of objects implemented on to of a ring buffer allowing the oldest element to be overridden once the specified <see cref="Capacity"/> is reached.
/// </summary>
/// <typeparam name="T">The type of the objects in this <see cref="CyclicQueue{T}"/></typeparam>
public class CyclicQueue<T>
{
    private readonly T?[] _buffer;
    private RingBufferPointer<T?> _endPointer;
    private RingBufferPointer<T?> _startPointer;

    /// <summary>
    /// The maximum amount of items this <see cref="CyclicQueue{T}"/> can hold before overriding the bottom most elements.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// The number of elements currently present in this <see cref="CyclicQueue{T}"/>.
    /// </summary>
    public int Count { get; private protected set; }

    /// <summary>
    /// Gets or sets the default <see cref="Action{T}"/> to be executed when an element is overridden due to maximum capacity reached.
    /// </summary>
    public Action<T?>? OnElementOverride { get; set; }

    /// <summary>
    /// Constructs a new <see cref="CyclicQueue{T}"/> with the specified maximum <paramref name="capacity"/> after which the first element is overridden by the last one.
    /// </summary>
    /// <param name="capacity">The maximum <paramref name="capacity"/> after which the last element is overridden by the first one.</param>
    public CyclicQueue(int capacity)
    {
        Capacity = capacity;
        _buffer = new T[capacity];
        _startPointer = new RingBufferPointer<T?>(_buffer);
        _endPointer = new RingBufferPointer<T?>(_buffer);
    }

    /// <summary>
    /// Removes all elements from this <see cref="CyclicQueue{T}"/>.
    /// </summary>
    public void Clear()
    {
        Count = 0;
        _startPointer = _endPointer;
    }

    /// <summary>
    /// Removes and returns the object at the beginning of the <see cref="CyclicQueue{T}"/>.
    /// </summary>
    /// <returns>The object at the beginning of the <see cref="CyclicQueue{T}"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T? Dequeue()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Queue empty.");
        }

        T? result = _buffer[_startPointer];
        Count--;
        if (Count > 0)
        {
            _startPointer++;
        }

        return result;
    }

    /// <summary>
    /// Adds an object to the end of the <see cref="CyclicQueue{T}"/>.
    /// </summary>
    /// <param name="element">The object to add to the end of the <see cref="CyclicQueue{T}"/>. May override the first element if the maximum <see cref="Capacity"/> is reached.</param>
    public void Enqueue(T? element)
    {
        if (Count == 0)
        {
            _buffer[_endPointer] = element;
            Count++;
        }
        else
        {
            if (_endPointer + 1 == _startPointer)
            {
                OnElementOverride?.Invoke(_buffer[_startPointer]);
            }

            _endPointer++;
            _buffer[_endPointer] = element;
            if (_endPointer == _startPointer)
            {
                _startPointer++;
            }
            else
            {
                Count++;
            }
        }
    }

    /// <summary>
    /// Returns the object at the beginning of the <see cref="CyclicQueue{T}"/> without removing it.
    /// </summary>
    /// <returns>The object at the beginning of the <see cref="CyclicQueue{T}"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T? Peek()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Queue empty.");
        }

        return _buffer[_startPointer];
    }
}
