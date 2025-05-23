// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.BackEnd;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.BuildException;
using Shouldly;
using Xunit;

#nullable disable

namespace Microsoft.Build.UnitTests.BackEnd
{
    /// <summary>
    /// Tests for the NodePacketTranslators
    /// </summary>
    public class BinaryTranslator_Tests
    {
        static BinaryTranslator_Tests()
        {
            SerializationContractInitializer.Initialize();
        }

        /// <summary>
        /// Tests the SerializationMode property
        /// </summary>
        [Fact]
        public void TestSerializationMode()
        {
            MemoryStream stream = new MemoryStream();
            using ITranslator readTranslator = BinaryTranslator.GetReadTranslator(stream, InterningBinaryReader.PoolingBuffer);
            Assert.Equal(TranslationDirection.ReadFromStream, readTranslator.Mode);

            using ITranslator writeTranslator = BinaryTranslator.GetWriteTranslator(stream);
            Assert.Equal(TranslationDirection.WriteToStream, writeTranslator.Mode);
        }

        /// <summary>
        /// Tests serializing bools.
        /// </summary>
        [Fact]
        public void TestSerializeBool()
        {
            HelperTestSimpleType(false, true);
            HelperTestSimpleType(true, false);
        }

        /// <summary>
        /// Tests serializing bytes.
        /// </summary>
        [Fact]
        public void TestSerializeByte()
        {
            byte val = 0x55;
            HelperTestSimpleType((byte)0, val);
            HelperTestSimpleType(val, (byte)0);
        }

        /// <summary>
        /// Tests serializing shorts.
        /// </summary>
        [Fact]
        public void TestSerializeShort()
        {
            short val = 0x55AA;
            HelperTestSimpleType((short)0, val);
            HelperTestSimpleType(val, (short)0);
        }

        /// <summary>
        /// Tests serializing longs.
        /// </summary>
        [Fact]
        public void TestSerializeLong()
        {
            long val = 0x55AABBCCDDEE;
            HelperTestSimpleType((long)0, val);
            HelperTestSimpleType(val, (long)0);
        }

        /// <summary>
        /// Tests serializing doubles.
        /// </summary>
        [Fact]
        public void TestSerializeDouble()
        {
            double val = 3.1416;
            HelperTestSimpleType((double)0, val);
            HelperTestSimpleType(val, (double)0);
        }

        /// <summary>
        /// Tests serializing TimeSpan.
        /// </summary>
        [Fact]
        public void TestSerializeTimeSpan()
        {
            TimeSpan val = TimeSpan.FromMilliseconds(123);
            HelperTestSimpleType(TimeSpan.Zero, val);
            HelperTestSimpleType(val, TimeSpan.Zero);
        }

        /// <summary>
        /// Tests serializing ints.
        /// </summary>
        [Fact]
        public void TestSerializeInt()
        {
            int val = 0x55AA55AA;
            HelperTestSimpleType((int)0, val);
            HelperTestSimpleType(val, (int)0);
        }

        /// <summary>
        /// Tests serializing strings.
        /// </summary>
        [Fact]
        public void TestSerializeString()
        {
            HelperTestSimpleType("foo", null);
            HelperTestSimpleType("", null);
            HelperTestSimpleType(null, null);
        }

        /// <summary>
        /// Tests serializing string arrays.
        /// </summary>
        [Fact]
        public void TestSerializeStringArray()
        {
            HelperTestArray(Array.Empty<string>(), StringComparer.Ordinal);
            HelperTestArray(new string[] { "foo", "bar" }, StringComparer.Ordinal);
            HelperTestArray(null, StringComparer.Ordinal);
        }

        /// <summary>
        /// Tests serializing string arrays.
        /// </summary>
        [Fact]
        public void TestSerializeStringList()
        {
            HelperTestList(new List<string>(), StringComparer.Ordinal);
            List<string> twoItems = new List<string>(2);
            twoItems.Add("foo");
            twoItems.Add("bar");
            HelperTestList(twoItems, StringComparer.Ordinal);
            HelperTestList(null, StringComparer.Ordinal);
        }

        /// <summary>
        /// Tests serializing DateTimes.
        /// </summary>
        [Fact]
        public void TestSerializeDateTime()
        {
            HelperTestSimpleType(new DateTime(), DateTime.Now);
            HelperTestSimpleType(DateTime.Now, new DateTime());
        }

