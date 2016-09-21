
namespace JWT.JSON
{
    

    public class JsonArray : JsonValue, System.Collections.Generic.IList<JsonValue>
	{
        System.Collections.Generic.List<JsonValue> list;

		public JsonArray (params JsonValue [] items)
		{
			list = new System.Collections.Generic.List<JsonValue> ();
			AddRange (items);
		}

		public JsonArray (System.Collections.Generic.IEnumerable<JsonValue> items)
		{
			if (items == null)
				throw new System.ArgumentNullException ("items");

			list = new System.Collections.Generic.List<JsonValue> (items);
		}

		public override int Count {
			get { return list.Count; }
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public override sealed JsonValue this [int index] {
			get { return list [index]; }
			set { list [index] = value; }
		}

		public override JsonType JsonType {
			get { return JsonType.Array; }
		}

		public void Add (JsonValue item)
		{
			if (item == null)
				throw new System.ArgumentNullException ("item");

			list.Add (item);
		}

		public void AddRange (System.Collections.Generic.IEnumerable<JsonValue> items)
		{
			if (items == null)
				throw new System.ArgumentNullException ("items");

			list.AddRange (items);
		}

		public void AddRange (params JsonValue [] items)
		{
			if (items == null)
				return;

			list.AddRange (items);
		}

		public void Clear ()
		{
			list.Clear ();
		}

		public bool Contains (JsonValue item)
		{
			return list.Contains (item);
		}

		public void CopyTo (JsonValue [] array, int arrayIndex)
		{
			list.CopyTo (array, arrayIndex);
		}

		public int IndexOf (JsonValue item)
		{
			return list.IndexOf (item);
		}

		public void Insert (int index, JsonValue item)
		{
			list.Insert (index, item);
		}

		public bool Remove (JsonValue item)
		{
			return list.Remove (item);
		}

		public void RemoveAt (int index)
		{
			list.RemoveAt (index);
		}

		public override void Save (System.IO.Stream stream)
		{
			if (stream == null)
				throw new System.ArgumentNullException ("stream");
			stream.WriteByte ((byte) '[');
			for (int i = 0; i < list.Count; i++) {
				JsonValue v = list [i];
				if (v != null)
					v.Save (stream);
				else {
					stream.WriteByte ((byte) 'n');
					stream.WriteByte ((byte) 'u');
					stream.WriteByte ((byte) 'l');
					stream.WriteByte ((byte) 'l');
				}

				if (i < Count - 1) {
					stream.WriteByte ((byte) ',');
					stream.WriteByte ((byte) ' ');
				}
			}
			stream.WriteByte ((byte) ']');
		}

        System.Collections.Generic.IEnumerator<JsonValue> System.Collections.Generic.IEnumerable<JsonValue>.GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return list.GetEnumerator ();
		}
	}
}
