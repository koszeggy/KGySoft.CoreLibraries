using System;
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using KGySoft.Drawing;
using KGySoft.Libraries;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;
using KGySoft.Libraries.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SystemFileRef = System.Resources.ResXFileRef;
using SystemDataNode = System.Resources.ResXDataNode;

namespace _LibrariesTest
{
    using System.ComponentModel.Design;

    public class TestBase
    {
        protected sealed class TypeResolver : ITypeResolutionService
        {
            #region ITypeResolutionService Members

            public Assembly GetAssembly(AssemblyName name, bool throwOnError)
            {
                throw new NotImplementedException();
            }

            public Assembly GetAssembly(AssemblyName name)
            {
                throw new NotImplementedException();
            }

            public string GetPathOfAssembly(AssemblyName name)
            {
                throw new NotImplementedException();
            }

            public Type GetType(string name, bool throwOnError, bool ignoreCase)
            {
                throw new NotImplementedException();
            }

            public Type GetType(string name, bool throwOnError)
            {
                return Reflector.ResolveType(name, true, true);
            }

            public Type GetType(string name)
            {
                return Reflector.ResolveType(name, true, true);
            }

            public void ReferenceAssembly(AssemblyName name)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        protected static void AssertDeepEquals(object reference, object check, bool forceEqualityByMembers = false)
        {
            var errors = new List<string>();
            AssertResult(CheckDeepEquals(reference, check, forceEqualityByMembers, errors, new HashSet<object>(ReferenceEqualityComparer.Comparer)), errors);
        }

        protected static void AssertItemsEqual(IEnumerable referenceObjects, IEnumerable targetObjects, bool forceEqualityByMembers = false)
        {
            var errors = new List<string>();
            AssertResult(CheckItemsEqual(referenceObjects, targetObjects, forceEqualityByMembers, errors, new HashSet<object>(ReferenceEqualityComparer.Comparer)), errors);
        }

        protected static void AssertMembersAndItemsEqual(object reference, object check)
        {
            var errors = new List<string>();
            AssertResult(CheckMembersAndItemsEqual(reference, check, errors, new HashSet<object>(ReferenceEqualityComparer.Comparer)), errors);
        }

        protected static bool DeepEquals(object reference, object check, bool forceEqualityByMembers = false)
            => CheckDeepEquals(reference, check, forceEqualityByMembers, null, new HashSet<object>(ReferenceEqualityComparer.Comparer));

        protected static bool ItemsEqual(IEnumerable referenceObjects, IEnumerable targetObjects, bool forceEqualityByMembers = false)
            => CheckItemsEqual(referenceObjects, targetObjects, forceEqualityByMembers, null, new HashSet<object>(ReferenceEqualityComparer.Comparer));

        protected static bool MembersAndItemsEqual(object reference, object check) 
            => CheckMembersAndItemsEqual(reference, check, null, new HashSet<object>(ReferenceEqualityComparer.Comparer));

