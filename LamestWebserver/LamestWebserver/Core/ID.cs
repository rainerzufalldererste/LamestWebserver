using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Provides functionailty for identifying objects.
    /// </summary>
    public class ID : IComparable<ID>
    {
        /// <summary>
        /// The internal ID.
        /// </summary>
        protected ulong[] _id;

        /// <summary>
        /// The precalculated ID as string.
        /// </summary>
        protected string _string_id = null;

        private const int CharsInUlong = 2 * sizeof(ulong);

        /// <summary>
        /// Constructs a new ID with random value.
        /// </summary>
        public ID()
        {
            _id = ConvertFromByteArray(Hash.GetByteHash());
        }

        /// <summary>
        /// Constructs a new ID with the given value.
        /// </summary>
        /// <param name="id">the internal ID to use.</param>
        public ID(string id)
        {
            if (id == null)
                throw new NullReferenceException(nameof(id));

            if (id.Length == 0 || id.Length % CharsInUlong != 0)
                throw new InvalidOperationException($"Length of {nameof(id)} has to be a multiple of {CharsInUlong}.");
            
            _string_id = id;
            _id = ConvertFromString(id);
        }

        /// <summary>
        /// Constructs a new ID with the given value.
        /// </summary>
        /// <param name="id">the internal ID to use.</param>
        public ID(byte[] id)
        {
            _id = ConvertFromByteArray(id);
        }

        /// <summary>
        /// Constructs a new ID with the given value.
        /// </summary>
        /// <param name="id">the internal ID to use.</param>
        public ID(ulong[] id)
        {
            _id = id;
        }

        /// <summary>
        /// The inner Value of the ID as string.
        /// </summary>
        public virtual string Value
        {
            get
            {
                if (_string_id != null)
                    return _string_id;

                _string_id = ConvertFromUlongArray(_id);

                return _string_id;
            }
            set
            {
                if (value == null)
                    throw new NullReferenceException(nameof(value));

                if (value.Length == 0 || value.Length % CharsInUlong != 0)
                    throw new InvalidOperationException($"Length of {nameof(value)} has to be a multiple of {CharsInUlong}.");

                _string_id = value;
                _id = ConvertFromString(value);
            }
        }

        /// <summary>
        /// Regenerates the internal value to a random new value.
        /// </summary>
        public virtual void RegenerateHash()
        {
            _id = ConvertFromByteArray(Hash.GetByteHash());
            _string_id = null;
        }

        /// <summary>
        /// Retrieves the internal Value as byte[].
        /// </summary>
        /// <returns>Returns the internal Value as byte[].</returns>
        public byte[] GetByteArray()
        {
            byte[] ret = new byte[_id.Length * sizeof(ulong)];

            Marshal.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(_id, 0), ret, 0, ret.Length);

            return ret;
        }
        
        /// <summary>
        /// Retrieves the internal Value as ulong[].
        /// </summary>
        /// <returns>Returns the internal Value as ulong[].</returns>
        public ulong[] GetUlongArray() => _id;

        /// <summary>
        /// Converts a given ID from string to ulong[].
        /// </summary>
        /// <param name="id">the ID as string.</param>
        /// <returns>the ID as ulong[].</returns>
        protected virtual ulong[] ConvertFromString(string id)
        {
            if (id == null)
                throw new NullReferenceException(nameof(id));

            if (id.Length == 0 || id.Length % CharsInUlong != 0)
                throw new InvalidOperationException($"Length of {nameof(id)} has to be a multiple of {CharsInUlong}.");

            ulong[] ret = new ulong[id.Length / CharsInUlong];

            int index = 0;
            int subindex = 0;

            for (int i = 0; i < id.Length; i++)
            {
                ulong b = 0;

                if (id[i] >= '0' && id[i] <= '9')
                    b = (ulong)(id[i] - '0') & 0x0F;
                else if (id[i] >= 'a' && id[i] <= 'f')
                    b = (ulong)(0xA + id[i] - 'a') & 0x0F;
                else if (id[i] >= 'A' && id[i] <= 'F')
                    b = (ulong)(0xA + id[i] - 'A') & 0x0F;
                else
                    throw new FormatException($"The given ID is not valid. (Character {i} is '{id[i]}')");

                ret[index] |= (b << (subindex++ * 4));

                if (subindex == CharsInUlong)
                {
                    subindex = 0;
                    index++;
                }
            }

            return ret;
        }

        /// <summary>
        /// Converts a given ID from byte[] to ulong[].
        /// </summary>
        /// <param name="id">the ID as byte[].</param>
        /// <returns>the ID as ulong[].</returns>
        protected virtual ulong[] ConvertFromByteArray(byte[] id)
        {
            if (id == null)
                throw new NullReferenceException(nameof(id));

            if (id.Length == 0 || id.Length % sizeof(ulong) != 0)
                throw new InvalidOperationException($"Length of {nameof(id)} has to be a multiple of {sizeof(ulong)}.");

            ulong[] ret = new ulong[id.Length / sizeof(ulong)];

            Marshal.Copy(id, 0, Marshal.UnsafeAddrOfPinnedArrayElement(ret, 0), id.Length);

            return ret;
        }

        /// <summary>
        /// Converts a given ID from ulong[] to string.
        /// </summary>
        /// <param name="id">the ID as ulong[].</param>
        /// <returns>the ID as string.</returns>
        protected virtual string ConvertFromUlongArray(ulong[] id)
        {
            char[] s = new char[id.Length * CharsInUlong];

            for (int i = 0; i < id.Length; i++)
            {
                for (int j = 0; j < CharsInUlong; j++)
                {
                    s[i * CharsInUlong + j] = Master.HexToCharLookupTable[(int)((ulong)(id[i] & (0xFul << (4 * j))) >> (4 * j))];
                }
            }

            return new string(s);
        }

        /// <inheritdoc />
        public int CompareTo(ID other)
        {
            if (other == null)
                throw new NullReferenceException(nameof(other));

            if (other._id.Length != _id.Length)
                throw new InvalidOperationException($"The IDs are not of the same length."); 

            for (int i = 0; i < other._id.Length; i++)
            {
                if (_id[i] < other._id[i])
                    return -1;
                if (_id[i] > other._id[i])
                    return 1;
            }

            return 0;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is ID))
                return false;

            if (_id.Length != ((ID)obj)._id.Length)
                return false;

            for (int i = 0; i < _id.Length; i++)
            {
                if (_id[i] != ((ID)obj)._id[i])
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int ret = 0;

            for (int i = 0; i < _id.Length; i++)
            {
                ret ^= (int)_id[i];
            }

            return ret;
        }

        /// <inheritdoc />
        public override string ToString() => Value;

        /// <summary>
        /// Compares two IDs.
        /// </summary>
        /// <param name="a">the first ID.</param>
        /// <param name="b">the second ID.</param>
        /// <returns>true if the comparison retrieves true.</returns>
        public static bool operator <(ID a, ID b) => a.CompareTo(b) < 0;

        /// <summary>
        /// Compares two IDs.
        /// </summary>
        /// <param name="a">the first ID.</param>
        /// <param name="b">the second ID.</param>
        /// <returns>true if the comparison retrieves true.</returns>
        public static bool operator >(ID a, ID b) => a.CompareTo(b) > 0;

        /// <summary>
        /// Compares two IDs.
        /// </summary>
        /// <param name="a">the first ID.</param>
        /// <param name="b">the second ID.</param>
        /// <returns>true if the comparison retrieves true.</returns>
        public static bool operator ==(ID a, ID b)
        {
            if (a == null ^ b == null)
                return false;

            if (a == null)
                return true;

            return a.Equals(b);
        }

        /// <summary>
        /// Compares two IDs.
        /// </summary>
        /// <param name="a">the first ID.</param>
        /// <param name="b">the second ID.</param>
        /// <returns>true if the comparison retrieves true.</returns>
        public static bool operator !=(ID a, ID b) => a != b;
    }

    /// <summary>
    /// A derivate of ID using a longer SHA3 hash by default.
    /// </summary>
    public class LongID : ID
    {
        /// <summary>
        /// Initializes a new LongID with a random SHA3 hash.
        /// </summary>
        public LongID()
        {
            _id = ConvertFromByteArray(Hash.GetComplexHashBytes());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public LongID(string id)
        {
            if (id == null)
                throw new NullReferenceException(nameof(id));

            _string_id = id;
            _id = ConvertFromString(id);
        }

        /// <inheritdoc />
        public LongID(byte[] id) : base(id) { }

        /// <inheritdoc />
        public LongID(ulong[] id) : base(id) { }

        /// <inheritdoc />
        public override string Value
        {
            get => base.Value;
            set
            {
                if (value == null)
                    throw new NullReferenceException(nameof(value));

                _id = ConvertFromString(value);
                _string_id = value;
            }
        }

        /// <inheritdoc />
        public override void RegenerateHash()
        {
            _id = ConvertFromByteArray(Hash.GetComplexHashBytes());
            _string_id = null;
        }

        /// <inheritdoc />
        protected override ulong[] ConvertFromString(string id)
        {
            if (id == null)
                throw new NullReferenceException(nameof(id));

            byte[] bytes = Convert.FromBase64String(id);

            return ConvertFromByteArray(bytes);
        }

        /// <inheritdoc />
        protected override string ConvertFromUlongArray(ulong[] id)
        {
            if (id == null)
                throw new NullReferenceException(nameof(id));

            byte[] bytes = new byte[id.Length * sizeof(ulong)];

            Marshal.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(id, 0), bytes, 0, bytes.Length);

            return Convert.ToBase64String(bytes);
        }
    }
}
