﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query;

public class NorthwindStringIncludeQueryInMemoryTest(NorthwindQueryInMemoryFixture<NoopModelCustomizer> fixture)
    : NorthwindStringIncludeQueryTestBase<NorthwindQueryInMemoryFixture<NoopModelCustomizer>>(fixture)
{
    // Right join not supported in InMemory
    public override Task Include_collection_with_right_join_clause_with_filter(bool async)
        => AssertTranslationFailed(() => base.Include_collection_with_right_join_clause_with_filter(async));
}