        private static bool CheckDeepEquals(object reference, object check, bool forceEqualityByMembers, List<string> errors, HashSet<object> checkedObjects)
        {
            if (reference == null && check == null)
                return true;

            Type typeRef = reference?.GetType();
            Type typeChk = check?.GetType();

            if (!Check(typeRef != null && typeChk != null, $"{typeRef?.ToString() ?? "null"} compared to {typeChk?.ToString() ?? "null"}", errors))
                return false;

            if (typeRef == typeof(AnyObjectSerializerWrapper))
                return CheckDeepEquals(Reflector.GetInstanceFieldByName(reference, "obj"), check, forceEqualityByMembers, errors, checkedObjects);

            if (typeRef == typeof(SystemFileRef))
                return Check(CheckDeepEquals(Reflector.ResolveType(((SystemFileRef)reference).TypeName), typeChk, forceEqualityByMembers, errors, checkedObjects), $"File reference type error. Expected type: {typeChk}", errors);

            if (typeRef == typeof(ResXFileRef))
                return Check(CheckDeepEquals(Reflector.ResolveType(((ResXFileRef)reference).TypeName), typeChk, forceEqualityByMembers, errors, checkedObjects), $"File reference type error. Expected type: {typeChk}", errors);

            if (typeRef == typeof(SystemDataNode))
                return CheckDeepEquals(((SystemDataNode)reference).GetValue((ITypeResolutionService)null), check, forceEqualityByMembers, errors, checkedObjects);

            if (typeRef == typeof(ResXDataNode))
                return CheckDeepEquals(((ResXDataNode)reference).GetValue(), check, forceEqualityByMembers, errors, checkedObjects);

            if (!Check(typeRef == typeChk, $"Types are different. {typeRef} <-> {typeChk}", errors))
                return false;

            if (typeRef == typeof(object))
                return true;

            // checking circular reference
            if (checkedObjects.Contains(reference))
                return true;
            checkedObjects.Add(reference);
            try
            {
                if (!(reference is string) && reference is IEnumerable)
                    return forceEqualityByMembers
                        ? CheckMembersAndItemsEqual(reference, check, errors, checkedObjects)
                        : CheckItemsEqual((IEnumerable)reference, (IEnumerable)check, false, errors, checkedObjects);

                if (reference is float floatRef && check is float floatCheck)
                    return Check(BitConverter.ToInt32(BitConverter.GetBytes(floatRef), 0) == BitConverter.ToInt32(BitConverter.GetBytes((float)check), 0), $"Float equality failed: {floatRef.ToRoundtripString()} <-> {floatCheck.ToRoundtripString()}. Binary representation: 0x{BitConverter.GetBytes(floatRef).ToHexValuesString()} <-> 0x{BitConverter.GetBytes(floatCheck).ToHexValuesString()}", errors);

                if (reference is double doubleRef && check is double doubleCheck)
                    return Check(BitConverter.DoubleToInt64Bits(doubleRef) == BitConverter.DoubleToInt64Bits(doubleCheck), $"Double equality failed: {doubleRef.ToRoundtripString()} <-> {doubleCheck.ToRoundtripString()}. Binary representation: 0x{BitConverter.GetBytes(doubleRef).ToHexValuesString()} <-> 0x{BitConverter.GetBytes(doubleCheck).ToHexValuesString()}", errors);

                if (reference is decimal decimalRef && check is decimal decimalCheck)
                    return Check(BinarySerializer.SerializeStruct(decimalRef).SequenceEqual(BinarySerializer.SerializeStruct(decimalCheck)), $"Decimal equality failed: {decimalRef.ToRoundtripString()} <-> {decimalCheck.ToRoundtripString()}. Binary representation: 0x{BinarySerializer.SerializeStruct(decimalRef).ToHexValuesString()} <-> 0x{BinarySerializer.SerializeStruct(decimalCheck).ToHexValuesString()}", errors);

                if (typeRef == typeof(StringBuilder))
                {
                    StringBuilder sbRef = (StringBuilder)reference;
                    StringBuilder sbCheck = (StringBuilder)check;
                    bool result = Check(sbRef.Capacity == sbCheck.Capacity, $"{nameof(StringBuilder)}.{nameof(StringBuilder.Capacity)} {sbRef.Capacity} <-> {sbCheck.Capacity}", errors);
                    result &= Check(sbRef.Length == sbCheck.Length, $"{nameof(StringBuilder)}.{nameof(StringBuilder.Length)} {sbRef.Length} <-> {sbCheck.Length}", errors);
                    result &= Check(sbRef.ToString() == sbCheck.ToString(), $"{nameof(StringBuilder)}: {sbRef} <-> {sbCheck}", errors);
                    return result;
                }

                if (reference is Stream stream)
                    return CheckStreams(stream, (Stream)check, errors);

                if (typeRef.IsGenericTypeOf(typeof(KeyValuePair<,>))
                    || typeRef == typeof(DictionaryEntry))
                {
                    string propName = nameof(DictionaryEntry.Key);
                    bool result = CheckMemberDeepEquals($"{typeRef}.{propName}", Reflector.GetInstancePropertyByName(reference, propName), Reflector.GetInstancePropertyByName(check, propName), forceEqualityByMembers, errors, checkedObjects);
                    propName = nameof(DictionaryEntry.Value);
                    result &= CheckMemberDeepEquals($"{typeRef}.{propName}", Reflector.GetInstancePropertyByName(reference, propName), Reflector.GetInstancePropertyByName(check, propName), forceEqualityByMembers, errors, checkedObjects);

                    return result;
                }

                if (typeRef == typeof(Bitmap))
                    return CheckImages((Bitmap)reference, (Bitmap)check, errors);

                if (typeRef == typeof(Metafile))
                    return CheckImages(((Metafile)reference).ToBitmap(((Metafile)reference).Size), ((Metafile)check).ToBitmap(((Metafile)check).Size), errors);

                if (typeRef == typeof(Icon))
                    return CheckImages(((Icon)reference).ToAlphaBitmap(), ((Icon)check).ToAlphaBitmap(), errors);

                if (typeRef == typeof(ImageListStreamer))
                {
                    var il1 = new ImageList { ImageStream = (ImageListStreamer)reference };
                    var il2 = new ImageList { ImageStream = (ImageListStreamer)check };
                    return CheckItemsEqual(il1.Images, il2.Images, forceEqualityByMembers, errors, checkedObjects);
                }

                // Structural equality if forced for non-primitive types or when Equals is not overridden
                if (forceEqualityByMembers && !typeRef.IsPrimitive && !typeof(IComparable).IsAssignableFrom(typeRef)
                    || !typeRef.GetMember(nameof(Equals), MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Any(m => m is MethodInfo mi && mi.GetParameters() is ParameterInfo[] parameters && parameters.Length == 1 && parameters[0].ParameterType == typeof(object) && mi.DeclaringType != mi.GetBaseDefinition().DeclaringType))
                    return CheckMembersAndItemsEqual(reference, check, errors, checkedObjects);

                // Equals as fallback
                return Check(Equals(reference, check), $"Equality check failed at type {typeRef}: {reference} <-> {check}", errors);
            }
            finally
            {
                checkedObjects.Remove(reference);
            }
        }

        private static bool CheckItemsEqual(IEnumerable referenceObjects, IEnumerable targetObjects, bool forceEqualityByMembers, List<string> errors, HashSet<object> checkedObjects)
        {
            Type type = referenceObjects.GetType();
#if !NET35
            if (type.IsGenericTypeOf(typeof(ConcurrentBag<>)))
            {
                referenceObjects = referenceObjects.Cast<object>().OrderBy(i => i).ToList();
                targetObjects = targetObjects.Cast<object>().OrderBy(i => i).ToList();
            }
#endif

            IEnumerator enumRef = referenceObjects.GetEnumerator();
            IEnumerator enumChk = targetObjects.GetEnumerator();

            int index = 0;
            bool result = true;
            while (enumRef.MoveNext())
            {
                if (!Check(enumChk.MoveNext(), $"{type}: Reference collection contains more than {index} objects.", errors))
                    return false;

                var subErrors = new List<string>();
                result &= CheckDeepEquals(enumRef.Current, enumChk.Current, forceEqualityByMembers, subErrors, checkedObjects);
                if (subErrors.Count > 0)
#if NET35
                    errors?.Add($"{type}[{index}]:{Environment.NewLine}\t{String.Join($"{Environment.NewLine}\t", subErrors.ToArray())}");
#else
                    errors?.Add($"{type}[{index}]:{Environment.NewLine}\t{String.Join($"{Environment.NewLine}\t", subErrors)}");
#endif

                index++;
            }

            result &= Check(!enumChk.MoveNext(), $"{type}: Target collection contains more than {index} objects.", errors);
            return result;
        }

        private static bool CheckMemberDeepEquals(string name, object reference, object check, bool forceEqualityByMembers, List<string> errors, HashSet<object> checkedObjects)
        {
            var subErrors = new List<string>();
            bool result = CheckDeepEquals(reference, check, forceEqualityByMembers, subErrors, checkedObjects);
            if (subErrors.Count > 0)
#if NET35
                errors?.Add($"{name}:{Environment.NewLine}\t{String.Join($"{Environment.NewLine}\t", subErrors.ToArray())}");
#else
                errors?.Add($"{name}:{Environment.NewLine}\t{String.Join($"{Environment.NewLine}\t", subErrors)}");
#endif
            return result;
        }

        private static bool CheckMembersAndItemsEqual(object reference, object check, List<string> errors, HashSet<object> checkedObjects)
        {
            if (reference == null && check == null)
                return true;

            Type typeRef = reference?.GetType();
            Type typeChk = check?.GetType();
            if (!Check(typeRef != null && typeChk != null, $"{typeRef?.ToString() ?? "null"} compared to {typeChk?.ToString() ?? "null"}", errors))
                return false;

            if (!Check(typeRef == typeChk, $"Types are different. {typeRef} <-> {typeChk}", errors))
                return false;

            bool result = true;

            // public fields
            foreach (FieldInfo field in typeRef.GetFields(BindingFlags.Instance | BindingFlags.Public))
                result &= CheckMemberDeepEquals($"{typeRef}.{field.Name}", Reflector.GetField(reference, field), Reflector.GetField(check, field), true, errors, checkedObjects);

            // public properties
            foreach (PropertyInfo property in reference.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetIndexParameters().Length == 0))
                result &= CheckMemberDeepEquals($"{typeRef}.{property.Name}", Reflector.GetProperty(reference, property), Reflector.GetProperty(check, property), true, errors, checkedObjects);

            // collection elements
            var collSrc = reference as IEnumerable;
            var collTarget = check as IEnumerable;
            if (collSrc != null && collTarget != null && !(reference is string || check is string))
                result &= CheckItemsEqual(collSrc, collTarget, true, errors, checkedObjects);
            return result;
        }

