using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core
{
    public class ID : NullCheckable, IComparable<ID>
    {
        protected ulong[] _id;
        protected string _string_id = null;

        private const int CharsInUlong = 2 * sizeof(ulong);

        public ID()
        {
            _id = ConvertFromByteArray(Hash.GetByteHash());
        }

        public ID(string id)
        {
            if (id == null)
                throw new NullReferenceException(nameof(id));

            if (id.Length == 0 || id.Length % CharsInUlong != 0)
                throw new InvalidOperationException($"Length of {nameof(id)} has to be a multiple of {CharsInUlong}.");
            
            _string_id = id;
            _id = ConvertFromString(id);
        }

        public ID(byte[] id)
        {
            _id = ConvertFromByteArray(id);
        }

        public ID(ulong[] id)
        {
            _id = id;
        }

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

        public byte[] GetByteArray()
        {
            byte[] ret = new byte[_id.Length * sizeof(ulong)];

            Marshal.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(_id, 0), ret, 0, ret.Length);

            return ret;
        }

        public ulong[] GetUlongArray() => _id;

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

        public override string ToString() => Value;
    }

    public class LongID : ID
    {
        public LongID()
        {
            _id = ConvertFromByteArray(Hash.GetComplexHashBytes());
        }

        public LongID(string id)
        {
            _string_id = id;
            _id = ConvertFromString(id);
        }

        public LongID(byte[] id) : base(id) { }

        public LongID(ulong[] id) : base(id) { }

        public override string Value
        {
            get => base.Value;
            set
            {
                _id = ConvertFromString(value);
                _string_id = value;
            }
        }

        protected override ulong[] ConvertFromString(string id)
        {
            byte[] bytes = Convert.FromBase64String(id);

            return ConvertFromByteArray(bytes);
        }

        protected override string ConvertFromUlongArray(ulong[] id)
        {
            byte[] bytes = new byte[id.Length * sizeof(ulong)];

            Marshal.Copy(Marshal.UnsafeAddrOfPinnedArrayElement(id, 0), bytes, 0, bytes.Length);

            return Convert.ToBase64String(bytes);
        }
    }
}
