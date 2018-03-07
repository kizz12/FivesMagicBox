using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tuple<T,U,Y,Z>
{
    public T Item1 { get; private set; }
    public U Item2 { get; private set; }
	public Y Item3 { get; private set; }
	public Z Item4 { get; private set; }

    public Tuple(T item1, U item2, Y item3, Z item4)
    {
        Item1 = item1;
        Item2 = item2;
		Item3 = item3;
		Item4 = item4;
    }
}

public static class Tuple
{
    public static Tuple<T, U, Y, Z> Create<T, U, Y, Z>(T item1, U item2, Y item3, Z item4)
    {
        return new Tuple<T, U, Y, Z>(item1, item2, item3, item4);
    }
}