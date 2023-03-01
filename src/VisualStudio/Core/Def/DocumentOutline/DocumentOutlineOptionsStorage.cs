﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Used to enable or disable the Document Outline feature
    /// </summary>
    internal sealed class DocumentOutlineOptionsStorage
    {
        // Flag is defined in VisualStudio\Core\Def\PackageRegistration.pkgdef.
        public static readonly Option2<bool> EnableDocumentOutline = new("dotnet_enable_document_outline", defaultValue: true);
    }
}
