        ///// <summary>
        ///// Converts the referred <paramref name="enumType"/> to a <see cref="DataTable"/>.
        ///// </summary>
        ///// <param name="enumType">The source enum type.</param>
        ///// <param name="valuesColumnName">This will be the name of the column that stores the enum values.</param>
        ///// <param name="namesColumnName">This will be name of the column the stores the enum names.</param>
        ///// <returns>A <see cref="DataTable"/> instance with two columns containing the value-name pairs of the specified enum.</returns>
        //public static DataTable EnumToDataTable(this Type enumType, string valuesColumnName, string namesColumnName)
        //{
        //    if (enumType == null)
        //        throw new ArgumentNullException("enumType");
        //    if (!enumType.IsEnum)
        //        throw new ArgumentException("Specified type must be an enum type", "enumType");
        //    if (valuesColumnName == null)
        //        throw new ArgumentNullException("valuesColumnName");
        //    if (namesColumnName == null)
        //        throw new ArgumentNullException("namesColumnName");

        //    Type underlyingType = Enum.GetUnderlyingType(enumType);
        //    DataTable dt = new DataTable();
        //    dt.Columns.Add(valuesColumnName, underlyingType);
        //    dt.Columns.Add(namesColumnName, typeof(string));

        //    foreach (object value in Enum.GetValues(enumType))
        //    {
        //        object valueMember = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        //        string displayMember = value.ToString();

        //        dt.Rows.Add(valueMember, displayMember);
        //    }

        //    return dt;
        //}

        ///// <summary>
        ///// Converts the referred <paramref name="enumType"/> to a <see cref="DataTable"/> with Value and Name columns containing enum fields.
        ///// </summary>
        ///// <param name="enumType">The source enum type.</param>
        //public static DataTable EnumToDataTable(this Type enumType)
        //{
        //    return EnumToDataTable(enumType, "Value", "Name");
        //}