        private static bool CheckStreams(Stream reference, Stream check, List<string> errors)
        {
            if (!Check(reference.Length == check.Length, $"Length of the streams are different: {reference.Length} <-> {check.Length}", errors))
                return false;
            if (reference.Length == 0L)
                return true;

            long origPosRef = reference.Position;
            long origPosCheck = check.Position;
            if (origPosRef != 0L)
            {
                if (!reference.CanSeek)
                {
                    errors?.Add("Cannot seek the reference stream - compare cannot be performed");
                    return true;
                }

                reference.Position = 0L;
            }
            if (origPosCheck != 0L)
            {
                if (!check.CanSeek)
                {
                    errors?.Add("Cannot seek the check stream - compare cannot be performed");
                    return true;
                }

                check.Position = 0L;
            }

            for (long i = 0; i < reference.Length; i++)
            {
                int r, c;
                if (!Check((r = reference.ReadByte()) == (c = check.ReadByte()), $"Streams are different at position {i}: {r} <-> {c}", errors))
                    return false;
            }

            if (reference.CanSeek)
                reference.Position = origPosRef;
            if (check.CanSeek)
                check.Position = origPosCheck;

            return true;
        }

        private static bool CheckImages(Bitmap reference, Bitmap check, List<string> errors)
        {
            // using the not so fast GetPixel compare. This works also for different pixel formats and raw formats.
            // There is a 2% brightness tolerance for icons
            if (!Check(reference.Size == check.Size, $"Images have different size: {reference.Size} <-> {check.Size}", errors))
                return false;

            bool isIcon = reference.RawFormat.Guid == ImageFormat.Icon.Guid;

            for (int y = 0; y < reference.Height; y++)
            {
                for (int x = 0; x < reference.Width; x++)
                {
                    Color c1, c2;
                    if (!Check((c1 = reference.GetPixel(x, y)) == (c2 = check.GetPixel(x, y)) || (isIcon && Math.Abs(c1.GetBrightness() - c2.GetBrightness()) < 0.02f), $"Pixels at {x};{y} are different: {c1}<->{c2}", errors))
                        return false;
                }
            }

            return true;
        }

