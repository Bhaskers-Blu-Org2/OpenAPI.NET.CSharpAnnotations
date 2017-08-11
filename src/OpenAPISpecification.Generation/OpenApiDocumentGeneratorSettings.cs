﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.OpenApiSpecification.Generation.DocumentFilters;
using Microsoft.OpenApiSpecification.Generation.OperationFilters;

namespace Microsoft.OpenApiSpecification.Generation
{
    /// <summary>
    /// Holds the settings to perform generation.
    /// </summary>
    public class OpenApiDocumentGeneratorSettings
    {
        /// <summary>
        /// Creates instance of <see cref="OpenApiDocumentGeneratorSettings"/>.
        /// </summary>
        /// <param name="operationFilters">The operation filers.</param>
        /// <param name="documentFilters">The document filters.</param>
        public OpenApiDocumentGeneratorSettings(
            IEnumerable<IOperationFilter> operationFilters,
            IEnumerable<IDocumentFilter> documentFilters)
        {
            OperationFilters = operationFilters;
            DocumentFilters = documentFilters;
        }
        
        /// <summary>
        /// Gets the list of document filters.
        /// </summary>
        public IEnumerable<IDocumentFilter> DocumentFilters { get; }

        /// <summary>
        /// Gets the list of operation filters.
        /// </summary>
        public IEnumerable<IOperationFilter> OperationFilters { get; }
    }
}