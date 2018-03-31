using System;

namespace LamestWebserver.Core
{
    /// <summary>
    /// A container type for IComparable&lt;T&gt; that always clamps the value at the given maximum and minimum value.
    /// </summary>
    /// <typeparam name="T">The Type of the clamped value. Must inherit from IComparable&lt;T&gt;.</typeparam>
    [Serializable]
    public class ClampedValue<T> : NullCheckable where T : IComparable<T>
    {
        private bool _minimumInitialized;
        private bool _maximumInitialized;

        private T _minimum;
        private T _maximum;
        private T _value;

        /// <summary>
        /// The minimum Value.
        /// </summary>
        public T Minimum
        {
            get
            {
                return _minimum;
            }
            set
            {
                if (_minimumInitialized)
                    throw new InvalidOperationException(nameof(Minimum) + " can only be set once.");

                _minimumInitialized = true;
                _minimum = value;
            }
        }

        /// <summary>
        /// The maximum Value.
        /// </summary>
        public T Maximum
        {
            get
            {
                return _maximum;
            }
            set
            {
                if (_maximumInitialized)
                    throw new InvalidOperationException(nameof(Maximum) + " can only be set once.");

                _maximumInitialized = true;
                _maximum = value;
            }
        }
        
        /// <summary>
        /// The clamped Value.
        /// </summary>
        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = Math.Clamp(value, Minimum, Maximum);
            }
        }

        /// <summary>
        /// Initializes a ClampedValue object.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="minimum">The minimum Value.</param>
        /// <param name="maximum">The maximum Value.</param>
        public ClampedValue(T value, T minimum, T maximum) : this(minimum, maximum)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a ClampedValue object without a value.
        /// </summary>
        /// <param name="minimum">The minimum Value.</param>
        /// <param name="maximum">The maximum Value.</param>
        public ClampedValue(T minimum, T maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>
        /// Deserialzation Constructor or non-initializing Constructor.
        /// </summary>
        public ClampedValue()
        {
            _minimumInitialized = false;
            _maximumInitialized = false;
        }

        /// <summary>
        /// Retrieves the Value from a ClampedValue&lt;T&gt;.
        /// </summary>
        /// <param name="clampedValue">The ClampedValue to retrieve from.</param>
        public static implicit operator T(ClampedValue<T> clampedValue) => clampedValue.Value;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return _value.Equals(obj);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
