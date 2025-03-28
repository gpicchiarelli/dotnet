﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Private.Windows.BinaryFormat.Serializer;

namespace System.Private.Windows.BinaryFormat;

/// <summary>
///  Helper to create and track records for <see cref="BinaryObjectString"/> and <see cref="MemberReference"/>
///  when duplicates are found.
/// </summary>
internal class StringRecordsCollection : IReadOnlyRecordMap
{
    private readonly Dictionary<string, int> _strings = [];
    private readonly Dictionary<int, MemberReference> _memberReferences = [];

    public int CurrentId { get; set; }

    public IRecord this[Id id] => _memberReferences[id];

    public StringRecordsCollection(int currentId) => CurrentId = currentId;

    /// <summary>
    ///  Returns the appropriate record for the given string.
    /// </summary>
    public IRecord? GetStringRecord(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (_strings.TryGetValue(value, out int id))
        {
            // The record with the data has already been retrieved, only a reference is needed now
            if (_memberReferences.TryGetValue(id, out MemberReference? memberReference))
            {
                return memberReference;
            }

            MemberReference reference = new(id);
            _memberReferences.Add(id, reference);
            return reference;
        }

        _strings[value] = CurrentId;
        IRecord record = new BinaryObjectString(CurrentId, value);
        CurrentId++;
        return record;
    }
}
