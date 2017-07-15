using System;
using System.Reflection.Emit;
using LamestWebserver.Synchronization;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Provides a threadsafe, auto-initializing Singleton for a given type.
    /// </summary>
    /// <typeparam name="T">The type of the singleton.</typeparam>
    public class Singleton<T> : NullCheckable
    {
        private UsableMutexSlim _singletonLock = new UsableMutexSlim();

        private bool _initialized = false;
        private T _instance;

        private Func<T> _getInstance = null;

        /// <summary>
        /// The Instance of the Singleton. The instance is automatically initalized when you first read from it.
        /// </summary>
        public T Instance
        {
            get
            {
                if (!_initialized) // Checked outside for higher performance in most cases but not threadsafe. Therefore checked again inside the lock.
                {
                    using (_singletonLock.Lock())
                    {
                        if (!_initialized)
                        {
                            _instance = _getInstance();
                            _initialized = true;
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Creates a new Singleton.
        /// </summary>
        /// <param name="getInstanceFunction">A function to create an instance of the given type. If null will be set to default constructor of this type.</param>
        /// <exception cref="MissingMethodException">Throws a MissingMethodException when no getInstanceFunction is given and the type does not contain a default constructor and is no ValueType.</exception>
        public Singleton(Func<T> getInstanceFunction = null)
        {
            _instance = default(T);
            _getInstance = getInstanceFunction;

            if (_getInstance == null)
            {
                var ctor = typeof(T).GetConstructor(Type.EmptyTypes);

                if (ctor == null)
                {
                    if (typeof(T).IsValueType)
                    {
                        // a value type requires the same amount of memory if not initialized, but probably the initialization is very expensive or something...

                        _getInstance = () => default(T);
                        return;
                    }
                    else
                    {
                        throw new MissingMethodException("There is no empty constructor defined for this type. Please implement an empty constructor or provide a initialization function.");
                    }
                }
                else // just for code clarity.
                {
                    // Create method to call the empty constructor for the given type.
                    // See https://stackoverflow.com/questions/10593630/create-delegate-from-constructor

                    DynamicMethod dynamic = new DynamicMethod(string.Empty, typeof(T), Type.EmptyTypes, typeof(T));
                    ILGenerator il = dynamic.GetILGenerator();

                    il.DeclareLocal(typeof(T));
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Stloc_0);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ret);

                    _getInstance = (Func<T>)dynamic.CreateDelegate(typeof(Func<T>));
                }
            }
        }

        /// <summary>
        /// Retrieves the Instance of the singleton.
        /// </summary>
        /// <param name="singleton">The current singleton.</param>
        /// <returns>Returns the Instance of the singleton.</returns>
        public static implicit operator T(Singleton<T> singleton) => singleton.Instance;

        /// <summary>
        /// Retrieves the Instance of the singleton.
        /// </summary>
        /// <returns>Returns the Instance of the singleton.</returns>
        public T GetInstance() => Instance;
    }
}
