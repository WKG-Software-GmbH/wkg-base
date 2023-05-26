namespace Wkg.Collections;

/// <summary>
/// A last-in, first-out collection of objects implemented on to of a ring buffer allowing the bottom most element to be overridden once the specified <see cref="Capacity"/> is reached.
/// </summary>
/// <typeparam name="T">The type of the objects in this <see cref="CyclicStack{T}"/></typeparam>
public class CyclicStack<T>
{
    private readonly T?[] _buffer;
    private RingBufferPointer<T?> _basePointer;
    private RingBufferPointer<T?> _stackPointer;

    /// <summary>
    /// The maximum amount of items this <see cref="CyclicStack{T}"/> can hold before overriding the bottom most elements.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// The number of elements currently present in this <see cref="CyclicStack{T}"/>.
    /// </summary>
    public int Count { get; private protected set; }

    /// <summary>
    /// Gets or sets the default <see cref="Action{T}"/> to be executed when an element is overridden due to maximum capacity reached.
    /// </summary>
    public virtual Action<T?>? OnElementOverride { get; set; }

    /// <summary>
    /// Constructs a new instance of the <see cref="CyclicStack{T}"/> class with the specified <see cref="Capacity"/>.
    /// </summary>
    /// <param name="capacity">The maximum amount of items this <see cref="CyclicStack{T}"/> can hold before overriding the bottom most elements.</param>
    public CyclicStack(int capacity)
    {
        Capacity = capacity;
        _buffer = new T[capacity];
        _stackPointer = new RingBufferPointer<T?>(_buffer);
        _basePointer = new RingBufferPointer<T?>(_buffer);
    }

    /// <summary>
    /// Removes all elements from this <see cref="CyclicStack{T}"/>.
    /// </summary>
    public virtual void Clear()
    {
        Count = 0;
        _stackPointer = _basePointer;
    }

    /// <summary>
    /// Returns the top most object of the <see cref="CyclicStack{T}"/> without removing it.
    /// </summary>
    /// <returns>The  top most object of the <see cref="CyclicStack{T}"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual T? Peek()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Stack empty.");
        }

        return _buffer[_stackPointer];
    }

    /// <summary>
    /// Removes and returns the top most object of the <see cref="CyclicStack{T}"/>.
    /// </summary>
    /// <returns>The top most object of the <see cref="CyclicStack{T}"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual T? Pop()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Stack empty.");
        }

        T? result = _buffer[_stackPointer];
        Count--;
        if (Count > 0)
        {
            _stackPointer--;
        }

        return result;
    }

    /// <summary>
    /// Adds an object to the top of the <see cref="CyclicStack{T}"/>.
    /// </summary>
    /// <param name="element">The object to add to the top of the <see cref="CyclicStack{T}"/>. May override the bottom most element if the maximum <see cref="Capacity"/> is reached.</param>
    public virtual void Push(T? element)
    {
        if (Count == 0)
        {
            _buffer[_stackPointer] = element;
            Count++;
        }
        else
        {
            if (_stackPointer + 1 == _basePointer)
            {
                OnElementOverride?.Invoke(_buffer[_basePointer]);
            }

            _stackPointer++;
            _buffer[_stackPointer] = element;
            if (_stackPointer == _basePointer)
            {
                _basePointer++;
            }
            else
            {
                Count++;
            }
        }
    }
}
