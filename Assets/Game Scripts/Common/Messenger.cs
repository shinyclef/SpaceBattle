using System;
using System.Collections.Generic;

public class Messenger
{
    private static Dictionary<object, Messenger> messengers;
    private Dictionary<Msg, object> listeners = new Dictionary<Msg, object>();

    public static Messenger Global { get; private set; }

    static Messenger()
    {
        messengers = new Dictionary<object, Messenger>();
        Global = new Messenger();
    }

    private Messenger() { }

    public Messenger(object obj)
    {
        messengers.Add(obj, this);
    }

    public static Messenger Get(object obj)
    {
        Messenger m;
        messengers.TryGetValue(obj, out m);
        return m;
    }


    /* ------------------------------------- */
    /* Messenger methods with no parameters. */
    /* ------------------------------------- */

    public void Post(Msg m)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action>;
            if (actions != null)
            {
                foreach (var a in actions)
                {
                    a();
                }
            }
        }
    }

    public void AddListener(Msg m, Action listener)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action>;
            actions.Add(listener);
        }
        else
        {
            listeners.Add(m, new HashSet<Action>() { listener });
        }
    }

    public void RemoveListener(Msg m, Action listener)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action>;
            actions.Remove(listener);
        }
    }


    /* --------------------------------------------------------- */
    /* Messenger methods with ONE generic type for data passing. */
    /* --------------------------------------------------------- */

    public void Post<T>(Msg m, T arg1)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action<T>>;
            if (actions != null)
            {
                foreach (var a in actions)
                {
                    a(arg1);
                }
            }
        }
    }

    public void AddListener<T>(Msg m, Action<T> listener)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action<T>>;
            actions.Add(listener);
        }
        else
        {
            listeners.Add(m, new HashSet<Action<T>>() { listener });
        }
    }

    public void RemoveListener<T>(Msg m, Action<T> listener)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action<T>>;
            actions.Remove(listener);
        }
    }


    /* ---------------------------------------------------------- */
    /* Messenger methods with TWO generic types for data passing. */
    /* ---------------------------------------------------------- */

    public void Post<T, U>(Msg m, T arg1, U arg2)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action<T, U>>;
            if (actions != null)
            {
                foreach (var a in actions)
                {
                    a(arg1, arg2);
                }
            }
        }
    }

    public void AddListener<T, U>(Msg m, Action<T, U> listener)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action<T, U>>;
            actions.Add(listener);
        }
        else
        {
            listeners.Add(m, new HashSet<Action<T, U>>() { listener });
        }
    }

    public void RemoveListener<T, U>(Msg m, Action<T, U> listener)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action<T, U>>;
            actions.Remove(listener);
        }
    }


    /* ------------------------------ */
    /* Messenger methods with params. */
    /* ------------------------------ */

    public void PostParams(Msg m, params object[] args)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action<object[]>>;
            if (actions != null)
            {
                foreach (var a in actions)
                {
                    a(args);
                }
            }
        }
    }

    public void AddListenerParams(Msg m, Action<object[]> listener)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action<object[]>>;
            actions.Add(listener);
        }
        else
        {
            listeners.Add(m, new HashSet<Action<object[]>>() { listener });
        }
    }

    public void RemoveListenerParams(Msg m, Action<object[]> listener)
    {
        object o;
        if (listeners.TryGetValue(m, out o))
        {
            var actions = o as HashSet<Action<object[]>>;
            actions.Remove(listener);
        }
    }
}