        private static void AssertResult(bool result, List<string> errors)
        {
            if (!result)
                Assert.Fail(String.Join(Environment.NewLine, errors
#if NET35
                    .ToArray()
#endif
                    ));
            else if (errors.Count > 0)
                Assert.Inconclusive(String.Join(Environment.NewLine, errors
#if NET35
                    .ToArray()
#endif
                    ));
        }

        private static bool Check(bool condition, string message, List<string> errors)
        {
            if (condition)
                return true;

            errors?.Add(message);
            return false;
        }

        protected static Metafile CreateTestMetafile()
        {
            Graphics refGraph = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr hdc = refGraph.GetHdc();
            Metafile result = new Metafile(hdc, EmfType.EmfOnly, "Test");

            //Draw some silly drawing
            using (var g = Graphics.FromImage(result))
            {
                var r = new Rectangle(0, 0, 100, 100);
                var reye1 = new Rectangle(20, 20, 20, 30);
                var reye2 = new Rectangle(60, 20, 20, 30);

                using (Pen pRed = new Pen(Color.Red, 2.5f))
                using (var pBlack = new Pen(Color.Black, 3))
                {
                    g.FillEllipse(Brushes.Yellow, r);
                    g.FillEllipse(Brushes.White, reye1);
                    g.FillEllipse(Brushes.White, reye2);
                    g.DrawEllipse(pBlack, reye1);
                    g.DrawEllipse(pBlack, reye2);
                    g.DrawBezier(pRed, new Point(10, 50), new Point(10, 100), new Point(90, 100), new Point(90, 50));
                }
            }

            refGraph.ReleaseHdc(hdc);
            refGraph.Dispose();
            return result;
        }

