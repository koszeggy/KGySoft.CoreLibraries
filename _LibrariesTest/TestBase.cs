using System;
using System.Collections;
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

        /// <summary>
        /// Return true for resource tests. This provides a special handling for <see cref="ResXFileRef"/> and <see cref="ResXDataNode"/> compare.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is resource test; otherwise, <c>false</c>.
        /// </value>
        protected virtual bool IsResourceTest => false;

        protected void CompareCollections(IEnumerable referenceObjects, IEnumerable targetObjects)
        {
            IEnumerator enumRef = referenceObjects.GetEnumerator();
            IEnumerator enumChk = targetObjects.GetEnumerator();
            while (enumRef.MoveNext())
            {
                Assert.IsTrue(enumChk.MoveNext(), "Reference contains more objects. {0} <-> {1}", referenceObjects, targetObjects);

                CompareObjects(enumRef.Current, enumChk.Current);
            }

            Assert.IsFalse(enumChk.MoveNext(), "Target collection contains more objects. {0} <-> {1}", referenceObjects, targetObjects);
        }

        protected void CompareCollections(IEnumerator enumRef, IEnumerator enumChk)
        {
            while (enumRef.MoveNext())
            {
                Assert.IsTrue(enumChk.MoveNext(), "Reference contains more objects. {0} <-> {1}", enumRef, enumChk);

                CompareObjects(enumRef.Current, enumChk.Current);
            }

            Assert.IsFalse(enumChk.MoveNext(), "Target collection contains more objects. {0} <-> {1}", enumRef, enumChk);
        }

        protected void CompareObjects(object reference, object check)
        {
            if (reference == null && check == null)
                return;

            if ((reference == null || check == null))
                Assert.Fail("null compared with not null");

            Type typeRef = reference.GetType();
            Type typeChk = check.GetType();

            if (typeRef == typeof(AnyObjectSerializerWrapper))
            {
                CompareObjects(Reflector.GetInstanceFieldByName(reference, "obj"), check);
                return;
            }

            if (IsResourceTest)
            {
                if (typeRef == typeof(SystemFileRef))
                {
                    Assert.AreSame(Reflector.ResolveType(((SystemFileRef)reference).TypeName), typeChk, "File reference type error. Expected type: {0}", typeChk);
                    return;
                }

                if (typeRef == typeof(ResXFileRef))
                {
                    Assert.AreSame(Reflector.ResolveType(((ResXFileRef)reference).TypeName), typeChk, "File reference type error. Expected type: {0}", typeChk);
                    return;
                }

                if (typeRef == typeof(SystemDataNode))
                {
                    CompareObjects(((SystemDataNode)reference).GetValue((ITypeResolutionService)null), check);
                    return;
                }

                if (typeRef == typeof(ResXDataNode))
                {
                    CompareObjects(((ResXDataNode)reference).GetValue(), check);
                    return;
                }
            }

            Assert.AreSame(typeRef, typeChk, "Types are different. {0} <-> {1}", typeRef.ToString(), typeChk.ToString());

            if (typeRef == typeof(object))
                return;

            if (!(reference is string) && reference is IEnumerable)
            {
                CompareCollections((IEnumerable)reference, (IEnumerable)check);
                return;
            }

            if (reference is float)
            {
                Assert.IsTrue(BitConverter.ToInt32(BitConverter.GetBytes((float)reference), 0) == BitConverter.ToInt32(BitConverter.GetBytes((float)check), 0),
                    "Float equality failed: {0} <-> {1}. Binary representation: 0x{2} <-> 0x{3}", reference, check, BitConverter.GetBytes((float)reference).ToHexValuesString(), BitConverter.GetBytes((float)check).ToHexValuesString());
                return;
            }

            if (reference is double)
            {
                Assert.IsTrue(BitConverter.DoubleToInt64Bits((double)reference) == BitConverter.DoubleToInt64Bits((double)check),
                    "Double equality failed: {0} <-> {1}. Binary representation: 0x{2} <-> 0x{3}", reference, check, BitConverter.GetBytes((double)reference).ToHexValuesString(), BitConverter.GetBytes((double)check).ToHexValuesString());
                return;
            }

            if (reference is decimal)
            {
                Assert.IsTrue(BinarySerializer.SerializeStruct((decimal)reference).SequenceEqual(BinarySerializer.SerializeStruct((decimal)check)),
                    "Decimal equality failed: {0} <-> {1}. Binary representation: 0x{2} <-> 0x{3}", reference, check, BinarySerializer.SerializeStruct((decimal)reference).ToHexValuesString(), BinarySerializer.SerializeStruct((decimal)check).ToHexValuesString());
                return;
            }

            if (typeRef == typeof(StringBuilder))
            {
                StringBuilder sbRef = (StringBuilder)reference;
                StringBuilder sbCheck = (StringBuilder)check;
                Assert.AreEqual(sbRef.Capacity, sbCheck.Capacity);
                Assert.AreEqual(sbRef.Length, sbCheck.Length);
                Assert.AreEqual(sbRef.ToString(), sbCheck.ToString());
                return;
            }

            if (reference is Stream)
            {
                CompareStreams((Stream)reference, (Stream)check);
                return;
            }

            if (typeRef.IsGenericType && typeRef.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                || typeRef == typeof(DictionaryEntry))
            {
                object keyRef = Reflector.GetInstancePropertyByName(reference, "Key");
                object keyChk = Reflector.GetInstancePropertyByName(check, "Key");
                if (!(keyRef is string) && keyRef is IEnumerable)
                    CompareCollections((IEnumerable)keyRef, (IEnumerable)keyChk);
                else
                    CompareObjects(keyRef, keyChk);

                object valueRef = Reflector.GetInstancePropertyByName(reference, "Value");
                object valueChk = Reflector.GetInstancePropertyByName(check, "Value");
                if (!(valueRef is string) && valueRef is IEnumerable)
                    CompareCollections((IEnumerable)valueRef, (IEnumerable)valueChk);
                else
                    CompareObjects(valueRef, valueChk);

                return;
            }

            if (typeRef == typeof(Bitmap))
            {
                CompareImages((Bitmap)reference, (Bitmap)check);
                return;
            }

            if (typeRef == typeof(Metafile))
            {
                CompareImages(((Metafile)reference).ToBitmap(((Metafile)reference).Size), ((Metafile)check).ToBitmap(((Metafile)check).Size));
                return;
            }

            if (typeRef == typeof(Icon))
            {
                CompareImages(((Icon)reference).ToAlphaBitmap(), ((Icon)check).ToAlphaBitmap());
                return;
            }

            if (typeRef == typeof(ImageListStreamer))
            {
                var il1 = new ImageList { ImageStream = (ImageListStreamer)reference };
                var il2 = new ImageList { ImageStream = (ImageListStreamer)check };
                CompareCollections(il1.Images, il2.Images);
                return;
            }

            Assert.AreEqual(reference, check, "Equality check failed at type {0}", reference.GetType().FullName);
        }

        private static void CompareStreams(Stream reference, Stream check)
        {
            Assert.AreEqual(reference.Length, check.Length, "Length of the streams are different: {0} <-> {1}", reference.Length, check.Length);
            if (reference.Length == 0L)
                return;

            long origPosRef = reference.Position;
            long origPosCheck = check.Position;
            if (origPosRef != 0L)
            {
                if (!reference.CanSeek)
                    Assert.Inconclusive("Cannot seek the reference stream - compare cannot be performed");
                reference.Position = 0L;
            }
            if (origPosCheck != 0L)
            {
                if (!check.CanSeek)
                    Assert.Inconclusive("Cannot seek the check stream - compare cannot be performed");
                check.Position = 0L;
            }

            for (long i = 0; i < reference.Length; i++)
            {
                int r, c;
                Assert.AreEqual(r = reference.ReadByte(), c = check.ReadByte(), "Streams are different at position {0}: {1} <-> {2}", i, r, c);
            }

            if (reference.CanSeek)
                reference.Position = origPosRef;
            if (check.CanSeek)
                check.Position = origPosCheck;
        }

        protected static void CompareImages(Bitmap reference, Bitmap check)
        {
            // using the not so fast GetPixel compare. This works also for different pixel formats and raw formats.
            // There is a 2% brightness tolerance for icons
            Assert.AreEqual(reference.Size, check.Size, String.Format("Images have different size: {0} <-> {1}", reference.Size, check.Size));
            bool isIcon = reference.RawFormat.Guid == ImageFormat.Icon.Guid;

            for (int y = 0; y < reference.Height; y++)
            {
                for (int x = 0; x < reference.Width; x++)
                {
                    Color c1, c2;
                    Assert.IsTrue((c1 = reference.GetPixel(x, y)) == (c2 = check.GetPixel(x, y))
                        || (isIcon && Math.Abs(c1.GetBrightness() - c2.GetBrightness()) < 0.02f),
                        "Pixels at {0};{1} are different: {2}<->{3}", x, y, c1, c2);
                }
            }
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

        protected static void Throws<T>(Action action)
            where T : Exception
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(T));
                return;
            }

            Assert.Fail("No exception was thrown");
        }

        protected static void CheckTestingFramework()
        {
#if NET35
            if (typeof(object).Assembly.GetName().Version != new Version(2, 0, 0, 0))
                Assert.Inconclusive("mscorlib version does not match to .NET 3.5: {0}. Add a global <TargetFrameworkVersion>v3.5</TargetFrameworkVersion> to csproj and try again", typeof(object).Assembly.GetName().Version);
#elif NET40 || NET45
            if (typeof(object).Assembly.GetName().Version != new Version(4, 0, 0, 0))
                Assert.Inconclusive("mscorlib version does not match to .NET 4.x: {0}. Add a global <TargetFrameworkVersion> to csproj and try again", typeof(object).Assembly.GetName().Version);
#endif
        }
    }
}
