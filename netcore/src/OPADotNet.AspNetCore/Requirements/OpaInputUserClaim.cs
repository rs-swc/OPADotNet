﻿/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

namespace OPADotNet.AspNetCore.Requirements
{
    /// <summary>
    /// Contains a single claim for a user
    /// </summary>
    class OpaInputUserClaim
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        public static OpaInputUserClaim FromClaim(Claim claim)
        {
            return new OpaInputUserClaim()
            {
                Type = claim.Type,
                Value = claim.Value
            };
        }
    }
}
