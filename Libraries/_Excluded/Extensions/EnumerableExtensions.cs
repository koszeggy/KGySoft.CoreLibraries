        ///// <summary>
        ///// Converts an <see cref="IEnumerable{T}"/> source to <see cref="DataTable"/>. All of the readable public properties will be put in the result table.
        ///// </summary>
        //public static DataTable ToDataTable<T>(this IEnumerable<T> source)
        //{
        //    if (source == null)
        //        throw new ArgumentNullException("source", Res.Get(Res.ArgumentNull));

        //    PropertyInfo[] columns = (from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
        //                              where p.CanRead && p.GetGetMethod().GetParameters().Length == 0
        //                              select p).ToArray();

        //    return ToDataTable(source, columns);
        //}

        ///// <summary>
        ///// Converts an <see cref="IEnumerable{T}"/> source to <see cref="DataTable"/>. Only defined properties will be put in the result table.
        ///// </summary>
        ///// <param name="source">Source collection.</param>
        ///// <param name="columns">Instance properties of <typeparamref name="T"/> that will be converted to columns in given order.</param>
        //public static DataTable ToDataTable<T>(this IEnumerable<T> source, params string[] columns)
        //{
        //    if (source == null)
        //        throw new ArgumentNullException("source", Res.Get(Res.ArgumentNull));

        //    if (columns == null)
        //        throw new ArgumentNullException("columns", Res.Get(Res.ArgumentNull));

        //    Type type = typeof(T);

        //    PropertyInfo[] props = (from propName in columns
        //                            select type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)).ToArray();

        //    return ToDataTable(source, props);
        //}

        ///// <summary>
        ///// Converts an <see cref="IEnumerable{T}"/> source to <see cref="DataTable"/>. Only defined properties will be put in the result table.
        ///// </summary>
        ///// <param name="source">Source collection.</param>
        ///// <param name="columns">Properties of <typeparamref name="T"/> will be converted to columns in given order.</param>
        //private static DataTable ToDataTable<T>(this IEnumerable<T> source, PropertyInfo[] columns)
        //{
        //    if (source == null)
        //        throw new ArgumentNullException("source", Res.Get(Res.ArgumentNull));

        //    if (columns == null)
        //        throw new ArgumentNullException("columns", Res.Get(Res.ArgumentNull));

        //    DataTable result = new DataTable();

        //    foreach (PropertyInfo prop in columns)
        //    {
        //        result.Columns.Add(prop.Name, prop.PropertyType);
        //    }

        //    foreach (T item in source)
        //    {
        //        DataRow row = result.NewRow();
        //        for (int i = 0; i < columns.Length; i++)
        //        {
        //            row[i] = Reflector.GetProperty(item, columns[i]);
        //        }
        //        result.Rows.Add(row);
        //    }
        //    return result;
        //}