        protected static Bitmap CreateTestTiff()
        {
            var msTiff = new MemoryStream();
            Images.InformationMultiSize.ExtractBitmaps().SaveAsMultipageTiff(msTiff);
            msTiff.Position = 0L;
            var tiffImage = new Bitmap(msTiff);
            return tiffImage;
        }

        protected static void Throws<T>(Action action, string expectedMessageContent = null)
            where T : Exception
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(T));
                Assert.IsTrue(expectedMessageContent == null || e.Message.Contains(expectedMessageContent), $"Expected message: {expectedMessageContent}{Environment.NewLine}Actual message:{e.Message}");
                return;
            }

            Assert.Fail("No exception was thrown");
        }

        protected static void CheckTestingFramework()
        {
#if NET35
            if (typeof(object).Assembly.GetName().Version != new Version(2, 0, 0, 0))
                Assert.Inconclusive("mscorlib version does not match to .NET 3.5: {typeof(object).Assembly.GetName().Version}. Add a global <TargetFrameworkVersion>v3.5</TargetFrameworkVersion> to csproj and try again");
#elif NET40 || NET45
            if (typeof(object).Assembly.GetName().Version != new Version(4, 0, 0, 0))
                Assert.Inconclusive($"mscorlib version does not match to .NET 4.x: {typeof(object).Assembly.GetName().Version}. Add a global <TargetFrameworkVersion> to csproj and try again");
#endif
        }

        protected static void CopyContent(object target, object source)
        {
            if (target == null || source == null)
                return;

            if (target is Array arrayTarget)
            {
                var arraySource = (Array)source;
                Array.Copy(arraySource, arrayTarget, arrayTarget.Length);
                return;
            }

            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    Reflector.SetField(target, field, Reflector.GetField(source, field));
            }
        }
    }
}