        /// <summary>
        /// Tests serializing enums.
        /// </summary>
        [Fact]
        public void TestSerializeEnum()
        {
            TranslationDirection value = TranslationDirection.ReadFromStream;
            TranslationHelpers.GetWriteTranslator().TranslateEnum(ref value, (int)value);

            TranslationDirection deserializedValue = TranslationDirection.WriteToStream;
            TranslationHelpers.GetReadTranslator().TranslateEnum(ref deserializedValue, (int)deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        [Fact]
        public void TestSerializeException()
        {
            Exception value = new ArgumentNullException("The argument was null");
            TranslationHelpers.GetWriteTranslator().TranslateException(ref value);

            Exception deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateException(ref deserializedValue);

            Assert.True(TranslationHelpers.CompareExceptions(value, deserializedValue, out string diffReason), diffReason);
        }

        [Fact]
        public void TestSerializeException_NestedWithStack()
        {
            Exception value = null;
            try
            {
                // Intentionally throw a nested exception with a stack trace.
                value = value.InnerException;
            }
            catch (Exception e)
            {
                value = new ArgumentNullException("The argument was null", e);
            }

            TranslationHelpers.GetWriteTranslator().TranslateException(ref value);

            Exception deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateException(ref deserializedValue);

            Assert.True(TranslationHelpers.CompareExceptions(value, deserializedValue, out string diffReason), diffReason);
        }

        [Fact]
        public void TestSerializeBuildException_NestedWithStack()
        {
            Exception value = null;
            try
            {
                throw new InvalidProjectFileException("sample message");
            }
            catch (Exception e)
            {
                try
                {
                    throw new ArgumentNullException("The argument was null", e);
                }
                catch (Exception exception)
                {
                    value = new InternalErrorException("Another message", exception);
                }
            }

            Assert.NotNull(value);
            TranslationHelpers.GetWriteTranslator().TranslateException(ref value);

            Exception deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateException(ref deserializedValue);

            Assert.True(TranslationHelpers.CompareExceptions(value, deserializedValue, out string diffReason), diffReason);
        }

        public static IEnumerable<object[]> GetBuildExceptionsAsTestData()
            => AppDomain
                .CurrentDomain
                .GetAssemblies()
                // TaskHost is copying code files - so has a copy of types with identical names.
                .Where(a => !a.FullName!.StartsWith("MSBuildTaskHost", StringComparison.CurrentCultureIgnoreCase))
                .SelectMany(s => s.GetTypes())
                .Where(BuildExceptionSerializationHelper.IsSupportedExceptionType)
                .Select(t => new object[] { t });

        [Theory]
        [MemberData(nameof(GetBuildExceptionsAsTestData))]
        public void TestSerializationOfBuildExceptions(Type exceptionType)
        {
            Exception e = (Exception)Activator.CreateInstance(
                exceptionType,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance,
                null,
                new object[] { "msg", new GenericBuildTransferredException() },
                System.Globalization.CultureInfo.CurrentCulture);
            Exception remote;
            try
            {
                throw e;
            }
            catch (Exception exception)
            {
                remote = exception;
            }

            Assert.NotNull(remote);
            TranslationHelpers.GetWriteTranslator().TranslateException(ref remote);

            Exception deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateException(ref deserializedValue);

            Assert.True(TranslationHelpers.CompareExceptions(remote, deserializedValue, out string diffReason, true), $"Exception type {exceptionType.FullName} not properly de/serialized: {diffReason}");
        }

        [Fact]
        public void TestInvalidProjectFileException_NestedWithStack()
        {
            Exception value = null;
            try
            {
                throw new InvalidProjectFileException("sample message", new InternalErrorException("Another message"));
            }
            catch (Exception e)
            {
                value = e;
            }

            TranslationHelpers.GetWriteTranslator().TranslateException(ref value);

            Exception deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateException(ref deserializedValue);

            Assert.True(TranslationHelpers.CompareExceptions(value, deserializedValue, out string diffReason, true), diffReason);
        }

        /// <summary>
        /// Tests serializing an object with a default constructor.
        /// </summary>
        [Fact]
        public void TestSerializeINodePacketSerializable()
        {
            DerivedClass value = new DerivedClass(1, 2);
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            DerivedClass deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value.BaseValue, deserializedValue.BaseValue);
            Assert.Equal(value.DerivedValue, deserializedValue.DerivedValue);
        }

        /// <summary>
        /// Tests serializing an object with a default constructor passed as null.
        /// </summary>
        [Fact]
        public void TestSerializeINodePacketSerializableNull()
        {
            DerivedClass value = null;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            DerivedClass deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Tests serializing an object requiring a factory to construct.
        /// </summary>
        [Fact]
        public void TestSerializeWithFactory()
        {
            BaseClass value = new BaseClass(1);
            TranslationHelpers.GetWriteTranslator().Translate(ref value, BaseClass.FactoryForDeserialization);

            BaseClass deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue, BaseClass.FactoryForDeserialization);

            Assert.Equal(value.BaseValue, deserializedValue.BaseValue);
        }

        /// <summary>
        /// Tests serializing an object requiring a factory to construct, passing null for the value.
        /// </summary>
        [Fact]
        public void TestSerializeWithFactoryNull()
        {
            BaseClass value = null;
            TranslationHelpers.GetWriteTranslator().Translate(ref value, BaseClass.FactoryForDeserialization);

            BaseClass deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue, BaseClass.FactoryForDeserialization);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Tests serializing an array of objects with default constructors.
        /// </summary>
        [Fact]
        public void TestSerializeArray()
        {
            DerivedClass[] value = new DerivedClass[] { new DerivedClass(1, 2), new DerivedClass(3, 4) };
            TranslationHelpers.GetWriteTranslator().TranslateArray(ref value);

            DerivedClass[] deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateArray(ref deserializedValue);

            Assert.True(TranslationHelpers.CompareCollections(value, deserializedValue, DerivedClass.Comparer));
        }

        /// <summary>
        /// Tests serializing an array of objects with default constructors, passing null for the array.
        /// </summary>
        [Fact]
        public void TestSerializeArrayNull()
        {
            DerivedClass[] value = null;
            TranslationHelpers.GetWriteTranslator().TranslateArray(ref value);

            DerivedClass[] deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateArray(ref deserializedValue);

            Assert.True(TranslationHelpers.CompareCollections(value, deserializedValue, DerivedClass.Comparer));
        }

        /// <summary>
        /// Tests serializing an array of objects requiring factories to construct.
        /// </summary>
        [Fact]
        public void TestSerializeArrayWithFactory()
        {
            BaseClass[] value = new BaseClass[] { new BaseClass(1), new BaseClass(2) };
            TranslationHelpers.GetWriteTranslator().TranslateArray(ref value, BaseClass.FactoryForDeserialization);

            BaseClass[] deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateArray(ref deserializedValue, BaseClass.FactoryForDeserialization);

            Assert.True(TranslationHelpers.CompareCollections(value, deserializedValue, BaseClass.Comparer));
        }

        /// <summary>
        /// Tests serializing an array of objects requiring factories to construct, passing null for the array.
        /// </summary>
        [Fact]
        public void TestSerializeArrayWithFactoryNull()
        {
            BaseClass[] value = null;
            TranslationHelpers.GetWriteTranslator().TranslateArray(ref value, BaseClass.FactoryForDeserialization);

            BaseClass[] deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateArray(ref deserializedValue, BaseClass.FactoryForDeserialization);

            Assert.True(TranslationHelpers.CompareCollections(value, deserializedValue, BaseClass.Comparer));
        }

        /// <summary>
        /// Tests interning strings within an intern scope.
        /// </summary>
        /// <remarks>
        /// Most of the string intern tests use casing differences to assert whether interning was successful, rather
        /// than asserting the underlying buffer contents. Although the intended use is to deduplicate many strings of the
        /// same casing, this is harder to validate this high level, so we focus on testing behavior here.
        /// </remarks>
        [Theory]
        [InlineData("foo", true)]
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("foo", false)]
        [InlineData("", false)]
        public void TestInternWithInterning(string value, bool nullable)
        {
            // Create a case mismatch to test if the string is deduplicated.
            string valueUpperCase = value?.ToUpperInvariant();
            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.Intern(ref value, nullable);
                translator.Intern(ref valueUpperCase, nullable);
            });

