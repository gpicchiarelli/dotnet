﻿#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "5d359099ffaf3096190a7267378a546e5814b6a10f2691fdce13ec7577332aec"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCoreGeneratedDocument.TestFiles_IntegrationTests_CodeGenerationIntegrationTest_NoLinePragmas), @"mvc.1.0.view", @"/TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml")]
namespace AspNetCoreGeneratedDocument
{
    #line default
    using global::System;
    using global::System.Collections.Generic;
    using global::System.Linq;
    using global::System.Threading.Tasks;
    using global::Microsoft.AspNetCore.Mvc;
    using global::Microsoft.AspNetCore.Mvc.Rendering;
    using global::Microsoft.AspNetCore.Mvc.ViewFeatures;
    #line default
    #line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"Sha256", @"5d359099ffaf3096190a7267378a546e5814b6a10f2691fdce13ec7577332aec", @"/TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemMetadataAttribute("Identifier", "/TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml")]
    [global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdateAttribute]
    #nullable restore
    internal sealed class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_NoLinePragmas : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    #nullable disable
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line (1,3)-(3,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"

    int i = 1;

#line default
#line hidden
#nullable disable

            WriteLiteral("\r\n");
#nullable restore
#line (5,2)-(6,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
while(i <= 10) {

#line default
#line hidden
#nullable disable

            WriteLiteral("    <p>Hello from C#, #");
            Write(
#nullable restore
#line (6,26)-(6,27) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
i

#line default
#line hidden
#nullable disable
            );
            WriteLiteral("</p>\r\n");
#nullable restore
#line (7,1)-(9,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
    i += 1;
}

#line default
#line hidden
#nullable disable

            WriteLiteral("\r\n");
#nullable restore
#line (10,2)-(11,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
if(i == 11) {

#line default
#line hidden
#nullable disable

            WriteLiteral("    <p>We wrote 10 lines!</p>\r\n");
#nullable restore
#line (12,1)-(13,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
}

#line default
#line hidden
#nullable disable

            WriteLiteral("\r\n");
#nullable restore
#line (14,2)-(16,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
switch(i) {
    case 11:

#line default
#line hidden
#nullable disable

            WriteLiteral("        <p>No really, we wrote 10 lines!</p>\r\n");
#nullable restore
#line (17,1)-(19,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
        break;
    default:

#line default
#line hidden
#nullable disable

            WriteLiteral("        <p>Actually, we didn\'t...</p>\r\n");
#nullable restore
#line (20,1)-(22,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
        break;
}

#line default
#line hidden
#nullable disable

            WriteLiteral("\r\n");
#nullable restore
#line (23,2)-(24,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
for(int j = 1; j <= 10; j += 2) {

#line default
#line hidden
#nullable disable

            WriteLiteral("    <p>Hello again from C#, #");
            Write(
#nullable restore
#line (24,32)-(24,33) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
j

#line default
#line hidden
#nullable disable
            );
            WriteLiteral("</p>\r\n");
#nullable restore
#line (25,1)-(26,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
}

#line default
#line hidden
#nullable disable

            WriteLiteral("\r\n");
#nullable restore
#line (27,2)-(28,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
try {

#line default
#line hidden
#nullable disable

            WriteLiteral("    <p>That time, we wrote 5 lines!</p>\r\n");
#nullable restore
#line (29,1)-(30,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
} catch(Exception ex) {

#line default
#line hidden
#nullable disable

            WriteLiteral("    <p>Oh no! An error occurred: ");
            Write(
#nullable restore
#line (30,36)-(30,46) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
ex.Message

#line default
#line hidden
#nullable disable
            );
            WriteLiteral("</p>\r\n");
#nullable restore
#line (31,1)-(33,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
}


#line default
#line hidden
#nullable disable

            WriteLiteral("<p>i is now ");
            Write(
#nullable restore
#line (34,14)-(34,15) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
i

#line default
#line hidden
#nullable disable
            );
            WriteLiteral("</p>\r\n\r\n");
#nullable restore
#line (36,2)-(37,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
lock(new object()) {

#line default
#line hidden
#nullable disable

            WriteLiteral("    <p>This block is locked, for your security!</p>\r\n");
#nullable restore
#line (38,1)-(38,2) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/NoLinePragmas.cshtml"
}

#line default
#line hidden
#nullable disable

        }
        #pragma warning restore 1998
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; } = default!;
        #nullable disable
    }
}
#pragma warning restore 1591
