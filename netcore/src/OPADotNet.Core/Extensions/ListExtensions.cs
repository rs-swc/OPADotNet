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
using System.Text;

namespace OPADotNet.Core.Extensions
{
    internal static class ListExtensions
    {
        public static bool AreEqual<T>(this List<T> list, List<T> other)
        {
            if (list == null && other == null)
            {
                return true;
            }
            if (list == null || other == null)
            {
                return false;
            }
            if (list.Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (!Equals(list[i], other[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