            string deserializedValue = null;
            string deserializedValueUpperCase = null;
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.Intern(ref deserializedValue, nullable);
                translator.Intern(ref deserializedValueUpperCase, nullable);
            });

            // All occurrences should deserialize to the first encountered value.
            Assert.Equal(value, deserializedValue);
            Assert.Equal(value, deserializedValueUpperCase);
        }

        /// <summary>
        /// Tests interning strings outside of an intern scope.
        /// All calls should be forwarded to the regular translate method.
        /// </summary>
        [Theory]
        [InlineData("foo", true)]
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("foo", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void TestInternNoInterning(string value, bool nullable)
        {
            TranslationHelpers.GetWriteTranslator().Intern(ref value, nullable);

            string deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Intern(ref deserializedValue, nullable);

            // If we haven't blown up so far, assume we've skipped interning.
            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Tests interning path-like strings within an intern scope.
        /// </summary>
        [Theory]
        [InlineData(@"C:/src/msbuild/artifacts/bin/SomeProject.Namespace/Debug/net472/SomeProject.NameSpace.dll", true)]
        [InlineData("foo", true)]
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData(@"C:/src/msbuild/artifacts/bin/SomeProject.Namespace/Debug/net472/SomeProject.NameSpace.dll", false)]
        [InlineData("foo", false)]
        [InlineData("", false)]
        public void TestInternPathWithInterning(string value, bool nullable)
        {
            // Create a case mismatch to test if the path parts are deduplicated.
            string valueUpperCase = value?.ToUpperInvariant();
            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.InternPath(ref value, nullable);
                translator.InternPath(ref valueUpperCase, nullable);
            });

            string deserializedValue = null;
            string deserializedValueUpperCase = null;
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.InternPath(ref deserializedValue, nullable);
                translator.InternPath(ref deserializedValueUpperCase, nullable);
            });

            // All occurrences should deserialize to the first encountered value.
            Assert.Equal(value, deserializedValue);
            Assert.Equal(value, deserializedValueUpperCase);
        }

        /// <summary>
        /// Tests interning components in path-like strings.
        /// </summary>
        [Fact]
        public void TestInternPathWithComponentsFirst()
        {
            // Create a case mismatch to test if the path parts are deduplicated.
            string directory = @"C:/SRC/MSBUILD/ARTIFACTS/BIN/SOMEPROJECT.NAMESPACE/DEBUG/NET472/";
            string fileName = @"SOMEPROJECT.NAMESPACE.DLL";
            string fullPath = @"C:/src/msbuild/artifacts/bin/SomeProject.Namespace/Debug/net472/SomeProject.NameSpace.dll";

            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 2, translator =>
            {
                translator.InternPath(ref directory);
                translator.InternPath(ref fileName);
                translator.InternPath(ref fullPath);
            });

            string deserializedDirectory = null;
            string deserializedFileName = null;
            string deserializedFullPath = null;
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 2, translator =>
            {
                translator.InternPath(ref deserializedDirectory);
                translator.InternPath(ref deserializedFileName);
                translator.InternPath(ref deserializedFullPath);
            });

            // The path components should be reconstructed using the first encountered value.
            Assert.Equal(directory, deserializedDirectory);
            Assert.Equal(fileName, deserializedFileName);
            Assert.Equal(Path.Combine(directory, fileName), deserializedFullPath);
        }

        /// <summary>
        /// Tests interning components in path-like strings.
        /// </summary>
        [Fact]
        public void TestInternPathWithFullPathFirst()
        {
            // Create a case mismatch to test if the path parts are deduplicated.
            string fullPath = @"c:/src/msbuild/artifacts/bin/someproject.namespace/debug/net472/someproject.namespace.dll";
            string directory = @"C:/SRC/MSBUILD/ARTIFACTS/BIN/SOMEPROJECT.NAMESPACE/DEBUG/NET472/";
            string fileName = @"SOMEPROJECT.NAMESPACE.DLL";

            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 2, translator =>
            {
                translator.InternPath(ref fullPath);
                translator.InternPath(ref directory);
                translator.InternPath(ref fileName);
            });

            string deserializedFullPath = null;
            string deserializedDirectory = null;
            string deserializedFileName = null;
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 2, translator =>
            {
                translator.InternPath(ref deserializedFullPath);
                translator.InternPath(ref deserializedDirectory);
                translator.InternPath(ref deserializedFileName);
            });

            // The path components should be reconstructed using the first encountered value.
            Assert.Equal(fullPath, deserializedFullPath);
            Assert.Equal(fullPath, Path.Combine(deserializedDirectory, deserializedFileName));
        }

        /// <summary>
        /// Tests serializing string arrays within an intern scope.
        /// </summary>
        [Fact]
        public void TestInternStringArrayWithInterning()
        {
            // Create a case mismatch to test if the string is deduplicated.
            string[] value1 = ["foo", "FOO"];
            string[] value2 = ["Foo", "fOO"];

            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.Intern(ref value1);
                translator.Intern(ref value2);
            });

            string[] deserializedValue1 = null;
            string[] deserializedValue2 = null;
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.Intern(ref deserializedValue1);
                translator.Intern(ref deserializedValue2);
            });

            // All occurrences should deserialize to the first encountered value.
            string[] expectedValue = ["foo", "foo"];
            Assert.True(TranslationHelpers.CompareCollections(expectedValue, deserializedValue1, StringComparer.Ordinal));
            Assert.True(TranslationHelpers.CompareCollections(expectedValue, deserializedValue2, StringComparer.Ordinal));
        }

        /// <summary>
        /// Tests serializing string arrays outside of an intern scope.
        /// All calls should be forwarded to the regular translate method.
        /// </summary>
        [Fact]
        public void TestInternStringArrayNoInterning()
        {
            string[] value1 = ["foo", "FOO"];
            string[] value2 = ["Foo", "fOO"];

            ITranslator translator = TranslationHelpers.GetWriteTranslator();
            translator.Intern(ref value1);
            translator.Intern(ref value2);

            string[] deserializedValue1 = null;
            string[] deserializedValue2 = null;
            translator = TranslationHelpers.GetReadTranslator();
            translator.Intern(ref deserializedValue1);
            translator.Intern(ref deserializedValue2);

            Assert.True(TranslationHelpers.CompareCollections(value1, deserializedValue1, StringComparer.Ordinal));
            Assert.True(TranslationHelpers.CompareCollections(value2, deserializedValue2, StringComparer.Ordinal));
        }

        /// <summary>
        /// End-to-end test using a mixture of interned and non-interned operations to ensure that we don't hit
        /// invalid states, as this will be the most common use case.
        /// </summary>
        [Fact]
        public void TestWithInterningMixedUsage()
        {
            string value1 = "Foobar";
            string value2 = "foobar";
            string valueToIntern = "FooBar";
            int value3 = 10;
            string value4 = "fooBar";

            // Create a case mismatch to test if the string is deduplicated.
            string valueToInternUpperCase = valueToIntern?.ToUpperInvariant();
            string value5 = "Foo_Bar";

            ITranslator translator = TranslationHelpers.GetWriteTranslator();

            // Interleave interned and non-interned operations.
            translator.Translate(ref value1);
            translator.WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.Translate(ref value2);
                translator.Intern(ref valueToIntern);
                translator.Translate(ref value3);
                translator.Intern(ref valueToInternUpperCase);
                translator.Translate(ref value4);
            });
            translator.Translate(ref value5);

            string deserializedValue1 = null;
            string deserializedValue2 = null;
            string deserializedInternedValue = null;
            int deserializedValue3 = -1;
            string deserializedValue4 = null;
            string deserializedInternedValueUpperCase = null;
            string deserializedValue5 = null;

            translator = TranslationHelpers.GetReadTranslator();

            // This will only succeed if both translators are correctly sequenced:
            // packet body -> intern header -> intern body -> packet body.
            translator.Translate(ref deserializedValue1);
            translator.WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.Translate(ref deserializedValue2);
                translator.Intern(ref deserializedInternedValue);
                translator.Translate(ref deserializedValue3);
                translator.Intern(ref deserializedInternedValueUpperCase);
                translator.Translate(ref deserializedValue4);
            });
            translator.Translate(ref deserializedValue5);

            // All non-interned values should maintain their original casing.
            Assert.Equal(value1, deserializedValue1);
            Assert.Equal(value2, deserializedValue2);
            Assert.Equal(value3, deserializedValue3);
            Assert.Equal(value4, deserializedValue4);
            Assert.Equal(value5, deserializedValue5);

            // All interned values should deserialize to the first encountered value.
            Assert.Equal(valueToIntern, deserializedInternedValue);
            Assert.Equal(valueToIntern, deserializedInternedValueUpperCase);
        }

        /// <summary>
        /// Tests interning path-like strings outside of an intern scope.
        /// All calls should be forwarded to the regular translate method.
        /// </summary>
        [Theory]
        [InlineData(@"C:/src/msbuild/artifacts/bin/SomeProject.Namespace/Debug/net472/SomeProject.NameSpace.dll", true)]
        [InlineData("foo", true)]
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("foo", false)]
        [InlineData(@"C:/src/msbuild/artifacts/bin/SomeProject.Namespace/Debug/net472/SomeProject.NameSpace.dll", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void TestInternPathNoInterning(string value, bool nullable)
        {
            TranslationHelpers.GetWriteTranslator().InternPath(ref value, nullable);

            string deserializedValue = null;
            TranslationHelpers.GetReadTranslator().InternPath(ref deserializedValue, nullable);

            // If we haven't blown up so far, assume we've skipped interning.
            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Tests no-op when nothing is written to the interner. E.g. a packet opens an intern scope, but none of its
        /// translatable child objects write anything.
        /// </summary>
        [Fact]
        public void TestWithInterningNoWritesDoesNotThrow()
        {
            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 128, translator =>
            {
            });
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 128, translator =>
            {
            });
        }

        /// <summary>
        /// Tests reusing a translator with different interning comparers.
        /// This is important if the translator is reused for multiple packet types with different case sensitivity.
        /// </summary>
        [Fact]
        public void TestWithInterningResetsComparerBetweenScopes()
        {
            string mixedCaseValue = "StringWithSomeCasing";
            string lowerCaseValue = "stringwithsomecasing";

            MemoryStream serializationStream = new();
            ITranslator writeTranslator = BinaryTranslator.GetWriteTranslator(serializationStream);
            ITranslator readTranslator = BinaryTranslator.GetReadTranslator(serializationStream, InterningBinaryReader.PoolingBuffer);

            writeTranslator.WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.Intern(ref mixedCaseValue);
                translator.Intern(ref lowerCaseValue);
            });

            serializationStream.Position = 0;

            string deserializedMixedCaseValue = null;
            string deserializedLowerCaseValue = null;

            readTranslator.WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                translator.Intern(ref deserializedMixedCaseValue);
                translator.Intern(ref deserializedLowerCaseValue);
            });

            // Only the first casing should be interned.
            Assert.Equal(mixedCaseValue, deserializedMixedCaseValue);
            Assert.Equal(mixedCaseValue, deserializedLowerCaseValue);

            // Simulate translator reuse by resetting the underlying stream.
            serializationStream.Position = 0;
            serializationStream.SetLength(0);

            writeTranslator.WithInterning(StringComparer.Ordinal, initialCapacity: 2, translator =>
            {
                translator.Intern(ref mixedCaseValue);
                translator.Intern(ref lowerCaseValue);
            });

            serializationStream.Position = 0;

            readTranslator.WithInterning(StringComparer.Ordinal, initialCapacity: 2, translator =>
            {
                translator.Intern(ref deserializedMixedCaseValue);
                translator.Intern(ref deserializedLowerCaseValue);
            });

            // Both casings should be interned if the comparer was correctly reset.
            Assert.Equal(mixedCaseValue, deserializedMixedCaseValue);
            Assert.Equal(lowerCaseValue, deserializedLowerCaseValue);
        }

        /// <summary>
        /// Tests throwing an exception on nested intern scopes, which is unsupported.
        /// </summary>
        [Fact]
        public void TestWithInterningThrowsOnNestedScopes()
        {
            _ = Assert.Throws<InvalidOperationException>(() =>
            {
                ITranslator translator = TranslationHelpers.GetWriteTranslator();
                translator.WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
                {
                    translator.WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
                    {
                    });
                });
            });

            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
            {
                // Reset the stream, since the broken write will result in an IO exception when read.
            });

            _ = Assert.Throws<InvalidOperationException>(() =>
            {
                ITranslator translator = TranslationHelpers.GetReadTranslator();
                translator.WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
                {
                    translator.WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 1, translator =>
                    {
                    });
                });
            });
        }

        /// <summary>
        /// Tests serializing a dictionary of { string, string }
        /// </summary>
        [Fact]
        public void TestSerializeDictionaryStringString()
        {
            Dictionary<string, string> value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            value["foo"] = "bar";
            value["alpha"] = "omega";

            TranslationHelpers.GetWriteTranslator().TranslateDictionary(ref value, StringComparer.OrdinalIgnoreCase);

            Dictionary<string, string> deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateDictionary(ref deserializedValue, StringComparer.OrdinalIgnoreCase);

            Assert.Equal(value.Count, deserializedValue.Count);
            Assert.Equal(value["foo"], deserializedValue["foo"]);
            Assert.Equal(value["alpha"], deserializedValue["alpha"]);
            Assert.Equal(value["FOO"], deserializedValue["FOO"]);
        }

        /// <summary>
        /// Tests serializing a dictionary of { string, string }, passing null.
        /// </summary>
        [Fact]
        public void TestSerializeDictionaryStringStringNull()
        {
            Dictionary<string, string> value = null;

            TranslationHelpers.GetWriteTranslator().TranslateDictionary(ref value, StringComparer.OrdinalIgnoreCase);

            Dictionary<string, string> deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateDictionary(ref deserializedValue, StringComparer.OrdinalIgnoreCase);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Tests serializing a dictionary of { string, T } where T requires a factory to construct and the dictionary
        /// requires a KeyComparer initializer.
        /// </summary>
        [Fact]
        public void TestSerializeDictionaryStringT()
        {
            Dictionary<string, BaseClass> value = new Dictionary<string, BaseClass>(StringComparer.OrdinalIgnoreCase);
            value["foo"] = new BaseClass(1);
            value["alpha"] = new BaseClass(2);

            TranslationHelpers.GetWriteTranslator().TranslateDictionary(ref value, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);

            Dictionary<string, BaseClass> deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateDictionary(ref deserializedValue, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);

            Assert.Equal(value.Count, deserializedValue.Count);
            Assert.Equal(0, BaseClass.Comparer.Compare(value["foo"], deserializedValue["foo"]));
            Assert.Equal(0, BaseClass.Comparer.Compare(value["alpha"], deserializedValue["alpha"]));
            Assert.Equal(0, BaseClass.Comparer.Compare(value["FOO"], deserializedValue["FOO"]));
        }

        /// <summary>
        /// Tests serializing a dictionary of { string, T } where T requires a factory to construct and the dictionary
        /// requires a KeyComparer initializer, passing null for the dictionary.
        /// </summary>
        [Fact]
        public void TestSerializeDictionaryStringTNull()
        {
            Dictionary<string, BaseClass> value = null;

            TranslationHelpers.GetWriteTranslator().TranslateDictionary(ref value, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);

            Dictionary<string, BaseClass> deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateDictionary(ref deserializedValue, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Tests serializing a dictionary of { string, T } where T requires a factory to construct and the dictionary
        /// has a default constructor.
        /// </summary>
        [Fact]
        public void TestSerializeDictionaryStringTNoComparer()
        {
            Dictionary<string, BaseClass> value = new Dictionary<string, BaseClass>();
            value["foo"] = new BaseClass(1);
            value["alpha"] = new BaseClass(2);

            TranslationHelpers.GetWriteTranslator().TranslateDictionary<Dictionary<string, BaseClass>, BaseClass>(ref value, BaseClass.FactoryForDeserialization);

            Dictionary<string, BaseClass> deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateDictionary<Dictionary<string, BaseClass>, BaseClass>(ref deserializedValue, BaseClass.FactoryForDeserialization);

            Assert.Equal(value.Count, deserializedValue.Count);
            Assert.Equal(0, BaseClass.Comparer.Compare(value["foo"], deserializedValue["foo"]));
            Assert.Equal(0, BaseClass.Comparer.Compare(value["alpha"], deserializedValue["alpha"]));
            Assert.False(deserializedValue.ContainsKey("FOO"));
        }

        /// <summary>
        /// Tests serializing a dictionary of { string, T } where T requires a factory to construct and the dictionary
        /// has a default constructor, passing null for the dictionary.
        /// </summary>
        [Fact]
        public void TestSerializeDictionaryStringTNoComparerNull()
        {
            Dictionary<string, BaseClass> value = null;

            TranslationHelpers.GetWriteTranslator().TranslateDictionary<Dictionary<string, BaseClass>, BaseClass>(ref value, BaseClass.FactoryForDeserialization);

            Dictionary<string, BaseClass> deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateDictionary<Dictionary<string, BaseClass>, BaseClass>(ref deserializedValue, BaseClass.FactoryForDeserialization);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Tests interning dictionaries of { string, string } within an intern scope.
        /// </summary>
        [Fact]
        public void TestInternDictionaryStringString()
        {
            Dictionary<string, string> value = new(StringComparer.OrdinalIgnoreCase)
            {
                ["foo"] = "bar",
                ["alpha"] = "omega",
            };
            Dictionary<string, string> valueUpperCase = new(StringComparer.OrdinalIgnoreCase)
            {
                ["FOO"] = "BAR",
                ["ALPHA"] = "OMEGA",
            };

            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 4, translator =>
            {
                translator.InternDictionary(ref value, StringComparer.OrdinalIgnoreCase);
                translator.InternDictionary(ref valueUpperCase, StringComparer.OrdinalIgnoreCase);
            });

            Dictionary<string, string> deserializedValue = null;
            Dictionary<string, string> deserializedValueUpperCase = null;
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 4, translator =>
            {
                translator.InternDictionary(ref deserializedValue, StringComparer.OrdinalIgnoreCase);
                translator.InternDictionary(ref deserializedValueUpperCase, StringComparer.OrdinalIgnoreCase);
            });

            Assert.Equal(value.Count, deserializedValue.Count);
            Assert.Equal(value["foo"], deserializedValue["foo"]);
            Assert.Equal(value["alpha"], deserializedValue["alpha"]);

            // All occurrences should deserialize to the first encountered value.
            // We don't test the keys since the dictionary already uses an ignore case comparer, and
            // we also want to test that the dictionary comparer matches on both sides.
            Assert.Equal(valueUpperCase.Count, deserializedValueUpperCase.Count);
            Assert.Equal(value["foo"], deserializedValueUpperCase["foo"]);
            Assert.Equal(value["alpha"], deserializedValueUpperCase["alpha"]);
        }

        /// <summary>
        /// Tests interning a dictionary of { string, T } within an intern scope.
        /// </summary>
        [Fact]
        public void TestInternDictionaryStringT()
        {
            // Since we don't have string values, mismatch the key comparer to verify that interning works.
            Dictionary<string, BaseClass> value = new(StringComparer.Ordinal)
            {
                ["foo"] = new BaseClass(1),
                ["alpha"] = new BaseClass(2),
            };
            Dictionary<string, BaseClass> valueUpperCase = new(StringComparer.Ordinal)
            {
                ["FOO"] = new BaseClass(1),
                ["ALPHA"] = new BaseClass(2),
            };

            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 2, translator =>
            {
                translator.InternDictionary(ref value, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);
                translator.InternDictionary(ref valueUpperCase, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);
            });

            Dictionary<string, BaseClass> deserializedValue = null;
            Dictionary<string, BaseClass> deserializedValueUpperCase = null;
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 2, translator =>
            {
                translator.InternDictionary(ref deserializedValue, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);
                translator.InternDictionary(ref deserializedValueUpperCase, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);
            });

            Assert.Equal(value.Count, deserializedValue.Count);
            Assert.Equal(0, BaseClass.Comparer.Compare(value["foo"], deserializedValue["foo"]));
            Assert.Equal(0, BaseClass.Comparer.Compare(value["alpha"], deserializedValue["alpha"]));

            // All occurrences should deserialize to the first encountered key.
            Assert.Equal(0, BaseClass.Comparer.Compare(valueUpperCase["FOO"], deserializedValueUpperCase["foo"]));
            Assert.Equal(0, BaseClass.Comparer.Compare(valueUpperCase["ALPHA"], deserializedValueUpperCase["alpha"]));
        }

        /// <summary>
        /// Tests interning dictionaries of { string, string } with path-like values within an intern scope.
        /// </summary>
        [Fact]
        public void TestInternPathDictionaryStringString()
        {
            Dictionary<string, string> value = new(StringComparer.OrdinalIgnoreCase)
            {
                ["foo"] = @"C:/src/msbuild/artifacts/bin/ProjectA.Namespace/Debug/net472/ProjectA.NameSpace.dll",
                ["alpha"] = @"C:/src/msbuild/artifacts/bin/ProjectB.Namespace/Debug/net472/ProjectB.NameSpace.dll",
            };
            Dictionary<string, string> valueUpperCase = new(StringComparer.OrdinalIgnoreCase)
            {
                ["FOO"] = @"C:/SRC/MSBUILD/ARTIFACTS/BIN/PROJECTA.NAMESPACE/DEBUG/NET472/PROJECTA.NAMESPACE.DLL",
                ["ALPHA"] = @"C:/SRC/MSBUILD/ARTIFACTS/BIN/PROJECTB.NAMESPACE/DEBUG/NET472/PROJECTB.NAMESPACE.DLL",
            };

            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 4, translator =>
            {
                translator.InternDictionary(ref value, StringComparer.OrdinalIgnoreCase);
                translator.InternDictionary(ref valueUpperCase, StringComparer.OrdinalIgnoreCase);
            });

            Dictionary<string, string> deserializedValue = null;
            Dictionary<string, string> deserializedValueUpperCase = null;
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 4, translator =>
            {
                translator.InternDictionary(ref deserializedValue, StringComparer.OrdinalIgnoreCase);
                translator.InternDictionary(ref deserializedValueUpperCase, StringComparer.OrdinalIgnoreCase);
            });

            Assert.Equal(value.Count, deserializedValue.Count);
            Assert.Equal(value["foo"], deserializedValue["foo"]);
            Assert.Equal(value["alpha"], deserializedValue["alpha"]);

            // All occurrences should deserialize to the first encountered value.
            // We don't test the keys since the dictionary already uses an ignore case comparer, and
            // we also want to test that the dictionary comparer matches on both sides.
            Assert.Equal(valueUpperCase.Count, deserializedValueUpperCase.Count);
            Assert.Equal(value["foo"], deserializedValueUpperCase["foo"]);
            Assert.Equal(value["alpha"], deserializedValueUpperCase["alpha"]);
        }

        /// <summary>
        /// Tests interning a dictionary of { string, T } with path-like keys within an intern scope.
        /// </summary>
        [Fact]
        public void TestInternPathDictionaryStringT()
        {
            const string PathA = @"C:/src/msbuild/artifacts/bin/ProjectA.Namespace/Debug/net472/ProjectA.NameSpace.dll";
            const string PathB = @"C:/src/msbuild/artifacts/bin/ProjectB.Namespace/Debug/net472/ProjectB.NameSpace.dll";

            // Since we don't have string values, mismatch the key comparer to verify that interning works.
            Dictionary<string, BaseClass> value = new(StringComparer.Ordinal)
            {
                [PathA] = new BaseClass(1),
                [PathB] = new BaseClass(2),
            };
            Dictionary<string, BaseClass> valueUpperCase = new(StringComparer.Ordinal)
            {
                [PathA.ToUpperInvariant()] = new BaseClass(1),
                [PathB.ToUpperInvariant()] = new BaseClass(2),
            };

            TranslationHelpers.GetWriteTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 2, translator =>
            {
                translator.InternDictionary(ref value, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);
                translator.InternDictionary(ref valueUpperCase, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);
            });

            Dictionary<string, BaseClass> deserializedValue = null;
            Dictionary<string, BaseClass> deserializedValueUpperCase = null;
            TranslationHelpers.GetReadTranslator().WithInterning(StringComparer.OrdinalIgnoreCase, initialCapacity: 2, translator =>
            {
                translator.InternDictionary(ref deserializedValue, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);
                translator.InternDictionary(ref deserializedValueUpperCase, StringComparer.OrdinalIgnoreCase, BaseClass.FactoryForDeserialization);
            });

            Assert.Equal(value.Count, deserializedValue.Count);
            Assert.Equal(0, BaseClass.Comparer.Compare(value[PathA], deserializedValue[PathA]));
            Assert.Equal(0, BaseClass.Comparer.Compare(value[PathB], deserializedValue[PathB]));

            // All occurrences should deserialize to the first encountered key.
            Assert.Equal(0, BaseClass.Comparer.Compare(valueUpperCase[PathA.ToUpperInvariant()], deserializedValueUpperCase[PathA]));
            Assert.Equal(0, BaseClass.Comparer.Compare(valueUpperCase[PathB.ToUpperInvariant()], deserializedValueUpperCase[PathB]));
        }


        [Theory]
        [InlineData("en")]
        [InlineData("en-US")]
        [InlineData("en-CA")]
        [InlineData("zh-HK")]
        [InlineData("sr-Cyrl-CS")]
        public void CultureInfo(string name)
        {
            CultureInfo value = new CultureInfo(name);
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            CultureInfo deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            deserializedValue.ShouldBe(value);
        }

        [Fact]
        public void CultureInfoAsNull()
        {
            CultureInfo value = null;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            CultureInfo deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            deserializedValue.ShouldBeNull();
        }

        [Theory]
        [InlineData("1.2")]
        [InlineData("1.2.3")]
        [InlineData("1.2.3.4")]
        public void Version(string version)
        {
            Version value = new Version(version);
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            Version deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            deserializedValue.ShouldBe(value);
        }

        [Fact]
        public void VersionAsNull()
        {
            Version value = null;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            Version deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            deserializedValue.ShouldBeNull();
        }

        [Fact]
        public void HashSetOfT()
        {
            HashSet<BaseClass> values = new()
            {
                new BaseClass(1),
                new BaseClass(2),
                null
            };
            TranslationHelpers.GetWriteTranslator().TranslateHashSet(ref values, BaseClass.FactoryForDeserialization, capacity => new());

            HashSet<BaseClass> deserializedValues = null;
            TranslationHelpers.GetReadTranslator().TranslateHashSet(ref deserializedValues, BaseClass.FactoryForDeserialization, capacity => new());

            deserializedValues.ShouldBe(values, ignoreOrder: true);
        }

        [Fact]
        public void HashSetOfTAsNull()
        {
            HashSet<BaseClass> value = null;
            TranslationHelpers.GetWriteTranslator().TranslateHashSet(ref value, BaseClass.FactoryForDeserialization, capacity => new());

            HashSet<BaseClass> deserializedValue = null;
            TranslationHelpers.GetReadTranslator().TranslateHashSet(ref deserializedValue, BaseClass.FactoryForDeserialization, capacity => new());

            deserializedValue.ShouldBeNull();
        }

        [Fact]
        public void AssemblyNameAsNull()
        {
            AssemblyName value = null;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            AssemblyName deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            deserializedValue.ShouldBeNull();
        }

        [Fact]
        public void AssemblyNameWithAllFields()
        {
            AssemblyName value = new()
            {
                Name = "a",
                Version = new Version(1, 2, 3),
                Flags = AssemblyNameFlags.PublicKey,
                ProcessorArchitecture = ProcessorArchitecture.X86,
                CultureInfo = new CultureInfo("zh-HK"),
                HashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA256,
                VersionCompatibility = AssemblyVersionCompatibility.SameMachine,
                CodeBase = "C:\\src",
                ContentType = AssemblyContentType.WindowsRuntime,
                CultureName = "zh-HK",
            };
            value.SetPublicKey(new byte[] { 3, 2, 1 });
            value.SetPublicKeyToken(new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 });

            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            AssemblyName deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            HelperAssertAssemblyNameEqual(value, deserializedValue);
        }

        [Fact]
        public void AssemblyNameWithMinimalFields()
        {
            AssemblyName value = new();

            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            AssemblyName deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            HelperAssertAssemblyNameEqual(value, deserializedValue);
        }

        /// <summary>
        /// Assert two AssemblyName objects values are same.
        /// Ignoring KeyPair, ContentType, CultureName as those are not serialized
        /// </summary>
        private static void HelperAssertAssemblyNameEqual(AssemblyName expected, AssemblyName actual)
        {
            actual.Name.ShouldBe(expected.Name);
            actual.Version.ShouldBe(expected.Version);
            actual.Flags.ShouldBe(expected.Flags);
            actual.ProcessorArchitecture.ShouldBe(expected.ProcessorArchitecture);
            actual.CultureInfo.ShouldBe(expected.CultureInfo);
            actual.HashAlgorithm.ShouldBe(expected.HashAlgorithm);
            actual.VersionCompatibility.ShouldBe(expected.VersionCompatibility);
            actual.CodeBase.ShouldBe(expected.CodeBase);

            actual.GetPublicKey().ShouldBe(expected.GetPublicKey());
            actual.GetPublicKeyToken().ShouldBe(expected.GetPublicKeyToken());
        }

        /// <summary>
        /// Helper for bool serialization.
        /// </summary>
        private void HelperTestSimpleType(bool initialValue, bool deserializedInitialValue)
        {
            bool value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            bool deserializedValue = deserializedInitialValue;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Helper for long serialization.
        /// </summary>
        private void HelperTestSimpleType(long initialValue, long deserializedInitialValue)
        {
            long value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            long deserializedValue = deserializedInitialValue;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Helper for double serialization.
        /// </summary>
        private void HelperTestSimpleType(double initialValue, double deserializedInitialValue)
        {
            double value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            double deserializedValue = deserializedInitialValue;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Helper for TimeSpan serialization.
        /// </summary>
        private void HelperTestSimpleType(TimeSpan initialValue, TimeSpan deserializedInitialValue)
        {
            TimeSpan value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            TimeSpan deserializedValue = deserializedInitialValue;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Helper for byte serialization.
        /// </summary>
        private void HelperTestSimpleType(byte initialValue, byte deserializedInitialValue)
        {
            byte value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            byte deserializedValue = deserializedInitialValue;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Helper for short serialization.
        /// </summary>
        private void HelperTestSimpleType(short initialValue, short deserializedInitialValue)
        {
            short value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            short deserializedValue = deserializedInitialValue;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Helper for int serialization.
        /// </summary>
        private void HelperTestSimpleType(int initialValue, int deserializedInitialValue)
        {
            int value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            int deserializedValue = deserializedInitialValue;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Helper for string serialization.
        /// </summary>
        private void HelperTestSimpleType(string initialValue, string deserializedInitialValue)
        {
            string value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            string deserializedValue = deserializedInitialValue;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Helper for DateTime serialization.
        /// </summary>
        private void HelperTestSimpleType(DateTime initialValue, DateTime deserializedInitialValue)
        {
            DateTime value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            DateTime deserializedValue = deserializedInitialValue;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.Equal(value, deserializedValue);
        }

        /// <summary>
        /// Helper for array serialization.
        /// </summary>
        private void HelperTestArray(string[] initialValue, IComparer<string> comparer)
        {
            string[] value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            string[] deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.True(TranslationHelpers.CompareCollections(value, deserializedValue, comparer));
        }

        /// <summary>
        /// Helper for list serialization.
        /// </summary>
        private void HelperTestList(List<string> initialValue, IComparer<string> comparer)
        {
            List<string> value = initialValue;
            TranslationHelpers.GetWriteTranslator().Translate(ref value);

            List<string> deserializedValue = null;
            TranslationHelpers.GetReadTranslator().Translate(ref deserializedValue);

            Assert.True(TranslationHelpers.CompareCollections(value, deserializedValue, comparer));
        }

        /// <summary>
        /// Base class for testing
        /// </summary>
        private class BaseClass : ITranslatable
        {
            /// <summary>
            /// A field.
            /// </summary>
            private int _baseValue;

            /// <summary>
            /// Constructor with value.
            /// </summary>
            public BaseClass(int val)
            {
                _baseValue = val;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            protected BaseClass()
            {
            }

            protected bool Equals(BaseClass other)
            {
                return _baseValue == other._baseValue;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                return Equals((BaseClass)obj);
            }

            public override int GetHashCode()
            {
                return _baseValue;
            }

            /// <summary>
            /// Gets a comparer.
            /// </summary>
            public static IComparer<BaseClass> Comparer
            {
                get { return new BaseClassComparer(); }
            }

            /// <summary>
            /// Gets the value.
            /// </summary>
            public int BaseValue
            {
                get { return _baseValue; }
            }

            #region INodePacketTranslatable Members

            /// <summary>
            /// Factory for serialization.
            /// </summary>
            public static BaseClass FactoryForDeserialization(ITranslator translator)
            {
                BaseClass packet = new BaseClass();
                packet.Translate(translator);
                return packet;
            }

            /// <summary>
            /// Serializes the class.
            /// </summary>
            public virtual void Translate(ITranslator translator)
            {
                translator.Translate(ref _baseValue);
            }

            #endregion

            /// <summary>
            /// Comparer for BaseClass.
            /// </summary>
            private sealed class BaseClassComparer : IComparer<BaseClass>
            {
                /// <summary>
                /// Constructor.
                /// </summary>
                public BaseClassComparer()
                {
                }

                #region IComparer<BaseClass> Members

                /// <summary>
                /// Compare two BaseClass objects.
                /// </summary>
                public int Compare(BaseClass x, BaseClass y)
                {
                    if (x._baseValue == y._baseValue)
                    {
                        return 0;
                    }

                    return -1;
                }
                #endregion
            }
        }

        /// <summary>
        /// Derived class for testing.
        /// </summary>
        private sealed class DerivedClass : BaseClass
        {
            /// <summary>
            /// A field.
            /// </summary>
            private int _derivedValue;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public DerivedClass()
            {
            }

            /// <summary>
            /// Constructor taking two values.
            /// </summary>
            public DerivedClass(int derivedValue, int baseValue)
                : base(baseValue)
            {
                _derivedValue = derivedValue;
            }

            /// <summary>
            /// Gets a comparer.
            /// </summary>
            public static new IComparer<DerivedClass> Comparer
            {
                get { return new DerivedClassComparer(); }
            }

            /// <summary>
            /// Returns the value.
            /// </summary>
            public int DerivedValue
            {
                get { return _derivedValue; }
            }

            #region INodePacketTranslatable Members

            /// <summary>
            /// Serializes the class.
            /// </summary>
            public override void Translate(ITranslator translator)
            {
                base.Translate(translator);
                translator.Translate(ref _derivedValue);
            }

            #endregion

            /// <summary>
            /// Comparer for DerivedClass.
            /// </summary>
            private sealed class DerivedClassComparer : IComparer<DerivedClass>
            {
                /// <summary>
                /// Constructor
                /// </summary>
                public DerivedClassComparer()
                {
                }

                #region IComparer<DerivedClass> Members

                /// <summary>
                /// Compares two DerivedClass objects.
                /// </summary>
                public int Compare(DerivedClass x, DerivedClass y)
                {
                    if (x._derivedValue == y._derivedValue)
                    {
                        return BaseClass.Comparer.Compare(x, y);
                    }

                    return -1;
                }
                #endregion
            }
        }
    }
}
